// Layered — Audience-specific changelog translator
// Copyright (C) 2026 Layered contributors
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU Affero General Public License for more details.
//
// You should have received a copy of the GNU Affero General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/>.

using System.Text.Json.Serialization;
using FluentValidation;
using Layered.Api.Configuration;
using Layered.Api.Contracts;
using Layered.Api.Validation;
using Layered.Core.Configuration;
using Layered.Core.Domain;
using Layered.Core.Domain.Interfaces;
using Layered.Core.Llm;
using Layered.Core.Services;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Structured logging via Serilog, configured from appsettings + sane defaults.
builder.Host.UseSerilog((context, _, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// ---------- Options ----------

builder.Services
    .AddOptions<LlmOptions>()
    .Bind(builder.Configuration.GetSection(LlmOptions.SectionName))
    .ValidateOnStart();

builder.Services
    .AddOptions<LayeredConfigOptions>()
    .Bind(builder.Configuration.GetSection(LayeredConfigOptions.SectionName));

// ---------- Semantic Kernel + LLM connector ----------
//
// Per the architecture rules, Semantic Kernel is used in exactly one place
// (ChangelogTranslatorService). The LlmConnectorRegistrar selects the
// IChatCompletionService at startup based on Llm:Provider, so self-hosters
// can swap between Anthropic, OpenAI, and Ollama by editing configuration
// alone — no recompile required.

var llmOptions = builder.Configuration
    .GetSection(LlmOptions.SectionName)
    .Get<LlmOptions>() ?? new LlmOptions();

new LlmConnectorRegistrar().Register(builder.Services, llmOptions);
builder.Services.AddKernel();

// ---------- Layered.Core services ----------

builder.Services.AddSingleton<IPromptTemplateProvider>(serviceProvider =>
    new FileSystemPromptTemplateProvider(
        serviceProvider.GetRequiredService<ILogger<FileSystemPromptTemplateProvider>>()));

builder.Services.AddSingleton<IChangelogTranslator, ChangelogTranslatorService>();

// ---------- Api services ----------

builder.Services.AddSingleton<LayeredProjectConfigStore>();

// FluentValidation auto-discovery — registers every AbstractValidator<T>
// in the Api assembly as scoped, including the nested validators that the
// composite request validators depend on.
builder.Services.AddValidatorsFromAssemblyContaining<TranslateRequestValidator>();

// Problem Details for consistent error responses.
builder.Services.AddProblemDetails();

// Accept and emit enum values as strings (e.g. "TechLead") rather than
// integers, so the JSON wire format matches the .layered.json file the
// CLI consumes.
builder.Services.ConfigureHttpJsonOptions(jsonOptions =>
{
    jsonOptions.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseStatusCodePages();

// ---------- Endpoints ----------

app.MapGet("/health", () =>
{
    var version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "0.0.0";
    return Results.Ok(new HealthResponse(Status: "healthy", Version: version));
})
.WithName("GetHealth");

app.MapPost("/translate", async (
    TranslateRequest request,
    IValidator<TranslateRequest> validator,
    IChangelogTranslator translator,
    CancellationToken cancellationToken) =>
{
    var validation = await validator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
    if (!validation.IsValid)
    {
        return Results.ValidationProblem(
            validation.ToDictionary(),
            statusCode: StatusCodes.Status400BadRequest);
    }

    var result = await translator
        .TranslateAsync(request.ToDomain(), cancellationToken)
        .ConfigureAwait(false);

    if (result.IsFailure)
    {
        return Results.Problem(
            title: "Translation failed",
            detail: result.Error,
            statusCode: StatusCodes.Status502BadGateway);
    }

    return Results.Ok(TranslateResponse.FromDomain(result.Value));
})
.WithName("PostTranslate");

app.MapPost("/configure", async (
    ConfigureRequest request,
    IValidator<ConfigureRequest> validator,
    LayeredProjectConfigStore configStore,
    IOptions<LayeredConfigOptions> configOptions,
    CancellationToken cancellationToken) =>
{
    var validation = await validator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
    if (!validation.IsValid)
    {
        return Results.ValidationProblem(
            validation.ToDictionary(),
            statusCode: StatusCodes.Status400BadRequest);
    }

    var projectConfig = new LayeredProjectConfig(
        Context: request.CompanyContext.ToDomain(),
        Audiences: request.AudienceConfigs.Select(a => a.ToDomain()).ToList());

    var configId = Guid.NewGuid();
    var path = await configStore
        .WriteAsync(configOptions.Value.OutputDirectory, projectConfig, cancellationToken)
        .ConfigureAwait(false);

    return Results.Ok(new ConfigureResponse(ConfigId: configId, Path: path));
})
.WithName("PostConfigure");

app.Run();

/// <summary>
/// Program entry point. Declared explicitly so test projects can reference
/// the assembly via <c>WebApplicationFactory&lt;Program&gt;</c>.
/// </summary>
public partial class Program;

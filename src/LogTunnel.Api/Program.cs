// LogTunnel — Audience-specific changelog translator
// Copyright (C) 2026 LogTunnel contributors
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
using LogTunnel.Api.Configuration;
using LogTunnel.Api.Contracts;
using LogTunnel.Api.Validation;
using LogTunnel.Core.Configuration;
using LogTunnel.Core.Domain;
using LogTunnel.Core.Domain.Interfaces;
using LogTunnel.Core.Llm;
using LogTunnel.Core.Services;
using LogTunnel.Infrastructure;
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
    .AddOptions<LogTunnelConfigOptions>()
    .Bind(builder.Configuration.GetSection(LogTunnelConfigOptions.SectionName));

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

// ---------- LogTunnel.Core services ----------

builder.Services.AddSingleton<IPromptTemplateProvider>(serviceProvider =>
    new FileSystemPromptTemplateProvider(
        serviceProvider.GetRequiredService<ILogger<FileSystemPromptTemplateProvider>>()));

builder.Services.AddSingleton<IChangelogTranslator, ChangelogTranslatorService>();

// ---------- LogTunnel.Infrastructure (optional Postgres data layer) ----------
//
// Postgres-backed DbContext + repositories. Wired in only when a
// connection string is supplied — self-hosters who run the API without
// a database can leave Postgres__ConnectionString empty and the
// /translate / /configure endpoints continue to work in stateless mode.
//
// Hosted-platform features (auth, dashboards, webhooks, etc.) live in
// the separate LogTunnel.Platform repo and consume this data layer
// through the same AddLogTunnelInfrastructure extension.
var postgresConnectionString = builder.Configuration.GetSection("Postgres")["ConnectionString"];
var dataLayerEnabled = !string.IsNullOrWhiteSpace(postgresConnectionString);
if (dataLayerEnabled)
{
    builder.Services.AddLogTunnelInfrastructure(postgresConnectionString!);
}

// ---------- CORS ----------
//
// Allowed origin comes from LogTunnel:Cors:Origin
// (LOGTUNNEL__CORS__ORIGIN env var). Defaults to the Vite dev server
// at http://localhost:5173 so the local React frontend works without
// extra config. AllowCredentials is on so the dashboard can send the
// JWT in an Authorization header alongside cookies if it ever needs to.
const string CorsPolicyName = "LogTunnelDashboard";
var corsOrigin = builder.Configuration.GetSection("LogTunnel:Cors")["Origin"]
                 ?? "http://localhost:5173";
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy => policy
        .WithOrigins(corsOrigin)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

// ---------- Api services ----------

builder.Services.AddSingleton<LogTunnelProjectConfigStore>();

// FluentValidation auto-discovery — registers every AbstractValidator<T>
// in the Api assembly as scoped, including the nested validators that the
// composite request validators depend on.
builder.Services.AddValidatorsFromAssemblyContaining<TranslateRequestValidator>();

// Problem Details for consistent error responses.
builder.Services.AddProblemDetails();

// Accept and emit enum values as strings (e.g. "TechLead") rather than
// integers, so the JSON wire format matches the .logtunnel.json file the
// CLI consumes.
builder.Services.ConfigureHttpJsonOptions(jsonOptions =>
{
    jsonOptions.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseStatusCodePages();

app.UseCors(CorsPolicyName);

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
    LogTunnelProjectConfigStore configStore,
    IOptions<LogTunnelConfigOptions> configOptions,
    CancellationToken cancellationToken) =>
{
    var validation = await validator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
    if (!validation.IsValid)
    {
        return Results.ValidationProblem(
            validation.ToDictionary(),
            statusCode: StatusCodes.Status400BadRequest);
    }

    var projectConfig = new LogTunnelProjectConfig(
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

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

using Layered.Core.Common;
using Layered.Core.Domain;
using Layered.Core.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Layered.Core.Services;

/// <summary>
/// Default <see cref="IChangelogTranslator"/>. For each requested
/// audience it loads the matching markdown prompt template, fills in the
/// company context and audience configuration, and invokes the kernel.
/// All audience translations run concurrently via
/// <see cref="Task.WhenAll(Task[])"/>.
/// </summary>
public sealed class ChangelogTranslatorService : IChangelogTranslator
{
    private const string ArgRawCommits = "raw_commits";
    private const string ArgProductDescription = "product_description";
    private const string ArgTargetCustomers = "target_customers";
    private const string ArgTerminology = "terminology";
    private const string ArgAdditionalContext = "additional_context";
    private const string ArgTone = "tone";
    private const string ArgFormat = "format";
    private const string ArgCustomInstructions = "custom_instructions";

    private readonly Kernel _kernel;
    private readonly IPromptTemplateProvider _promptTemplateProvider;
    private readonly ILogger<ChangelogTranslatorService> _logger;

    /// <summary>
    /// Create a new translator service.
    /// </summary>
    /// <param name="kernel">Configured Semantic Kernel instance with a chat completion connector.</param>
    /// <param name="promptTemplateProvider">Provider that supplies prompt templates per audience.</param>
    /// <param name="logger">Logger used for diagnostic and error messages.</param>
    public ChangelogTranslatorService(
        Kernel kernel,
        IPromptTemplateProvider promptTemplateProvider,
        ILogger<ChangelogTranslatorService> logger)
    {
        _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        _promptTemplateProvider = promptTemplateProvider ?? throw new ArgumentNullException(nameof(promptTemplateProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Result<ChangelogOutput>> TranslateAsync(
        TranslationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
            return Result<ChangelogOutput>.Failure("Translation request is required.");
        if (string.IsNullOrWhiteSpace(request.RawCommits))
            return Result<ChangelogOutput>.Failure("Raw commits are required.");
        if (request.Context is null)
            return Result<ChangelogOutput>.Failure("Company context is required.");
        if (request.Audiences is null || request.Audiences.Count == 0)
            return Result<ChangelogOutput>.Failure("At least one audience configuration is required.");

        var distinctAudienceCount = request.Audiences.Select(a => a.Type).Distinct().Count();
        if (distinctAudienceCount != request.Audiences.Count)
            return Result<ChangelogOutput>.Failure("Audience configurations must not contain duplicate audience types.");

        _logger.LogInformation(
            "Translating commits for {AudienceCount} audiences",
            request.Audiences.Count);

        try
        {
            var translationTasks = request.Audiences
                .Select(audience => TranslateForAudienceAsync(request, audience, cancellationToken))
                .ToArray();

            var results = await Task.WhenAll(translationTasks).ConfigureAwait(false);

            var outputs = new Dictionary<AudienceType, string>(results.Length);
            foreach (var (audienceType, text) in results)
                outputs[audienceType] = text;

            var output = new ChangelogOutput(
                Id: Guid.NewGuid(),
                GeneratedAt: DateTimeOffset.UtcNow,
                Outputs: outputs);

            return Result<ChangelogOutput>.Success(output);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Translation failed for {AudienceCount} audiences",
                request.Audiences.Count);
            return Result<ChangelogOutput>.Failure($"Translation failed: {ex.Message}");
        }
    }

    private async Task<(AudienceType Audience, string Text)> TranslateForAudienceAsync(
        TranslationRequest request,
        AudienceConfig audience,
        CancellationToken cancellationToken)
    {
        var template = await _promptTemplateProvider
            .GetTemplateAsync(audience.Type, cancellationToken)
            .ConfigureAwait(false);

        var arguments = new KernelArguments
        {
            [ArgRawCommits] = request.RawCommits,
            [ArgProductDescription] = request.Context.ProductDescription,
            [ArgTargetCustomers] = request.Context.TargetCustomers,
            [ArgTerminology] = request.Context.Terminology,
            [ArgAdditionalContext] = request.Context.AdditionalContext ?? string.Empty,
            [ArgTone] = audience.Tone,
            [ArgFormat] = audience.Format,
            [ArgCustomInstructions] = audience.CustomInstructions ?? string.Empty,
        };

        var function = _kernel.CreateFunctionFromPrompt(template);
        var invocationResult = await _kernel
            .InvokeAsync(function, arguments, cancellationToken)
            .ConfigureAwait(false);

        var text = invocationResult.GetValue<string>()?.Trim() ?? string.Empty;
        return (audience.Type, text);
    }
}

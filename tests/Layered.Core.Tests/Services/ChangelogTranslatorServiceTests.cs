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

using Layered.Core.Domain;
using Layered.Core.Domain.Interfaces;
using Layered.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using NSubstitute;

namespace Layered.Core.Tests.Services;

public class ChangelogTranslatorServiceTests
{
    private const string TemplateText = "test prompt {{$raw_commits}}";

    private static readonly CompanyContext SampleContext = new(
        ProductDescription: "B2B invoicing tool",
        TargetCustomers: "Freelancers",
        Terminology: "members not users",
        AdditionalContext: null);

    private static readonly AudienceConfig TechLeadAudience = new(
        Type: AudienceType.TechLead,
        Tone: "tech",
        Format: "bullets",
        CustomInstructions: null);

    private static readonly AudienceConfig ManagerAudience = new(
        Type: AudienceType.Manager,
        Tone: "clear",
        Format: "short",
        CustomInstructions: null);

    private static readonly AudienceConfig CeoAudience = new(
        Type: AudienceType.CEO,
        Tone: "plain",
        Format: "3 bullets",
        CustomInstructions: null);

    private static readonly AudienceConfig PublicAudience = new(
        Type: AudienceType.Public,
        Tone: "friendly",
        Format: "paragraphs",
        CustomInstructions: null);

    [Fact]
    public async Task TranslateAsync_returns_failure_when_request_is_null()
    {
        var translator = CreateTranslator(out _, out _);

        var result = await translator.TranslateAsync(request: null!);

        Assert.True(result.IsFailure);
        Assert.Contains("required", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\n\t ")]
    public async Task TranslateAsync_returns_failure_when_raw_commits_are_empty(string rawCommits)
    {
        var translator = CreateTranslator(out _, out _);
        var request = new TranslationRequest(
            RawCommits: rawCommits,
            Context: SampleContext,
            Audiences: new[] { TechLeadAudience });

        var result = await translator.TranslateAsync(request);

        Assert.True(result.IsFailure);
        Assert.Contains("commits", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TranslateAsync_returns_failure_when_audience_list_is_empty()
    {
        var translator = CreateTranslator(out _, out _);
        var request = new TranslationRequest(
            RawCommits: "fix: bug",
            Context: SampleContext,
            Audiences: Array.Empty<AudienceConfig>());

        var result = await translator.TranslateAsync(request);

        Assert.True(result.IsFailure);
        Assert.Contains("audience", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TranslateAsync_returns_failure_when_audience_types_are_duplicated()
    {
        var translator = CreateTranslator(out _, out _);
        var request = new TranslationRequest(
            RawCommits: "fix: bug",
            Context: SampleContext,
            Audiences: new[] { TechLeadAudience, TechLeadAudience });

        var result = await translator.TranslateAsync(request);

        Assert.True(result.IsFailure);
        Assert.Contains("duplicate", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TranslateAsync_returns_success_with_one_output_per_requested_audience()
    {
        var fake = new FakeChatCompletionService((history, _) =>
            Task.FromResult($"rendered: {history.Count} messages"));
        var translator = CreateTranslator(fake, out var promptProvider);

        var request = new TranslationRequest(
            RawCommits: "fix: bug",
            Context: SampleContext,
            Audiences: new[] { TechLeadAudience, ManagerAudience, CeoAudience });

        var result = await translator.TranslateAsync(request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.NotEqual(Guid.Empty, result.Value.Id);
        Assert.Equal(3, result.Value.Outputs.Count);

        Assert.Contains(AudienceType.TechLead, result.Value.Outputs.Keys);
        Assert.Contains(AudienceType.Manager, result.Value.Outputs.Keys);
        Assert.Contains(AudienceType.CEO, result.Value.Outputs.Keys);
        Assert.All(result.Value.Outputs.Values, text => Assert.StartsWith("rendered:", text));

        // The provider was asked once per requested audience.
        await promptProvider.Received(1).GetTemplateAsync(AudienceType.TechLead, Arg.Any<CancellationToken>());
        await promptProvider.Received(1).GetTemplateAsync(AudienceType.Manager, Arg.Any<CancellationToken>());
        await promptProvider.Received(1).GetTemplateAsync(AudienceType.CEO, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TranslateAsync_runs_all_audiences_in_parallel()
    {
        var audiences = new[] { TechLeadAudience, ManagerAudience, CeoAudience, PublicAudience };

        // An async barrier that only releases its task once every audience
        // has reached it. If the translator ran the audiences sequentially
        // the first call would hit the barrier and never see the others,
        // the timeout below would fire, and the test would fail. The
        // barrier is async-friendly (TCS-based) so it does not depend on
        // having spare thread-pool threads.
        var arrived = 0;
        var allArrivedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var fake = new FakeChatCompletionService(async (_, ct) =>
        {
            if (Interlocked.Increment(ref arrived) >= audiences.Length)
                allArrivedTcs.TrySetResult();
            await allArrivedTcs.Task.WaitAsync(TimeSpan.FromSeconds(2), ct);
            return "ok";
        });

        var translator = CreateTranslator(fake, out _);
        var request = new TranslationRequest(
            RawCommits: "fix: bug",
            Context: SampleContext,
            Audiences: audiences);

        var result = await translator.TranslateAsync(request);

        Assert.True(result.IsSuccess);
        Assert.Equal(audiences.Length, fake.CallCount);
        Assert.Equal(audiences.Length, result.Value.Outputs.Count);
    }

    [Fact]
    public async Task TranslateAsync_propagates_cancellation_token_to_chat_completion_service()
    {
        using var cts = new CancellationTokenSource();

        // The fake cancels the source the first time it is reached, then
        // honours the same token by throwing OperationCanceledException —
        // proving the translator passed the caller's token all the way
        // down to the chat-completion service.
        var fake = new FakeChatCompletionService((_, ct) =>
        {
            cts.Cancel();
            ct.ThrowIfCancellationRequested();
            return Task.FromResult("unreachable");
        });

        var translator = CreateTranslator(fake, out _);
        var request = new TranslationRequest(
            RawCommits: "fix: bug",
            Context: SampleContext,
            Audiences: new[] { TechLeadAudience });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => translator.TranslateAsync(request, cts.Token));
    }

    private static ChangelogTranslatorService CreateTranslator(
        out FakeChatCompletionService fake,
        out IPromptTemplateProvider promptProvider)
    {
        fake = new FakeChatCompletionService();
        return CreateTranslator(fake, out promptProvider);
    }

    private static ChangelogTranslatorService CreateTranslator(
        FakeChatCompletionService fake,
        out IPromptTemplateProvider promptProvider)
    {
        promptProvider = Substitute.For<IPromptTemplateProvider>();
        promptProvider
            .GetTemplateAsync(Arg.Any<AudienceType>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(TemplateText));

        var kernelBuilder = Kernel.CreateBuilder();
        kernelBuilder.Services.AddSingleton<IChatCompletionService>(fake);
        var kernel = kernelBuilder.Build();

        return new ChangelogTranslatorService(
            kernel,
            promptProvider,
            NullLogger<ChangelogTranslatorService>.Instance);
    }
}

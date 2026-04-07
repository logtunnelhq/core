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

using System.Runtime.CompilerServices;
using System.Text;
using Anthropic;
using Anthropic.Core;
using Anthropic.Models.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Services;

namespace Layered.Core.Llm;

/// <summary>
/// Adapts the official Anthropic .NET SDK
/// (<c>Anthropic.AnthropicClient</c>) to Semantic Kernel's
/// <see cref="IChatCompletionService"/> contract so the existing
/// <c>ChangelogTranslatorService</c> can drive Claude through the
/// kernel without any provider-specific code in the translator.
/// </summary>
/// <remarks>
/// Phase 1 only needs the synchronous request/response path used by
/// prompt-template functions. Streaming is intentionally not supported
/// — the translator never asks for it, and adding incremental decoding
/// would be dead weight.
/// </remarks>
public sealed class AnthropicChatCompletionService : IChatCompletionService, IDisposable
{
    private const int DefaultMaxTokens = 4096;

    private readonly AnthropicClient _client;
    private readonly string _modelId;
    private readonly Dictionary<string, object?> _attributes;
    private readonly ILogger<AnthropicChatCompletionService> _logger;

    /// <summary>Create a new Anthropic-backed chat completion service.</summary>
    /// <param name="apiKey">Anthropic API key.</param>
    /// <param name="modelId">Default Claude model identifier (e.g. <c>claude-sonnet-4-20250514</c>).</param>
    /// <param name="logger">Logger used for diagnostic and error messages.</param>
    public AnthropicChatCompletionService(
        string apiKey,
        string modelId,
        ILogger<AnthropicChatCompletionService> logger)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("Anthropic API key must not be empty.", nameof(apiKey));
        if (string.IsNullOrWhiteSpace(modelId))
            throw new ArgumentException("Anthropic model id must not be empty.", nameof(modelId));

        _client = new AnthropicClient(new ClientOptions { ApiKey = apiKey });
        _modelId = modelId;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _attributes = new Dictionary<string, object?>
        {
            [AIServiceExtensions.ModelIdKey] = modelId,
        };
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?> Attributes => _attributes;

    /// <inheritdoc />
    public async Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        if (chatHistory is null)
            throw new ArgumentNullException(nameof(chatHistory));

        var (systemPrompt, messages) = ConvertChatHistory(chatHistory);
        if (messages.Count == 0)
            throw new InvalidOperationException(
                "Anthropic chat completion requires at least one user or assistant message.");

        var parameters = string.IsNullOrEmpty(systemPrompt)
            ? new MessageCreateParams
            {
                Model = _modelId,
                MaxTokens = DefaultMaxTokens,
                Messages = messages,
            }
            : new MessageCreateParams
            {
                Model = _modelId,
                MaxTokens = DefaultMaxTokens,
                Messages = messages,
                System = systemPrompt,
            };

        _logger.LogDebug(
            "Calling Anthropic Messages API with model {Model}, {MessageCount} messages",
            _modelId,
            messages.Count);

        var response = await _client.Messages
            .Create(parameters, cancellationToken)
            .ConfigureAwait(false);

        var text = ExtractText(response);
        var result = new ChatMessageContent(
            role: AuthorRole.Assistant,
            content: text,
            modelId: _modelId,
            innerContent: response);

        return new[] { result };
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // The translator only consumes the synchronous path. Yielding the
        // full response in a single chunk keeps callers that opportunistically
        // call the streaming API working without doubling the implementation
        // surface area.
        var contents = await GetChatMessageContentsAsync(
            chatHistory,
            executionSettings,
            kernel,
            cancellationToken).ConfigureAwait(false);

        foreach (var content in contents)
        {
            yield return new StreamingChatMessageContent(
                role: content.Role,
                content: content.Content,
                modelId: _modelId);
        }
    }

    /// <inheritdoc />
    public void Dispose() => _client.Dispose();

    private static (string SystemPrompt, List<MessageParam> Messages) ConvertChatHistory(
        ChatHistory chatHistory)
    {
        var systemBuilder = new StringBuilder();
        var messages = new List<MessageParam>(chatHistory.Count);

        foreach (var message in chatHistory)
        {
            var content = message.Content ?? string.Empty;

            if (message.Role == AuthorRole.System)
            {
                if (systemBuilder.Length > 0)
                    systemBuilder.Append('\n');
                systemBuilder.Append(content);
                continue;
            }

            var role = message.Role == AuthorRole.Assistant
                ? Role.Assistant.ToString().ToLowerInvariant()
                : Role.User.ToString().ToLowerInvariant();

            messages.Add(new MessageParam
            {
                Role = role,
                Content = content,
            });
        }

        return (systemBuilder.ToString(), messages);
    }

    private static string ExtractText(Message response)
    {
        var sb = new StringBuilder();
        foreach (var block in response.Content)
        {
            if (block.TryPickText(out var textBlock))
                sb.Append(textBlock.Text);
        }
        return sb.ToString();
    }
}

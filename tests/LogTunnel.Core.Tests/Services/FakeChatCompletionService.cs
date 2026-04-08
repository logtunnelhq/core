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

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace LogTunnel.Core.Tests.Services;

/// <summary>
/// Hand-rolled <see cref="IChatCompletionService"/> stub for the
/// translator tests. NSubstitute can't substitute the SK
/// <see cref="Kernel"/> (it is sealed), so the tests construct a real
/// kernel and inject this stub at the chat-completion seam instead.
/// </summary>
internal sealed class FakeChatCompletionService : IChatCompletionService
{
    private readonly Func<ChatHistory, CancellationToken, Task<string>> _handler;
    private int _callCount;

    public FakeChatCompletionService(Func<ChatHistory, CancellationToken, Task<string>>? handler = null)
    {
        _handler = handler ?? ((_, _) => Task.FromResult("ok"));
    }

    public IReadOnlyDictionary<string, object?> Attributes { get; } = new Dictionary<string, object?>();

    public int CallCount => Volatile.Read(ref _callCount);

    public async Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _callCount);
        var text = await _handler(chatHistory, cancellationToken).ConfigureAwait(false);
        return new[] { new ChatMessageContent(AuthorRole.Assistant, text) };
    }

    public IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Streaming is not exercised by the translator tests.");
}

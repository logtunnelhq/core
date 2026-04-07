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

namespace Layered.Api.Configuration;

/// <summary>
/// Bound configuration for the LLM connector. Populated from the
/// <c>Llm</c> configuration section, which can be supplied via
/// <c>appsettings.json</c> or environment variables of the form
/// <c>LLM__PROVIDER</c>, <c>LLM__MODEL</c>, <c>LLM__APIKEY</c>,
/// <c>LLM__BASEURL</c>.
/// </summary>
/// <remarks>
/// The provider name is bound as a string (not an enum) so the connector
/// registrar can produce a friendly error listing every supported value
/// when an unknown provider is configured.
/// </remarks>
public sealed class LlmOptions
{
    /// <summary>Configuration section name in <c>appsettings.json</c>.</summary>
    public const string SectionName = "Llm";

    /// <summary>
    /// Provider name. Must match one of <see cref="LlmProvider"/> values
    /// (case-insensitive). Examples: <c>Anthropic</c>, <c>OpenAI</c>,
    /// <c>Ollama</c>.
    /// </summary>
    public string Provider { get; set; } = nameof(LlmProvider.Anthropic);

    /// <summary>
    /// Model identifier passed to the chosen provider, e.g.
    /// <c>claude-sonnet-4-20250514</c> for Anthropic, <c>gpt-4o-mini</c>
    /// for OpenAI, or <c>llama3.2</c> for Ollama.
    /// </summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// API key used to authenticate with the chosen provider. Required
    /// for Anthropic and OpenAI; ignored for Ollama (which is unauthenticated
    /// by default).
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Optional base URL override. Useful for OpenAI-compatible
    /// endpoints (Azure OpenAI, vLLM, OpenRouter, etc.) and required for
    /// non-default Ollama installations. When unset, providers fall back
    /// to their built-in defaults.
    /// </summary>
    public string? BaseUrl { get; set; }
}

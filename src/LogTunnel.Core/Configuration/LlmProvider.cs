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

namespace LogTunnel.Core.Configuration;

/// <summary>
/// Identifies which LLM backend the translator will route through.
/// New values added here must also be handled by the connector registrar
/// or startup will fail fast with a clear error.
/// </summary>
public enum LlmProvider
{
    /// <summary>Anthropic Claude via the official Anthropic .NET SDK.</summary>
    Anthropic,

    /// <summary>OpenAI (or OpenAI-compatible API) via Microsoft.SemanticKernel.Connectors.OpenAI.</summary>
    OpenAI,

    /// <summary>Self-hosted Ollama via Microsoft.SemanticKernel.Connectors.Ollama.</summary>
    Ollama,
}

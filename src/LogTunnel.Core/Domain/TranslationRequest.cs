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

namespace LogTunnel.Core.Domain;

// The central concept — one translation job
/// <summary>
/// A single translation job: raw commit text plus the company context and
/// audience configurations needed to produce one changelog per audience.
/// </summary>
/// <param name="RawCommits">Raw Git commit messages, typically newline-separated.</param>
/// <param name="ChangedFiles">
/// Optional pre-formatted list of file paths touched by the commits
/// (one path per line). When present the LLM can cross-check the
/// commit messages against the actual file changes. When null the
/// prompt section is omitted gracefully.
/// </param>
/// <param name="Context">Company context used to frame language and terminology.</param>
/// <param name="Audiences">Audience configurations to render outputs for.</param>
public record TranslationRequest(
    string RawCommits,
    string? ChangedFiles,
    CompanyContext Context,
    IReadOnlyList<AudienceConfig> Audiences
);

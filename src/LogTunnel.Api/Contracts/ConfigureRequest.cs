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

namespace LogTunnel.Api.Contracts;

/// <summary>
/// Body of <c>POST /configure</c>. Captures the company context and
/// audience configuration that will be persisted as a
/// <c>.logtunnel.json</c> file on disk for use by the CLI.
/// </summary>
/// <param name="CompanyContext">Company context to persist.</param>
/// <param name="AudienceConfigs">Audience configurations to persist.</param>
public sealed record ConfigureRequest(
    CompanyContextDto CompanyContext,
    IReadOnlyList<AudienceConfigDto> AudienceConfigs);

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

using LogTunnel.Core.Domain;

namespace LogTunnel.Core.Configuration;

/// <summary>
/// On-disk shape of <c>.logtunnel.json</c>. The CLI reads this file when
/// running translations against a local repository, and the API writes
/// it from <c>POST /configure</c>; the field names are part of a public
/// contract — do not rename without updating both hosts in lock-step.
/// </summary>
/// <param name="Context">Persisted company context.</param>
/// <param name="Audiences">Persisted audience configurations.</param>
public sealed record LogTunnelProjectConfig(
    CompanyContext Context,
    IReadOnlyList<AudienceConfig> Audiences);

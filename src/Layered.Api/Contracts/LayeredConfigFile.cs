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

namespace Layered.Api.Contracts;

/// <summary>
/// On-disk shape of <c>.layered.json</c>. The CLI reads this file when
/// running translations against a local repository, so the field names
/// here are part of a public contract — do not rename without updating
/// the CLI in lock-step.
/// </summary>
/// <param name="Context">Persisted company context.</param>
/// <param name="Audiences">Persisted audience configurations.</param>
public sealed record LayeredConfigFile(
    CompanyContextDto Context,
    IReadOnlyList<AudienceConfigDto> Audiences)
{
    /// <summary>Build a <see cref="LayeredConfigFile"/> from a <see cref="ConfigureRequest"/>.</summary>
    public static LayeredConfigFile FromRequest(ConfigureRequest request) => new(
        Context: request.CompanyContext,
        Audiences: request.AudienceConfigs);
}

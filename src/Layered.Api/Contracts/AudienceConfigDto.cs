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

namespace Layered.Api.Contracts;

/// <summary>
/// Wire-format representation of <see cref="AudienceConfig"/> used by the
/// HTTP API.
/// </summary>
/// <param name="Type">Which audience this configuration targets.</param>
/// <param name="Tone">Desired tone, e.g. "technical and direct".</param>
/// <param name="Format">Desired output format, e.g. "bullet points, max 5 items".</param>
/// <param name="CustomInstructions">Optional extra instructions appended to the prompt.</param>
public sealed record AudienceConfigDto(
    AudienceType Type,
    string Tone,
    string Format,
    string? CustomInstructions)
{
    /// <summary>Map this DTO to its <see cref="AudienceConfig"/> domain representation.</summary>
    public AudienceConfig ToDomain() => new(
        Type: Type,
        Tone: Tone,
        Format: Format,
        CustomInstructions: CustomInstructions);
}

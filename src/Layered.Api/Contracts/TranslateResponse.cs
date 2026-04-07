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
/// Response body for <c>POST /translate</c>. Returns the rendered
/// changelog for every requested audience.
/// </summary>
/// <param name="Id">Unique identifier for this output.</param>
/// <param name="GeneratedAt">When the output was produced.</param>
/// <param name="Outputs">Map of audience type to rendered changelog text.</param>
public sealed record TranslateResponse(
    Guid Id,
    DateTimeOffset GeneratedAt,
    IReadOnlyDictionary<AudienceType, string> Outputs)
{
    /// <summary>Build a response from the translator's <see cref="ChangelogOutput"/>.</summary>
    public static TranslateResponse FromDomain(ChangelogOutput output) => new(
        Id: output.Id,
        GeneratedAt: output.GeneratedAt,
        Outputs: output.Outputs);
}

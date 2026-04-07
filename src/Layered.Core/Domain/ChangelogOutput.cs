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

namespace Layered.Core.Domain;

/// <summary>
/// The result of a translation job: one rendered changelog string per
/// audience that was requested.
/// </summary>
/// <param name="Id">Unique identifier for this output.</param>
/// <param name="GeneratedAt">When the output was produced.</param>
/// <param name="Outputs">Map of audience type to rendered changelog text.</param>
public record ChangelogOutput(
    Guid Id,
    DateTimeOffset GeneratedAt,
    IReadOnlyDictionary<AudienceType, string> Outputs
);

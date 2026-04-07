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
/// Per-audience configuration that shapes how the translator renders one
/// changelog variant.
/// </summary>
/// <param name="Type">Which audience this configuration targets.</param>
/// <param name="Tone">Desired tone, e.g. "technical and direct".</param>
/// <param name="Format">Desired output format, e.g. "bullet points, max 5 items".</param>
/// <param name="CustomInstructions">Optional extra instructions appended to the prompt.</param>
public record AudienceConfig(
    AudienceType Type,      // TechLead | Manager | CEO | Public
    string Tone,            // e.g. "technical and direct"
    string Format,          // e.g. "bullet points, max 5 items"
    string? CustomInstructions
);

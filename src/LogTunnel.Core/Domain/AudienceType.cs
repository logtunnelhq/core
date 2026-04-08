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

/// <summary>
/// Identifies which audience a translated changelog entry is intended for.
/// Each value maps to a distinct system prompt under /docs/prompts/.
/// </summary>
public enum AudienceType
{
    /// <summary>Technical detail, PR references, breaking changes flagged.</summary>
    TechLead,

    /// <summary>What shipped, business impact, risks, no jargon.</summary>
    Manager,

    /// <summary>Pure business language, three bullet points max, outcomes not features.</summary>
    CEO,

    /// <summary>Customer-facing, positive framing, features only not fixes.</summary>
    Public
}

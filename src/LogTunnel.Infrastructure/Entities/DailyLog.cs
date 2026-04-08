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

namespace LogTunnel.Infrastructure.Entities;

/// <summary>
/// EF Core entity for the <c>daily_logs</c> table — the centerpiece of
/// Phase 2. One row per <c>(user, day)</c>. Editable until midnight in
/// the tenant timezone, then frozen by the freeze worker; subsequent
/// edits append to <see cref="DailyLogRevision"/> instead of mutating
/// this row.
/// </summary>
public sealed class DailyLog
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }

    /// <summary>The calendar day in the tenant's timezone — not a UTC instant.</summary>
    public DateOnly LogDate { get; set; }

    /// <summary>What the developer wrote, plain text.</summary>
    public string RawNote { get; set; } = string.Empty;

    /// <summary>
    /// Optional list of project ids the day touched. Stored as a uuid[]
    /// in Postgres; no FK enforcement, by design — the array is a hint
    /// for filtering, not a hard relationship.
    /// </summary>
    public Guid[] ProjectTags { get; set; } = Array.Empty<Guid>();

    /// <summary>
    /// Optional structured blocker flag: <c>"need_help"</c>,
    /// <c>"waiting"</c>, <c>"unclear"</c>, or null for "no blocker".
    /// Read directly by the team-lead UI; never sent to the LLM.
    /// </summary>
    public string? BlockerStatus { get; set; }

    /// <summary>Optional free-text follow-up describing the blocker.</summary>
    public string? BlockerNote { get; set; }

    /// <summary>
    /// Set when the day rolls over in the tenant timezone. Null = the
    /// log is still editable in place. Once set, edits go to
    /// <see cref="DailyLogRevision"/>.
    /// </summary>
    public DateTimeOffset? FrozenAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

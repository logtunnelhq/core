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
/// EF Core entity for the <c>daily_log_revisions</c> append-only audit
/// trail. Once <see cref="DailyLog.FrozenAt"/> is set, every subsequent
/// edit writes a new row here instead of mutating the parent
/// <see cref="DailyLog"/>.
/// </summary>
public sealed class DailyLogRevision
{
    public Guid Id { get; set; }
    public Guid DailyLogId { get; set; }

    public string RawNote { get; set; } = string.Empty;

    public Guid[] ProjectTags { get; set; } = Array.Empty<Guid>();

    public string? BlockerStatus { get; set; }
    public string? BlockerNote { get; set; }

    /// <summary>Optional one-line reason supplied by the editor.</summary>
    public string? EditReason { get; set; }

    public Guid EditedBy { get; set; }
    public DateTimeOffset EditedAt { get; set; }
}

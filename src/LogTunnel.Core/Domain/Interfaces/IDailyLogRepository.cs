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

using LogTunnel.Core.Common;
using LogTunnel.Core.Domain.Entities;

namespace LogTunnel.Core.Domain.Interfaces;

/// <summary>
/// Read/write access to <see cref="DailyLog"/> rows and the append-only
/// <see cref="DailyLogRevision"/> trail. Implementations are responsible
/// for honouring the freeze rule: once
/// <see cref="DailyLog.FrozenAt"/> is set, edits go through
/// <see cref="AddRevisionAsync"/> instead of mutating the parent row.
/// </summary>
public interface IDailyLogRepository
{
    /// <summary>Fetch a daily log by tenant and primary key.</summary>
    Task<Result<DailyLog>> GetByIdAsync(
        Guid tenantId,
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>Fetch the daily log for a user on a specific day, if any.</summary>
    Task<Result<DailyLog?>> GetForUserAndDateAsync(
        Guid tenantId,
        Guid userId,
        DateOnly logDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// List a user's daily logs in an inclusive date range, ordered
    /// most recent day first.
    /// </summary>
    Task<Result<IReadOnlyList<DailyLog>>> ListByUserAsync(
        Guid tenantId,
        Guid userId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);

    /// <summary>List every revision attached to a daily log, oldest first.</summary>
    Task<Result<IReadOnlyList<DailyLogRevision>>> ListRevisionsAsync(
        Guid dailyLogId,
        CancellationToken cancellationToken = default);

    /// <summary>Insert a fresh daily log. Fails if one already exists for <c>(tenant, user, date)</c>.</summary>
    Task<Result<DailyLog>> AddAsync(DailyLog dailyLog, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an unfrozen daily log in place. Fails if the log is
    /// already frozen — callers should switch to
    /// <see cref="AddRevisionAsync"/> at that point.
    /// </summary>
    Task<Result<DailyLog>> UpdateAsync(DailyLog dailyLog, CancellationToken cancellationToken = default);

    /// <summary>Append a post-freeze revision to a daily log.</summary>
    Task<Result<DailyLogRevision>> AddRevisionAsync(
        DailyLogRevision revision,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// List every daily log on a given day in a tenant whose
    /// <see cref="DailyLog.BlockerStatus"/> is set. Used by the team
    /// lead's "show me today's blockers" view.
    /// </summary>
    Task<Result<IReadOnlyList<DailyLog>>> ListBlockersForTenantAndDateAsync(
        Guid tenantId,
        DateOnly logDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk-fetch daily logs for many users on a single day. Used by
    /// the team-stand-up view to fetch every member's log in one
    /// query rather than N+1.
    /// </summary>
    Task<Result<IReadOnlyList<DailyLog>>> ListByUsersAndDateAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> userIds,
        DateOnly logDate,
        CancellationToken cancellationToken = default);

    /// <summary>List every daily log in a tenant within an inclusive date range.</summary>
    Task<Result<IReadOnlyList<DailyLog>>> ListByTenantInRangeAsync(
        Guid tenantId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk-flip every daily log in a tenant whose
    /// <see cref="DailyLog.LogDate"/> is strictly less than
    /// <paramref name="cutoffDate"/> and whose
    /// <see cref="DailyLog.FrozenAt"/> is still null. Returns the
    /// number of rows that were frozen. Used by the daily-log freeze
    /// background service at the start of each tenant-local day.
    /// </summary>
    Task<Result<int>> FreezeLogsBeforeDateAsync(
        Guid tenantId,
        DateOnly cutoffDate,
        DateTimeOffset frozenAt,
        CancellationToken cancellationToken = default);
}

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
using LogTunnel.Core.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LogTunnel.Infrastructure.Repositories;

internal sealed class DailyLogRepository : IDailyLogRepository
{
    private readonly LogTunnelDbContext _db;

    public DailyLogRepository(LogTunnelDbContext db) => _db = db;

    public async Task<Result<DailyLog?>> GetForUserAndDateAsync(
        Guid tenantId,
        Guid userId,
        DateOnly logDate,
        CancellationToken cancellationToken = default)
    {
        var log = await _db.DailyLogs
            .FirstOrDefaultAsync(
                d => d.TenantId == tenantId && d.UserId == userId && d.LogDate == logDate,
                cancellationToken)
            .ConfigureAwait(false);
        return Result<DailyLog?>.Success(log);
    }

    public async Task<Result<DailyLog>> AddAsync(DailyLog dailyLog, CancellationToken cancellationToken = default)
    {
        if (dailyLog is null) return Result<DailyLog>.Failure("DailyLog is required.");

        try
        {
            dailyLog.CreatedAt = DateTimeOffset.UtcNow;
            dailyLog.UpdatedAt = DateTimeOffset.UtcNow;
            _db.DailyLogs.Add(dailyLog);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result<DailyLog>.Success(dailyLog);
        }
        catch (DbUpdateException ex)
        {
            return Result<DailyLog>.Failure($"Failed to insert daily log: {ex.GetBaseException().Message}");
        }
    }

    public async Task<Result<DailyLog>> UpdateAsync(DailyLog dailyLog, CancellationToken cancellationToken = default)
    {
        if (dailyLog is null) return Result<DailyLog>.Failure("DailyLog is required.");

        var existing = await _db.DailyLogs
            .FirstOrDefaultAsync(d => d.Id == dailyLog.Id, cancellationToken)
            .ConfigureAwait(false);
        if (existing is null)
            return Result<DailyLog>.Failure($"DailyLog {dailyLog.Id} not found.");

        if (existing.FrozenAt is not null)
            return Result<DailyLog>.Failure(
                $"DailyLog {dailyLog.Id} is frozen as of {existing.FrozenAt}; use AddRevisionAsync for post-freeze edits.");

        existing.RawNote = dailyLog.RawNote;
        existing.ProjectTags = dailyLog.ProjectTags;
        existing.BlockerStatus = dailyLog.BlockerStatus;
        existing.BlockerNote = dailyLog.BlockerNote;
        existing.UpdatedAt = DateTimeOffset.UtcNow;

        try
        {
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result<DailyLog>.Success(existing);
        }
        catch (DbUpdateException ex)
        {
            return Result<DailyLog>.Failure($"Failed to update daily log: {ex.GetBaseException().Message}");
        }
    }

    public async Task<Result<DailyLogRevision>> AddRevisionAsync(
        DailyLogRevision revision,
        CancellationToken cancellationToken = default)
    {
        if (revision is null) return Result<DailyLogRevision>.Failure("Revision is required.");

        try
        {
            revision.EditedAt = DateTimeOffset.UtcNow;
            _db.DailyLogRevisions.Add(revision);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result<DailyLogRevision>.Success(revision);
        }
        catch (DbUpdateException ex)
        {
            return Result<DailyLogRevision>.Failure(
                $"Failed to insert daily log revision: {ex.GetBaseException().Message}");
        }
    }

    public async Task<Result<IReadOnlyList<DailyLog>>> ListBlockersForTenantAndDateAsync(
        Guid tenantId,
        DateOnly logDate,
        CancellationToken cancellationToken = default)
    {
        var logs = await _db.DailyLogs
            .Where(d => d.TenantId == tenantId && d.LogDate == logDate && d.BlockerStatus != null)
            .OrderBy(d => d.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return Result<IReadOnlyList<DailyLog>>.Success(logs);
    }
}

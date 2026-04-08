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

internal sealed class TranslationRepository : ITranslationRepository
{
    private readonly LogTunnelDbContext _db;

    public TranslationRepository(LogTunnelDbContext db) => _db = db;

    public async Task<Result<Translation?>> GetAsync(
        Guid tenantId,
        string scopeKind,
        Guid scopeId,
        DateOnly dateFrom,
        DateOnly dateTo,
        string audience,
        CancellationToken cancellationToken = default)
    {
        var translation = await _db.Translations
            .FirstOrDefaultAsync(
                t => t.TenantId == tenantId
                  && t.ScopeKind == scopeKind
                  && t.ScopeId == scopeId
                  && t.DateFrom == dateFrom
                  && t.DateTo == dateTo
                  && t.Audience == audience,
                cancellationToken)
            .ConfigureAwait(false);
        return Result<Translation?>.Success(translation);
    }

    public async Task<Result<Translation>> AddAsync(Translation translation, CancellationToken cancellationToken = default)
    {
        if (translation is null) return Result<Translation>.Failure("Translation is required.");

        try
        {
            translation.GeneratedAt = DateTimeOffset.UtcNow;
            _db.Translations.Add(translation);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result<Translation>.Success(translation);
        }
        catch (DbUpdateException ex)
        {
            return Result<Translation>.Failure($"Failed to insert translation: {ex.GetBaseException().Message}");
        }
    }

    public async Task<Result<Translation?>> LeaseNextPendingAsync(CancellationToken cancellationToken = default)
    {
        // Use a SELECT ... FOR UPDATE SKIP LOCKED transaction so two
        // workers can run side-by-side without grabbing the same row.
        // We then flip status to a sentinel that prevents another
        // worker from picking it up while we render.
        await using var tx = await _db.Database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        var leased = await _db.Translations
            .FromSqlRaw(
                "SELECT * FROM translations WHERE status = 'pending' " +
                "ORDER BY generated_at ASC LIMIT 1 FOR UPDATE SKIP LOCKED")
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (leased is null)
        {
            await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
            return Result<Translation?>.Success(null);
        }

        // Mark with a transient status so subsequent leases skip it
        // even after the row lock releases.
        leased.Status = "rendering";
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
        return Result<Translation?>.Success(leased);
    }

    public async Task<Result<Translation>> MarkReadyAsync(
        Guid translationId,
        string content,
        CancellationToken cancellationToken = default)
    {
        var existing = await _db.Translations
            .FirstOrDefaultAsync(t => t.Id == translationId, cancellationToken)
            .ConfigureAwait(false);
        if (existing is null) return Result<Translation>.Failure($"Translation {translationId} not found.");

        existing.Content = content;
        existing.Status = "ready";
        existing.FailureReason = null;
        existing.GeneratedAt = DateTimeOffset.UtcNow;

        try
        {
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result<Translation>.Success(existing);
        }
        catch (DbUpdateException ex)
        {
            return Result<Translation>.Failure($"Failed to mark translation ready: {ex.GetBaseException().Message}");
        }
    }

    public async Task<Result<Translation>> MarkFailedAsync(
        Guid translationId,
        string failureReason,
        CancellationToken cancellationToken = default)
    {
        var existing = await _db.Translations
            .FirstOrDefaultAsync(t => t.Id == translationId, cancellationToken)
            .ConfigureAwait(false);
        if (existing is null) return Result<Translation>.Failure($"Translation {translationId} not found.");

        existing.Status = "failed";
        existing.FailureReason = failureReason;
        existing.GeneratedAt = DateTimeOffset.UtcNow;

        try
        {
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result<Translation>.Success(existing);
        }
        catch (DbUpdateException ex)
        {
            return Result<Translation>.Failure($"Failed to mark translation failed: {ex.GetBaseException().Message}");
        }
    }
}

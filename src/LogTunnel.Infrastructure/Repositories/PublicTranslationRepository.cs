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

internal sealed class PublicTranslationRepository : IPublicTranslationRepository
{
    private readonly LogTunnelDbContext _db;

    public PublicTranslationRepository(LogTunnelDbContext db) => _db = db;

    public async Task<Result<PublicTranslation>> GetByIdAsync(
        Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        var row = await _db.PublicTranslations
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Id == id, cancellationToken)
            .ConfigureAwait(false);
        return row is null
            ? Result<PublicTranslation>.Failure($"PublicTranslation {id} not found in tenant {tenantId}.")
            : Result<PublicTranslation>.Success(row);
    }

    public async Task<Result<IReadOnlyList<PublicTranslation>>> ListByTenantAsync(
        Guid tenantId, string? workflowStatus, CancellationToken cancellationToken = default)
    {
        var query = _db.PublicTranslations.Where(p => p.TenantId == tenantId);
        if (!string.IsNullOrWhiteSpace(workflowStatus))
            query = query.Where(p => p.WorkflowStatus == workflowStatus);

        var rows = await query
            .OrderByDescending(p => p.UpdatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return Result<IReadOnlyList<PublicTranslation>>.Success(rows);
    }

    public async Task<Result<PublicTranslation>> UpdateContentAsync(
        Guid publicTranslationId,
        string editedContent,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        var existing = await _db.PublicTranslations
            .FirstOrDefaultAsync(p => p.Id == publicTranslationId, cancellationToken)
            .ConfigureAwait(false);
        if (existing is null)
            return Result<PublicTranslation>.Failure($"PublicTranslation {publicTranslationId} not found.");
        if (existing.WorkflowStatus != "draft")
            return Result<PublicTranslation>.Failure(
                $"Cannot edit a public translation in '{existing.WorkflowStatus}' state. Only 'draft' is editable.");

        var now = DateTimeOffset.UtcNow;
        existing.EditedContent = editedContent;
        existing.UpdatedAt = now;

        _db.PublicTranslationEvents.Add(new PublicTranslationEvent
        {
            PublicTranslationId = existing.Id,
            EventType = "edited",
            ActorId = actorId,
            Notes = null,
            OccurredAt = now,
        });

        try
        {
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result<PublicTranslation>.Success(existing);
        }
        catch (DbUpdateException ex)
        {
            return Result<PublicTranslation>.Failure(
                $"Failed to update public translation: {ex.GetBaseException().Message}");
        }
    }

    public async Task<Result<PublicTranslation>> AddAsync(PublicTranslation publicTranslation, CancellationToken cancellationToken = default)
    {
        if (publicTranslation is null) return Result<PublicTranslation>.Failure("PublicTranslation is required.");

        try
        {
            publicTranslation.CreatedAt = DateTimeOffset.UtcNow;
            publicTranslation.UpdatedAt = DateTimeOffset.UtcNow;
            _db.PublicTranslations.Add(publicTranslation);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result<PublicTranslation>.Success(publicTranslation);
        }
        catch (DbUpdateException ex)
        {
            return Result<PublicTranslation>.Failure(
                $"Failed to insert public translation: {ex.GetBaseException().Message}");
        }
    }

    public async Task<Result<PublicTranslation>> TransitionStatusAsync(
        Guid publicTranslationId,
        string newStatus,
        Guid actorId,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var existing = await _db.PublicTranslations
            .FirstOrDefaultAsync(p => p.Id == publicTranslationId, cancellationToken)
            .ConfigureAwait(false);
        if (existing is null)
            return Result<PublicTranslation>.Failure($"PublicTranslation {publicTranslationId} not found.");

        // State machine. Allowed transitions:
        //   draft     → approved          (marketing approves the draft)
        //   approved  → draft             (marketing rejects, sends back)
        //   approved  → published          (marketing publishes)
        //   published → published          (no-op for re-publish; rejected here)
        //   published → ... → draft        (unpublish path — not in scope yet)
        var allowed = (existing.WorkflowStatus, newStatus) switch
        {
            ("draft",     "approved")  => true,
            ("approved",  "draft")     => true,
            ("approved",  "published") => true,
            _                          => false,
        };
        if (!allowed)
        {
            return Result<PublicTranslation>.Failure(
                $"Cannot transition public translation from '{existing.WorkflowStatus}' to '{newStatus}'.");
        }

        var now = DateTimeOffset.UtcNow;
        existing.WorkflowStatus = newStatus;
        existing.UpdatedAt = now;

        switch (newStatus)
        {
            case "approved":
                existing.ApprovedBy = actorId;
                existing.ApprovedAt = now;
                break;
            case "published":
                existing.PublishedAt = now;
                break;
        }

        // Append the audit event in the same SaveChanges so the transition
        // and the audit trail are atomic.
        var eventType = (existing.WorkflowStatus, newStatus) switch
        {
            (_, "approved")  => "approved",
            (_, "published") => "published",
            (_, "draft")     => "rejected",
            _                => "edited",
        };
        _db.PublicTranslationEvents.Add(new PublicTranslationEvent
        {
            PublicTranslationId = existing.Id,
            EventType = eventType,
            ActorId = actorId,
            Notes = notes,
            OccurredAt = now,
        });

        try
        {
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result<PublicTranslation>.Success(existing);
        }
        catch (DbUpdateException ex)
        {
            return Result<PublicTranslation>.Failure(
                $"Failed to transition public translation: {ex.GetBaseException().Message}");
        }
    }
}

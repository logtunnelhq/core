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

internal sealed class InvitationRepository : IInvitationRepository
{
    private readonly LogTunnelDbContext _db;

    public InvitationRepository(LogTunnelDbContext db) => _db = db;

    public async Task<Result<Invitation>> AddAsync(Invitation invitation, CancellationToken cancellationToken = default)
    {
        if (invitation is null) return Result<Invitation>.Failure("Invitation is required.");

        try
        {
            invitation.CreatedAt = DateTimeOffset.UtcNow;
            _db.Invitations.Add(invitation);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result<Invitation>.Success(invitation);
        }
        catch (DbUpdateException ex)
        {
            return Result<Invitation>.Failure($"Failed to insert invitation: {ex.GetBaseException().Message}");
        }
    }

    public async Task<Result<Invitation?>> FindByHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
            return Result<Invitation?>.Failure("Token hash must not be empty.");

        var invitation = await _db.Invitations
            .FirstOrDefaultAsync(i => i.TokenHash == tokenHash, cancellationToken)
            .ConfigureAwait(false);
        return Result<Invitation?>.Success(invitation);
    }

    public async Task<Result<Invitation>> MarkAcceptedAsync(
        Guid invitationId,
        Guid acceptedUserId,
        CancellationToken cancellationToken = default)
    {
        var existing = await _db.Invitations
            .FirstOrDefaultAsync(i => i.Id == invitationId, cancellationToken)
            .ConfigureAwait(false);
        if (existing is null)
            return Result<Invitation>.Failure($"Invitation {invitationId} not found.");

        if (existing.AcceptedAt is not null)
            return Result<Invitation>.Failure($"Invitation {invitationId} has already been accepted.");

        existing.AcceptedAt = DateTimeOffset.UtcNow;
        existing.AcceptedUserId = acceptedUserId;

        try
        {
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result<Invitation>.Success(existing);
        }
        catch (DbUpdateException ex)
        {
            return Result<Invitation>.Failure($"Failed to mark invitation accepted: {ex.GetBaseException().Message}");
        }
    }
}

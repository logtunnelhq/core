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

internal sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly LogTunnelDbContext _db;

    public RefreshTokenRepository(LogTunnelDbContext db) => _db = db;

    public async Task<Result<RefreshToken>> AddAsync(RefreshToken token, CancellationToken cancellationToken = default)
    {
        if (token is null) return Result<RefreshToken>.Failure("RefreshToken is required.");

        try
        {
            token.CreatedAt = DateTimeOffset.UtcNow;
            _db.RefreshTokens.Add(token);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result<RefreshToken>.Success(token);
        }
        catch (DbUpdateException ex)
        {
            return Result<RefreshToken>.Failure($"Failed to insert refresh token: {ex.GetBaseException().Message}");
        }
    }

    public async Task<Result<RefreshToken?>> FindByHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
            return Result<RefreshToken?>.Failure("Token hash must not be empty.");

        var token = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken)
            .ConfigureAwait(false);
        return Result<RefreshToken?>.Success(token);
    }

    public async Task<Result<bool>> RevokeAsync(Guid tokenId, CancellationToken cancellationToken = default)
    {
        var existing = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.Id == tokenId, cancellationToken)
            .ConfigureAwait(false);
        if (existing is null)
            return Result<bool>.Failure($"Refresh token {tokenId} not found.");

        if (existing.RevokedAt is not null)
            return Result<bool>.Success(true);

        existing.RevokedAt = DateTimeOffset.UtcNow;
        try
        {
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result<bool>.Success(true);
        }
        catch (DbUpdateException ex)
        {
            return Result<bool>.Failure($"Failed to revoke refresh token: {ex.GetBaseException().Message}");
        }
    }
}

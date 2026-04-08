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
/// Read/write access to <see cref="RefreshToken"/> rows. Backs the
/// JWT refresh-token rotation flow in <c>LogTunnel.Platform</c>.
/// </summary>
public interface IRefreshTokenRepository
{
    /// <summary>Insert a freshly-issued refresh token row.</summary>
    Task<Result<RefreshToken>> AddAsync(RefreshToken token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Look up a refresh token by its SHA-256 hash. Returns
    /// <c>Result.Success(null)</c> when no row matches.
    /// </summary>
    Task<Result<RefreshToken?>> FindByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark a refresh token as revoked (typically as part of a rotation
    /// or an explicit logout). Idempotent — revoking an already-revoked
    /// token is a no-op success.
    /// </summary>
    Task<Result<bool>> RevokeAsync(Guid tokenId, CancellationToken cancellationToken = default);
}

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

namespace LogTunnel.Core.Domain.Entities;

/// <summary>
/// EF Core entity for the <c>refresh_tokens</c> table. Persists rolled
/// refresh tokens for the JWT auth flow. The token itself is never
/// stored — only its SHA-256 hash, so a database leak doesn't hand an
/// attacker a working token. Each row models a single device session;
/// rotating means inserting a new row and setting <see cref="RevokedAt"/>
/// on the previous one in the same transaction.
/// </summary>
public sealed class RefreshToken
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }

    /// <summary>SHA-256 hex of the refresh token. Indexed for the <c>/auth/refresh</c> lookup.</summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>Set on rotation or explicit logout. Null = active.</summary>
    public DateTimeOffset? RevokedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

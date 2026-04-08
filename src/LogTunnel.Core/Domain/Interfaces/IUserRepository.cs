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
/// Read/write access to <see cref="User"/> rows plus the team and
/// project membership joins. Aggregates user identity + team /
/// project assignment because the data is always queried together
/// in the role-based dashboard.
/// </summary>
public interface IUserRepository
{
    /// <summary>Fetch a user by tenant and primary key.</summary>
    Task<Result<User>> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);

    /// <summary>Look up a user by login email within a tenant.</summary>
    Task<Result<User>> FindByEmailAsync(Guid tenantId, string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Look up a user by login email across all tenants. Used by
    /// <c>/auth/login</c> where the caller has only an email and no
    /// tenant context. Returns the first row whose email matches
    /// (case-sensitive). Phase 2 v1 ships with one bootstrap tenant
    /// so collisions are not yet a concern; promoting this to a
    /// global unique constraint is a follow-up if multi-tenant login
    /// becomes a real requirement.
    /// </summary>
    Task<Result<User>> FindByEmailAcrossTenantsAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolve a user from a git author email (case-insensitive). Used by
    /// the webhook ingester to populate <see cref="Commit.AuthorUserId"/>.
    /// </summary>
    Task<Result<User>> FindByGitEmailAsync(Guid tenantId, string gitEmail, CancellationToken cancellationToken = default);

    /// <summary>List every user in the tenant, ordered by display name.</summary>
    Task<Result<IReadOnlyList<User>>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>Insert a new user. Returns the persisted entity.</summary>
    Task<Result<User>> AddAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>Add a user to a team in the given role (<c>"member"</c> or <c>"lead"</c>).</summary>
    Task<Result<bool>> AddToTeamAsync(Guid teamId, Guid userId, string role, CancellationToken cancellationToken = default);

    /// <summary>Remove a user from a team. Idempotent — succeeds when no row matches.</summary>
    Task<Result<bool>> RemoveFromTeamAsync(Guid teamId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Add a user to a project as a regular member.</summary>
    Task<Result<bool>> AddToProjectAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Remove a user from a project. Idempotent — succeeds when no row matches.</summary>
    Task<Result<bool>> RemoveFromProjectAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default);
}

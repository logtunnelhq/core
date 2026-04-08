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
/// Read/write access to <see cref="Team"/> rows.
/// <see cref="TeamMember"/> joins are managed via
/// <see cref="IUserRepository.AddToTeamAsync"/>.
/// </summary>
public interface ITeamRepository
{
    /// <summary>Fetch a team by tenant and primary key.</summary>
    Task<Result<Team>> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);

    /// <summary>List every team in the tenant, ordered by name.</summary>
    Task<Result<IReadOnlyList<Team>>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// List the teams a specific user belongs to, paired with their
    /// membership role on each. Used by the team-lead dashboard's
    /// "my teams" picker — users can see their own memberships
    /// without admin privileges.
    /// </summary>
    Task<Result<IReadOnlyList<UserTeamMembership>>> ListByUserAsync(
        Guid tenantId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// List the members of a team — joined against
    /// <see cref="User"/> rows so callers see display names alongside
    /// the membership role (<c>"member"</c> or <c>"lead"</c>).
    /// </summary>
    Task<Result<IReadOnlyList<TeamMembership>>> ListMembersAsync(
        Guid teamId, CancellationToken cancellationToken = default);

    /// <summary>Insert a new team.</summary>
    Task<Result<Team>> AddAsync(Team team, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result row for <see cref="ITeamRepository.ListMembersAsync"/>.
/// Carries the user identity together with the membership role on
/// that team.
/// </summary>
public sealed record TeamMembership(User User, string Role);

/// <summary>
/// Result row for <see cref="ITeamRepository.ListByUserAsync"/>.
/// Carries the team plus the user's membership role on it.
/// </summary>
public sealed record UserTeamMembership(Team Team, string Role);

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

internal sealed class TeamRepository : ITeamRepository
{
    private readonly LogTunnelDbContext _db;

    public TeamRepository(LogTunnelDbContext db) => _db = db;

    public async Task<Result<Team>> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        var team = await _db.Teams
            .FirstOrDefaultAsync(t => t.TenantId == tenantId && t.Id == id, cancellationToken)
            .ConfigureAwait(false);
        return team is null
            ? Result<Team>.Failure($"Team {id} not found in tenant {tenantId}.")
            : Result<Team>.Success(team);
    }

    public async Task<Result<IReadOnlyList<Team>>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var rows = await _db.Teams
            .Where(t => t.TenantId == tenantId)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return Result<IReadOnlyList<Team>>.Success(rows);
    }

    public async Task<Result<IReadOnlyList<TeamMembership>>> ListMembersAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        var rows = await (
            from tm in _db.TeamMembers
            join u in _db.Users on tm.UserId equals u.Id
            where tm.TeamId == teamId
            orderby u.DisplayName
            select new TeamMembership(u, tm.Role)
        ).ToListAsync(cancellationToken).ConfigureAwait(false);
        return Result<IReadOnlyList<TeamMembership>>.Success(rows);
    }

    public async Task<Result<Team>> AddAsync(Team team, CancellationToken cancellationToken = default)
    {
        if (team is null) return Result<Team>.Failure("Team is required.");

        try
        {
            _db.Teams.Add(team);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result<Team>.Success(team);
        }
        catch (DbUpdateException ex)
        {
            return Result<Team>.Failure($"Failed to insert team: {ex.GetBaseException().Message}");
        }
    }
}

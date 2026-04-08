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

internal sealed class ProjectRepository : IProjectRepository
{
    private readonly LogTunnelDbContext _db;

    public ProjectRepository(LogTunnelDbContext db) => _db = db;

    public async Task<Result<Project>> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        var project = await _db.Projects
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Id == id, cancellationToken)
            .ConfigureAwait(false);
        return project is null
            ? Result<Project>.Failure($"Project {id} not found in tenant {tenantId}.")
            : Result<Project>.Success(project);
    }

    public async Task<Result<IReadOnlyList<Project>>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var rows = await _db.Projects
            .Where(p => p.TenantId == tenantId)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return Result<IReadOnlyList<Project>>.Success(rows);
    }

    public async Task<Result<IReadOnlyList<Project>>> ListByUserAsync(
        Guid tenantId, Guid userId, CancellationToken cancellationToken = default)
    {
        var rows = await (
            from pm in _db.ProjectMembers
            join p in _db.Projects on pm.ProjectId equals p.Id
            where pm.UserId == userId && p.TenantId == tenantId
            orderby p.Name
            select p
        ).ToListAsync(cancellationToken).ConfigureAwait(false);
        return Result<IReadOnlyList<Project>>.Success(rows);
    }

    public async Task<Result<IReadOnlyList<User>>> ListMembersAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var rows = await (
            from pm in _db.ProjectMembers
            join u in _db.Users on pm.UserId equals u.Id
            where pm.ProjectId == projectId
            orderby u.DisplayName
            select u
        ).ToListAsync(cancellationToken).ConfigureAwait(false);
        return Result<IReadOnlyList<User>>.Success(rows);
    }

    public async Task<Result<Project>> AddAsync(Project project, CancellationToken cancellationToken = default)
    {
        if (project is null) return Result<Project>.Failure("Project is required.");

        try
        {
            _db.Projects.Add(project);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result<Project>.Success(project);
        }
        catch (DbUpdateException ex)
        {
            return Result<Project>.Failure($"Failed to insert project: {ex.GetBaseException().Message}");
        }
    }
}

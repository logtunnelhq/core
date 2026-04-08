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

internal sealed class UserRepository : IUserRepository
{
    private readonly LogTunnelDbContext _db;

    public UserRepository(LogTunnelDbContext db) => _db = db;

    public async Task<Result<User>> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Id == id, cancellationToken)
            .ConfigureAwait(false);
        return user is null
            ? Result<User>.Failure($"User {id} not found in tenant {tenantId}.")
            : Result<User>.Success(user);
    }

    public async Task<Result<User>> FindByEmailAsync(Guid tenantId, string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Result<User>.Failure("Email must not be empty.");

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Email == email, cancellationToken)
            .ConfigureAwait(false);
        return user is null
            ? Result<User>.Failure($"User '{email}' not found in tenant {tenantId}.")
            : Result<User>.Success(user);
    }

    public async Task<Result<User>> FindByEmailAcrossTenantsAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Result<User>.Failure("Email must not be empty.");

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken)
            .ConfigureAwait(false);
        return user is null
            ? Result<User>.Failure($"User '{email}' not found.")
            : Result<User>.Success(user);
    }

    public async Task<Result<User>> FindByGitEmailAsync(Guid tenantId, string gitEmail, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(gitEmail))
            return Result<User>.Failure("Git email must not be empty.");

        var normalised = gitEmail.ToLowerInvariant();
        var user = await _db.Users
            .FirstOrDefaultAsync(
                u => u.TenantId == tenantId && u.GitEmail != null && u.GitEmail.ToLower() == normalised,
                cancellationToken)
            .ConfigureAwait(false);
        return user is null
            ? Result<User>.Failure($"User with git email '{gitEmail}' not found in tenant {tenantId}.")
            : Result<User>.Success(user);
    }

    public async Task<Result<IReadOnlyList<User>>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var rows = await _db.Users
            .Where(u => u.TenantId == tenantId)
            .OrderBy(u => u.DisplayName)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return Result<IReadOnlyList<User>>.Success(rows);
    }

    public async Task<Result<User>> AddAsync(User user, CancellationToken cancellationToken = default)
    {
        if (user is null) return Result<User>.Failure("User is required.");

        try
        {
            _db.Users.Add(user);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result<User>.Success(user);
        }
        catch (DbUpdateException ex)
        {
            return Result<User>.Failure($"Failed to insert user: {ex.GetBaseException().Message}");
        }
    }

    public async Task<Result<bool>> AddToTeamAsync(Guid teamId, Guid userId, string role, CancellationToken cancellationToken = default)
    {
        if (role is not ("member" or "lead"))
            return Result<bool>.Failure($"Invalid team member role '{role}'. Expected 'member' or 'lead'.");

        try
        {
            _db.TeamMembers.Add(new TeamMember
            {
                TeamId = teamId,
                UserId = userId,
                Role = role,
                JoinedAt = DateTimeOffset.UtcNow,
            });
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result<bool>.Success(true);
        }
        catch (DbUpdateException ex)
        {
            return Result<bool>.Failure($"Failed to add user to team: {ex.GetBaseException().Message}");
        }
    }

    public async Task<Result<bool>> RemoveFromTeamAsync(Guid teamId, Guid userId, CancellationToken cancellationToken = default)
    {
        var existing = await _db.TeamMembers
            .FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.UserId == userId, cancellationToken)
            .ConfigureAwait(false);
        if (existing is null) return Result<bool>.Success(true); // idempotent

        try
        {
            _db.TeamMembers.Remove(existing);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result<bool>.Success(true);
        }
        catch (DbUpdateException ex)
        {
            return Result<bool>.Failure($"Failed to remove user from team: {ex.GetBaseException().Message}");
        }
    }

    public async Task<Result<bool>> AddToProjectAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _db.ProjectMembers.Add(new ProjectMember
            {
                ProjectId = projectId,
                UserId = userId,
                JoinedAt = DateTimeOffset.UtcNow,
            });
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result<bool>.Success(true);
        }
        catch (DbUpdateException ex)
        {
            return Result<bool>.Failure($"Failed to add user to project: {ex.GetBaseException().Message}");
        }
    }

    public async Task<Result<bool>> RemoveFromProjectAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default)
    {
        var existing = await _db.ProjectMembers
            .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId, cancellationToken)
            .ConfigureAwait(false);
        if (existing is null) return Result<bool>.Success(true); // idempotent

        try
        {
            _db.ProjectMembers.Remove(existing);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result<bool>.Success(true);
        }
        catch (DbUpdateException ex)
        {
            return Result<bool>.Failure($"Failed to remove user from project: {ex.GetBaseException().Message}");
        }
    }
}

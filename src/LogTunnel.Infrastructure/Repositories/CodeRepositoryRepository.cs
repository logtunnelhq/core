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

internal sealed class CodeRepositoryRepository : ICodeRepositoryRepository
{
    private readonly LogTunnelDbContext _db;

    public CodeRepositoryRepository(LogTunnelDbContext db) => _db = db;

    public async Task<Result<CodeRepository>> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        var repository = await _db.Repositories
            .FirstOrDefaultAsync(r => r.TenantId == tenantId && r.Id == id, cancellationToken)
            .ConfigureAwait(false);
        return repository is null
            ? Result<CodeRepository>.Failure($"Repository {id} not found in tenant {tenantId}.")
            : Result<CodeRepository>.Success(repository);
    }

    public async Task<Result<CodeRepository>> GetByIdAcrossTenantsAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        var repository = await _db.Repositories
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            .ConfigureAwait(false);
        return repository is null
            ? Result<CodeRepository>.Failure($"Repository {id} not found.")
            : Result<CodeRepository>.Success(repository);
    }

    public async Task<Result<IReadOnlyList<CodeRepository>>> ListByTenantAsync(
        Guid tenantId, CancellationToken cancellationToken = default)
    {
        var rows = await _db.Repositories
            .Where(r => r.TenantId == tenantId)
            .OrderBy(r => r.RemoteUrl)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return Result<IReadOnlyList<CodeRepository>>.Success(rows);
    }

    public async Task<Result<IReadOnlyList<RepositoryProjectMapping>>> ListProjectMappingsAsync(
        Guid repositoryId, CancellationToken cancellationToken = default)
    {
        var rows = await _db.RepositoryProjectMappings
            .Where(m => m.RepositoryId == repositoryId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return Result<IReadOnlyList<RepositoryProjectMapping>>.Success(rows);
    }

    public async Task<Result<CodeRepository>> AddAsync(CodeRepository repository, CancellationToken cancellationToken = default)
    {
        if (repository is null) return Result<CodeRepository>.Failure("Repository is required.");

        try
        {
            _db.Repositories.Add(repository);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result<CodeRepository>.Success(repository);
        }
        catch (DbUpdateException ex)
        {
            return Result<CodeRepository>.Failure($"Failed to insert repository: {ex.GetBaseException().Message}");
        }
    }

    public async Task<Result<RepositoryProjectMapping>> AddProjectMappingAsync(
        Guid repositoryId,
        Guid projectId,
        string? pathFilter,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var mapping = new RepositoryProjectMapping
            {
                RepositoryId = repositoryId,
                ProjectId = projectId,
                PathFilter = pathFilter,
                CreatedAt = DateTimeOffset.UtcNow,
            };
            _db.RepositoryProjectMappings.Add(mapping);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result<RepositoryProjectMapping>.Success(mapping);
        }
        catch (DbUpdateException ex)
        {
            return Result<RepositoryProjectMapping>.Failure(
                $"Failed to insert repository project mapping: {ex.GetBaseException().Message}");
        }
    }

    public async Task<Result<bool>> RemoveProjectMappingAsync(
        Guid repositoryId,
        Guid projectId,
        string? pathFilter,
        CancellationToken cancellationToken = default)
    {
        // The composite key includes COALESCE(path_filter, '') so the
        // null and "" cases must collapse together for the comparison.
        var existing = await _db.RepositoryProjectMappings
            .FirstOrDefaultAsync(
                m => m.RepositoryId == repositoryId
                  && m.ProjectId == projectId
                  && m.PathFilter == pathFilter,
                cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
            return Result<bool>.Success(true); // idempotent

        try
        {
            _db.RepositoryProjectMappings.Remove(existing);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result<bool>.Success(true);
        }
        catch (DbUpdateException ex)
        {
            return Result<bool>.Failure(
                $"Failed to remove repository project mapping: {ex.GetBaseException().Message}");
        }
    }
}

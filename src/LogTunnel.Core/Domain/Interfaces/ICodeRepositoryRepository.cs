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
/// Read/write access to <see cref="CodeRepository"/> rows and the
/// <see cref="RepositoryProjectMapping"/> joins. Named with the
/// <c>Code</c> prefix on the entity / interface to dodge the
/// double-noun "RepositoryRepository" trap.
/// </summary>
public interface ICodeRepositoryRepository
{
    /// <summary>Fetch a code repository by tenant and primary key.</summary>
    Task<Result<CodeRepository>> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetch a code repository by primary key without a tenant scope.
    /// Used by the webhook receivers, which know the repository id
    /// from the URL but do not yet have a JWT to derive the tenant
    /// from. Returns the row's tenant on success so the caller can
    /// stamp it onto the inserted commit / delivery rows.
    /// </summary>
    Task<Result<CodeRepository>> GetByIdAcrossTenantsAsync(
        Guid id, CancellationToken cancellationToken = default);

    /// <summary>List every code repository in the tenant, ordered by remote URL.</summary>
    Task<Result<IReadOnlyList<CodeRepository>>> ListByTenantAsync(
        Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>List every project mapping for a repository.</summary>
    Task<Result<IReadOnlyList<RepositoryProjectMapping>>> ListProjectMappingsAsync(
        Guid repositoryId, CancellationToken cancellationToken = default);

    /// <summary>Insert a new code repository.</summary>
    Task<Result<CodeRepository>> AddAsync(CodeRepository repository, CancellationToken cancellationToken = default);

    /// <summary>
    /// Map a repository into a project, optionally with a glob path
    /// filter (<c>"/frontend/**"</c>, etc.) for monorepo support.
    /// </summary>
    Task<Result<RepositoryProjectMapping>> AddProjectMappingAsync(
        Guid repositoryId,
        Guid projectId,
        string? pathFilter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove a single project mapping. The composite key is
    /// <c>(repositoryId, projectId, COALESCE(pathFilter, ''))</c>, so
    /// removing a glob-filtered mapping requires passing the same
    /// glob — passing <c>null</c> removes the unfiltered mapping.
    /// Returns success even if no row matches (idempotent).
    /// </summary>
    Task<Result<bool>> RemoveProjectMappingAsync(
        Guid repositoryId,
        Guid projectId,
        string? pathFilter,
        CancellationToken cancellationToken = default);
}

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
}

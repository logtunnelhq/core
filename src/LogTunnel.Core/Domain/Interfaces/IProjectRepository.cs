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
/// Read/write access to <see cref="Project"/> rows.
/// <see cref="ProjectMember"/> joins are managed via
/// <see cref="IUserRepository.AddToProjectAsync"/>.
/// </summary>
public interface IProjectRepository
{
    /// <summary>Fetch a project by tenant and primary key.</summary>
    Task<Result<Project>> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);

    /// <summary>List every project in the tenant, ordered by name.</summary>
    Task<Result<IReadOnlyList<Project>>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>List the members of a project, joined against the user table.</summary>
    Task<Result<IReadOnlyList<User>>> ListMembersAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>Insert a new project.</summary>
    Task<Result<Project>> AddAsync(Project project, CancellationToken cancellationToken = default);
}

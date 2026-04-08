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
/// Read/write access to <see cref="Tenant"/> rows. Tenancy boundary —
/// no other repository operates above the tenant level.
/// </summary>
public interface ITenantRepository
{
    /// <summary>Fetch a tenant by primary key.</summary>
    Task<Result<Tenant>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Fetch a tenant by its url-safe slug.</summary>
    Task<Result<Tenant>> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>List every active tenant. Used by the daily-log freeze worker to walk timezones.</summary>
    Task<Result<IReadOnlyList<Tenant>>> ListActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>Insert a new tenant. Returns the persisted entity (with server-generated columns populated).</summary>
    Task<Result<Tenant>> AddAsync(Tenant tenant, CancellationToken cancellationToken = default);
}

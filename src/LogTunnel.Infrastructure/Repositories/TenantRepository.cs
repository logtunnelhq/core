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

internal sealed class TenantRepository : ITenantRepository
{
    private readonly LogTunnelDbContext _db;

    public TenantRepository(LogTunnelDbContext db) => _db = db;

    public async Task<Result<Tenant>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == id, cancellationToken).ConfigureAwait(false);
        return tenant is null
            ? Result<Tenant>.Failure($"Tenant {id} not found.")
            : Result<Tenant>.Success(tenant);
    }

    public async Task<Result<Tenant>> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return Result<Tenant>.Failure("Tenant slug must not be empty.");

        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Slug == slug, cancellationToken).ConfigureAwait(false);
        return tenant is null
            ? Result<Tenant>.Failure($"Tenant '{slug}' not found.")
            : Result<Tenant>.Success(tenant);
    }

    public async Task<Result<Tenant>> AddAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        if (tenant is null) return Result<Tenant>.Failure("Tenant is required.");

        try
        {
            _db.Tenants.Add(tenant);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result<Tenant>.Success(tenant);
        }
        catch (DbUpdateException ex)
        {
            return Result<Tenant>.Failure($"Failed to insert tenant: {ex.GetBaseException().Message}");
        }
    }
}

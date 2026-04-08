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

internal sealed class StandupExportRepository : IStandupExportRepository
{
    private readonly LogTunnelDbContext _db;

    public StandupExportRepository(LogTunnelDbContext db) => _db = db;

    public async Task<Result<StandupExport>> AddAsync(StandupExport export, CancellationToken cancellationToken = default)
    {
        if (export is null) return Result<StandupExport>.Failure("StandupExport is required.");

        try
        {
            export.ExportedAt = DateTimeOffset.UtcNow;
            _db.StandupExports.Add(export);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result<StandupExport>.Success(export);
        }
        catch (DbUpdateException ex)
        {
            return Result<StandupExport>.Failure($"Failed to insert standup export: {ex.GetBaseException().Message}");
        }
    }
}

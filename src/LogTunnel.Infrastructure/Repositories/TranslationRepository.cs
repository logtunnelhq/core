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

internal sealed class TranslationRepository : ITranslationRepository
{
    private readonly LogTunnelDbContext _db;

    public TranslationRepository(LogTunnelDbContext db) => _db = db;

    public async Task<Result<Translation?>> GetAsync(
        Guid tenantId,
        string scopeKind,
        Guid scopeId,
        DateOnly dateFrom,
        DateOnly dateTo,
        string audience,
        CancellationToken cancellationToken = default)
    {
        var translation = await _db.Translations
            .FirstOrDefaultAsync(
                t => t.TenantId == tenantId
                  && t.ScopeKind == scopeKind
                  && t.ScopeId == scopeId
                  && t.DateFrom == dateFrom
                  && t.DateTo == dateTo
                  && t.Audience == audience,
                cancellationToken)
            .ConfigureAwait(false);
        return Result<Translation?>.Success(translation);
    }

    public async Task<Result<Translation>> AddAsync(Translation translation, CancellationToken cancellationToken = default)
    {
        if (translation is null) return Result<Translation>.Failure("Translation is required.");

        try
        {
            translation.GeneratedAt = DateTimeOffset.UtcNow;
            _db.Translations.Add(translation);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result<Translation>.Success(translation);
        }
        catch (DbUpdateException ex)
        {
            return Result<Translation>.Failure($"Failed to insert translation: {ex.GetBaseException().Message}");
        }
    }
}

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
/// Read/write access to persisted <see cref="Translation"/> rows.
/// Lookups are by the <c>(tenant, scope, date_range, audience)</c>
/// natural key.
/// </summary>
public interface ITranslationRepository
{
    /// <summary>
    /// Fetch the persisted translation for a given scope and date range,
    /// if any. Returns <c>null</c> when nothing has been generated yet.
    /// </summary>
    Task<Result<Translation?>> GetAsync(
        Guid tenantId,
        string scopeKind,
        Guid scopeId,
        DateOnly dateFrom,
        DateOnly dateTo,
        string audience,
        CancellationToken cancellationToken = default);

    /// <summary>Insert a freshly-rendered translation row.</summary>
    Task<Result<Translation>> AddAsync(Translation translation, CancellationToken cancellationToken = default);
}

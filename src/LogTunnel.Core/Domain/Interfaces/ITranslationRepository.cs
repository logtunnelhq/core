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

    /// <summary>
    /// Atomically pick up the oldest <c>status='pending'</c> row across
    /// all tenants. Used by the translation worker to pull work off
    /// the queue. Implementations should use a row-level lock or
    /// equivalent so two worker instances don't double-process the
    /// same row. Returns <c>null</c> when nothing is pending.
    /// </summary>
    Task<Result<Translation?>> LeaseNextPendingAsync(CancellationToken cancellationToken = default);

    /// <summary>Mark a translation as ready and store the rendered content.</summary>
    Task<Result<Translation>> MarkReadyAsync(
        Guid translationId,
        string content,
        CancellationToken cancellationToken = default);

    /// <summary>Mark a translation as failed with a free-text reason.</summary>
    Task<Result<Translation>> MarkFailedAsync(
        Guid translationId,
        string failureReason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create or reset a translation row for the supplied composite
    /// key. If a row already exists and is <c>"ready"</c> or
    /// <c>"failed"</c>, flips it back to <c>"pending"</c> so the
    /// worker re-renders. If already <c>"pending"</c> or
    /// <c>"rendering"</c>, leaves it alone (idempotent). Returns the
    /// upserted row.
    /// </summary>
    Task<Result<Translation>> UpsertPendingAsync(
        Guid tenantId,
        string scopeKind,
        Guid scopeId,
        DateOnly dateFrom,
        DateOnly dateTo,
        string audience,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// List every translation for a scope + date range, across all
    /// audiences. Returns up to 4 rows (one per audience) ordered by
    /// audience name.
    /// </summary>
    Task<Result<IReadOnlyList<Translation>>> ListByScopeAsync(
        Guid tenantId,
        string scopeKind,
        Guid scopeId,
        DateOnly dateFrom,
        DateOnly dateTo,
        CancellationToken cancellationToken = default);
}

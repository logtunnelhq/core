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
/// Read/write access to <see cref="PublicTranslation"/> rows and the
/// append-only <see cref="PublicTranslationEvent"/> trail. Models the
/// marketing edit / approve / publish workflow.
/// </summary>
/// <summary>
/// Result row for
/// <see cref="IPublicTranslationRepository.ListPublishedByTenantAsync"/>.
/// Carries the marketing-edited content (or the original LLM output
/// when marketing left it untouched), the date range the translation
/// covers, and the publish timestamp for ordering.
/// </summary>
public sealed record PublishedPublicTranslation(
    Guid PublicTranslationId,
    Guid TranslationId,
    string Content,
    DateOnly DateFrom,
    DateOnly DateTo,
    DateTimeOffset PublishedAt,
    string? PublicSlug);

public interface IPublicTranslationRepository
{
    /// <summary>Fetch a public translation by tenant and primary key.</summary>
    Task<Result<PublicTranslation>> GetByIdAsync(
        Guid tenantId,
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// List public translations for a tenant, optionally filtered by
    /// <paramref name="workflowStatus"/>. Pass <c>null</c> to return
    /// every status.
    /// </summary>
    Task<Result<IReadOnlyList<PublicTranslation>>> ListByTenantAsync(
        Guid tenantId,
        string? workflowStatus,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// List every published public translation for a tenant alongside
    /// the immutable original content from the underlying
    /// <see cref="Translation"/>. Used by the unauthenticated public
    /// changelog page; ordered by <c>published_at</c> descending.
    /// </summary>
    Task<Result<IReadOnlyList<PublishedPublicTranslation>>> ListPublishedByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Open a draft public translation on top of an existing
    /// <see cref="Translation"/> whose audience is <c>"Public"</c>. The
    /// new row starts with <c>workflow_status = 'draft'</c>.
    /// </summary>
    Task<Result<PublicTranslation>> AddAsync(PublicTranslation publicTranslation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update the marketing-edited content. Only permitted when the
    /// row is in <c>'draft'</c> — once approved or published the
    /// content is frozen.
    /// </summary>
    Task<Result<PublicTranslation>> UpdateContentAsync(
        Guid publicTranslationId,
        string editedContent,
        Guid actorId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Move a public translation through the workflow:
    /// <c>draft → approved → published</c>. Each transition appends a
    /// <see cref="PublicTranslationEvent"/> for audit.
    /// </summary>
    Task<Result<PublicTranslation>> TransitionStatusAsync(
        Guid publicTranslationId,
        string newStatus,
        Guid actorId,
        string? notes,
        CancellationToken cancellationToken = default);
}

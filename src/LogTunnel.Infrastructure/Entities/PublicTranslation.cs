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

namespace LogTunnel.Infrastructure.Entities;

/// <summary>
/// EF Core entity for the <c>public_translations</c> table — marketing's
/// edit / approve / publish workflow on top of a Public-audience
/// <see cref="Translation"/>. The original LLM output stays in
/// <see cref="Translation.Content"/> (immutable); marketing's edits live
/// in <see cref="EditedContent"/>.
/// </summary>
public sealed class PublicTranslation
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    /// <summary>One-to-one with the underlying <see cref="Translation"/>. Always points at a row whose <see cref="Translation.Audience"/> = <c>"Public"</c>.</summary>
    public Guid TranslationId { get; set; }

    /// <summary>Marketing's edited copy. Null until they first open the draft.</summary>
    public string? EditedContent { get; set; }

    /// <summary><c>"draft"</c>, <c>"approved"</c>, or <c>"published"</c>.</summary>
    public string WorkflowStatus { get; set; } = "draft";

    public Guid? ApprovedBy { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }

    /// <summary>URL path on the public changelog page. Set at publish time.</summary>
    public string? PublicSlug { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

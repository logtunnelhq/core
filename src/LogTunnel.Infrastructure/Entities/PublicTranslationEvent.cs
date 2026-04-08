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
/// EF Core entity for the <c>public_translation_events</c> append-only
/// audit trail. One row per workflow transition or edit on a
/// <see cref="PublicTranslation"/>.
/// </summary>
public sealed class PublicTranslationEvent
{
    public Guid Id { get; set; }
    public Guid PublicTranslationId { get; set; }

    /// <summary><c>"edited"</c>, <c>"approved"</c>, <c>"rejected"</c>, <c>"published"</c>, or <c>"unpublished"</c>.</summary>
    public string EventType { get; set; } = string.Empty;

    public Guid ActorId { get; set; }

    /// <summary>Optional one-line note from the actor.</summary>
    public string? Notes { get; set; }

    public DateTimeOffset OccurredAt { get; set; }
}

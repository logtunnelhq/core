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
/// EF Core entity for the <c>translations</c> table — the persisted
/// output of <c>ChangelogTranslatorService</c>. One row per
/// <c>(scope, date_range, audience)</c>; the background worker re-renders
/// when <see cref="InputsHash"/> drifts from the actual upstream commits
/// and daily logs.
/// </summary>
public sealed class Translation
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    /// <summary><c>"user"</c>, <c>"team"</c>, <c>"project"</c>, or <c>"tenant"</c>.</summary>
    public string ScopeKind { get; set; } = string.Empty;

    /// <summary>Polymorphic foreign key — points into the table named by <see cref="ScopeKind"/>.</summary>
    public Guid ScopeId { get; set; }

    public DateOnly DateFrom { get; set; }

    /// <summary>Inclusive. For daily standups <see cref="DateFrom"/> = <see cref="DateTo"/>.</summary>
    public DateOnly DateTo { get; set; }

    /// <summary><c>"TechLead"</c>, <c>"Manager"</c>, <c>"CEO"</c>, or <c>"Public"</c>.</summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>The LLM's original markdown output. Immutable once written.</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>SHA-256 of <c>(commit_shas ∪ daily_log_ids ∪ company_context_version)</c>. Drives re-render detection.</summary>
    public string InputsHash { get; set; } = string.Empty;

    public DateTimeOffset GeneratedAt { get; set; }

    /// <summary>User who triggered the render. Null for system-triggered renders.</summary>
    public Guid? GeneratedBy { get; set; }

    /// <summary><c>"pending"</c>, <c>"ready"</c>, or <c>"failed"</c>.</summary>
    public string Status { get; set; } = "pending";

    public string? FailureReason { get; set; }
}

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

namespace LogTunnel.Core.Domain.Entities;

/// <summary>
/// EF Core entity for the <c>standup_exports</c> table — analytics +
/// audit trail of "team lead clicked Export". One row per export action.
/// </summary>
public sealed class StandupExport
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public Guid ExportedBy { get; set; }

    public Guid? TeamId { get; set; }
    public Guid? ProjectId { get; set; }

    public DateOnly LogDate { get; set; }

    /// <summary><c>"slack"</c>, <c>"teams"</c>, <c>"copy"</c>, or <c>"email"</c>.</summary>
    public string Channel { get; set; } = string.Empty;

    /// <summary>
    /// Raw JSONB blob containing the rendered block returned to the
    /// client. Stored as a string here; the EF configuration in step 4
    /// maps this property to the <c>payload jsonb</c> column.
    /// </summary>
    public string PayloadJson { get; set; } = "{}";

    public DateTimeOffset ExportedAt { get; set; }
}

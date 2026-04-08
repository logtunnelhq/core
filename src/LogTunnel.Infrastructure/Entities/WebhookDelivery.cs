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
/// EF Core entity for the <c>webhook_deliveries</c> table — debugging +
/// idempotency for GitHub retries. The
/// <c>(RepositoryId, DeliveryId)</c> unique index dedupes retries.
/// </summary>
public sealed class WebhookDelivery
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid RepositoryId { get; set; }

    public string EventType { get; set; } = string.Empty;

    /// <summary>GitHub's <c>X-GitHub-Delivery</c> header. Used for dedupe.</summary>
    public string DeliveryId { get; set; } = string.Empty;

    public bool SignatureValid { get; set; }

    /// <summary>
    /// Raw JSONB blob containing the GitHub webhook payload. Stored as a
    /// string here; the EF configuration in step 4 maps this property to
    /// the <c>payload jsonb</c> column.
    /// </summary>
    public string PayloadJson { get; set; } = "{}";

    public DateTimeOffset? ProcessedAt { get; set; }

    /// <summary><c>"ok"</c>, <c>"duplicate"</c>, <c>"invalid_signature"</c>, or <c>"error"</c>.</summary>
    public string? Outcome { get; set; }

    public DateTimeOffset ReceivedAt { get; set; }
}

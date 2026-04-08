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
/// Read/write access to <see cref="WebhookDelivery"/> rows. The
/// <see cref="ExistsByDeliveryIdAsync"/> check is the idempotency
/// guard for GitHub's at-least-once retry semantics.
/// </summary>
public interface IWebhookDeliveryRepository
{
    /// <summary>
    /// Returns <c>true</c> when a webhook with the same
    /// <see cref="WebhookDelivery.RepositoryId"/> +
    /// <see cref="WebhookDelivery.DeliveryId"/> has already been
    /// processed.
    /// </summary>
    Task<Result<bool>> ExistsByDeliveryIdAsync(
        Guid repositoryId,
        string deliveryId,
        CancellationToken cancellationToken = default);

    /// <summary>Insert a new webhook delivery record.</summary>
    Task<Result<WebhookDelivery>> AddAsync(WebhookDelivery delivery, CancellationToken cancellationToken = default);
}

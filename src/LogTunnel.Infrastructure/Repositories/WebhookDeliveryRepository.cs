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

internal sealed class WebhookDeliveryRepository : IWebhookDeliveryRepository
{
    private readonly LogTunnelDbContext _db;

    public WebhookDeliveryRepository(LogTunnelDbContext db) => _db = db;

    public async Task<Result<bool>> ExistsByDeliveryIdAsync(
        Guid repositoryId,
        string deliveryId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deliveryId))
            return Result<bool>.Failure("DeliveryId must not be empty.");

        var exists = await _db.WebhookDeliveries
            .AnyAsync(w => w.RepositoryId == repositoryId && w.DeliveryId == deliveryId, cancellationToken)
            .ConfigureAwait(false);
        return Result<bool>.Success(exists);
    }

    public async Task<Result<WebhookDelivery>> AddAsync(WebhookDelivery delivery, CancellationToken cancellationToken = default)
    {
        if (delivery is null) return Result<WebhookDelivery>.Failure("WebhookDelivery is required.");

        try
        {
            delivery.ReceivedAt = DateTimeOffset.UtcNow;
            _db.WebhookDeliveries.Add(delivery);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result<WebhookDelivery>.Success(delivery);
        }
        catch (DbUpdateException ex)
        {
            return Result<WebhookDelivery>.Failure($"Failed to insert webhook delivery: {ex.GetBaseException().Message}");
        }
    }
}

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

using LogTunnel.Core.Domain.Entities;
using LogTunnel.Infrastructure.Repositories;

namespace LogTunnel.Infrastructure.Tests.Repositories;

[Collection("postgres")]
public class WebhookDeliveryRepositoryTests
{
    private readonly PostgresFixture _fixture;

    public WebhookDeliveryRepositoryTests(PostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Add_then_ExistsByDeliveryId_round_trip()
    {
        await using var db = _fixture.CreateDbContext();
        var tenants = new TenantRepository(db);
        var repos = new CodeRepositoryRepository(db);
        var deliveries = new WebhookDeliveryRepository(db);

        var tenant = TestSeed.NewTenant();
        await tenants.AddAsync(tenant);
        var repository = TestSeed.NewRepository(tenant.Id);
        await repos.AddAsync(repository);

        var deliveryId = "ghd-" + Guid.NewGuid().ToString("N");

        // Before insert: not present.
        var beforeExists = await deliveries.ExistsByDeliveryIdAsync(repository.Id, deliveryId);
        Assert.True(beforeExists.IsSuccess);
        Assert.False(beforeExists.Value);

        var delivery = new WebhookDelivery
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            RepositoryId = repository.Id,
            EventType = "push",
            DeliveryId = deliveryId,
            SignatureValid = true,
            PayloadJson = """{"ref":"refs/heads/main"}""",
            Outcome = "ok",
        };
        var added = await deliveries.AddAsync(delivery);
        Assert.True(added.IsSuccess, added.IsFailure ? added.Error : null);

        // After insert: present, idempotency-check returns true.
        var afterExists = await deliveries.ExistsByDeliveryIdAsync(repository.Id, deliveryId);
        Assert.True(afterExists.IsSuccess);
        Assert.True(afterExists.Value);
    }
}

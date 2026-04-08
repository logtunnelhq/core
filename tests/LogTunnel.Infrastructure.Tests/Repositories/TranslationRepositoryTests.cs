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
public class TranslationRepositoryTests
{
    private readonly PostgresFixture _fixture;

    public TranslationRepositoryTests(PostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Add_then_Get_round_trip_for_user_scope()
    {
        await using var db = _fixture.CreateDbContext();
        var tenants = new TenantRepository(db);
        var users = new UserRepository(db);
        var translations = new TranslationRepository(db);

        var tenant = TestSeed.NewTenant();
        await tenants.AddAsync(tenant);
        var user = TestSeed.NewUser(tenant.Id);
        await users.AddAsync(user);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var translation = new Translation
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            ScopeKind = "user",
            ScopeId = user.Id,
            DateFrom = today,
            DateTo = today,
            Audience = "TechLead",
            Content = "- Did the thing",
            InputsHash = "hash-1",
            Status = "ready",
        };

        var added = await translations.AddAsync(translation);
        Assert.True(added.IsSuccess, added.IsFailure ? added.Error : null);

        var fetched = await translations.GetAsync(
            tenant.Id, "user", user.Id, today, today, "TechLead");
        Assert.True(fetched.IsSuccess);
        Assert.NotNull(fetched.Value);
        Assert.Equal("- Did the thing", fetched.Value!.Content);
        Assert.Equal("ready", fetched.Value.Status);
    }
}

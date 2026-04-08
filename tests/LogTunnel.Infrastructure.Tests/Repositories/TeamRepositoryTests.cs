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

using LogTunnel.Infrastructure.Repositories;

namespace LogTunnel.Infrastructure.Tests.Repositories;

[Collection("postgres")]
public class TeamRepositoryTests
{
    private readonly PostgresFixture _fixture;

    public TeamRepositoryTests(PostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Add_then_GetById_round_trip()
    {
        await using var db = _fixture.CreateDbContext();
        var tenants = new TenantRepository(db);
        var teams = new TeamRepository(db);

        var tenant = TestSeed.NewTenant();
        await tenants.AddAsync(tenant);

        var team = TestSeed.NewTeam(tenant.Id);
        var added = await teams.AddAsync(team);
        Assert.True(added.IsSuccess, added.IsFailure ? added.Error : null);

        var fetched = await teams.GetByIdAsync(tenant.Id, team.Id);
        Assert.True(fetched.IsSuccess, fetched.IsFailure ? fetched.Error : null);
        Assert.Equal(team.Slug, fetched.Value.Slug);
    }
}

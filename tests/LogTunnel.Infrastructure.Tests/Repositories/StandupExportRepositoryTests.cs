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
using Microsoft.EntityFrameworkCore;

namespace LogTunnel.Infrastructure.Tests.Repositories;

[Collection("postgres")]
public class StandupExportRepositoryTests
{
    private readonly PostgresFixture _fixture;

    public StandupExportRepositoryTests(PostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Add_round_trip()
    {
        await using var db = _fixture.CreateDbContext();
        var tenants = new TenantRepository(db);
        var users = new UserRepository(db);
        var teams = new TeamRepository(db);
        var exports = new StandupExportRepository(db);

        var tenant = TestSeed.NewTenant();
        await tenants.AddAsync(tenant);
        var lead = TestSeed.NewUser(tenant.Id, role: "team_lead");
        await users.AddAsync(lead);
        var team = TestSeed.NewTeam(tenant.Id);
        await teams.AddAsync(team);

        var export = new StandupExport
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            ExportedBy = lead.Id,
            TeamId = team.Id,
            ProjectId = null,
            LogDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Channel = "slack",
            PayloadJson = """{"blocks":[]}""",
        };

        var added = await exports.AddAsync(export);
        Assert.True(added.IsSuccess, added.IsFailure ? added.Error : null);

        await using var verifyDb = _fixture.CreateDbContext();
        var fetched = await verifyDb.StandupExports.FirstOrDefaultAsync(e => e.Id == export.Id);
        Assert.NotNull(fetched);
        Assert.Equal("slack", fetched!.Channel);
        Assert.Equal(team.Id, fetched.TeamId);
    }
}

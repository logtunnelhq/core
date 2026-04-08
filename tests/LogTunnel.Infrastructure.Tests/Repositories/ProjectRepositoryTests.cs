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
public class ProjectRepositoryTests
{
    private readonly PostgresFixture _fixture;

    public ProjectRepositoryTests(PostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Add_then_GetById_round_trip()
    {
        await using var db = _fixture.CreateDbContext();
        var tenants = new TenantRepository(db);
        var projects = new ProjectRepository(db);

        var tenant = TestSeed.NewTenant();
        await tenants.AddAsync(tenant);

        var project = TestSeed.NewProject(tenant.Id);
        var added = await projects.AddAsync(project);
        Assert.True(added.IsSuccess, added.IsFailure ? added.Error : null);

        var fetched = await projects.GetByIdAsync(tenant.Id, project.Id);
        Assert.True(fetched.IsSuccess, fetched.IsFailure ? fetched.Error : null);
        Assert.Equal(project.Slug, fetched.Value.Slug);
    }
}

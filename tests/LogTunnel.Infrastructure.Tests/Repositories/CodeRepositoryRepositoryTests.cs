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
public class CodeRepositoryRepositoryTests
{
    private readonly PostgresFixture _fixture;

    public CodeRepositoryRepositoryTests(PostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Add_GetById_AddProjectMapping_round_trip()
    {
        await using var db = _fixture.CreateDbContext();
        var tenants = new TenantRepository(db);
        var projects = new ProjectRepository(db);
        var repos = new CodeRepositoryRepository(db);

        var tenant = TestSeed.NewTenant();
        await tenants.AddAsync(tenant);
        var project = TestSeed.NewProject(tenant.Id);
        await projects.AddAsync(project);

        var repository = TestSeed.NewRepository(tenant.Id);
        var added = await repos.AddAsync(repository);
        Assert.True(added.IsSuccess, added.IsFailure ? added.Error : null);

        var fetched = await repos.GetByIdAsync(tenant.Id, repository.Id);
        Assert.True(fetched.IsSuccess, fetched.IsFailure ? fetched.Error : null);
        Assert.Equal(repository.RemoteUrl, fetched.Value.RemoteUrl);

        var mapped = await repos.AddProjectMappingAsync(repository.Id, project.Id, "/frontend/**");
        Assert.True(mapped.IsSuccess, mapped.IsFailure ? mapped.Error : null);
        Assert.Equal(project.Id, mapped.Value.ProjectId);
        Assert.Equal("/frontend/**", mapped.Value.PathFilter);
    }
}

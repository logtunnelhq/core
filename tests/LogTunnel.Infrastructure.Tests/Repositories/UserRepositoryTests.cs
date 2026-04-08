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
public class UserRepositoryTests
{
    private readonly PostgresFixture _fixture;

    public UserRepositoryTests(PostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Add_then_FindByEmail_and_FindByGitEmail_round_trip()
    {
        await using var db = _fixture.CreateDbContext();
        var tenants = new TenantRepository(db);
        var users = new UserRepository(db);

        var tenant = TestSeed.NewTenant();
        await tenants.AddAsync(tenant);

        var user = TestSeed.NewUser(tenant.Id);
        var added = await users.AddAsync(user);
        Assert.True(added.IsSuccess, added.IsFailure ? added.Error : null);

        var byEmail = await users.FindByEmailAsync(tenant.Id, user.Email);
        Assert.True(byEmail.IsSuccess, byEmail.IsFailure ? byEmail.Error : null);
        Assert.Equal(user.Id, byEmail.Value.Id);

        var byGitEmail = await users.FindByGitEmailAsync(tenant.Id, user.GitEmail!);
        Assert.True(byGitEmail.IsSuccess, byGitEmail.IsFailure ? byGitEmail.Error : null);
        Assert.Equal(user.Id, byGitEmail.Value.Id);
    }

    [Fact]
    public async Task AddToTeam_and_AddToProject_round_trip()
    {
        await using var db = _fixture.CreateDbContext();
        var tenants = new TenantRepository(db);
        var users = new UserRepository(db);
        var teams = new TeamRepository(db);
        var projects = new ProjectRepository(db);

        var tenant = TestSeed.NewTenant();
        await tenants.AddAsync(tenant);
        var user = TestSeed.NewUser(tenant.Id);
        await users.AddAsync(user);
        var team = TestSeed.NewTeam(tenant.Id);
        await teams.AddAsync(team);
        var project = TestSeed.NewProject(tenant.Id);
        await projects.AddAsync(project);

        var addedToTeam = await users.AddToTeamAsync(team.Id, user.Id, "lead");
        Assert.True(addedToTeam.IsSuccess, addedToTeam.IsFailure ? addedToTeam.Error : null);

        var addedToProject = await users.AddToProjectAsync(project.Id, user.Id);
        Assert.True(addedToProject.IsSuccess, addedToProject.IsFailure ? addedToProject.Error : null);
    }
}

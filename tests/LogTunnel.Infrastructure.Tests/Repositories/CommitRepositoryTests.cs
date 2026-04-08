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
public class CommitRepositoryTests
{
    private readonly PostgresFixture _fixture;

    public CommitRepositoryTests(PostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Add_with_path_filter_mapping_writes_commit_projects()
    {
        await using var db = _fixture.CreateDbContext();
        var tenants = new TenantRepository(db);
        var projects = new ProjectRepository(db);
        var repos = new CodeRepositoryRepository(db);
        var commits = new CommitRepository(db);

        var tenant = TestSeed.NewTenant();
        await tenants.AddAsync(tenant);

        var frontend = TestSeed.NewProject(tenant.Id);
        var api = TestSeed.NewProject(tenant.Id);
        await projects.AddAsync(frontend);
        await projects.AddAsync(api);

        var repository = TestSeed.NewRepository(tenant.Id);
        await repos.AddAsync(repository);

        // Two filters: /frontend/** → frontend project, /api/** → api project.
        await repos.AddProjectMappingAsync(repository.Id, frontend.Id, "/frontend/**");
        await repos.AddProjectMappingAsync(repository.Id, api.Id, "/api/**");

        var commit = new Commit
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            RepositoryId = repository.Id,
            Sha = "deadbeef" + Guid.NewGuid().ToString("N")[..8],
            Message = "feat: touch frontend only",
            AuthorEmail = "alice@example.com",
            AuthoredAt = DateTimeOffset.UtcNow,
            ChangedFiles = new[] { "/frontend/src/App.tsx", "/frontend/README.md" },
        };

        var added = await commits.AddAsync(commit);
        Assert.True(added.IsSuccess, added.IsFailure ? added.Error : null);

        // commit_projects should contain only the frontend mapping.
        await using var verifyDb = _fixture.CreateDbContext();
        var mappings = await verifyDb.CommitProjects
            .Where(cp => cp.CommitId == commit.Id)
            .ToListAsync();

        Assert.Single(mappings);
        Assert.Equal(frontend.Id, mappings[0].ProjectId);
    }

    [Fact]
    public async Task Add_is_idempotent_on_repository_id_plus_sha()
    {
        await using var db = _fixture.CreateDbContext();
        var tenants = new TenantRepository(db);
        var repos = new CodeRepositoryRepository(db);
        var commits = new CommitRepository(db);

        var tenant = TestSeed.NewTenant();
        await tenants.AddAsync(tenant);
        var repository = TestSeed.NewRepository(tenant.Id);
        await repos.AddAsync(repository);

        var sha = "abc123" + Guid.NewGuid().ToString("N")[..8];
        var commit1 = new Commit
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            RepositoryId = repository.Id,
            Sha = sha,
            Message = "first ingest",
            AuthorEmail = "x@example.com",
            AuthoredAt = DateTimeOffset.UtcNow,
            ChangedFiles = Array.Empty<string>(),
        };
        var first = await commits.AddAsync(commit1);
        Assert.True(first.IsSuccess);

        // Re-ingest the same (repo, sha) — should return the existing row,
        // not raise a unique-constraint violation.
        var commit2 = new Commit
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            RepositoryId = repository.Id,
            Sha = sha,
            Message = "second ingest",
            AuthorEmail = "x@example.com",
            AuthoredAt = DateTimeOffset.UtcNow,
            ChangedFiles = Array.Empty<string>(),
        };
        var second = await commits.AddAsync(commit2);
        Assert.True(second.IsSuccess, second.IsFailure ? second.Error : null);
        Assert.Equal(commit1.Id, second.Value.Id);
    }
}

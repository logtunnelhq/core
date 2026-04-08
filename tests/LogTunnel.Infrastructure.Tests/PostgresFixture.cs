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

using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace LogTunnel.Infrastructure.Tests;

/// <summary>
/// xUnit collection fixture that owns a single Postgres container for
/// the whole assembly. Brings the container up once on startup, applies
/// the EF Core migration, and hands out fresh
/// <see cref="LogTunnelDbContext"/> instances per test. Tests don't
/// share data — each test creates its own tenant fixture and asserts
/// on what it created — so no rollback or per-test cleanup is needed.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("logtunnel_tests")
        .WithUsername("logtunnel")
        .WithPassword("logtunnel")
        .Build();

    /// <summary>Connection string for the running container.</summary>
    public string ConnectionString => _container.GetConnectionString();

    /// <summary>
    /// Build a fresh <see cref="LogTunnelDbContext"/> against the
    /// running container. Each call returns a new instance — caller is
    /// responsible for disposing.
    /// </summary>
    public LogTunnelDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<LogTunnelDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new LogTunnelDbContext(options);
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        await _container.StartAsync().ConfigureAwait(false);
        await using var db = CreateDbContext();
        // Apply the EF Core migration once for the whole test session.
        await db.Database.MigrateAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

/// <summary>
/// xUnit collection definition that pins every test class with
/// <c>[Collection("postgres")]</c> to the single shared
/// <see cref="PostgresFixture"/>. The collection makes xUnit run those
/// classes serially against the same container, which is what we want
/// — one Postgres start, many tests.
/// </summary>
[CollectionDefinition("postgres")]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>;

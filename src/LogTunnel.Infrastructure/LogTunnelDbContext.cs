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
using Microsoft.EntityFrameworkCore;

namespace LogTunnel.Infrastructure;

/// <summary>
/// EF Core <see cref="DbContext"/> for the LogTunnel Phase 2 Postgres
/// schema. One <see cref="DbSet{TEntity}"/> per table; per-entity Fluent
/// configuration lives in <c>Configurations/</c> and is picked up via
/// <see cref="ModelBuilder.ApplyConfigurationsFromAssembly"/> so
/// <see cref="OnModelCreating"/> stays short.
/// </summary>
public sealed class LogTunnelDbContext : DbContext
{
    /// <summary>Create a new <see cref="LogTunnelDbContext"/>.</summary>
    /// <param name="options">Options supplied by DI; the connection string is bound here.</param>
    public LogTunnelDbContext(DbContextOptions<LogTunnelDbContext> options)
        : base(options)
    {
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<CodeRepository> Repositories => Set<CodeRepository>();
    public DbSet<RepositoryProjectMapping> RepositoryProjectMappings => Set<RepositoryProjectMapping>();
    public DbSet<Commit> Commits => Set<Commit>();
    public DbSet<CommitProject> CommitProjects => Set<CommitProject>();
    public DbSet<DailyLog> DailyLogs => Set<DailyLog>();
    public DbSet<DailyLogRevision> DailyLogRevisions => Set<DailyLogRevision>();
    public DbSet<Translation> Translations => Set<Translation>();
    public DbSet<PublicTranslation> PublicTranslations => Set<PublicTranslation>();
    public DbSet<PublicTranslationEvent> PublicTranslationEvents => Set<PublicTranslationEvent>();
    public DbSet<StandupExport> StandupExports => Set<StandupExport>();
    public DbSet<WebhookDelivery> WebhookDeliveries => Set<WebhookDelivery>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // pgcrypto provides gen_random_uuid() on older Postgres builds; on
        // 13+ the function is in core but the extension is harmless.
        modelBuilder.HasPostgresExtension("pgcrypto");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LogTunnelDbContext).Assembly);
    }
}

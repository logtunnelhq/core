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

using LogTunnel.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogTunnel.Infrastructure.Configurations;

internal sealed class RepositoryProjectMappingConfiguration : IEntityTypeConfiguration<RepositoryProjectMapping>
{
    public void Configure(EntityTypeBuilder<RepositoryProjectMapping> builder)
    {
        builder.ToTable("repository_project_mappings");

        // Synthetic surrogate Id instead of the SQL composite key with
        // COALESCE(path_filter, '') because EF Core 8 doesn't model
        // expression-based primary keys. Two filtered unique indexes
        // below replicate the original semantics: at most one row per
        // (repo, project, path_filter) when path_filter is set, plus at
        // most one row per (repo, project) when path_filter is null.
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(x => x.RepositoryId).HasColumnName("repository_id");
        builder.Property(x => x.ProjectId).HasColumnName("project_id");
        builder.Property(x => x.PathFilter).HasColumnName("path_filter");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");

        builder.HasOne<CodeRepository>()
            .WithMany()
            .HasForeignKey(x => x.RepositoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Project>()
            .WithMany()
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.RepositoryId, x.ProjectId, x.PathFilter })
            .IsUnique()
            .HasFilter("path_filter IS NOT NULL")
            .HasDatabaseName("ux_repository_project_mappings_filtered");

        builder.HasIndex(x => new { x.RepositoryId, x.ProjectId })
            .IsUnique()
            .HasFilter("path_filter IS NULL")
            .HasDatabaseName("ux_repository_project_mappings_null_filter");
    }
}

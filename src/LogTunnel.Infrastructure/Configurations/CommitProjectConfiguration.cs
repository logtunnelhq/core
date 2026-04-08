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
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogTunnel.Infrastructure.Configurations;

internal sealed class CommitProjectConfiguration : IEntityTypeConfiguration<CommitProject>
{
    public void Configure(EntityTypeBuilder<CommitProject> builder)
    {
        builder.ToTable("commit_projects");

        builder.HasKey(x => new { x.CommitId, x.ProjectId });

        builder.Property(x => x.CommitId).HasColumnName("commit_id");
        builder.Property(x => x.ProjectId).HasColumnName("project_id");

        builder.HasOne<Commit>()
            .WithMany()
            .HasForeignKey(x => x.CommitId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Project>()
            .WithMany()
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.ProjectId).HasDatabaseName("commit_projects_project_idx");
    }
}

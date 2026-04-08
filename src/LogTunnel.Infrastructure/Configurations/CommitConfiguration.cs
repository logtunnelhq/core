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

internal sealed class CommitConfiguration : IEntityTypeConfiguration<Commit>
{
    public void Configure(EntityTypeBuilder<Commit> builder)
    {
        builder.ToTable("commits");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(x => x.TenantId).HasColumnName("tenant_id");
        builder.Property(x => x.RepositoryId).HasColumnName("repository_id");
        builder.Property(x => x.Sha).HasColumnName("sha");
        builder.Property(x => x.ParentShas).HasColumnName("parent_shas").HasColumnType("text[]").HasDefaultValueSql("'{}'");
        builder.Property(x => x.Message).HasColumnName("message");
        builder.Property(x => x.AuthorEmail).HasColumnName("author_email");
        builder.Property(x => x.AuthorUserId).HasColumnName("author_user_id");
        builder.Property(x => x.AuthoredAt).HasColumnName("authored_at");
        builder.Property(x => x.ChangedFiles).HasColumnName("changed_files").HasColumnType("text[]").HasDefaultValueSql("'{}'");
        builder.Property(x => x.IngestedAt).HasColumnName("ingested_at").HasDefaultValueSql("now()");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<CodeRepository>()
            .WithMany()
            .HasForeignKey(x => x.RepositoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.AuthorUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.RepositoryId, x.Sha }).IsUnique();

        builder.HasIndex(x => new { x.TenantId, x.AuthoredAt })
            .HasDatabaseName("commits_tenant_authored_idx")
            .IsDescending(false, true);

        builder.HasIndex(x => new { x.AuthorUserId, x.AuthoredAt })
            .HasDatabaseName("commits_author_authored_idx")
            .IsDescending(false, true)
            .HasFilter("author_user_id IS NOT NULL");

        builder.HasIndex(x => x.ChangedFiles)
            .HasDatabaseName("commits_changed_files_gin")
            .HasMethod("gin");
    }
}

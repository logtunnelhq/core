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

internal sealed class DailyLogConfiguration : IEntityTypeConfiguration<DailyLog>
{
    public void Configure(EntityTypeBuilder<DailyLog> builder)
    {
        builder.ToTable("daily_logs", t =>
        {
            t.HasCheckConstraint("ck_daily_logs_blocker_status",
                "blocker_status IS NULL OR blocker_status IN ('need_help','waiting','unclear')");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(x => x.TenantId).HasColumnName("tenant_id");
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.LogDate).HasColumnName("log_date");
        builder.Property(x => x.RawNote).HasColumnName("raw_note");
        builder.Property(x => x.ProjectTags).HasColumnName("project_tags").HasColumnType("uuid[]").HasDefaultValueSql("'{}'");
        builder.Property(x => x.BlockerStatus).HasColumnName("blocker_status");
        builder.Property(x => x.BlockerNote).HasColumnName("blocker_note");
        builder.Property(x => x.FrozenAt).HasColumnName("frozen_at");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.TenantId, x.UserId, x.LogDate }).IsUnique();

        builder.HasIndex(x => new { x.TenantId, x.LogDate })
            .HasDatabaseName("daily_logs_tenant_date_idx")
            .IsDescending(false, true);

        builder.HasIndex(x => new { x.TenantId, x.LogDate })
            .HasDatabaseName("daily_logs_blockers_idx")
            .HasFilter("blocker_status IS NOT NULL");

        builder.HasIndex(x => x.ProjectTags)
            .HasDatabaseName("daily_logs_project_tags_gin")
            .HasMethod("gin");
    }
}

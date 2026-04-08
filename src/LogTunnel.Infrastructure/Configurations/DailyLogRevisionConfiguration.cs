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

internal sealed class DailyLogRevisionConfiguration : IEntityTypeConfiguration<DailyLogRevision>
{
    public void Configure(EntityTypeBuilder<DailyLogRevision> builder)
    {
        builder.ToTable("daily_log_revisions", t =>
        {
            t.HasCheckConstraint("ck_daily_log_revisions_blocker_status",
                "blocker_status IS NULL OR blocker_status IN ('need_help','waiting','unclear')");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(x => x.DailyLogId).HasColumnName("daily_log_id");
        builder.Property(x => x.RawNote).HasColumnName("raw_note");
        builder.Property(x => x.ProjectTags).HasColumnName("project_tags").HasColumnType("uuid[]").HasDefaultValueSql("'{}'");
        builder.Property(x => x.BlockerStatus).HasColumnName("blocker_status");
        builder.Property(x => x.BlockerNote).HasColumnName("blocker_note");
        builder.Property(x => x.EditReason).HasColumnName("edit_reason");
        builder.Property(x => x.EditedBy).HasColumnName("edited_by");
        builder.Property(x => x.EditedAt).HasColumnName("edited_at").HasDefaultValueSql("now()");

        builder.HasOne<DailyLog>()
            .WithMany()
            .HasForeignKey(x => x.DailyLogId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.EditedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.DailyLogId, x.EditedAt })
            .HasDatabaseName("daily_log_revisions_log_idx")
            .IsDescending(false, true);
    }
}

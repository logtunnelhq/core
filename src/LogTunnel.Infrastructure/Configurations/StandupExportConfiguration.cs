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

internal sealed class StandupExportConfiguration : IEntityTypeConfiguration<StandupExport>
{
    public void Configure(EntityTypeBuilder<StandupExport> builder)
    {
        builder.ToTable("standup_exports", t =>
        {
            t.HasCheckConstraint("ck_standup_exports_channel",
                "channel IN ('slack','teams','copy','email')");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(x => x.TenantId).HasColumnName("tenant_id");
        builder.Property(x => x.ExportedBy).HasColumnName("exported_by");
        builder.Property(x => x.TeamId).HasColumnName("team_id");
        builder.Property(x => x.ProjectId).HasColumnName("project_id");
        builder.Property(x => x.LogDate).HasColumnName("log_date");
        builder.Property(x => x.Channel).HasColumnName("channel");
        builder.Property(x => x.PayloadJson).HasColumnName("payload").HasColumnType("jsonb");
        builder.Property(x => x.ExportedAt).HasColumnName("exported_at").HasDefaultValueSql("now()");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.ExportedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Team>()
            .WithMany()
            .HasForeignKey(x => x.TeamId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<Project>()
            .WithMany()
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.TenantId, x.LogDate })
            .HasDatabaseName("standup_exports_tenant_date_idx")
            .IsDescending(false, true);
    }
}

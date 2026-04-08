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

internal sealed class PublicTranslationConfiguration : IEntityTypeConfiguration<PublicTranslation>
{
    public void Configure(EntityTypeBuilder<PublicTranslation> builder)
    {
        builder.ToTable("public_translations", t =>
        {
            t.HasCheckConstraint("ck_public_translations_workflow_status",
                "workflow_status IN ('draft','approved','published')");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(x => x.TenantId).HasColumnName("tenant_id");
        builder.Property(x => x.TranslationId).HasColumnName("translation_id");
        builder.Property(x => x.EditedContent).HasColumnName("edited_content");
        builder.Property(x => x.WorkflowStatus).HasColumnName("workflow_status").HasDefaultValue("draft");
        builder.Property(x => x.ApprovedBy).HasColumnName("approved_by");
        builder.Property(x => x.ApprovedAt).HasColumnName("approved_at");
        builder.Property(x => x.PublishedAt).HasColumnName("published_at");
        builder.Property(x => x.PublicSlug).HasColumnName("public_slug");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Translation>()
            .WithMany()
            .HasForeignKey(x => x.TranslationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.ApprovedBy)
            .OnDelete(DeleteBehavior.SetNull);

        // 1-to-1: at most one public_translations row per translation.
        builder.HasIndex(x => x.TranslationId).IsUnique();

        builder.HasIndex(x => new { x.TenantId, x.WorkflowStatus })
            .HasDatabaseName("public_translations_status_idx");

        builder.HasIndex(x => new { x.TenantId, x.PublicSlug })
            .IsUnique()
            .HasFilter("public_slug IS NOT NULL")
            .HasDatabaseName("public_translations_slug_idx");
    }
}

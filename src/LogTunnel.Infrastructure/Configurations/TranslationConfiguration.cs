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

internal sealed class TranslationConfiguration : IEntityTypeConfiguration<Translation>
{
    public void Configure(EntityTypeBuilder<Translation> builder)
    {
        builder.ToTable("translations", t =>
        {
            t.HasCheckConstraint("ck_translations_scope_kind",
                "scope_kind IN ('user','team','project','tenant')");
            t.HasCheckConstraint("ck_translations_audience",
                "audience IN ('TechLead','Manager','CEO','Public')");
            t.HasCheckConstraint("ck_translations_status",
                "status IN ('pending','rendering','ready','failed')");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(x => x.TenantId).HasColumnName("tenant_id");
        builder.Property(x => x.ScopeKind).HasColumnName("scope_kind");
        builder.Property(x => x.ScopeId).HasColumnName("scope_id");
        builder.Property(x => x.DateFrom).HasColumnName("date_from");
        builder.Property(x => x.DateTo).HasColumnName("date_to");
        builder.Property(x => x.Audience).HasColumnName("audience");
        builder.Property(x => x.Content).HasColumnName("content");
        builder.Property(x => x.InputsHash).HasColumnName("inputs_hash");
        builder.Property(x => x.GeneratedAt).HasColumnName("generated_at").HasDefaultValueSql("now()");
        builder.Property(x => x.GeneratedBy).HasColumnName("generated_by");
        builder.Property(x => x.Status).HasColumnName("status").HasDefaultValue("ready");
        builder.Property(x => x.FailureReason).HasColumnName("failure_reason");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.GeneratedBy)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.TenantId, x.ScopeKind, x.ScopeId, x.DateFrom, x.DateTo, x.Audience }).IsUnique();

        builder.HasIndex(x => new { x.TenantId, x.GeneratedAt })
            .HasDatabaseName("translations_tenant_generated_idx")
            .IsDescending(false, true);

        builder.HasIndex(x => new { x.TenantId, x.Status })
            .HasDatabaseName("translations_pending_idx")
            .HasFilter("status = 'pending'");
    }
}

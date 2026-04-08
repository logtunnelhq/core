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

internal sealed class CodeRepositoryConfiguration : IEntityTypeConfiguration<CodeRepository>
{
    public void Configure(EntityTypeBuilder<CodeRepository> builder)
    {
        // Entity is named CodeRepository to dodge the data-access "repository"
        // pattern collision; the actual table stays 'repositories' so the SQL
        // matches Schema/001_initial.sql verbatim.
        builder.ToTable("repositories", t =>
        {
            t.HasCheckConstraint("ck_repositories_host", "host IN ('github')");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(x => x.TenantId).HasColumnName("tenant_id");
        builder.Property(x => x.Host).HasColumnName("host");
        builder.Property(x => x.RemoteUrl).HasColumnName("remote_url");
        builder.Property(x => x.DefaultBranch).HasColumnName("default_branch").HasDefaultValue("main");
        builder.Property(x => x.WebhookSecret).HasColumnName("webhook_secret");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.TenantId, x.RemoteUrl }).IsUnique();
    }
}

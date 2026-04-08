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

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", t =>
        {
            t.HasCheckConstraint("ck_users_dashboard_role", "dashboard_role IN ('developer','team_lead','manager','ceo','marketing','admin')");
            t.HasCheckConstraint("ck_users_status", "status IN ('active','disabled')");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(x => x.TenantId).HasColumnName("tenant_id");
        builder.Property(x => x.Email).HasColumnName("email");
        builder.Property(x => x.DisplayName).HasColumnName("display_name");
        builder.Property(x => x.PasswordHash).HasColumnName("password_hash");
        builder.Property(x => x.DashboardRole).HasColumnName("dashboard_role");
        builder.Property(x => x.GitEmail).HasColumnName("git_email");
        builder.Property(x => x.Status).HasColumnName("status").HasDefaultValue("active");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.TenantId, x.Email }).IsUnique();

        // Functional index over lower(git_email) per the raw SQL — EF Core
        // 8 supports IsCreatedConcurrently / HasFilter / HasMethod /
        // HasDatabaseName but not arbitrary expression columns, so we
        // declare a regular composite index here and the migration in
        // step 5 will be hand-tweaked to match the SQL functional form.
        builder.HasIndex(x => new { x.TenantId, x.GitEmail })
            .HasDatabaseName("users_git_email_idx");
    }
}

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

internal sealed class TeamMemberConfiguration : IEntityTypeConfiguration<TeamMember>
{
    public void Configure(EntityTypeBuilder<TeamMember> builder)
    {
        builder.ToTable("team_members", t =>
        {
            t.HasCheckConstraint("ck_team_members_role", "role IN ('member','lead')");
        });

        builder.HasKey(x => new { x.TeamId, x.UserId });

        builder.Property(x => x.TeamId).HasColumnName("team_id");
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.Role).HasColumnName("role");
        builder.Property(x => x.JoinedAt).HasColumnName("joined_at").HasDefaultValueSql("now()");

        builder.HasOne<Team>()
            .WithMany()
            .HasForeignKey(x => x.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.UserId).HasDatabaseName("team_members_user_idx");
    }
}

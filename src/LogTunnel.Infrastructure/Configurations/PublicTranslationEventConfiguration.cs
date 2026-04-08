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

internal sealed class PublicTranslationEventConfiguration : IEntityTypeConfiguration<PublicTranslationEvent>
{
    public void Configure(EntityTypeBuilder<PublicTranslationEvent> builder)
    {
        builder.ToTable("public_translation_events", t =>
        {
            t.HasCheckConstraint("ck_public_translation_events_event_type",
                "event_type IN ('edited','approved','rejected','published','unpublished')");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(x => x.PublicTranslationId).HasColumnName("public_translation_id");
        builder.Property(x => x.EventType).HasColumnName("event_type");
        builder.Property(x => x.ActorId).HasColumnName("actor_id");
        builder.Property(x => x.Notes).HasColumnName("notes");
        builder.Property(x => x.OccurredAt).HasColumnName("occurred_at").HasDefaultValueSql("now()");

        builder.HasOne<PublicTranslation>()
            .WithMany()
            .HasForeignKey(x => x.PublicTranslationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.ActorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.PublicTranslationId, x.OccurredAt })
            .HasDatabaseName("public_translation_events_pt_idx")
            .IsDescending(false, true);
    }
}

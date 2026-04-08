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

internal sealed class WebhookDeliveryConfiguration : IEntityTypeConfiguration<WebhookDelivery>
{
    public void Configure(EntityTypeBuilder<WebhookDelivery> builder)
    {
        builder.ToTable("webhook_deliveries", t =>
        {
            t.HasCheckConstraint("ck_webhook_deliveries_outcome",
                "outcome IS NULL OR outcome IN ('ok','duplicate','invalid_signature','error')");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(x => x.TenantId).HasColumnName("tenant_id");
        builder.Property(x => x.RepositoryId).HasColumnName("repository_id");
        builder.Property(x => x.EventType).HasColumnName("event_type");
        builder.Property(x => x.DeliveryId).HasColumnName("delivery_id");
        builder.Property(x => x.SignatureValid).HasColumnName("signature_valid");
        builder.Property(x => x.PayloadJson).HasColumnName("payload").HasColumnType("jsonb");
        builder.Property(x => x.ProcessedAt).HasColumnName("processed_at");
        builder.Property(x => x.Outcome).HasColumnName("outcome");
        builder.Property(x => x.ReceivedAt).HasColumnName("received_at").HasDefaultValueSql("now()");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<CodeRepository>()
            .WithMany()
            .HasForeignKey(x => x.RepositoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.RepositoryId, x.DeliveryId }).IsUnique();

        builder.HasIndex(x => new { x.RepositoryId, x.ReceivedAt })
            .HasDatabaseName("webhook_deliveries_repo_received_idx")
            .IsDescending(false, true);
    }
}

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

internal sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("tenants", t =>
        {
            t.HasCheckConstraint("ck_tenants_llm_provider", "llm_provider IN ('Anthropic','OpenAI','Ollama')");
            t.HasCheckConstraint("ck_tenants_status", "status IN ('active','suspended')");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(x => x.Name).HasColumnName("name");
        builder.Property(x => x.Slug).HasColumnName("slug");
        builder.Property(x => x.Timezone).HasColumnName("timezone").HasDefaultValue("UTC");
        builder.Property(x => x.LlmProvider).HasColumnName("llm_provider");
        builder.Property(x => x.LlmModel).HasColumnName("llm_model");
        builder.Property(x => x.LlmApiKeyEnc).HasColumnName("llm_api_key_enc");
        builder.Property(x => x.LlmBaseUrl).HasColumnName("llm_base_url");
        builder.Property(x => x.CompanyContextJson).HasColumnName("company_context").HasColumnType("jsonb");
        builder.Property(x => x.Status).HasColumnName("status").HasDefaultValue("active");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

        builder.HasIndex(x => x.Slug).IsUnique();
    }
}

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

namespace LogTunnel.Infrastructure.Entities;

/// <summary>
/// EF Core entity for the <c>tenants</c> table. POCO only — table name,
/// column mappings, constraints, and the JSONB conversion for
/// <see cref="CompanyContextJson"/> are configured in step 4 via Fluent
/// API.
/// </summary>
public sealed class Tenant
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>URL-safe identifier (e.g. <c>"acme"</c>). Unique across the system.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>IANA timezone name. Drives daily-log freeze at local midnight.</summary>
    public string Timezone { get; set; } = "UTC";

    /// <summary>Mirrors <c>LogTunnel.Core.Configuration.LlmOptions.Provider</c>.</summary>
    public string LlmProvider { get; set; } = string.Empty;

    public string LlmModel { get; set; } = string.Empty;

    /// <summary>AES-encrypted API key. Nullable because Ollama doesn't need one.</summary>
    public byte[]? LlmApiKeyEnc { get; set; }

    public string? LlmBaseUrl { get; set; }

    /// <summary>
    /// Raw JSONB blob mirroring <c>LogTunnel.Core.Domain.CompanyContext</c>.
    /// Stored as a string here; the EF configuration in step 4 maps this
    /// property to the <c>company_context jsonb</c> column.
    /// </summary>
    public string CompanyContextJson { get; set; } = "{}";

    public string Status { get; set; } = "active";

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

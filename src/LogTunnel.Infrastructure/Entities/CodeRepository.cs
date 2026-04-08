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
/// EF Core entity for the <c>repositories</c> table. Named
/// <c>CodeRepository</c> rather than <c>Repository</c> to avoid colliding
/// with the data-access "repository" pattern naming used elsewhere in
/// the project — same reason
/// <c>ICodeRepositoryRepository</c> carries the <c>Code</c> prefix.
/// Step 4 configures the table name override.
/// </summary>
public sealed class CodeRepository
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    /// <summary>Git host. Currently only <c>"github"</c>.</summary>
    public string Host { get; set; } = "github";

    /// <summary>Canonical https url, e.g. <c>"https://github.com/acme/web"</c>.</summary>
    public string RemoteUrl { get; set; } = string.Empty;

    public string DefaultBranch { get; set; } = "main";

    /// <summary>Per-repo HMAC secret used to verify incoming GitHub webhook signatures.</summary>
    public string WebhookSecret { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

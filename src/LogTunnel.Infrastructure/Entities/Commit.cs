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
/// EF Core entity for the <c>commits</c> table. Append-only — no
/// <c>UpdatedAt</c>; commits are written once at webhook ingest time and
/// never mutated.
/// </summary>
public sealed class Commit
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid RepositoryId { get; set; }

    public string Sha { get; set; } = string.Empty;
    public string[] ParentShas { get; set; } = Array.Empty<string>();

    /// <summary>Subject + body, joined with two newlines.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Raw author email from git.</summary>
    public string AuthorEmail { get; set; } = string.Empty;

    /// <summary>Resolved <see cref="User"/> id, populated by matching <see cref="User.GitEmail"/>. Nullable for unmatched / external authors.</summary>
    public Guid? AuthorUserId { get; set; }

    public DateTimeOffset AuthoredAt { get; set; }

    /// <summary>List of file paths touched by the commit, used by the path-filter mapper.</summary>
    public string[] ChangedFiles { get; set; } = Array.Empty<string>();

    public DateTimeOffset IngestedAt { get; set; }
}

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
/// EF Core entity for the <c>repository_project_mappings</c> join table.
/// Many-to-many: one repo can map to multiple projects with optional
/// <see cref="PathFilter"/> globs (monorepo support — commits touching
/// <c>/frontend/**</c> map to the Frontend project, <c>/api/**</c> to the
/// API project, etc.).
/// </summary>
/// <remarks>
/// The composite key in Postgres is
/// <c>(repository_id, project_id, COALESCE(path_filter, ''))</c> so the
/// same <c>(repo, project)</c> pair can coexist with different path
/// filters. EF Core's primary key configuration in step 4 uses a synthetic
/// surrogate key to mirror this without modelling the COALESCE expression
/// directly.
/// </remarks>
public sealed class RepositoryProjectMapping
{
    public Guid Id { get; set; }

    public Guid RepositoryId { get; set; }
    public Guid ProjectId { get; set; }

    /// <summary>Optional glob, e.g. <c>"/frontend/**"</c>. Null means "all paths in this repo".</summary>
    public string? PathFilter { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

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

using LogTunnel.Core.Common;
using LogTunnel.Core.Domain.Entities;

namespace LogTunnel.Core.Domain.Interfaces;

/// <summary>
/// Read/write access to <see cref="Commit"/> rows plus the
/// <see cref="CommitProject"/> materialised join. <see cref="AddAsync"/>
/// is responsible for evaluating <see cref="Commit.ChangedFiles"/>
/// against the repository's <see cref="RepositoryProjectMapping"/>
/// rules and writing the matching <see cref="CommitProject"/> entries
/// in the same transaction.
/// </summary>
public interface ICommitRepository
{
    /// <summary>
    /// Insert a commit and derive its <see cref="CommitProject"/> rows.
    /// Idempotent on <c>(repository_id, sha)</c> — re-ingesting an
    /// already-known commit returns the existing entity.
    /// </summary>
    Task<Result<Commit>> AddAsync(Commit commit, CancellationToken cancellationToken = default);

    /// <summary>List commits in a repository within a date range, ordered most-recent first.</summary>
    Task<Result<IReadOnlyList<Commit>>> ListByRepositoryAsync(
        Guid repositoryId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// List commits authored by a specific user within an inclusive
    /// UTC range, ordered most-recent first. Used by the daily-log
    /// "what did I commit yesterday?" view.
    /// </summary>
    Task<Result<IReadOnlyList<Commit>>> ListByAuthorAsync(
        Guid tenantId,
        Guid authorUserId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default);
}

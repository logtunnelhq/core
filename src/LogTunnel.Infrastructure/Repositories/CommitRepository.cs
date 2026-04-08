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
using LogTunnel.Core.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LogTunnel.Infrastructure.Repositories;

internal sealed class CommitRepository : ICommitRepository
{
    private readonly LogTunnelDbContext _db;

    public CommitRepository(LogTunnelDbContext db) => _db = db;

    public async Task<Result<Commit>> AddAsync(Commit commit, CancellationToken cancellationToken = default)
    {
        if (commit is null) return Result<Commit>.Failure("Commit is required.");

        try
        {
            // Idempotency: re-ingesting an already-known commit (same
            // repository_id + sha) returns the existing row rather than
            // erroring on the unique constraint. GitHub retries webhooks
            // freely so this matters in practice.
            var existing = await _db.Commits
                .FirstOrDefaultAsync(
                    c => c.RepositoryId == commit.RepositoryId && c.Sha == commit.Sha,
                    cancellationToken)
                .ConfigureAwait(false);
            if (existing is not null)
                return Result<Commit>.Success(existing);

            commit.IngestedAt = DateTimeOffset.UtcNow;
            _db.Commits.Add(commit);

            // Walk the repository's project mappings and decide which
            // projects this commit belongs to. NULL path_filter matches
            // every commit; otherwise we treat the filter as a glob and
            // do a prefix match against changed_files (good enough for
            // the leading-glob '/frontend/**' style; richer globs land
            // when a real customer asks for them).
            var mappings = await _db.RepositoryProjectMappings
                .Where(m => m.RepositoryId == commit.RepositoryId)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var matchedProjectIds = new HashSet<Guid>();
            foreach (var mapping in mappings)
            {
                if (mapping.PathFilter is null)
                {
                    matchedProjectIds.Add(mapping.ProjectId);
                    continue;
                }

                var prefix = NormalisePathFilter(mapping.PathFilter);
                if (commit.ChangedFiles.Any(f => f.StartsWith(prefix, StringComparison.Ordinal)))
                    matchedProjectIds.Add(mapping.ProjectId);
            }

            foreach (var projectId in matchedProjectIds)
            {
                _db.CommitProjects.Add(new CommitProject
                {
                    CommitId = commit.Id,
                    ProjectId = projectId,
                });
            }

            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result<Commit>.Success(commit);
        }
        catch (DbUpdateException ex)
        {
            return Result<Commit>.Failure($"Failed to insert commit: {ex.GetBaseException().Message}");
        }
    }

    public async Task<Result<IReadOnlyList<Commit>>> ListByRepositoryAsync(
        Guid repositoryId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
    {
        var commits = await _db.Commits
            .Where(c => c.RepositoryId == repositoryId && c.AuthoredAt >= from && c.AuthoredAt <= to)
            .OrderByDescending(c => c.AuthoredAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return Result<IReadOnlyList<Commit>>.Success(commits);
    }

    private static string NormalisePathFilter(string pathFilter)
    {
        // '/frontend/**' -> '/frontend/'
        // '/api/'        -> '/api/'
        // 'frontend/**'  -> 'frontend/'
        var trimmed = pathFilter.TrimEnd('*').TrimEnd('/');
        return trimmed.Length == 0 ? string.Empty : trimmed + "/";
    }
}

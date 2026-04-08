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
/// Read/write access to <see cref="Invitation"/> rows. Backs the
/// admin-driven team-member invite flow.
/// </summary>
public interface IInvitationRepository
{
    /// <summary>Insert a freshly-issued invitation.</summary>
    Task<Result<Invitation>> AddAsync(Invitation invitation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Look up an invitation by its SHA-256 token hash. Returns
    /// <c>Result.Success(null)</c> when nothing matches.
    /// </summary>
    Task<Result<Invitation?>> FindByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark an invitation as accepted, recording the user that was
    /// created. The store enforces single-use semantics — calling this
    /// twice on the same invitation fails.
    /// </summary>
    Task<Result<Invitation>> MarkAcceptedAsync(
        Guid invitationId,
        Guid acceptedUserId,
        CancellationToken cancellationToken = default);
}

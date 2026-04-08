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

namespace LogTunnel.Core.Domain.Entities;

/// <summary>
/// EF Core entity for the <c>invitations</c> table. When an admin posts
/// to <c>/auth/invite</c> a row lands here with the requested email,
/// display name, and dashboard role plus a SHA-256 hash of a one-time
/// invitation token. The invitee (or the admin sharing the link with
/// them out-of-band) calls <c>/auth/accept-invitation</c> with the
/// raw token plus a chosen password to convert the invitation into a
/// real <see cref="User"/> row in the same tenant.
/// </summary>
public sealed class Invitation
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DashboardRole { get; set; } = string.Empty;

    /// <summary>SHA-256 hex of the invitation token. Lookup column for <c>/auth/accept-invitation</c>.</summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>Set when the invitee successfully calls <c>/auth/accept-invitation</c>. Null = still pending.</summary>
    public DateTimeOffset? AcceptedAt { get; set; }

    /// <summary>Populated alongside <see cref="AcceptedAt"/> with the id of the user that was created.</summary>
    public Guid? AcceptedUserId { get; set; }

    /// <summary>The admin who created the invitation.</summary>
    public Guid InvitedBy { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

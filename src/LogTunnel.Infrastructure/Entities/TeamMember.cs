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
/// EF Core entity for the <c>team_members</c> join table. Composite
/// primary key <c>(TeamId, UserId)</c> is configured in step 4.
/// <see cref="Role"/> = <c>"lead"</c> is what makes a user a team lead
/// for that specific team — independent of <see cref="User.DashboardRole"/>.
/// </summary>
public sealed class TeamMember
{
    public Guid TeamId { get; set; }
    public Guid UserId { get; set; }

    /// <summary><c>"member"</c> or <c>"lead"</c>.</summary>
    public string Role { get; set; } = "member";

    public DateTimeOffset JoinedAt { get; set; }
}

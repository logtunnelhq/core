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
/// EF Core entity for the <c>users</c> table. POCO only — column mappings
/// and the <c>(tenant_id, email)</c> unique index are configured in
/// step 4 via Fluent API.
/// </summary>
public sealed class User
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    /// <summary>Login email. Unique within a tenant; not globally unique.</summary>
    public string Email { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    /// <summary>ASP.NET Core Identity password hash.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// UI default role: <c>developer</c>, <c>team_lead</c>, <c>manager</c>,
    /// <c>ceo</c>, <c>marketing</c>, or <c>admin</c>. Per-team membership
    /// (<see cref="TeamMember.Role"/>) refines actual permissions.
    /// </summary>
    public string DashboardRole { get; set; } = string.Empty;

    /// <summary>
    /// The email that appears as the author in this user's commits, if it
    /// differs from the login email. Used to backfill
    /// <see cref="Commit.AuthorUserId"/>.
    /// </summary>
    public string? GitEmail { get; set; }

    public string Status { get; set; } = "active";

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

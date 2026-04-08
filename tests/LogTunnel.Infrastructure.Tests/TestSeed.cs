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

using LogTunnel.Core.Domain.Entities;

namespace LogTunnel.Infrastructure.Tests;

/// <summary>
/// Lightweight factory helpers for the round-trip tests. Every helper
/// returns an entity with all required fields filled in and unique
/// identifiers (Guid + a random suffix on the slug-style fields) so
/// concurrent tests against the shared Postgres container don't clash.
/// </summary>
internal static class TestSeed
{
    public static Tenant NewTenant(string? slug = null) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Acme Test",
        Slug = slug ?? "acme-" + Guid.NewGuid().ToString("N")[..8],
        Timezone = "UTC",
        LlmProvider = "Anthropic",
        LlmModel = "claude-sonnet-4-20250514",
        CompanyContextJson = """{"productDescription":"test","targetCustomers":"devs","terminology":"none","additionalContext":null}""",
        Status = "active",
    };

    public static User NewUser(Guid tenantId, string role = "developer") => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        Email = $"user-{Guid.NewGuid():N}@example.com",
        DisplayName = "Test User",
        PasswordHash = "x",
        DashboardRole = role,
        GitEmail = $"git-{Guid.NewGuid():N}@example.com",
        Status = "active",
    };

    public static Team NewTeam(Guid tenantId) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        Name = "Backend",
        Slug = "backend-" + Guid.NewGuid().ToString("N")[..8],
    };

    public static Project NewProject(Guid tenantId) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        Name = "Frontend App",
        Slug = "frontend-" + Guid.NewGuid().ToString("N")[..8],
        Description = "test project",
    };

    public static CodeRepository NewRepository(Guid tenantId) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        Host = "github",
        RemoteUrl = $"https://github.com/test/{Guid.NewGuid():N}",
        DefaultBranch = "main",
        WebhookSecret = "secret-" + Guid.NewGuid().ToString("N"),
    };
}

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
using LogTunnel.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LogTunnel.Infrastructure.Tests.Repositories;

[Collection("postgres")]
public class PublicTranslationRepositoryTests
{
    private readonly PostgresFixture _fixture;

    public PublicTranslationRepositoryTests(PostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Add_then_TransitionStatus_through_workflow_writes_events()
    {
        await using var db = _fixture.CreateDbContext();
        var tenants = new TenantRepository(db);
        var users = new UserRepository(db);
        var translations = new TranslationRepository(db);
        var publicRepo = new PublicTranslationRepository(db);

        var tenant = TestSeed.NewTenant();
        await tenants.AddAsync(tenant);
        var marketing = TestSeed.NewUser(tenant.Id, role: "marketing");
        await users.AddAsync(marketing);

        // Public-audience translation underneath the workflow row.
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var translation = new Translation
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            ScopeKind = "tenant",
            ScopeId = tenant.Id,
            DateFrom = today,
            DateTo = today,
            Audience = "Public",
            Content = "We shipped a thing.",
            InputsHash = "h",
            Status = "ready",
        };
        await translations.AddAsync(translation);

        var publicTranslation = new PublicTranslation
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            TranslationId = translation.Id,
            EditedContent = null,
            WorkflowStatus = "draft",
        };
        var added = await publicRepo.AddAsync(publicTranslation);
        Assert.True(added.IsSuccess, added.IsFailure ? added.Error : null);

        // draft → approved
        var approved = await publicRepo.TransitionStatusAsync(
            publicTranslation.Id, "approved", marketing.Id, "looks good");
        Assert.True(approved.IsSuccess, approved.IsFailure ? approved.Error : null);
        Assert.Equal("approved", approved.Value.WorkflowStatus);
        Assert.NotNull(approved.Value.ApprovedAt);

        // approved → published
        var published = await publicRepo.TransitionStatusAsync(
            publicTranslation.Id, "published", marketing.Id, null);
        Assert.True(published.IsSuccess, published.IsFailure ? published.Error : null);
        Assert.Equal("published", published.Value.WorkflowStatus);
        Assert.NotNull(published.Value.PublishedAt);

        // Two audit events should exist for this public translation.
        await using var verifyDb = _fixture.CreateDbContext();
        var events = await verifyDb.PublicTranslationEvents
            .Where(e => e.PublicTranslationId == publicTranslation.Id)
            .OrderBy(e => e.OccurredAt)
            .ToListAsync();
        Assert.Equal(2, events.Count);
        Assert.Equal("approved", events[0].EventType);
        Assert.Equal("published", events[1].EventType);
    }
}

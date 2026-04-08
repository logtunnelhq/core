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

namespace LogTunnel.Infrastructure.Tests.Repositories;

[Collection("postgres")]
public class DailyLogRepositoryTests
{
    private readonly PostgresFixture _fixture;

    public DailyLogRepositoryTests(PostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Add_GetForUserAndDate_round_trip_with_blocker()
    {
        await using var db = _fixture.CreateDbContext();
        var tenants = new TenantRepository(db);
        var users = new UserRepository(db);
        var dailyLogs = new DailyLogRepository(db);

        var tenant = TestSeed.NewTenant();
        await tenants.AddAsync(tenant);
        var user = TestSeed.NewUser(tenant.Id);
        await users.AddAsync(user);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var log = new DailyLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            UserId = user.Id,
            LogDate = today,
            RawNote = "Spent the morning on checkout",
            ProjectTags = Array.Empty<Guid>(),
            BlockerStatus = "need_help",
            BlockerNote = "Stripe sandbox is rejecting test cards",
        };

        var added = await dailyLogs.AddAsync(log);
        Assert.True(added.IsSuccess, added.IsFailure ? added.Error : null);

        var fetched = await dailyLogs.GetForUserAndDateAsync(tenant.Id, user.Id, today);
        Assert.True(fetched.IsSuccess);
        Assert.NotNull(fetched.Value);
        Assert.Equal("need_help", fetched.Value!.BlockerStatus);
        Assert.Equal("Spent the morning on checkout", fetched.Value.RawNote);

        var blockers = await dailyLogs.ListBlockersForTenantAndDateAsync(tenant.Id, today);
        Assert.True(blockers.IsSuccess);
        Assert.Contains(blockers.Value, l => l.Id == log.Id);
    }

    [Fact]
    public async Task Update_succeeds_pre_freeze_and_fails_post_freeze()
    {
        await using var db = _fixture.CreateDbContext();
        var tenants = new TenantRepository(db);
        var users = new UserRepository(db);
        var dailyLogs = new DailyLogRepository(db);

        var tenant = TestSeed.NewTenant();
        await tenants.AddAsync(tenant);
        var user = TestSeed.NewUser(tenant.Id);
        await users.AddAsync(user);

        var log = new DailyLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            UserId = user.Id,
            LogDate = DateOnly.FromDateTime(DateTime.UtcNow),
            RawNote = "draft",
        };
        await dailyLogs.AddAsync(log);

        // Pre-freeze: update succeeds.
        log.RawNote = "edited before midnight";
        var preFreezeUpdate = await dailyLogs.UpdateAsync(log);
        Assert.True(preFreezeUpdate.IsSuccess, preFreezeUpdate.IsFailure ? preFreezeUpdate.Error : null);
        Assert.Equal("edited before midnight", preFreezeUpdate.Value.RawNote);

        // Simulate the freeze worker by setting FrozenAt directly via a
        // separate DbContext (avoids interfering with the repository's
        // tracked entities).
        await using (var freezeDb = _fixture.CreateDbContext())
        {
            var tracked = await freezeDb.DailyLogs.FindAsync(log.Id);
            tracked!.FrozenAt = DateTimeOffset.UtcNow;
            await freezeDb.SaveChangesAsync();
        }

        // Post-freeze: update is rejected.
        await using var verifyDb = _fixture.CreateDbContext();
        var verifyRepo = new DailyLogRepository(verifyDb);
        log.RawNote = "tried to edit after midnight";
        var postFreezeUpdate = await verifyRepo.UpdateAsync(log);
        Assert.True(postFreezeUpdate.IsFailure);
        Assert.Contains("frozen", postFreezeUpdate.Error, StringComparison.OrdinalIgnoreCase);

        // Post-freeze: a revision row writes successfully.
        var revision = new DailyLogRevision
        {
            Id = Guid.NewGuid(),
            DailyLogId = log.Id,
            RawNote = "audited revision",
            EditedBy = user.Id,
            EditReason = "fixed a typo",
        };
        var revisionResult = await verifyRepo.AddRevisionAsync(revision);
        Assert.True(revisionResult.IsSuccess, revisionResult.IsFailure ? revisionResult.Error : null);
    }
}

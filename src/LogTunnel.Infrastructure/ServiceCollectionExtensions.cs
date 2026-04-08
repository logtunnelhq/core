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

using LogTunnel.Core.Domain.Interfaces;
using LogTunnel.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LogTunnel.Infrastructure;

/// <summary>
/// Composition-root extension methods for wiring
/// <see cref="LogTunnelDbContext"/> and the Phase 2 repositories into a
/// host's <see cref="IServiceCollection"/>.
/// </summary>
/// <remarks>
/// This is the one place in the codebase that requires a static class
/// — the <see cref="IServiceCollection"/> extension-method form is
/// fixed by the framework. The "no static classes" rule applies to
/// behavioural code that should be DI-resolvable; this file is
/// composition-root glue with no business logic, so it gets a pass.
/// </remarks>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register <see cref="LogTunnelDbContext"/> against the supplied
    /// Postgres <paramref name="connectionString"/> plus all eleven
    /// Phase 2 repositories. Call this once from the upcoming
    /// <c>LogTunnel.Platform</c> host.
    /// </summary>
    /// <param name="services">The application service collection.</param>
    /// <param name="connectionString">Standard Npgsql connection string.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddLogTunnelInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string must not be empty.", nameof(connectionString));

        services.AddDbContext<LogTunnelDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITeamRepository, TeamRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<ICodeRepositoryRepository, CodeRepositoryRepository>();
        services.AddScoped<ICommitRepository, CommitRepository>();
        services.AddScoped<IDailyLogRepository, DailyLogRepository>();
        services.AddScoped<ITranslationRepository, TranslationRepository>();
        services.AddScoped<IPublicTranslationRepository, PublicTranslationRepository>();
        services.AddScoped<IStandupExportRepository, StandupExportRepository>();
        services.AddScoped<IWebhookDeliveryRepository, WebhookDeliveryRepository>();

        return services;
    }
}

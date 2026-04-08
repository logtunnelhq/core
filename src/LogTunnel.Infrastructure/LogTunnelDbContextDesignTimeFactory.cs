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

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LogTunnel.Infrastructure;

/// <summary>
/// Design-time factory used by <c>dotnet ef migrations add</c> and
/// <c>dotnet ef migrations script</c>. The connection string here is a
/// placeholder — design-time tooling never actually opens a connection;
/// it only needs a configured <see cref="LogTunnelDbContext"/> to read
/// the model from. At runtime the real connection string is bound via
/// <c>AddLogTunnelInfrastructure</c> in step 7.
/// </summary>
internal sealed class LogTunnelDbContextDesignTimeFactory
    : IDesignTimeDbContextFactory<LogTunnelDbContext>
{
    public LogTunnelDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<LogTunnelDbContext>()
            .UseNpgsql("Host=localhost;Database=logtunnel_design;Username=postgres;Password=postgres")
            .Options;

        return new LogTunnelDbContext(options);
    }
}

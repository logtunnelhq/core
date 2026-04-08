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

namespace LogTunnel.Api.Configuration;

/// <summary>
/// Bound configuration for the on-disk LogTunnel configuration file written
/// by <c>POST /configure</c>.
/// </summary>
public sealed class LogTunnelConfigOptions
{
    /// <summary>Configuration section name in <c>appsettings.json</c>.</summary>
    public const string SectionName = "LogTunnel:Config";

    /// <summary>
    /// Directory to which <c>.logtunnel.json</c> is written. Defaults to the
    /// current working directory so the file appears at the root of the
    /// repository the API was launched from.
    /// </summary>
    public string OutputDirectory { get; set; } = Directory.GetCurrentDirectory();
}

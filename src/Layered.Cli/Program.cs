// Layered — Audience-specific changelog translator
// Copyright (C) 2026 Layered contributors
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

using System.CommandLine;
using Layered.Cli.Commands;
using Layered.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// The CLI shares its configuration sources (env vars, appsettings.json)
// with Layered.Api so the same LLM__PROVIDER / LLM__APIKEY work in both
// hosts, but it deliberately keeps host-time wiring minimal: heavyweight
// LLM registration happens inside the translate command's handler so
// that `layered configure` can run without an API key configured.
var hostBuilder = Host.CreateApplicationBuilder(
    new HostApplicationBuilderSettings { Args = Array.Empty<string>() });

hostBuilder.Logging.ClearProviders();
hostBuilder.Logging.AddSimpleConsole(options => options.SingleLine = true);
hostBuilder.Logging.SetMinimumLevel(LogLevel.Warning);
// The translator and Semantic Kernel both log full stack traces on
// failure; the CLI already prints a clean, single-line error message
// for the user, so silence those framework loggers to keep stderr
// readable.
hostBuilder.Logging.AddFilter("Microsoft.SemanticKernel", LogLevel.None);
hostBuilder.Logging.AddFilter("Layered.Core", LogLevel.None);

hostBuilder.Services.AddSingleton<LayeredProjectConfigStore>();
hostBuilder.Services.AddSingleton<TranslateCommand>();
hostBuilder.Services.AddSingleton<ConfigureCommand>();

using var host = hostBuilder.Build();

var rootCommand = new RootCommand(
    "Layered — translate raw Git commits into audience-specific changelogs.");

rootCommand.Add(host.Services.GetRequiredService<TranslateCommand>().Build());
rootCommand.Add(host.Services.GetRequiredService<ConfigureCommand>().Build());

return await rootCommand.Parse(args).InvokeAsync().ConfigureAwait(false);

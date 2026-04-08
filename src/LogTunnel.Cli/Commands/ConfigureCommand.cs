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

using System.CommandLine;
using LogTunnel.Core.Configuration;
using LogTunnel.Core.Domain;

namespace LogTunnel.Cli.Commands;

/// <summary>
/// <c>logtunnel configure</c> — interactively prompt for the company
/// context fields, seed a sensible default audience set, and save the
/// result as <c>.logtunnel.json</c> in the current working directory so
/// subsequent <c>logtunnel translate</c> invocations pick it up.
/// </summary>
/// <remarks>
/// Configure intentionally does not require any LLM configuration: a
/// user can run <c>logtunnel configure</c> on a fresh checkout before
/// they have an API key, and only need to set <c>LLM__*</c> later when
/// they want to actually translate.
/// </remarks>
public sealed class ConfigureCommand
{
    private readonly LogTunnelProjectConfigStore _configStore;

    /// <summary>Create a new configure command.</summary>
    public ConfigureCommand(LogTunnelProjectConfigStore configStore)
    {
        _configStore = configStore ?? throw new ArgumentNullException(nameof(configStore));
    }

    /// <summary>Build the System.CommandLine <see cref="Command"/>.</summary>
    public Command Build()
    {
        var command = new Command(
            "configure",
            "Interactively create a .logtunnel.json file in the current directory.");

        command.SetAction((_, cancellationToken) => RunAsync(cancellationToken));
        return command;
    }

    private async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            var directory = Directory.GetCurrentDirectory();
            var targetPath = Path.Combine(directory, LogTunnelProjectConfigStore.DefaultFileName);

            if (File.Exists(targetPath) && !ConfirmOverwrite(targetPath))
            {
                Console.WriteLine("Aborted. Existing configuration left untouched.");
                return 1;
            }

            Console.WriteLine();
            Console.WriteLine("LogTunnel project configuration");
            Console.WriteLine("=============================");
            Console.WriteLine();
            Console.WriteLine("Answer four short questions about your product. The CLI will write");
            Console.WriteLine("a .logtunnel.json file you can edit later, with sensible defaults for");
            Console.WriteLine("all four audiences (tech lead, manager, CEO, public).");
            Console.WriteLine();

            var productDescription = PromptRequired(
                "Product description (what is the product and what does it do?)");
            var targetCustomers = PromptRequired(
                "Target customers (who is it built for?)");
            var terminology = PromptRequired(
                "Terminology rules (e.g. 'say members not users')");
            var additionalContext = PromptOptional(
                "Additional context (anything else the model should know — leave blank to skip)");

            var context = new CompanyContext(
                ProductDescription: productDescription,
                TargetCustomers: targetCustomers,
                Terminology: terminology,
                AdditionalContext: string.IsNullOrWhiteSpace(additionalContext) ? null : additionalContext);

            var config = new LogTunnelProjectConfig(
                Context: context,
                Audiences: DefaultAudiences);

            var writtenPath = await _configStore
                .WriteAsync(directory, config, cancellationToken)
                .ConfigureAwait(false);

            Console.WriteLine();
            Console.WriteLine($"Wrote {writtenPath}");
            Console.WriteLine("Edit it to customise tone, format, or custom instructions per audience.");
            return 0;
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("Configuration cancelled.");
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Configuration failed: {ex.Message}");
            return 1;
        }
    }

    private static bool ConfirmOverwrite(string targetPath)
    {
        Console.Write($"{targetPath} already exists. Overwrite? [y/N]: ");
        var response = Console.ReadLine()?.Trim();
        return response is not null
            && (response.Equals("y", StringComparison.OrdinalIgnoreCase)
                || response.Equals("yes", StringComparison.OrdinalIgnoreCase));
    }

    private static string PromptRequired(string label)
    {
        while (true)
        {
            Console.Write(label + ": ");
            var line = Console.ReadLine();

            if (line is null)
                throw new InvalidOperationException(
                    "Reached end of input before all required fields were filled in.");

            var trimmed = line.Trim();
            if (trimmed.Length > 0)
                return trimmed;

            Console.WriteLine("This field is required. Please enter a value.");
        }
    }

    private static string PromptOptional(string label)
    {
        Console.Write(label + ": ");
        return Console.ReadLine()?.Trim() ?? string.Empty;
    }

    private static IReadOnlyList<AudienceConfig> DefaultAudiences => new[]
    {
        new AudienceConfig(
            Type: AudienceType.TechLead,
            Tone: "Technical and precise.",
            Format: "Bullet points grouped by area, with PR or commit references retained.",
            CustomInstructions: null),
        new AudienceConfig(
            Type: AudienceType.Manager,
            Tone: "Clear and business-aware. Avoid jargon.",
            Format: "Short bullets describing what shipped, who is affected, and any risks.",
            CustomInstructions: null),
        new AudienceConfig(
            Type: AudienceType.CEO,
            Tone: "Plain English, outcome-focused.",
            Format: "At most three bullet points. No technical detail.",
            CustomInstructions: null),
        new AudienceConfig(
            Type: AudienceType.Public,
            Tone: "Positive, customer-facing, friendly.",
            Format: "Two or three short paragraphs. Features only — never mention bug fixes.",
            CustomInstructions: null),
    };
}

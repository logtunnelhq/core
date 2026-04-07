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
using Layered.Core.Configuration;
using Layered.Core.Domain;
using Layered.Core.Domain.Interfaces;
using Layered.Core.Llm;
using Layered.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Layered.Cli.Commands;

/// <summary>
/// <c>layered translate</c> — load <c>.layered.json</c>, build a
/// <see cref="TranslationRequest"/>, run
/// <see cref="IChangelogTranslator"/> in-process, and either pretty-print
/// each audience to stdout or write one markdown file per audience to
/// <c>--output</c>.
/// </summary>
/// <remarks>
/// The translate command is the only place in the CLI that touches the
/// LLM connector. It builds its own service provider on demand using
/// <see cref="LlmConnectorRegistrar"/> so that <c>layered configure</c>
/// can run without an API key configured at all.
/// </remarks>
public sealed class TranslateCommand
{
    private readonly IConfiguration _configuration;
    private readonly ILoggerFactory _loggerFactory;
    private readonly LayeredProjectConfigStore _configStore;

    /// <summary>Create a new translate command.</summary>
    public TranslateCommand(
        IConfiguration configuration,
        ILoggerFactory loggerFactory,
        LayeredProjectConfigStore configStore)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _configStore = configStore ?? throw new ArgumentNullException(nameof(configStore));
    }

    /// <summary>Build the System.CommandLine <see cref="Command"/> tree.</summary>
    public Command Build()
    {
        var inputOption = new Option<FileInfo>("--input", "-i")
        {
            Description = "Path to a file containing raw Git commit messages.",
            Required = true,
        };

        var configOption = new Option<FileInfo>("--config", "-c")
        {
            Description = $"Path to {LayeredProjectConfigStore.DefaultFileName}. Defaults to the current working directory.",
            DefaultValueFactory = _ => new FileInfo(
                Path.Combine(Directory.GetCurrentDirectory(), LayeredProjectConfigStore.DefaultFileName)),
        };

        var audienceOption = new Option<AudienceType?>("--audience", "-a")
        {
            Description = $"Optional filter — render only one audience. One of: {string.Join(", ", Enum.GetNames<AudienceType>())}.",
            CustomParser = result =>
            {
                var token = result.Tokens.SingleOrDefault();
                if (token is null)
                    return null;

                if (Enum.TryParse<AudienceType>(token.Value, ignoreCase: true, out var parsed) &&
                    Enum.IsDefined(parsed))
                {
                    return parsed;
                }

                result.AddError(
                    $"Unknown audience '{token.Value}'. Expected one of: {string.Join(", ", Enum.GetNames<AudienceType>())}.");
                return null;
            },
        };

        var outputOption = new Option<DirectoryInfo?>("--output", "-o")
        {
            Description = "Optional directory to write per-audience .md files into. When omitted, output is printed to stdout.",
        };

        var command = new Command(
            "translate",
            "Translate raw Git commits into one changelog per audience.")
        {
            inputOption,
            configOption,
            audienceOption,
            outputOption,
        };

        command.SetAction((parseResult, cancellationToken) => RunAsync(
            inputFile: parseResult.GetValue(inputOption)!,
            configFile: parseResult.GetValue(configOption)!,
            audienceFilter: parseResult.GetValue(audienceOption),
            outputDirectory: parseResult.GetValue(outputOption),
            cancellationToken: cancellationToken));

        return command;
    }

    private async Task<int> RunAsync(
        FileInfo inputFile,
        FileInfo configFile,
        AudienceType? audienceFilter,
        DirectoryInfo? outputDirectory,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!inputFile.Exists)
            {
                Console.Error.WriteLine($"Input file not found: {inputFile.FullName}");
                return 1;
            }

            var rawCommits = await File.ReadAllTextAsync(inputFile.FullName, cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(rawCommits))
            {
                Console.Error.WriteLine($"Input file is empty: {inputFile.FullName}");
                return 1;
            }

            var projectConfig = await _configStore
                .ReadAsync(configFile.FullName, cancellationToken)
                .ConfigureAwait(false);

            var audiences = ResolveAudiences(projectConfig.Audiences, audienceFilter);
            if (audiences is null)
            {
                Console.Error.WriteLine(
                    $"Audience '{audienceFilter}' is not configured in {configFile.FullName}.");
                return 1;
            }

            var request = new TranslationRequest(
                RawCommits: rawCommits,
                Context: projectConfig.Context,
                Audiences: audiences);

            await using var serviceProvider = BuildLlmServiceProvider();
            var translator = serviceProvider.GetRequiredService<IChangelogTranslator>();

            var result = await translator
                .TranslateAsync(request, cancellationToken)
                .ConfigureAwait(false);

            if (result.IsFailure)
            {
                Console.Error.WriteLine($"Translation failed: {result.Error}");
                return 1;
            }

            if (outputDirectory is not null)
            {
                await WriteOutputsToDirectoryAsync(result.Value.Outputs, outputDirectory, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                PrintOutputsToConsole(result.Value.Outputs);
            }

            return 0;
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("Translation cancelled.");
            return 1;
        }
        catch (FileNotFoundException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
        catch (InvalidDataException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Unexpected error: {ex.Message}");
            return 1;
        }
    }

    private static IReadOnlyList<AudienceConfig>? ResolveAudiences(
        IReadOnlyList<AudienceConfig> configured,
        AudienceType? filter)
    {
        if (filter is null)
            return configured;

        var filtered = configured.Where(a => a.Type == filter.Value).ToList();
        return filtered.Count == 0 ? null : filtered;
    }

    private ServiceProvider BuildLlmServiceProvider()
    {
        var llmOptions = _configuration
            .GetSection(LlmOptions.SectionName)
            .Get<LlmOptions>() ?? new LlmOptions();

        var services = new ServiceCollection();

        // Forward the host's logger factory so the inner service provider
        // emits log lines through the same console sink that the rest of
        // the CLI uses.
        services.AddSingleton(_loggerFactory);
        services.AddLogging();

        new LlmConnectorRegistrar().Register(services, llmOptions);
        services.AddKernel();

        services.AddSingleton<IPromptTemplateProvider>(serviceProvider =>
            new FileSystemPromptTemplateProvider(
                serviceProvider.GetRequiredService<ILogger<FileSystemPromptTemplateProvider>>()));

        services.AddSingleton<IChangelogTranslator, ChangelogTranslatorService>();

        return services.BuildServiceProvider();
    }

    private static async Task WriteOutputsToDirectoryAsync(
        IReadOnlyDictionary<AudienceType, string> outputs,
        DirectoryInfo outputDirectory,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(outputDirectory.FullName);
        foreach (var (audience, text) in outputs)
        {
            var fileName = AudienceFileName(audience);
            var filePath = Path.Combine(outputDirectory.FullName, fileName);
            await File.WriteAllTextAsync(filePath, text, cancellationToken).ConfigureAwait(false);
            Console.WriteLine($"Wrote {filePath}");
        }
    }

    private static void PrintOutputsToConsole(IReadOnlyDictionary<AudienceType, string> outputs)
    {
        foreach (var (audience, text) in outputs)
        {
            var label = AudienceLabel(audience);
            var rule = new string('=', label.Length + 8);
            Console.WriteLine();
            Console.WriteLine(rule);
            Console.WriteLine($"==  {label}  ==");
            Console.WriteLine(rule);
            Console.WriteLine();
            Console.WriteLine(text);
        }
        Console.WriteLine();
    }

    private static string AudienceFileName(AudienceType audience) => audience switch
    {
        AudienceType.TechLead => "tech-lead.md",
        AudienceType.Manager => "manager.md",
        AudienceType.CEO => "ceo.md",
        AudienceType.Public => "public.md",
        _ => $"{audience.ToString().ToLowerInvariant()}.md",
    };

    private static string AudienceLabel(AudienceType audience) => audience switch
    {
        AudienceType.TechLead => "Tech Lead",
        AudienceType.Manager => "Manager",
        AudienceType.CEO => "CEO",
        AudienceType.Public => "Public",
        _ => audience.ToString(),
    };
}

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

using System.Collections.Concurrent;
using LogTunnel.Core.Domain;
using LogTunnel.Core.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LogTunnel.Core.Services;

/// <summary>
/// <see cref="IPromptTemplateProvider"/> that loads markdown templates
/// from a directory on disk. Templates are read once per audience and
/// cached in memory for the lifetime of the provider, so this should
/// typically be registered as a singleton.
/// </summary>
public sealed class FileSystemPromptTemplateProvider : IPromptTemplateProvider
{
    private const string TechLeadFileName = "tech-lead.md";
    private const string ManagerFileName = "manager.md";
    private const string CeoFileName = "ceo.md";
    private const string PublicFileName = "public.md";
    private const string DefaultPromptsSubdirectory = "prompts";

    private readonly string _rootDirectory;
    private readonly ILogger<FileSystemPromptTemplateProvider> _logger;
    private readonly ConcurrentDictionary<AudienceType, string> _cache = new();

    /// <summary>
    /// Create a provider that loads templates from
    /// <paramref name="rootDirectory"/>.
    /// </summary>
    /// <param name="rootDirectory">Directory containing the audience markdown files.</param>
    /// <param name="logger">Logger used for diagnostic messages.</param>
    public FileSystemPromptTemplateProvider(
        string rootDirectory,
        ILogger<FileSystemPromptTemplateProvider> logger)
    {
        if (string.IsNullOrWhiteSpace(rootDirectory))
            throw new ArgumentException("Root directory must not be empty.", nameof(rootDirectory));

        _rootDirectory = Path.IsPathRooted(rootDirectory)
            ? rootDirectory
            : Path.GetFullPath(rootDirectory);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Convenience constructor that defaults to
    /// <c>{AppContext.BaseDirectory}/prompts</c>, which is where the build
    /// drops the markdown files copied from <c>/docs/prompts/</c>.
    /// </summary>
    public FileSystemPromptTemplateProvider(ILogger<FileSystemPromptTemplateProvider> logger)
        : this(Path.Combine(AppContext.BaseDirectory, DefaultPromptsSubdirectory), logger)
    {
    }

    /// <inheritdoc />
    public async Task<string> GetTemplateAsync(
        AudienceType audience,
        CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(audience, out var cached))
            return cached;

        var fileName = MapAudienceToFileName(audience);
        var path = Path.Combine(_rootDirectory, fileName);

        if (!File.Exists(path))
            throw new FileNotFoundException(
                $"Prompt template for audience '{audience}' not found at '{path}'.",
                path);

        _logger.LogDebug("Loading prompt template for {Audience} from {Path}", audience, path);
        var content = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
        _cache[audience] = content;
        return content;
    }

    private static string MapAudienceToFileName(AudienceType audience) => audience switch
    {
        AudienceType.TechLead => TechLeadFileName,
        AudienceType.Manager => ManagerFileName,
        AudienceType.CEO => CeoFileName,
        AudienceType.Public => PublicFileName,
        _ => throw new ArgumentOutOfRangeException(nameof(audience), audience, "Unknown audience type."),
    };
}

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

using System.Text.Json;
using System.Text.Json.Serialization;
using Layered.Api.Configuration;
using Layered.Api.Contracts;
using Microsoft.Extensions.Options;

namespace Layered.Api.Services;

/// <summary>
/// Persists a <see cref="LayeredConfigFile"/> to disk as
/// <c>.layered.json</c>. The file lives in the directory configured via
/// <see cref="LayeredConfigOptions.OutputDirectory"/> so the CLI can pick
/// it up later from the same repository.
/// </summary>
public sealed class LayeredConfigFileWriter
{
    /// <summary>Standard file name for the persisted Layered configuration.</summary>
    public const string FileName = ".layered.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly LayeredConfigOptions _options;
    private readonly ILogger<LayeredConfigFileWriter> _logger;

    /// <summary>Create a new writer.</summary>
    /// <param name="options">Bound <see cref="LayeredConfigOptions"/> for the output directory.</param>
    /// <param name="logger">Logger used for diagnostic messages.</param>
    public LayeredConfigFileWriter(
        IOptions<LayeredConfigOptions> options,
        ILogger<LayeredConfigFileWriter> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Serialize <paramref name="file"/> to JSON and write it to
    /// <see cref="FileName"/> in the configured output directory.
    /// </summary>
    /// <param name="file">The configuration file to persist.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The absolute path of the file that was written.</returns>
    public async Task<string> WriteAsync(
        LayeredConfigFile file,
        CancellationToken cancellationToken = default)
    {
        if (file is null)
            throw new ArgumentNullException(nameof(file));

        Directory.CreateDirectory(_options.OutputDirectory);
        var fullPath = Path.Combine(_options.OutputDirectory, FileName);

        var json = JsonSerializer.Serialize(file, SerializerOptions);
        await File.WriteAllTextAsync(fullPath, json, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Wrote Layered config to {Path}", fullPath);
        return fullPath;
    }
}

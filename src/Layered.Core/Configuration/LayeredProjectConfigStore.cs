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

namespace Layered.Core.Configuration;

/// <summary>
/// Reads and writes <c>.layered.json</c> on disk. Both Layered.Api
/// (<c>POST /configure</c>) and Layered.Cli (<c>layered translate</c>
/// and <c>layered configure</c>) go through this store so the file
/// format stays consistent across hosts.
/// </summary>
public sealed class LayeredProjectConfigStore
{
    /// <summary>Standard file name for the persisted Layered configuration.</summary>
    public const string DefaultFileName = ".layered.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// Deserialize a <see cref="LayeredProjectConfig"/> from
    /// <paramref name="filePath"/>. Throws
    /// <see cref="FileNotFoundException"/> when the file does not exist
    /// and <see cref="InvalidDataException"/> when the JSON cannot be
    /// parsed.
    /// </summary>
    /// <param name="filePath">Absolute or relative path to the JSON file.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public async Task<LayeredProjectConfig> ReadAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path must not be empty.", nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException(
                $"Layered project config not found at '{filePath}'. Run 'layered configure' to create one.",
                filePath);

        await using var stream = File.OpenRead(filePath);
        try
        {
            var config = await JsonSerializer
                .DeserializeAsync<LayeredProjectConfig>(stream, SerializerOptions, cancellationToken)
                .ConfigureAwait(false);

            if (config is null)
                throw new InvalidDataException($"'{filePath}' deserialized to a null configuration.");

            return config;
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException(
                $"'{filePath}' is not a valid Layered project config: {ex.Message}",
                ex);
        }
    }

    /// <summary>
    /// Serialize <paramref name="config"/> to <see cref="DefaultFileName"/>
    /// inside <paramref name="directory"/>, creating the directory if it
    /// does not already exist.
    /// </summary>
    /// <param name="directory">Target directory.</param>
    /// <param name="config">Configuration to persist.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The absolute path of the file that was written.</returns>
    public async Task<string> WriteAsync(
        string directory,
        LayeredProjectConfig config,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(directory))
            throw new ArgumentException("Directory must not be empty.", nameof(directory));
        if (config is null)
            throw new ArgumentNullException(nameof(config));

        Directory.CreateDirectory(directory);
        var fullPath = Path.Combine(directory, DefaultFileName);

        var json = JsonSerializer.Serialize(config, SerializerOptions);
        await File.WriteAllTextAsync(fullPath, json, cancellationToken).ConfigureAwait(false);

        return fullPath;
    }
}

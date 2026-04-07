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

using Layered.Core.Domain;
using Layered.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Layered.Core.Tests.Services;

public class FileSystemPromptTemplateProviderTests : IDisposable
{
    private readonly string _tempDirectory;

    public FileSystemPromptTemplateProviderTests()
    {
        _tempDirectory = Path.Combine(
            Path.GetTempPath(),
            "layered-prompt-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
            Directory.Delete(_tempDirectory, recursive: true);
    }

    [Theory]
    [InlineData(AudienceType.TechLead, "tech-lead.md", "TECH LEAD CONTENT")]
    [InlineData(AudienceType.Manager, "manager.md", "MANAGER CONTENT")]
    [InlineData(AudienceType.CEO, "ceo.md", "CEO CONTENT")]
    [InlineData(AudienceType.Public, "public.md", "PUBLIC CONTENT")]
    public async Task GetTemplateAsync_returns_correct_template_for_each_audience(
        AudienceType audience,
        string fileName,
        string expectedContent)
    {
        await File.WriteAllTextAsync(
            Path.Combine(_tempDirectory, fileName),
            expectedContent);

        var provider = CreateProvider();

        var actual = await provider.GetTemplateAsync(audience);

        Assert.Equal(expectedContent, actual);
    }

    [Fact]
    public async Task GetTemplateAsync_caches_so_each_audience_is_only_read_from_disk_once()
    {
        var path = Path.Combine(_tempDirectory, "tech-lead.md");
        await File.WriteAllTextAsync(path, "first read");

        var provider = CreateProvider();

        // First read primes the cache from disk.
        var first = await provider.GetTemplateAsync(AudienceType.TechLead);

        // Mutate the file underneath the provider so that any subsequent
        // read would observe the new content if it actually went to disk.
        await File.WriteAllTextAsync(path, "MUTATED ON DISK");

        // Second read must come from the cache, returning the original
        // content rather than the mutated value on disk.
        var second = await provider.GetTemplateAsync(AudienceType.TechLead);

        Assert.Equal("first read", first);
        Assert.Equal("first read", second);

        // And once the file is gone entirely, a third read still succeeds
        // because the cache holds it — proving the disk is never touched
        // for the same audience after the first call.
        File.Delete(path);
        var third = await provider.GetTemplateAsync(AudienceType.TechLead);
        Assert.Equal("first read", third);
    }

    [Fact]
    public async Task GetTemplateAsync_throws_FileNotFoundException_when_template_missing()
    {
        var provider = CreateProvider();

        var ex = await Assert.ThrowsAsync<FileNotFoundException>(
            () => provider.GetTemplateAsync(AudienceType.TechLead));

        Assert.Contains("TechLead", ex.Message);
        Assert.Contains(_tempDirectory, ex.Message);
    }

    private FileSystemPromptTemplateProvider CreateProvider() =>
        new(_tempDirectory, NullLogger<FileSystemPromptTemplateProvider>.Instance);
}

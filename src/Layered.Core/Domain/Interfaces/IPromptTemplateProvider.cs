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

namespace Layered.Core.Domain.Interfaces;

/// <summary>
/// Loads the system-prompt template for a given audience. Implementations
/// may read from disk, embedded resources, a database, or memory; the
/// translator service depends only on this abstraction.
/// </summary>
public interface IPromptTemplateProvider
{
    /// <summary>
    /// Returns the prompt template text for the supplied
    /// <paramref name="audience"/>. Implementations are expected to throw
    /// <see cref="FileNotFoundException"/> when no template can be located.
    /// </summary>
    /// <param name="audience">The audience whose template to load.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task<string> GetTemplateAsync(
        AudienceType audience,
        CancellationToken cancellationToken = default);
}

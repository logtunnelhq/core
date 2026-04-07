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

using Layered.Core.Common;

namespace Layered.Core.Domain.Interfaces;

/// <summary>
/// Translates a batch of raw Git commits into one changelog entry per
/// requested audience. Implementations are expected to invoke the LLM
/// once per audience and may run those calls concurrently.
/// </summary>
public interface IChangelogTranslator
{
    /// <summary>
    /// Run a translation job. Returns a successful <see cref="Result{T}"/>
    /// containing one rendered changelog per audience, or a failure result
    /// describing why the translation could not be produced.
    /// </summary>
    /// <param name="request">The translation request to fulfil.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task<Result<ChangelogOutput>> TranslateAsync(
        TranslationRequest request,
        CancellationToken cancellationToken = default);
}

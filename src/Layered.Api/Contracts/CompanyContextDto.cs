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

namespace Layered.Api.Contracts;

/// <summary>
/// Wire-format representation of <see cref="CompanyContext"/> used by the
/// HTTP API. Kept separate from the domain record so the API surface can
/// evolve independently of the core domain model.
/// </summary>
/// <param name="ProductDescription">Short description of what the product is and does.</param>
/// <param name="TargetCustomers">Who the product is built for.</param>
/// <param name="Terminology">Naming and language conventions, e.g. "say members not users".</param>
/// <param name="AdditionalContext">Optional free-form extra context for the translator.</param>
public sealed record CompanyContextDto(
    string ProductDescription,
    string TargetCustomers,
    string Terminology,
    string? AdditionalContext)
{
    /// <summary>Map this DTO to its <see cref="CompanyContext"/> domain representation.</summary>
    public CompanyContext ToDomain() => new(
        ProductDescription: ProductDescription,
        TargetCustomers: TargetCustomers,
        Terminology: Terminology,
        AdditionalContext: AdditionalContext);
}

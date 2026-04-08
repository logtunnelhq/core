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

namespace LogTunnel.Core.Domain;

/// <summary>
/// Describes the company whose commits are being translated. Supplied per
/// translation request so the model can frame language and terminology
/// correctly for the audience.
/// </summary>
/// <param name="ProductDescription">Short description of what the product is and does.</param>
/// <param name="TargetCustomers">Who the product is built for.</param>
/// <param name="Terminology">Naming and language conventions, e.g. "say members not users".</param>
/// <param name="AdditionalContext">Optional free-form extra context for the translator.</param>
public record CompanyContext(
    string ProductDescription,
    string TargetCustomers,
    string Terminology, // e.g. "say members not users"
    string? AdditionalContext
);

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

using FluentValidation;
using LogTunnel.Api.Contracts;

namespace LogTunnel.Api.Validation;

/// <summary>
/// Validates <see cref="CompanyContextDto"/>. Required fields must be
/// present and within sensible length limits so prompt assembly cannot
/// be sabotaged by oversized inputs.
/// </summary>
public sealed class CompanyContextDtoValidator : AbstractValidator<CompanyContextDto>
{
    /// <summary>Configure validation rules for company context.</summary>
    public CompanyContextDtoValidator()
    {
        RuleFor(x => x.ProductDescription)
            .NotEmpty()
            .MaximumLength(2_000);

        RuleFor(x => x.TargetCustomers)
            .NotEmpty()
            .MaximumLength(2_000);

        RuleFor(x => x.Terminology)
            .NotEmpty()
            .MaximumLength(4_000);

        RuleFor(x => x.AdditionalContext)
            .MaximumLength(8_000);
    }
}

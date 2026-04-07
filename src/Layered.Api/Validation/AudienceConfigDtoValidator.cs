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

using FluentValidation;
using Layered.Api.Contracts;

namespace Layered.Api.Validation;

/// <summary>
/// Validates <see cref="AudienceConfigDto"/>. Ensures the audience type
/// is a known enum value and that tone/format are present and bounded.
/// </summary>
public sealed class AudienceConfigDtoValidator : AbstractValidator<AudienceConfigDto>
{
    /// <summary>Configure validation rules for an audience configuration.</summary>
    public AudienceConfigDtoValidator()
    {
        RuleFor(x => x.Type)
            .IsInEnum();

        RuleFor(x => x.Tone)
            .NotEmpty()
            .MaximumLength(500);

        RuleFor(x => x.Format)
            .NotEmpty()
            .MaximumLength(500);

        RuleFor(x => x.CustomInstructions)
            .MaximumLength(2_000);
    }
}

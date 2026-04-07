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
/// Validates the body of <c>POST /translate</c>. Enforces that raw
/// commits, company context, and at least one audience are present, and
/// rejects duplicate audience types so the translator does not produce
/// colliding outputs.
/// </summary>
public sealed class TranslateRequestValidator : AbstractValidator<TranslateRequest>
{
    private const int MaxRawCommitsLength = 100_000;
    private const int MaxAudienceCount = 16;

    /// <summary>Configure validation rules for a translate request.</summary>
    public TranslateRequestValidator(
        CompanyContextDtoValidator companyContextValidator,
        AudienceConfigDtoValidator audienceConfigValidator)
    {
        RuleFor(x => x.RawCommits)
            .NotEmpty()
            .MaximumLength(MaxRawCommitsLength);

        RuleFor(x => x.Context)
            .NotNull()
            .SetValidator(companyContextValidator!);

        RuleFor(x => x.Audiences)
            .NotNull()
            .Must(a => a is { Count: > 0 })
                .WithMessage("At least one audience configuration is required.")
            .Must(a => a is null || a.Count <= MaxAudienceCount)
                .WithMessage($"At most {MaxAudienceCount} audience configurations are allowed.")
            .Must(HaveDistinctAudienceTypes)
                .WithMessage("Audiences must not contain duplicate types.");

        RuleForEach(x => x.Audiences)
            .SetValidator(audienceConfigValidator);
    }

    private static bool HaveDistinctAudienceTypes(IReadOnlyList<AudienceConfigDto>? audiences)
    {
        if (audiences is null || audiences.Count == 0)
            return true;
        return audiences.Select(a => a.Type).Distinct().Count() == audiences.Count;
    }
}

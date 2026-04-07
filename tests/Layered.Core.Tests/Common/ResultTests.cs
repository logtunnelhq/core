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

namespace Layered.Core.Tests.Common;

public class ResultTests
{
    [Fact]
    public void Success_factory_sets_IsSuccess_true_and_exposes_value()
    {
        var result = Result<int>.Success(42);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Failure_factory_sets_IsSuccess_false_and_records_error()
    {
        var result = Result<int>.Failure("something broke");

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal("something broke", result.Error);
    }

    [Fact]
    public void Value_is_null_on_failure_for_reference_types()
    {
        var result = Result<string>.Failure("nope");

        Assert.True(result.IsFailure);
        Assert.Null(result.Value);
    }
}

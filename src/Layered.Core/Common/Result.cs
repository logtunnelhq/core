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

namespace Layered.Core.Common;

/// <summary>
/// Represents the outcome of an operation that can either succeed with a
/// value of type <typeparamref name="T"/> or fail with an error message.
/// Used for expected failures in place of exceptions, per the Layered
/// architecture rules.
/// </summary>
/// <typeparam name="T">The type of the value produced on success.</typeparam>
public sealed class Result<T>
{
    private readonly T _value;
    private readonly string _error;

    private Result(T value, string error, bool isSuccess)
    {
        _value = value;
        _error = error;
        IsSuccess = isSuccess;
    }

    /// <summary>True when the operation succeeded.</summary>
    public bool IsSuccess { get; }

    /// <summary>True when the operation failed.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// The success value. Throws <see cref="InvalidOperationException"/> if
    /// the result is a failure — callers must check <see cref="IsSuccess"/>
    /// first.
    /// </summary>
    public T Value => IsSuccess
        ? _value
        : throw new InvalidOperationException("Cannot read Value from a failed Result. Check IsSuccess first.");

    /// <summary>
    /// The error message. Throws <see cref="InvalidOperationException"/> if
    /// the result is a success — callers must check <see cref="IsFailure"/>
    /// first.
    /// </summary>
    public string Error => IsFailure
        ? _error
        : throw new InvalidOperationException("Cannot read Error from a successful Result. Check IsFailure first.");

    /// <summary>Create a successful result wrapping <paramref name="value"/>.</summary>
    public static Result<T> Success(T value) => new(value, string.Empty, isSuccess: true);

    /// <summary>Create a failed result with the supplied error message.</summary>
    public static Result<T> Failure(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
            throw new ArgumentException("Failure result requires a non-empty error message.", nameof(error));
        return new Result<T>(default!, error, isSuccess: false);
    }
}

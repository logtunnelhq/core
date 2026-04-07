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

namespace Layered.Api.Configuration;

/// <summary>
/// Bound configuration for the Anthropic API client. Populated from the
/// <c>Anthropic</c> configuration section, which is fed by environment
/// variables of the form <c>ANTHROPIC__APIKEY</c>.
/// </summary>
public sealed class AnthropicOptions
{
    /// <summary>Configuration section name in <c>appsettings.json</c>.</summary>
    public const string SectionName = "Anthropic";

    /// <summary>The default Claude model identifier used when no override is supplied.</summary>
    public const string DefaultModel = "claude-sonnet-4-20250514";

    /// <summary>API key used to authenticate calls to Anthropic.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Claude model identifier to invoke for translations.</summary>
    public string Model { get; set; } = DefaultModel;
}

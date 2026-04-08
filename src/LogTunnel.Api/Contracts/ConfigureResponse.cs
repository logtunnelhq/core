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

namespace LogTunnel.Api.Contracts;

/// <summary>
/// Response body for <c>POST /configure</c>. Returns the identifier of
/// the saved configuration plus the absolute path of the file that was
/// written so callers can locate it later.
/// </summary>
/// <param name="ConfigId">Identifier assigned to this configuration.</param>
/// <param name="Path">Absolute path of the persisted <c>.logtunnel.json</c> file.</param>
public sealed record ConfigureResponse(
    Guid ConfigId,
    string Path);

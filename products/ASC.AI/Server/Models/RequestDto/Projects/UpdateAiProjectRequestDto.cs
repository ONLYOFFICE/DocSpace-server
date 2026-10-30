// Copyright (C) Ascensio System SIA, 2009-2026
//
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied
// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For
// details, see the GNU AGPL at: https://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA by email at info@onlyoffice.com
// or by postal mail at 20A-6 Ernesta Birznieka-Upisha Street, Riga,
// LV-1050, Latvia, European Union.
//
// The interactive user interfaces in modified versions of the Program
// are required to display Appropriate Legal Notices in accordance with
// Section 5 of the GNU AGPL version 3.
//
// No trademark rights are granted under this License.
//
// All non-code elements of the Product, including illustrations,
// icon sets, and technical writing content, are licensed under the
// Creative Commons Attribution-ShareAlike 4.0 International License:
// https://creativecommons.org/licenses/by-sa/4.0/legalcode
//
// This license applies only to such non-code elements and does not
// modify or replace the licensing terms applicable to the Program's
// source code, which remains licensed under the GNU Affero General
// Public License v3.
//
// SPDX-License-Identifier: AGPL-3.0-only

namespace ASC.AI.Models.RequestDto.Projects;

/// <summary>
/// The fields of an AI project to change.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class UpdateAiProjectRequest
{
    /// <summary>
    /// New project name.
    /// </summary>
    /// <example>Marketing research</example>
    [StringLength(170)]
    public string? Title { get; set; }

    /// <summary>
    /// Background color of the project icon as six hexadecimal digits without `#`.
    /// </summary>
    /// <example>FF6600</example>
    [RegularExpression("^[0-9a-fA-F]{6}$")]
    public string? Color { get; set; }

    /// <summary>
    /// Identifier of the picture drawn on the project while it has no logo.
    /// </summary>
    /// <example>bookmark</example>
    [StringLength(50)]
    public string? Cover { get; set; }

    /// <summary>
    /// Tag names the project carries from now on.
    /// </summary>
    /// <example>["marketing","q3"]</example>
    public IEnumerable<string>? Tags { get; set; }

    /// <summary>
    /// Logo cropped from a picture the caller uploaded beforehand.
    /// </summary>
    public LogoRequest? Logo { get; set; }
}

/// <summary>
/// Request to update an AI project.
/// </summary>
public class UpdateAiProjectRequestDto
{
    /// <summary>
    /// ID of the project.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "id")]
    public required int Id { get; set; }

    /// <summary>
    /// The fields to change.
    /// </summary>
    [FromBody]
    public required UpdateAiProjectRequest UpdateProject { get; set; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Source)]
public static partial class UpdateAiProjectRequestMapper
{
    public static partial UpdateRoomRequest Map(this UpdateAiProjectRequest source);
}

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
/// Request to retrieve a filtered and paginated list of AI projects.
/// </summary>
public class GetAiProjectListRequestDto
{
    /// <summary>
    /// ID of a user or a group whose projects are listed.
    /// </summary>
    /// <example>9924256a-739c-462b-9a5b-0b1a4e42f2a1</example>
    [FromQuery(Name = "subjectId")]
    public Guid? SubjectId { get; set; }

    /// <summary>
    /// ID of the user who owns the listed projects.
    /// </summary>
    /// <example>9924256a-739c-462b-9a5b-0b1a4e42f2a1</example>
    [FromQuery(Name = "subjectOwnerId")]
    public Guid? SubjectOwnerId { get; set; }

    /// <summary>
    /// Whether to list only the projects without tags.
    /// </summary>
    /// <example>false</example>
    [FromQuery(Name = "withoutTags")]
    public bool? WithoutTags { get; set; }

    /// <summary>
    /// Tag names as a JSON array of strings.
    /// </summary>
    /// <example>["marketing","q3"]</example>
    [FromQuery(Name = "tags")]
    public string? Tags { get; set; }

    /// <summary>
    /// Whether to invert the `subjectId` filter.
    /// </summary>
    /// <example>false</example>
    [FromQuery(Name = "excludeSubject")]
    public bool? ExcludeSubject { get; set; }

    /// <summary>
    /// Maximum number of projects in the page.
    /// </summary>
    /// <example>25</example>
    [FromQuery(Name = "count")]
    [Range(1, ApiContext.MaxCount)]
    public int Count { get; set; } = ApiContext.DefaultCount;

    /// <summary>
    /// Number of projects to skip.
    /// </summary>
    /// <example>0</example>
    [FromQuery(Name = "startIndex")]
    public int StartIndex { get; set; }

    /// <summary>
    /// Field to sort by.
    /// </summary>
    /// <example>DateAndTime</example>
    [FromQuery(Name = "sortBy")]
    public string? SortBy { get; set; }

    /// <summary>
    /// Sort direction.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "sortOrder")]
    public SortOrder SortOrder { get; set; }

    /// <summary>
    /// Text the project title must contain.
    /// </summary>
    /// <example>marketing</example>
    [FromQuery(Name = "filterValue")]
    public string? Text { get; set; }
}

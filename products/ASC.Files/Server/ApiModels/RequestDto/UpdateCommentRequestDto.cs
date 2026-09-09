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

namespace ASC.Files.ApiModels.RequestDto;

/// <summary>
/// The comment to store on one version of a file.
/// </summary>
public class UpdateComment
{
    /// <summary>The longest a version comment may be — the width of the `comment` column.</summary>
    public const int MaxCommentLength = 255;

    /// <summary>
    /// The version the comment belongs to, as reported by `GET api/2.0/files/file/{fileId}/edit/history`. A version
    /// that does not exist is rejected as an invalid request.
    /// </summary>
    /// <example>1</example>
    [Range(1, int.MaxValue)]
    public required int Version { get; set; }

    /// <summary>
    /// The note that explains what changed in that version, as the version history shows it. An empty text clears the
    /// note, and a longer one is cut rather than refused, so read the stored text from the answer.
    /// </summary>
    /// <example>This is a comment</example>
    [StringLength(MaxCommentLength)]
    public string Comment { get; set; }
}

/// <summary>
/// The request that replaces the comment on one version of a file.
/// </summary>
public class UpdateCommentRequestDto<T>
{
    /// <summary>
    /// The file whose version comment is replaced.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "fileId")]
    public required T FileId { get; set; }

    /// <summary>
    /// The version and the comment to store on it.
    /// </summary>
    /// <example>{"version": 1, "comment": "Prices updated for Q3"}</example>
    [FromBody]
    public required UpdateComment File { get; set; }
}
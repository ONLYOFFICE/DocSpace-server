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

namespace ASC.Files.Core.ApiModels.ResponseDto;

/// <summary>
/// Everything an editor needs in order to show what one revision of a file changed.
/// </summary>
public class EditHistoryDataDto
{
    /// <summary>
    /// The address the editor downloads the recorded changes of this revision from. It is filled in only when the
    /// portal has a change record for the revision; without it the revision can be shown as a whole document but not
    /// as a set of changes.
    /// </summary>
    /// <example>https://example.com/changes</example>
    [Url]
    public string ChangesUrl { get; set; }

    /// <summary>
    /// The document key of the revision being shown, which the editing service uses to identify it and to reuse the
    /// copy it has cached.
    /// </summary>
    /// <example>doc1</example>
    public required string Key { get; set; }

    /// <summary>
    /// The revision this one is compared against. It arrives together with `changesUrl`, and when the revision shown
    /// is the first one the file ever had, it points at the blank template the file was created from instead of at an
    /// earlier revision.
    /// </summary>
    /// <example>{"url": "https://example.com/prev.docx", "key": "prev-doc-key"}</example>
    public EditHistoryUrlDto Previous { get; set; }

    /// <summary>
    /// The signature over the whole answer, as a JSON Web Token that the editing service verifies before it accepts
    /// the addresses in it. Empty when the portal runs without a document-service secret.
    /// </summary>
    /// <example>eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJ2ZXJzaW9uIjoxfQ.7HxQ0Zx1</example>
    public string Token { get; set; }

    /// <summary>
    /// The address the content of this revision is served from. It is meant for the editing service and carries its
    /// own key, which is valid for a limited time.
    /// </summary>
    /// <example>https://example.com/file.docx</example>
    [Url]
    public required string Url { get; set; }

    /// <summary>
    /// Echoes the revision that was asked for, so it reports 0 when the request named no version and the current
    /// revision was taken.
    /// </summary>
    /// <example>1</example>
    public required int Version { get; init; }

    /// <summary>
    /// The format of the revision being shown, as an extension without the leading dot.
    /// </summary>
    /// <example>docx</example>
    public required string FileType { get; set; }
}

/// <summary>
/// The address, document key and format of the revision a comparison is made against.
/// </summary>
public class EditHistoryUrlDto
{
    /// <summary>
    /// The document key of that revision. When the file has no earlier revision the portal generates a fresh key for
    /// the template it falls back to, so the value is not always one an earlier revision ever had.
    /// </summary>
    /// <example>doc_v2_20260101</example>
    public string Key { get; init; }

    /// <summary>
    /// The address that revision's content is served from. It is meant for the editing service and carries its own
    /// key, which is valid for a limited time.
    /// </summary>
    /// <example>https://files.example.com/history/doc_v2_20260101.docx</example>
    [Url]
    public string Url { get; init; }

    /// <summary>
    /// The format of that revision, as an extension without the leading dot.
    /// </summary>
    /// <example>docx</example>
    public string FileType { get; set; }
}

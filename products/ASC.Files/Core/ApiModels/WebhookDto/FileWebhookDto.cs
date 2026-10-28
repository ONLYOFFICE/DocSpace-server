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

namespace ASC.Files.Core.ApiModels.WebhookDto;

// A copy of FileDto. Not carried: thumbnailUrl, dimensions, viewAccessibility, formFillingStatus, hasDraft and the
// form-role fields. Each needs per-request work the REST helper does and a delivery must not; dimensions in particular
// opens the file stream to measure the image.

/// <summary>
/// The file carried by a file.* webhook, and by form.filled.out and form.stopped.
/// </summary>
/// <remarks>
/// Carries no thumbnail, image dimensions or form-filling state; read them from the REST API by the file ID.
/// </remarks>
public class FileWebhookDto<T> : FileEntryWebhookDto<T>
{
    /// <inheritdoc/>
    public override FileEntryType FileEntryType => FileEntryType.File;

    /// <summary>
    /// The number of the current version of the file, starting at 1 and growing with every saved version.
    /// </summary>
    /// <example>1</example>
    public int Version { get; set; }

    /// <summary>
    /// The number of the version group the current version belongs to; the versions saved during one editing session
    /// share a group.
    /// </summary>
    /// <example>1</example>
    public int VersionGroup { get; set; }

    /// <summary>
    /// The size of the current version of the file in bytes.
    /// </summary>
    /// <example>12345</example>
    public long ContentLength { get; set; }

    /// <summary>
    /// The kind of content the file holds, derived from its extension: a document, a spreadsheet, a presentation, a
    /// PDF, an image and so on.
    /// </summary>
    /// <example>7</example>
    public FileType FileType { get; set; }

    /// <summary>
    /// The file extension, including the leading dot.
    /// </summary>
    /// <example>.docx</example>
    public string FileExst { get; set; }

    /// <summary>
    /// The comment saved with the current version, either typed by the user or set by the portal for the action
    /// that made the version.
    /// </summary>
    /// <example>Created</example>
    public string Comment { get; set; }

    /// <summary>
    /// The URL the file content can be downloaded from. Carries no share token.
    /// </summary>
    /// <example>https://example.com/filehandler.ashx?action=download&amp;fileid=10</example>
    public string ViewUrl { get; set; }

    /// <summary>
    /// The URL the file can be opened at in a browser. Carries no share token.
    /// </summary>
    /// <example>https://example.com/doceditor?fileid=10</example>
    public string WebUrl { get; set; }

    /// <summary>
    /// True when the file is end-to-end encrypted in a private room; absent otherwise.
    /// </summary>
    /// <example>false</example>
    public bool? Encrypted { get; set; }

    /// <summary>
    /// True when the file is locked, so that only the user who locked it can edit it; absent otherwise.
    /// </summary>
    /// <example>false</example>
    public bool? Locked { get; set; }

    /// <summary>
    /// The name of the user who locked the file, when `locked` is true.
    /// </summary>
    /// <example>Mike Zanyatski</example>
    public string LockedBy { get; set; }

    /// <summary>
    /// True for every PDF file, with or without form fields, since the editor can open any PDF as a form; absent for
    /// other files. It does not tell a fillable form from an ordinary PDF document.
    /// </summary>
    /// <example>true</example>
    public bool? IsForm { get; set; }

    /// <summary>
    /// True when custom filtering is turned on for the spreadsheet; absent otherwise.
    /// </summary>
    /// <example>false</example>
    public bool? CustomFilterEnabled { get; set; }

    /// <summary>
    /// The name of the user who turned the custom filter on, when `customFilterEnabled` is true.
    /// </summary>
    /// <example>Mike Zanyatski</example>
    public string CustomFilterEnabledBy { get; set; }

    /// <summary>
    /// When the file was last opened, in UTC; absent when that is not known.
    /// </summary>
    /// <example>2021-01-01T00:00:00Z</example>
    public DateTime? LastOpened { get; set; }

    /// <summary>
    /// The state of the file's indexing for the AI features of the portal; absent when it does not apply.
    /// </summary>
    /// <example>0</example>
    public VectorizationStatus? VectorizationStatus { get; set; }
}

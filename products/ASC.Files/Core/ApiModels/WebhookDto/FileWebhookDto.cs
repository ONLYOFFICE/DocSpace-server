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

/// <summary>
/// The file carried by a file.* webhook, and by form.filled.out and form.stopped.
/// </summary>
/// <remarks>
/// A copy of FileDto. See FileEntryWebhookDto for what the webhook contract leaves out and why.
/// </remarks>
[WebhookPayload(WebhookPayloadKind.File)]
public class FileWebhookDto<T> : FileEntryWebhookDto<T>
{
    /// <inheritdoc/>
    public override FileEntryType FileEntryType => FileEntryType.File;

    /// <summary>
    /// The file version.
    /// </summary>
    /// <example>1</example>
    public int Version { get; set; }

    /// <summary>
    /// The version group of the file.
    /// </summary>
    /// <example>1</example>
    public int VersionGroup { get; set; }

    /// <summary>
    /// The file size in bytes.
    /// </summary>
    /// <example>12345</example>
    public long ContentLength { get; set; }

    /// <summary>
    /// The file type.
    /// </summary>
    /// <example>7</example>
    public FileType FileType { get; set; }

    /// <summary>
    /// The file extension, including the leading dot.
    /// </summary>
    /// <example>.docx</example>
    public string FileExst { get; set; }

    /// <summary>
    /// The comment on the current file version.
    /// </summary>
    /// <example>Created</example>
    public string Comment { get; set; }

    /// <summary>
    /// The URL the file content can be downloaded from.
    /// </summary>
    /// <example>https://example.com/filehandler.ashx?action=download&amp;fileid=10</example>
    public string ViewUrl { get; set; }

    /// <summary>
    /// The URL the file can be opened at in a browser.
    /// </summary>
    /// <example>https://example.com/doceditor?fileid=10</example>
    public string WebUrl { get; set; }

    /// <summary>
    /// Specifies whether the file is encrypted or not.
    /// </summary>
    /// <example>false</example>
    public bool? Encrypted { get; set; }

    /// <summary>
    /// Specifies whether the file is locked or not.
    /// </summary>
    /// <example>false</example>
    public bool? Locked { get; set; }

    /// <summary>
    /// The user name of whoever locked the file.
    /// </summary>
    /// <example>Mike Zanyatski</example>
    public string LockedBy { get; set; }

    /// <summary>
    /// Specifies whether the file is a form or not.
    /// </summary>
    /// <example>false</example>
    public bool? IsPdf { get; set; }

    /// <summary>
    /// Specifies whether a custom filter is enabled for the file or not.
    /// </summary>
    /// <example>false</example>
    public bool? CustomFilterEnabled { get; set; }

    /// <summary>
    /// The user name of whoever enabled the custom filter.
    /// </summary>
    /// <example>Mike Zanyatski</example>
    public string CustomFilterEnabledBy { get; set; }

    /// <summary>
    /// The UTC date when the file was last opened.
    /// </summary>
    /// <example>2021-01-01T00:00:00Z</example>
    public DateTime? LastOpened { get; set; }

    /// <summary>
    /// The AI vectorization status of the file.
    /// </summary>
    /// <example>0</example>
    public VectorizationStatus? VectorizationStatus { get; set; }
}

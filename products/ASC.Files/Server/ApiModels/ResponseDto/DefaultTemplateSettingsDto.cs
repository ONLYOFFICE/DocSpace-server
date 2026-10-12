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
/// The blank document the portal creates for each extension it covers.
/// </summary>
public class DefaultTemplateSettingsDto
{
    /// <summary>
    /// One entry per extension the portal's built-in template set covers, whether or not a custom blank has been
    /// chosen for it, so the list is never empty and its length follows the template set rather than the number of
    /// custom blanks. Entries come in the order an interface shows them: text document, spreadsheet, presentation and
    /// PDF first, everything else by extension.
    /// </summary>
    /// <example>[{"fileExtension": ".docx", "fileTitle": "Company letter.docx", "selectedFile": 123}]</example>
    public required IEnumerable<DefaultTemplateItemDto> Items { get; set; }
}

/// <summary>
/// The blank document configured for one extension.
/// </summary>
public class DefaultTemplateItemDto
{
    /// <summary>
    /// The copy stored in the portal that serves as the blank for this extension. A null means no custom blank has
    /// been chosen and new documents start from the portal's built-in one; the other fields of the entry are then
    /// empty as well.
    /// </summary>
    /// <example>123</example>
    public int? SelectedFile { get; set; }
    /// <summary>
    /// The extension the entry describes, in lower case with the leading dot. It is the value to send back when this
    /// blank is replaced or reset.
    /// </summary>
    /// <example>.docx</example>
    public required string FileExtension { get; set; }

    /// <summary>
    /// The name the custom blank was copied under, useful for showing which document was chosen. Empty while the
    /// built-in blank is in use.
    /// </summary>
    /// <example>Company letter.docx</example>
    public string FileTitle { get; set; }

    /// <summary>
    /// When the custom blank was last changed, in the time zone of the portal. Null while the built-in blank is in
    /// use.
    /// </summary>
    /// <example>2026-03-18T11:42:07</example>
    public DateTime? LastModified { get; set; }

    /// <summary>
    /// The size of the custom blank in bytes. Null while the built-in blank is in use.
    /// </summary>
    /// <example>1024</example>
    public long? FileSize { get; set; }

    /// <summary>
    /// The address the custom blank can be downloaded from, already carrying the access key of the calling account.
    /// Empty while the built-in blank is in use.
    /// </summary>
    /// <example>https://example.com/filehandler.ashx?action=download&amp;fileid=123</example>
    public string ViewUrl { get; set; }
}
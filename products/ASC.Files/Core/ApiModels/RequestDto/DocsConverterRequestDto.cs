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


namespace ASC.Files.Core.ApiModels.RequestDto;

/// <summary>
/// What to convert, where to keep the result, and the parameters the document service is handed. Everything below the
/// first two fields is the contract of the document service itself, named and shaped exactly as it names it, so that
/// the portal passes the request on rather than translating it. What the portal can read off the file - its format,
/// its name and the key of its revision - is not asked for and cannot be overridden.
/// </summary>
public class DocsConverterRequestDto
{
    /// <summary>
    /// The file to convert. It is read from the portal by the server, which is why the caller never hands over an
    /// address and the document service never has to reach back into the portal.
    /// </summary>
    /// <example>1234</example>
    public required int FileId { get; set; }

    /// <summary>
    /// Where the converted file is saved. Leaving it out saves the result in the My documents section of the caller.
    /// </summary>
    /// <example>5678</example>
    public int? FolderId { get; set; }

    /// <summary>
    /// The encoding of a csv or txt source, as a code page number: 1251 for Cyrillic, 65001 for UTF-8, and so on.
    /// Leaving it out lets the document service guess, which garbles non-latin text.
    /// </summary>
    /// <example>65001</example>
    public int? CodePage { get; set; }

    /// <summary>
    /// The character separating the values of a csv source: 0 - none, 1 - tab, 2 - semicolon, 3 - colon, 4 - comma,
    /// 5 - space.
    /// </summary>
    /// <example>4</example>
    public int? Delimiter { get; set; }

    /// <summary>
    /// The layout of forms printed as pdf documents or images.
    /// </summary>
    /// <example>{"drawPlaceHolders": true, "drawFormHighlight": false, "isPrint": true}</example>
    public DocumentLayout DocumentLayout { get; set; }

    /// <summary>
    /// How the text of a pdf, xps or oxps source is read back.
    /// </summary>
    /// <example>{"textAssociation": "plainLine"}</example>
    public DocumentRenderer DocumentRenderer { get; set; }

    /// <summary>
    /// The format the document is converted to.
    /// </summary>
    /// <example>pdf</example>
    [JsonPropertyName("outputtype")]
    public required string OutputType { get; set; }

    /// <summary>
    /// The password of the source document, for a document that is protected with one.
    /// </summary>
    /// <example>qwerty</example>
    public string Password { get; set; }

    /// <summary>
    /// The display format for currency, date and time when a spreadsheet is converted to PDF.
    /// </summary>
    /// <example>en-US</example>
    public string Region { get; set; }

    /// <summary>
    /// How a thumbnail is fitted, when the target format is an image.
    /// </summary>
    /// <example>{"aspect": 1, "first": true, "height": 100, "width": 100}</example>
    public ThumbnailData Thumbnail { get; set; }

    /// <summary>
    /// How a spreadsheet is laid out on the page, when it is converted to PDF.
    /// </summary>
    /// <example>{"ignorePrintArea": false, "orientation": "landscape", "fitToHeight": 0, "fitToWidth": 1}</example>
    public SpreadsheetLayout SpreadsheetLayout { get; set; }

    /// <summary>
    /// The PDF settings, used when the target format is PDF.
    /// </summary>
    /// <example>{"form": true}</example>
    public PdfData Pdf { get; set; }
}

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
/// <example>
/// {
///   "fileId": 1234,
///   "folderId": 5678,
///   "outputtype": "pdf",
///   "pdf": { "form": true }
/// }
/// </example>
public class ConvertFileRequestDto
{
    /// <summary>
    /// The id of the portal file to convert, as reported by a folder listing such as `GET api/2.0/files/{folderId}`.
    /// </summary>
    /// <example>1234</example>
    public required int FileId { get; set; }

    /// <summary>
    /// The id of the folder to save the converted file in, as reported by a folder listing such as
    /// `GET api/2.0/files/{folderId}`.
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
    public ConvertDocumentLayoutRequest DocumentLayout { get; set; }

    /// <summary>
    /// How the text of a pdf, xps or oxps source is read back.
    /// </summary>
    /// <example>{"textAssociation": "plainLine"}</example>
    public ConvertDocumentRendererRequest DocumentRenderer { get; set; }

    /// <summary>
    /// The extension of the format to convert to, without the dot. The formats a source can become are listed under
    /// `extsConvertible` in `GET api/2.0/files/settings`, keyed by the source extension, both with a leading dot there.
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
    /// The culture code whose currency, date and time formats a spreadsheet converted to PDF is shown in.
    /// </summary>
    /// <example>en-US</example>
    public string Region { get; set; }

    /// <summary>
    /// How a thumbnail is fitted, when the target format is an image.
    /// </summary>
    /// <example>{"aspect": 1, "first": true, "height": 100, "width": 100}</example>
    public ConvertThumbnailRequest Thumbnail { get; set; }

    /// <summary>
    /// How a spreadsheet is laid out on the page, when it is converted to PDF.
    /// </summary>
    /// <example>{"ignorePrintArea": false, "orientation": "landscape", "fitToHeight": 0, "fitToWidth": 1}</example>
    public ConvertSpreadsheetLayoutRequest SpreadsheetLayout { get; set; }

    /// <summary>
    /// The PDF settings, used when the target format is PDF.
    /// </summary>
    /// <example>{"form": true}</example>
    public ConvertPdfRequest Pdf { get; set; }
}


/// <summary>
/// The settings of a PDF result.
/// </summary>
/// <example>
/// {
///   "form": true
/// }
/// </example>
public class ConvertPdfRequest
{
    /// <summary>
    /// Whether the result is a fillable PDF form: true keeps the form fields of the source as fields to fill in,
    /// false saves a plain PDF.
    /// </summary>
    /// <example>true</example>
    public bool Form { get; set; }
}

/// <summary>
/// The layout of forms printed as pdf documents or images.
/// </summary>
/// <example>
/// {
///   "drawPlaceHolders": true,
///   "drawFormHighlight": false,
///   "isPrint": true
/// }
/// </example>
public class ConvertDocumentLayoutRequest
{
    /// <summary>
    /// Whether an empty form field shows its placeholder text in the result; false leaves it blank.
    /// </summary>
    /// <example>true</example>
    public bool? DrawPlaceHolders { get; set; }

    /// <summary>
    /// Whether form fields keep their highlight colour in the result; false draws them without it.
    /// </summary>
    /// <example>false</example>
    public bool? DrawFormHighlight { get; set; }

    /// <summary>
    /// Whether the print mode is turned on. It only applies to a docx converted into pdf: with the print mode off
    /// the highlight flag does nothing and the placeholder flag saves the forms in the pdf.
    /// </summary>
    /// <example>true</example>
    public bool? IsPrint { get; set; }
}

/// <summary>
/// How the text of a pdf, xps or oxps source is read back when converting from it.
/// </summary>
/// <example>
/// {
///   "textAssociation": "plainLine"
/// }
/// </example>
public class ConvertDocumentRendererRequest
{
    /// <summary>
    /// The rendering mode: blockChar reads the text by single characters, blockLine by separate lines, plainLine as
    /// plain text with a paragraph per line, plainParagraph as plain text with lines combined into paragraphs.
    /// </summary>
    /// <example>plainLine</example>
    public string TextAssociation { get; set; }
}

/// <summary>
/// How a thumbnail is fitted. A field left out takes the document service default.
/// </summary>
/// <example>
/// {
///   "aspect": 1,
///   "first": true,
///   "height": 100,
///   "width": 100
/// }
/// </example>
public class ConvertThumbnailRequest
{
    /// <summary>
    /// The mode to fit the image to the height and width specified:
    /// 0 - stretch file to fit height and width;
    /// 1 - keep the aspect for the image;
    /// 2 - convert the metric size of the page into pixels at 96 dpi.
    /// </summary>
    /// <example>1</example>
    public int? Aspect { get; set; }

    /// <summary>
    /// Whether a thumbnail is made of the first page only; false makes one of every page.
    /// </summary>
    /// <example>true</example>
    public bool? First { get; set; }

    /// <summary>
    /// The thumbnail height in pixels; not used with aspect 2.
    /// </summary>
    /// <example>100</example>
    public int? Height { get; set; }

    /// <summary>
    /// The thumbnail width in pixels; not used with aspect 2.
    /// </summary>
    /// <example>100</example>
    public int? Width { get; set; }
}

/// <summary>
/// How a spreadsheet converted to PDF is laid out on the page. A field left out takes the document service default.
/// </summary>
/// <example>
/// {
///   "ignorePrintArea": false,
///   "orientation": "landscape",
///   "fitToHeight": 0,
///   "fitToWidth": 1,
///   "margins": { "left": "0.7in", "right": "0.7in", "top": "0.75in", "bottom": "0.75in" },
///   "pageSize": { "width": "8.27in", "height": "11.69in" }
/// }
/// </example>
public class ConvertSpreadsheetLayoutRequest
{
    /// <summary>
    /// Whether the print area chosen in the spreadsheet is ignored; false converts only that area.
    /// </summary>
    /// <example>false</example>
    public bool? IgnorePrintArea { get; set; }

    /// <summary>
    /// The page orientation of the output PDF file: portrait or landscape.
    /// </summary>
    /// <example>landscape</example>
    public string Orientation { get; set; }

    /// <summary>
    /// The number of pages the converted area is fitted into in height; 0 does not fit it.
    /// </summary>
    /// <example>0</example>
    public int? FitToHeight { get; set; }

    /// <summary>
    /// The scale of the output PDF file, in percent.
    /// </summary>
    /// <example>100</example>
    public int? Scale { get; set; }

    /// <summary>
    /// The number of pages the converted area is fitted into in width; 0 does not fit it.
    /// </summary>
    /// <example>1</example>
    public int? FitToWidth { get; set; }

    /// <summary>
    /// Whether the row and column headings are printed in the output PDF file.
    /// </summary>
    /// <example>false</example>
    public bool? Headings { get; set; }

    /// <summary>
    /// Whether the grid lines are printed in the output PDF file.
    /// </summary>
    /// <example>false</example>
    public bool? GridLines { get; set; }

    /// <summary>
    /// The margins of the output PDF file.
    /// </summary>
    /// <example>{"left": "0.7in", "right": "0.7in", "top": "0.75in", "bottom": "0.75in"}</example>
    public ConvertSpreadsheetMarginsRequest Margins { get; set; }

    /// <summary>
    /// The page size of the output PDF file.
    /// </summary>
    /// <example>{"width": "8.27in", "height": "11.69in"}</example>
    public ConvertSpreadsheetPageSizeRequest PageSize { get; set; }
}

/// <summary>
/// The margins of the output PDF file, each a length with its unit: mm, cm or in.
/// </summary>
/// <example>
/// {
///   "left": "0.7in",
///   "right": "0.7in",
///   "top": "0.75in",
///   "bottom": "0.75in"
/// }
/// </example>
public class ConvertSpreadsheetMarginsRequest
{
    /// <summary>
    /// The left margin of the output PDF file.
    /// </summary>
    /// <example>0.7in</example>
    public string Left { get; set; }

    /// <summary>
    /// The right margin of the output PDF file.
    /// </summary>
    /// <example>0.7in</example>
    public string Right { get; set; }

    /// <summary>
    /// The top margin of the output PDF file.
    /// </summary>
    /// <example>0.75in</example>
    public string Top { get; set; }

    /// <summary>
    /// The bottom margin of the output PDF file.
    /// </summary>
    /// <example>0.75in</example>
    public string Bottom { get; set; }
}

/// <summary>
/// The page size of the output PDF file, each side a length with its unit: mm, cm or in.
/// </summary>
/// <example>
/// {
///   "width": "8.27in",
///   "height": "11.69in"
/// }
/// </example>
public class ConvertSpreadsheetPageSizeRequest
{
    /// <summary>
    /// The page height of the output PDF file.
    /// </summary>
    /// <example>11.69in</example>
    public string Height { get; set; }

    /// <summary>
    /// The page width of the output PDF file.
    /// </summary>
    /// <example>8.27in</example>
    public string Width { get; set; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Source)]
public static partial class ConvertFileRequestDtoMapper
{
    // the controller loads the file and the folder itself
    [MapperIgnoreSource(nameof(ConvertFileRequestDto.FileId))]
    [MapperIgnoreSource(nameof(ConvertFileRequestDto.FolderId))]
    // the format, the name and the key are read off the file, never taken from the request
    [MapperIgnoreTarget(nameof(ConvertFromFileBody.FileType))]
    [MapperIgnoreTarget(nameof(ConvertFromFileBody.Title))]
    [MapperIgnoreTarget(nameof(ConvertFromFileBody.Key))]
    [MapperIgnoreTarget(nameof(ConvertFromFileBody.Watermark))]
    public static partial ConvertFromFileBody MapToConvertFromFileBody(this ConvertFileRequestDto source);
}

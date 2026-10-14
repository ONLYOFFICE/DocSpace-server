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
/// The document options the editor applies when it opens the file.
/// </summary>
public class DocumentOptionsDto
{
    /// <summary>
    /// The document watermark parameters.
    /// </summary>
    [JsonPropertyName("watermark_on_draw")]
    public WatermarkOnDrawDto WatermarkOnDraw { get; init; }
}

/// <summary>
/// The watermark the editor draws over every page.
/// </summary>
public class WatermarkOnDrawDto
{
    /// <summary>
    /// Defines the watermark width measured in millimeters.
    /// </summary>
    /// <example>150</example>
    [JsonPropertyName("width")]
    public double Width { get; init; }

    /// <summary>
    /// Defines the watermark height measured in millimeters.
    /// </summary>
    /// <example>100</example>
    [JsonPropertyName("height")]
    public double Height { get; init; }

    /// <summary>
    /// Defines the watermark margins measured in millimeters.
    /// </summary>
    /// <example>[10, 10, 10, 10]</example>
    [JsonPropertyName("margins")]
    public int[] Margins { get; init; }

    /// <summary>
    /// Defines the watermark fill color.
    /// </summary>
    /// <example>#FF0000</example>
    [JsonPropertyName("fill")]
    public string Fill { get; init; }

    /// <summary>
    /// Defines the watermark rotation angle.
    /// </summary>
    /// <example>45</example>
    [JsonPropertyName("rotate")]
    public int Rotate { get; init; }

    /// <summary>
    /// Defines the watermark transparency percentage.
    /// </summary>
    /// <example>0.4</example>
    [JsonPropertyName("transparent")]
    public double Transparent { get; init; }

    /// <summary>
    /// The list of paragraphs of the watermark.
    /// </summary>
    /// <example>[ { "align": 2, "runs": [{"fill": [124, 124, 124], "text": "CONFIDENTIAL", "fontSize": 26}] } ]</example>
    [JsonPropertyName("paragraphs")]
    public List<WatermarkParagraphDto> Paragraphs { get; init; }
}

/// <summary>
/// One paragraph of the watermark text.
/// </summary>
public class WatermarkParagraphDto
{
    /// <summary>
    /// The paragraph align.
    /// </summary>
    /// <example>2</example>
    [JsonPropertyName("align")]
    public int Align { get; init; }

    /// <summary>
    /// The list of text runs from the paragraph.
    /// </summary>
    /// <example>[{"fill": [124, 124, 124], "text": "CONFIDENTIAL", "fontSize": 26}]</example>
    [JsonPropertyName("runs")]
    public List<WatermarkTextRunDto> Runs { get; init; }
}

/// <summary>
/// A run of watermark text with its own colour and size.
/// </summary>
public class WatermarkTextRunDto
{
    /// <summary>
    /// The fill color of the text run in RGB format.
    /// </summary>
    /// <example>[124, 124, 124]</example>
    [JsonPropertyName("fill")]
    public int[] Fill { get; init; }

    /// <summary>
    /// The run text.
    /// </summary>
    /// <example>CONFIDENTIAL</example>
    [JsonPropertyName("text")]
    public string Text { get; init; }

    /// <summary>
    /// The font size of the text run in points.
    /// </summary>
    /// <example>26</example>
    [JsonPropertyName("font-size")]
    public string FontSize { get; init; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class DocumentOptionsDtoMapper
{
    public static partial DocumentOptionsDto Map(this ASC.Web.Files.Services.DocumentService.Options source);
}

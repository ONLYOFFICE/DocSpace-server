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

namespace Textile;

/// <summary>
/// A utility class for global things used by the TextileFormatter.
/// </summary>
internal static class Globals
{
    #region Global Regex Patterns

    public const string HorizontalAlignPattern = @"(?:[()]*(\<(?!>)|(?<!<)\>|\<\>|=)[()]*)";
    public const string VerticalAlignPattern = @"[\-^~]";
    public const string CssClassPattern = @"(?:\([^)]+\))";
    public const string LanguagePattern = @"(?:\[[^]]+\])";
    public const string CssStylePattern = @"(?:\{[^}]+\})";
    public const string ColumnSpanPattern = @"(?:\\\d+)";
    public const string RowSpanPattern = @"(?:/\d+)";

    public const string AlignPattern = "(?<align>" + HorizontalAlignPattern + "?" + VerticalAlignPattern + "?|" + VerticalAlignPattern + "?" + HorizontalAlignPattern + "?)";
    public const string SpanPattern = "(?<span>" + ColumnSpanPattern + "?" + RowSpanPattern + "?|" + RowSpanPattern + "?" + ColumnSpanPattern + "?)";
    public const string BlockModifiersPattern = "(?<atts>" + CssClassPattern + "?" + CssStylePattern + "?" + LanguagePattern + "?|" +
                                                    CssStylePattern + "?" + LanguagePattern + "?" + CssClassPattern + "?|" +
                                                    LanguagePattern + "?" + CssStylePattern + "?" + CssClassPattern + "?)";

    public const string PunctuationPattern = @"[\!""#\$%&'()\*\+,\-\./:;<=>\?@\[\\\]\^_`{}~]";

    public const string HtmlAttributesPattern = @"(\s+\w+=((""[^""]+"")|('[^']+')))*";

    #endregion

    /// <summary>
    /// Image alignment tags, mapped to their HTML meanings.
    /// </summary>
    public static Dictionary<string, string> ImageAlign { get; set; }
    /// <summary>
    /// Horizontal text alignment tags, mapped to their HTML meanings.
    /// </summary>
    public static Dictionary<string, string> HorizontalAlign { get; set; }
    /// <summary>
    /// Vertical text alignment tags, mapped to their HTML meanings.
    /// </summary>
    public static Dictionary<string, string> VerticalAlign { get; set; }

    static Globals()
    {
        ImageAlign = new Dictionary<string, string>
        {
            ["<"] = "left",
            ["="] = "center",
            [">"] = "right"
        };

        HorizontalAlign = new Dictionary<string, string>
        {
            ["<"] = "left",
            ["="] = "center",
            [">"] = "right",
            ["<>"] = "justify"
        };

        VerticalAlign = new Dictionary<string, string>
        {
            ["^"] = "top",
            ["-"] = "middle",
            ["~"] = "bottom"
        };
    }

    public static string EncodeHTMLLink(string url)
    {
        url = url.Replace("&amp;", "&#38;");
        url = Regex.Replace(url, "&(?=[^#])", "&#38;");
        return url;
    }
}
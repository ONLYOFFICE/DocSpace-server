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

namespace Textile.Blocks;

public class GlyphBlockModifier : BlockModifier
{
    public override string ModifyLine(string line)
    {
        line = Regex.Replace(line, "\"\\z", "\" ");

        // fix: hackish
        string[,] glyphs = {
                            { @"([^\s[{(>_*])?\'(?(1)|(\s|s\b|" + Globals.PunctuationPattern + "))", "$1&#8217;$2" },    //  single closing
                            { @"\'", "&#8216;" },                                                   //  single opening
                            { @"([^\s[{(>_*])?""(?(1)|(\s|" + Globals.PunctuationPattern + "))", "$1&#8221;$2" },        //  double closing
                            { @"""", "&#8220;" },                                                   //  double opening
                            { @"\b( )?\.{3}", "$1&#8230;" },                                        //  ellipsis
                            { @"\b([A-Z][A-Z0-9]{2,})\b(?:[(]([^)]*)[)])", "<acronym title=\"$2\">$1</acronym>" },        //  3+ uppercase acronym
                            { @"(\s)?--(\s)?", "$1&#8212;$2" },                                     //  em dash
                            { @"\s-\s", " &#8211; " },                                              //  en dash
                            { @"(\d+)( )?x( )?(\d+)", "$1$2&#215;$3$4" },                           //  dimension sign
                            { @"\b ?[([](TM|tm)[])]", "&#8482;" },                                  //  trademark
                            { @"\b ?[([](R|r)[])]", "&#174;" },                                     //  registered
                            { @"\b ?[([](C|c)[])]", "&#169;" }                                      //  copyright
                            };

        var sb = new StringBuilder();

        if (!Regex.IsMatch(line, "<.*>"))
        {
            // If no HTML, do a simple search & replace.
            for (var i = 0; i < glyphs.GetLength(0); ++i)
            {
                line = Regex.Replace(line, glyphs[i, 0], glyphs[i, 1]);
            }
            sb.Append(line);
        }
        else
        {
            var splits = Regex.Split(line, "(<.*?>)");
            var offtags = "code|pre|notextile";
            var codepre = false;

            foreach (var split in splits)
            {
                var modifiedSplit = split;
                if (modifiedSplit.Length == 0)
                {
                    continue;
                }

                if (Regex.IsMatch(modifiedSplit, "<(" + offtags + ")>"))
                {
                    codepre = true;
                }

                if (Regex.IsMatch(modifiedSplit, @"<\/(" + offtags + ")>"))
                {
                    codepre = false;
                }

                if (!Regex.IsMatch(modifiedSplit, "<.*>") && !codepre)
                {
                    for (var i = 0; i < glyphs.GetLength(0); ++i)
                    {
                        modifiedSplit = Regex.Replace(modifiedSplit, glyphs[i, 0], glyphs[i, 1]);
                    }
                }

                // do htmlspecial if between <code>
                if (codepre)
                {
                    //TODO: htmlspecialchars(line)
                    //line = Regex.Replace(line, @"&lt;(\/?" + offtags + ")&gt;", "<$1>");
                    //line = line.Replace("&amp;#", "&#");
                }

                sb.Append(modifiedSplit);
            }
        }

        return sb.ToString();
    }
}
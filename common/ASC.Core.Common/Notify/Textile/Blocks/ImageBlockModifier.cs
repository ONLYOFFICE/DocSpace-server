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

public class ImageBlockModifier : BlockModifier
{
    public override string ModifyLine(string line)
    {
        line = Regex.Replace(line,
                                @"\!" +                   // opening !
                                @"(?<algn>\<|\=|\>)?" +   // optional alignment atts
                                Globals.BlockModifiersPattern + // optional style, public class atts
                                @"(?:\. )?" +             // optional dot-space
                                @"(?<url>[^\s(!]+)" +     // presume this is the src
                                @"\s?" +                  // optional space
                                @"(?:\((?<title>([^\)]+))\))?" +// optional title
                                @"\!" +                   // closing
                                @"(?::(?<href>(\S+)))?" +     // optional href
                                @"(?=\s|\.|,|;|\)|\||$)",               // lookahead: space or simple punctuation or end of string
                            ImageFormatMatchEvaluator
                            );
        return line;
    }

    private string ImageFormatMatchEvaluator(Match m)
    {
        var atts = BlockAttributesParser.ParseBlockAttributes(m.Groups["atts"].Value, "img");
        if (m.Groups["algn"].Length > 0)
        {
            atts += " align=\"" + Globals.ImageAlign[m.Groups["algn"].Value] + "\"";
        }

        if (m.Groups["title"].Length > 0)
        {
            atts += " title=\"" + m.Groups["title"].Value + "\"";
            atts += " alt=\"" + m.Groups["title"].Value + "\"";
        }
        else
        {
            atts += " alt=\"\"";
        }
        // Get Image Size?

        var res = "<img src=\"" + m.Groups["url"].Value + "\"" + atts + " />";

        if (m.Groups["href"].Length > 0)
        {
            var href = m.Groups["href"].Value;
            var end = string.Empty;
            var endMatch = Regex.Match(href, @"(.*)(?<end>\.|,|;|\))$");
            if (m.Success && !string.IsNullOrEmpty(endMatch.Groups["end"].Value))
            {
                href = href[..^1];
                end = endMatch.Groups["end"].Value;
            }
            res = "<a href=\"" + Globals.EncodeHTMLLink(href) + "\">" + res + "</a>" + end;
        }

        return res;
    }
}
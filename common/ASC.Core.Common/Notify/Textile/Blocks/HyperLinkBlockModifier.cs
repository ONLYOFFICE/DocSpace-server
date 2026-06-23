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

public class HyperLinkBlockModifier : BlockModifier
{
    private readonly string _rel = string.Empty;

    public override string ModifyLine(string line)
    {
        line = Regex.Replace(line,
                                @"(?<pre>[\s[{(]|" + Globals.PunctuationPattern + ")?" +       // $pre
                                "\"" +									// start
                                Globals.BlockModifiersPattern +			// attributes
                                "(?<text>[\\w\\W]+?)" +					// text
                                @"\s?" +
                                @"(?:\((?<title>[^)]+)\)(?=""))?" +		// title
                                "\":" +
                                @"""(?<url>\S+[^""]+)""" +						// url
                                @"(?<slash>\/)?" +						// slash
                                @"(?<post>[^\w\/;]*)" +					// post
                                @"(?=\s|$)",
                                HyperLinksFormatMatchEvaluator);
        return line;
    }

    private string HyperLinksFormatMatchEvaluator(Match m)
    {
        //TODO: check the URL
        var atts = BlockAttributesParser.ParseBlockAttributes(m.Groups["atts"].Value, "a");
        if (m.Groups["title"].Length > 0)
        {
            atts += " title=\"" + m.Groups["title"].Value + "\"";
        }

        var linkText = m.Groups["text"].Value.Trim(' ');

        var str = m.Groups["pre"].Value + "<a ";
        if (!string.IsNullOrEmpty(_rel))
        {
            str += "ref=\"" + _rel + "\" ";
        }

        str += "href=\"" +
                m.Groups["url"].Value + m.Groups["slash"].Value + "\"" +
                atts +
                ">" + linkText + "</a>" + m.Groups["post"].Value;
        return str;
    }
}
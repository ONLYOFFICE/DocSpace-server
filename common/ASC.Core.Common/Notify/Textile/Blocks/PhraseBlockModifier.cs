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

public abstract class PhraseBlockModifier : BlockModifier
{
    protected string PhraseModifierFormat(string input, string modifier, string tag)
    {
        // All phrase modifiers are one character, or a double character. Sometimes,
        // there's an additional escape character for the regex ('\').
        var compressedModifier = modifier;
        if (modifier.Length == 4)
        {
            compressedModifier = modifier[..2];
        }
        else if (modifier.Length == 2)
        {
            if (modifier[0] != '\\')
            {
                compressedModifier = modifier[0].ToString();
            }
            //else: compressedModifier = modifier;
        }
        //else: compressedModifier = modifier;

        // We try to remove the Textile tag used for the formatting from
        // the punctuation pattern, so that we match the end of the formatted
        // zone correctly.
        var punctuationPattern = Globals.PunctuationPattern.Replace(compressedModifier, "");

        // Now we can do the replacement.
        var pmme = new PhraseModifierMatchEvaluator(tag);
        var res = Regex.Replace(input,
                                        @"(?<=\s|" + punctuationPattern + @"|[{\(\[]|^)" +
                                        modifier +
                                        Globals.BlockModifiersPattern +
                                        @"(:(?<cite>(\S+)))?" +
                                        "(?<content>[^" + compressedModifier + "]*)" +
                                        "(?<end>" + punctuationPattern + "*)" +
                                        modifier +
                                        @"(?=[\]\)}]|" + punctuationPattern + @"+|\s|$)",
                                    pmme.MatchEvaluator
                                    );
        return res;
    }

    private sealed class PhraseModifierMatchEvaluator(string tag)
    {
        public string MatchEvaluator(Match m)
        {
            if (m.Groups["content"].Length == 0)
            {
                // It's possible that the "atts" match groups eats the contents
                // when the user didn't want to give block attributes, but the content
                // happens to match the syntax. For example: "*(blah)*".
                if (m.Groups["atts"].Length == 0)
                {
                    return m.ToString();
                }

                return "<" + tag + ">" + m.Groups["atts"].Value + m.Groups["end"].Value + "</" + tag + ">";
            }

            var atts = BlockAttributesParser.ParseBlockAttributes(m.Groups["atts"].Value, tag);
            if (m.Groups["cite"].Length > 0)
            {
                atts += " cite=\"" + m.Groups["cite"] + "\"";
            }

            var res = "<" + tag + atts + ">" +
                            m.Groups["content"].Value + m.Groups["end"].Value +
                            "</" + tag + ">";
            return res;
        }
    }
}
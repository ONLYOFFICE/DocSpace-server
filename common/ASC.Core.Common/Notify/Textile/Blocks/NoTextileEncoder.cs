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

public static class NoTextileEncoder
{
    /// <summary>
    /// The ampersand is escaped before the table below and unescaped after it, which is what makes the
    /// pair of methods here exact inverses of each other.
    ///
    /// Without it the round trip cannot tell the two kinds of <c>&amp;lt;</c> apart: the one this encoder
    /// produced from a real <c>&lt;</c>, and the one that was already in the value because the sending
    /// code called <c>HtmlEncode()</c> on user input. Decoding turned both back into <c>&lt;</c>, so the
    /// escaping every action applies was undone at the very last step (bug 82910). Escaping the ampersand
    /// first turns the second kind into <c>&amp;amp;lt;</c>, which the table no longer matches, and the
    /// final step restores it as <c>&amp;lt;</c> — still escaped.
    /// </summary>
    private const string Ampersand = "&";
    private const string EncodedAmpersand = "&amp;";

    private static readonly string[,] _textileModifiers = {
                            { "\"", "&#34;" },
                            { "%", "&#37;" },
                            { "*", "&#42;" },
                            { "+", "&#43;" },
                            { "-", "&#45;" },
                            { "<", "&lt;" },   // or "&#60;"
            				{ "=", "&#61;" },
                            { ">", "&gt;" },   // or "&#62;"
            				{ "?", "&#63;" },
                            { "^", "&#94;" },
                            { "_", "&#95;" },
                            { "~", "&#126;" },
                            { "@", "&#64;" },
                            { "'", "&#39;" },
                            { "|", "&#124;" },
                            { "!", "&#33;" },
                            { "(", "&#40;" },
                            { ")", "&#41;" },
                            { ".", "&#46;" },
                            { "x", "&#120;" }
                        };


    public static string EncodeNoTextileZones(string tmp, string patternPrefix, string patternSuffix, string[] exceptions = null)
    {
        string evaluator(Match m)
        {
            var toEncode = m.Groups["notex"].Value;
            if (toEncode.Length == 0)
            {
                return string.Empty;
            }

            // First, so that an entity already present in the value survives the round trip as an
            // entity. Never subject to the exceptions: the two methods have to stay symmetrical.
            toEncode = toEncode.Replace(Ampersand, EncodedAmpersand);

            for (var i = 0; i < _textileModifiers.GetLength(0); ++i)
            {
                if (exceptions == null || Array.IndexOf(exceptions, _textileModifiers[i, 0]) < 0)
                {
                    toEncode = toEncode.Replace(_textileModifiers[i, 0], _textileModifiers[i, 1]);
                }
            }
            return patternPrefix + toEncode + patternSuffix;
        }
        tmp = Regex.Replace(tmp, "(" + patternPrefix + "(?<notex>.+?)" + patternSuffix + ")*", evaluator);
        return tmp;
    }

    public static string DecodeNoTextileZones(string tmp, string patternPrefix, string patternSuffix, string[] exceptions = null)
    {
        string evaluator(Match m)
        {
            var toEncode = m.Groups["notex"].Value;
            for (var i = 0; i < _textileModifiers.GetLength(0); ++i)
            {
                if (exceptions == null || Array.IndexOf(exceptions, _textileModifiers[i, 0]) < 0)
                {
                    toEncode = toEncode.Replace(_textileModifiers[i, 1], _textileModifiers[i, 0]);
                }
            }

            // Last, mirroring the encoder: only what is left after the table has run is an ampersand
            // the value itself carried.
            toEncode = toEncode.Replace(EncodedAmpersand, Ampersand);

            return toEncode;
        }
        tmp = Regex.Replace(tmp, "(" + patternPrefix + "(?<notex>.+?)" + patternSuffix + ")*", evaluator);
        return tmp;
    }
}
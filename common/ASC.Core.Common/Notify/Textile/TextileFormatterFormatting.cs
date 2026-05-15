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

public partial class TextileFormatter
{
    private readonly Regex _velocityArguments =
        new("nostyle(?<arg>.*?)/nostyle", RegexOptions.IgnoreCase | RegexOptions.Singleline);

    private string ArgMatchReplace(Match match)
    {
        return match.Result("${arg}");
    }

    #region Formatting Methods

    /// <summary>
    /// Formats the given text.
    /// </summary>
    /// <param name="input">The text to format.</param>
    public void Format(string input)
    {
        Output.Begin();

        // Clean the text...
        var str = PrepareInputForFormatting(input);
        // ...and format each line.
        foreach (var line in str.Split('\n'))
        {
            var tmp = line;

            // Let's see if the current state(s) is(are) finished...
            while (CurrentState != null && CurrentState.ShouldExit(tmp))
            {
                PopState();
            }

            if (!Regex.IsMatch(tmp, @"^\s*$"))
            {
                // Figure out the new state for this text line, if possible.
                if (CurrentState == null || CurrentState.ShouldParseForNewFormatterState(tmp))
                {
                    tmp = HandleFormattingState(tmp);
                }
                // else, the current state doesn't want to be superceded by
                // a new one. We'll leave him be.

                // Modify the line with our block modifiers.
                if (CurrentState == null || CurrentState.ShouldFormatBlocks(tmp))
                {
                    foreach (var blockModifier in _blockModifiers)
                    {
                        //TODO: if not disabled...
                        tmp = blockModifier.ModifyLine(tmp);
                    }

                    for (var i = _blockModifiers.Count - 1; i >= 0; i--)
                    {
                        var blockModifier = _blockModifiers[i];
                        tmp = blockModifier.Conclude(tmp);
                    }
                }

                tmp = _velocityArguments.Replace(tmp, ArgMatchReplace);

                // Format the current line.
                CurrentState.FormatLine(tmp);
            }
        }
        // We're done. There might be a few states still on
        // the stack (for example if the text ends with a nested
        // list), so we must pop them all so that they have
        // their "Exit" method called correctly.
        while (_stackOfStates.Count > 0)
        {
            PopState();
        }

        Output.End();
    }

    #endregion

    #region Preparation Methods

    /// <summary>
    /// Cleans up a text before formatting.
    /// </summary>
    /// <param name="input">The text to clean up.</param>
    /// <returns>The clean text.</returns>
    /// This method cleans stuff like line endings, so that
    /// we don't have to bother with it while formatting.
    private string PrepareInputForFormatting(string input)
    {
        input = CleanWhiteSpace(input);
        return input;
    }

    private string CleanWhiteSpace(string text)
    {
        text = text.Replace("\r\n", "\n");
        text = text.Replace("\t", "");
        text = Regex.Replace(text, @"\n{3,}", "\n\n");
        text = Regex.Replace(text, @"\n *\n", "\n\n");
        text = Regex.Replace(text, "\"$", "\" ");
        return text;
    }

    #endregion
}
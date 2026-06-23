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
/// Base class for formatter states.
/// </summary>
/// A formatter state describes the current situation
/// of the text being currently processed. A state can
/// write HTML code when entered, exited, and can modify
/// each line of text it receives.
public abstract class FormatterState
{
    /// <summary>
    /// The formatter this state belongs to.
    /// </summary>
    public TextileFormatter Formatter { get; }

    /// <summary>
    /// Public constructor.
    /// </summary>
    /// <param name="formatter">The parent formatter.</param>
    protected FormatterState(TextileFormatter formatter)
    {
        Formatter = formatter;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="input"></param>
    /// <param name="m"></param>
    /// <returns></returns>
    public abstract string Consume(string input, Match m);

    /// <summary>
    /// Method called when the state is entered.
    /// </summary>
    public abstract void Enter();
    /// <summary>
    /// Method called when the state is exited.
    /// </summary>
    public abstract void Exit();
    /// <summary>
    /// Method called when a line of text should be written
    /// to the web form.
    /// </summary>
    /// <param name="input">The line of text.</param>
    public abstract void FormatLine(string input);

    ///// <summary>
    ///// Returns whether this state can last for more than one line.
    ///// </summary>
    ///// <returns>A boolean value stating whether this state is only for one line.</returns>
    ///// This method should return true only if this state is genuinely
    ///// multi-line. For example, a header text is only one line long. You can
    ///// have several consecutive lines of header texts, but they are not the same
    ///// header - just several headers one after the other.
    ///// Bulleted and numbered lists are good examples of multi-line states.
    //abstract public bool IsOneLineOnly();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public abstract bool ShouldExit(string input);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public virtual bool ShouldNestState(FormatterState other)
    {
        return false;
    }

    /// <summary>
    /// Returns whether block formatting (quick phrase modifiers, etc.) should be
    /// applied to this line.
    /// </summary>
    /// <param name="input">The line of text</param>
    /// <returns>Whether the line should be formatted for blocks</returns>
    public virtual bool ShouldFormatBlocks(string input)
    {
        return true;
    }

    /// <summary>
    /// Returns whether the current state accepts being superceded by another one
    /// we would possibly find by parsing the input line of text.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public virtual bool ShouldParseForNewFormatterState(string input)
    {
        return true;
    }

    /// <summary>
    /// Gets the formatting state we should fallback to if we don't find anything
    /// relevant in a line of text.
    /// </summary>
    public virtual Type FallbackFormattingState => typeof(ParagraphFormatterState);

    protected FormatterState CurrentFormatterState => Formatter.CurrentState;

    protected void ChangeFormatterState(FormatterState formatterState)
    {
        Formatter.ChangeState(formatterState);
    }
}
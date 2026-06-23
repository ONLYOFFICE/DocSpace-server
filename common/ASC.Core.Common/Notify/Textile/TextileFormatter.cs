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
/// Class for formatting Textile input into HTML.
/// </summary>
/// This class takes raw Textile text and sends the
/// formatted, ready to display HTML string to the
/// outputter defined in the constructor of the
/// class.
public partial class TextileFormatter
{
    static TextileFormatter()
    {
        RegisterFormatterState(typeof(HeaderFormatterState));
        RegisterFormatterState(typeof(PaddingFormatterState));
        RegisterFormatterState(typeof(BlockQuoteFormatterState));
        RegisterFormatterState(typeof(ParagraphFormatterState));
        RegisterFormatterState(typeof(FootNoteFormatterState));
        RegisterFormatterState(typeof(OrderedListFormatterState));
        RegisterFormatterState(typeof(UnorderedListFormatterState));
        RegisterFormatterState(typeof(TableFormatterState));
        RegisterFormatterState(typeof(TableRowFormatterState));
        RegisterFormatterState(typeof(CodeFormatterState));
        RegisterFormatterState(typeof(PreFormatterState));
        RegisterFormatterState(typeof(PreCodeFormatterState));
        RegisterFormatterState(typeof(NoTextileFormatterState));
        RegisterFormatterState(typeof(CompleteTagFormatterState));

        RegisterBlockModifier(new NoTextileBlockModifier());
        RegisterBlockModifier(new CodeBlockModifier());
        RegisterBlockModifier(new PreBlockModifier());
        RegisterBlockModifier(new HyperLinkBlockModifier());
        RegisterBlockModifier(new ImageBlockModifier());
        RegisterBlockModifier(new GlyphBlockModifier());
        RegisterBlockModifier(new EmphasisPhraseBlockModifier());
        RegisterBlockModifier(new StrongPhraseBlockModifier());
        RegisterBlockModifier(new ItalicPhraseBlockModifier());
        RegisterBlockModifier(new BoldPhraseBlockModifier());
        RegisterBlockModifier(new CitePhraseBlockModifier());
        RegisterBlockModifier(new DeletedPhraseBlockModifier());
        RegisterBlockModifier(new InsertedPhraseBlockModifier());
        RegisterBlockModifier(new SuperScriptPhraseBlockModifier());
        RegisterBlockModifier(new SubScriptPhraseBlockModifier());
        RegisterBlockModifier(new SpanPhraseBlockModifier());
        RegisterBlockModifier(new FootNoteReferenceBlockModifier());

        //TODO: capitals block modifier
    }

    /// <summary>
    /// Public constructor, where the formatter is hooked up
    /// to an outputter.
    /// </summary>
    /// <param name="output">The outputter to be used.</param>
    public TextileFormatter(IOutputter output)
    {
        Output = output;
    }

    #region Properties for Output

    /// <summary>
    /// The ouputter to which the formatted text
    /// is sent to.
    /// </summary>
    public IOutputter Output { get; }

    /// <summary>
    /// The offset for the header tags.
    /// </summary>
    /// When the formatted text is inserted into another page
    /// there might be a need to offset all headers (h1 becomes
    /// h4, for instance). The header offset allows this.
    public int HeaderOffset { get; set; }

    #endregion

    #region Properties for Conversion

    public bool FormatImages
    {
        get => IsBlockModifierEnabled(typeof(ImageBlockModifier));
        set => SwitchBlockModifier(typeof(ImageBlockModifier), value);
    }

    public bool FormatLinks
    {
        get => IsBlockModifierEnabled(typeof(HyperLinkBlockModifier));
        set => SwitchBlockModifier(typeof(HyperLinkBlockModifier), value);
    }

    public bool FormatLists
    {
        get => IsBlockModifierEnabled(typeof(OrderedListFormatterState));
        set
        {
            SwitchBlockModifier(typeof(OrderedListFormatterState), value);
            SwitchBlockModifier(typeof(UnorderedListFormatterState), value);
        }
    }

    public bool FormatFootNotes
    {
        get => IsBlockModifierEnabled(typeof(FootNoteReferenceBlockModifier));
        set
        {
            SwitchBlockModifier(typeof(FootNoteReferenceBlockModifier), value);
            SwitchFormatterState(typeof(FootNoteFormatterState), value);
        }
    }

    public bool FormatTables
    {
        get => IsFormatterStateEnabled(typeof(TableFormatterState));
        set
        {
            SwitchFormatterState(typeof(TableFormatterState), value);
            SwitchFormatterState(typeof(TableRowFormatterState), value);
        }
    }

    /// <summary>
    /// Attribute to add to all links.
    /// </summary>
    public string Rel { get; set; } = string.Empty;

    #endregion

    #region Utility Methods

    /// <summary>
    /// Utility method for quickly formatting a text without having
    /// to create a TextileFormatter with an IOutputter.
    /// </summary>
    /// <param name="input">The string to format</param>
    /// <returns>The formatted version of the string</returns>
    public static string FormatString(string input)
    {
        var s = new StringBuilderTextileFormatter();
        var f = new TextileFormatter(s);
        f.Format(input);
        return s.GetFormattedText();
    }

    /// <summary>
    /// Utility method for formatting a text with a given outputter.
    /// </summary>
    /// <param name="input">The string to format</param>
    /// <param name="outputter">The IOutputter to use</param>
    public static void FormatString(string input, IOutputter outputter)
    {
        var f = new TextileFormatter(outputter);
        f.Format(input);
    }

    #endregion
}
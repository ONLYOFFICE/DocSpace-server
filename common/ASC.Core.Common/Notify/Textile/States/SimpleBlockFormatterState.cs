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

namespace Textile.States;

public abstract class SimpleBlockFormatterState(TextileFormatter formatter) : FormatterState(formatter)
{
    internal const string PatternBegin = @"^\s*(?<tag>";
    internal const string PatternEnd = ")" + Globals.AlignPattern + Globals.BlockModifiersPattern + @"\.(?:\s+)?(?<content>.*)$";

    public string Tag { get; private set; }

    public string AlignInfo { get; private set; }

    public string AttInfo { get; private set; }

    public override string Consume(string input, Match m)
    {
        Tag = m.Groups["tag"].Value;
        AlignInfo = m.Groups["align"].Value;
        AttInfo = m.Groups["atts"].Value;
        input = m.Groups["content"].Value;

        OnContextAcquired();

        Formatter.ChangeState(this);

        return input;
    }

    public override bool ShouldNestState(FormatterState other)
    {
        var blockFormatterState = (SimpleBlockFormatterState)other;
        return blockFormatterState.Tag != Tag ||
                blockFormatterState.AlignInfo != AlignInfo ||
                blockFormatterState.AttInfo != AttInfo;
    }

    protected virtual void OnContextAcquired()
    {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    protected string FormattedAlignment()
    {
        return BlockAttributesParser.ParseBlockAttributes(AlignInfo);
    }

    protected string FormattedStyles(string element)
    {
        return BlockAttributesParser.ParseBlockAttributes(AttInfo, element);
    }

    protected string FormattedStylesAndAlignment(string element)
    {
        return BlockAttributesParser.ParseBlockAttributes(AlignInfo + AttInfo, element);
    }
}
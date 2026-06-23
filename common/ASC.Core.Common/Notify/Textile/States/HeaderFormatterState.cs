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

[FormatterState(PatternBegin + "pad[0-9]+" + PatternEnd)]
public class PaddingFormatterState(TextileFormatter formatter) : SimpleBlockFormatterState(formatter)
{
    public int HeaderLevel { get; private set; }


    public override void Enter()
    {
        for (var i = 0; i < HeaderLevel; i++)
        {
            Formatter.Output.Write($"<br {FormattedStylesAndAlignment("br")}/>");
        }
    }

    public override void Exit()
    {
    }

    protected override void OnContextAcquired()
    {
        var m = Regex.Match(Tag, "^pad(?<lvl>[0-9]+)");
        HeaderLevel = int.Parse(m.Groups["lvl"].Value);
    }

    public override void FormatLine(string input)
    {
        Formatter.Output.Write(input);
    }

    public override bool ShouldExit(string intput)
    {
        return true;
    }

    public override bool ShouldNestState(FormatterState other)
    {
        return false;
    }
}

/// <summary>
/// Formatting state for headers and titles.
/// </summary>
[FormatterState(PatternBegin + "h[0-9]+" + PatternEnd)]
public class HeaderFormatterState(TextileFormatter f) : SimpleBlockFormatterState(f)
{
    public int HeaderLevel { get; private set; }

    public override void Enter()
    {
        Formatter.Output.Write("<h" + HeaderLevel + FormattedStylesAndAlignment("h" + HeaderLevel) + ">");
    }

    public override void Exit()
    {
        Formatter.Output.WriteLine("</h" + HeaderLevel + ">");
    }

    protected override void OnContextAcquired()
    {
        var m = Regex.Match(Tag, "^h(?<lvl>[0-9]+)");
        HeaderLevel = int.Parse(m.Groups["lvl"].Value);
    }

    public override void FormatLine(string input)
    {
        Formatter.Output.Write(input);
    }

    public override bool ShouldExit(string intput)
    {
        return true;
    }

    public override bool ShouldNestState(FormatterState other)
    {
        return false;
    }
}
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

[FormatterState(@"^\s*(" + Globals.AlignPattern + Globals.BlockModifiersPattern + @"\.\s?)?" +
                                @"\|(?<content>.*)\|\s*$")]
public class TableRowFormatterState(TextileFormatter f) : FormatterState(f)
{
    private string _attsInfo;
    private string _alignInfo;

    public override string Consume(string input, Match m)
    {
        _alignInfo = m.Groups["align"].Value;
        _attsInfo = m.Groups["atts"].Value;
        input = "|" + m.Groups["content"].Value + "|";

        if (Formatter.CurrentState is not TableFormatterState)
        {
            var s = new TableFormatterState(Formatter);
            Formatter.ChangeState(s);
        }

        Formatter.ChangeState(this);

        return input;
    }

    public override bool ShouldNestState(FormatterState other)
    {
        return false;
    }

    public override void Enter()
    {
        Formatter.Output.WriteLine("<tr" + FormattedStylesAndAlignment() + ">");
    }

    public override void Exit()
    {
        Formatter.Output.WriteLine("</tr>");
    }

    public override void FormatLine(string input)
    {
        // can get: Align & Classes

        var sb = new StringBuilder();
        var cellsInput = input.Split('|');
        for (var i = 1; i < cellsInput.Length - 1; i++)
        {
            var cellInput = cellsInput[i];
            var tcp = new TableCellParser(cellInput);
            sb.Append(tcp.GetLineFragmentFormatting());
        }

        Formatter.Output.WriteLine(sb.ToString());
    }

    public override bool ShouldExit(string input)
    {
        return true;
    }

    protected string FormattedStylesAndAlignment()
    {
        return BlockAttributesParser.ParseBlockAttributes(_alignInfo + _attsInfo);
    }
}
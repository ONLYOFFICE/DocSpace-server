// (c) Copyright Ascensio System SIA 2009-2024
// 
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
// 
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
// 
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
// 
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

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
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

namespace ASC.Notify.Textile;

[Scope]
public partial class MarkDownStyler : IPatternStyler
{
    private static readonly Regex _velocityArguments = new(NVelocityPatternFormatter.NoStylePreffix + "(?<arg>.*?)" + NVelocityPatternFormatter.NoStyleSuffix, RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    public Task ApplyFormatingAsync(NoticeMessage message)
    {
        var body = string.Empty;
        if (!string.IsNullOrEmpty(message.Subject))
        {
            body += _velocityArguments.Replace(message.Subject, ArgMatchReplace) + Environment.NewLine;
            message.Subject = string.Empty;
        }
        if (string.IsNullOrEmpty(message.Body))
        {
            return Task.CompletedTask;
        }

        var lines = message.Body.Split([Environment.NewLine, "\n"], StringSplitOptions.None);
        foreach (var line in lines)
        {
            if (string.IsNullOrEmpty(line)) { body += Environment.NewLine; continue; }
            var lineToAdd = _velocityArguments.Replace(line, ArgMatchReplace);
            if (lineToAdd.StartsWith("h1.")) { lineToAdd = lineToAdd.Substring("h1.".Length); }
            body += LinkRegex().Replace(lineToAdd, EvalLink) + Environment.NewLine;
        }
        body = PlainTextRegex().Replace(body, "");
        body = HtmlLinkReplacer().Replace(body, @"[$2]($1)");
        body = HttpUtility.HtmlDecode(body);
        body = BoldReplacer().Replace(body, "*");
        body = StrikeThroughReplacer().Replace(body, "~");
        body = UnderlineReplacer().Replace(body, "__");
        body = ItalicReplacer().Replace(body, "_");
        body = TagReplacer().Replace(body, "");
        body = MultilineBreaksReplacer().Replace(body, Environment.NewLine);
        body = SymbolReplacer().Replace(body, m => m.Groups[1].Success ? $@"[{LinkSymbolReplacer().Replace(m.Groups[1].Value, @"\$&")}]({m.Groups[2].Value})" : $@"\{m.Value}");
        message.Body = body;
        return Task.CompletedTask;
    }

    private static string EvalLink(Match match)
    {
        if (match.Success)
        {
            if (match.Groups["text"].Success && match.Groups["link"].Success)
            {
                return $"[{match.Groups["text"].Value}]({match.Groups["link"].Value})";
            }
        }
        return match.Value;
    }

    private static string ArgMatchReplace(Match match)
    {
        return match.Result("${arg}");
    }

    [GeneratedRegex(@"""(?<text>[\w\W]+?)"":""(?<link>[^""]+)""", RegexOptions.Compiled | RegexOptions.Singleline)]
    private static partial Regex LinkRegex();

    [GeneratedRegex(@"(<(p|div).*?>)|(<\/(p|div)>)", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.CultureInvariant)]
    private static partial Regex PlainTextRegex();

    [GeneratedRegex(@"<(.|\n)*?>", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.CultureInvariant)]
    private static partial Regex TagReplacer();

    [GeneratedRegex(@"(?:\r\n|\r(?!\n)|(?!<\r)\n){3,}", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex MultilineBreaksReplacer();

    [GeneratedRegex(@"\[(.*?)]\(([^()]*)\)|[]\\[(){}*_|#+=.!~>`-]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SymbolReplacer();

    [GeneratedRegex(@"[]\\[(){}*_|#+=.!~>`-]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex LinkSymbolReplacer();

    [GeneratedRegex(@"<(strong|\/strong)\\>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BoldReplacer();

    [GeneratedRegex(@"<(s|\/s)\\>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex StrikeThroughReplacer();

    [GeneratedRegex(@"<(u|\/u)\\>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex UnderlineReplacer();

    [GeneratedRegex(@"<(em|\/em)\\>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ItalicReplacer();

    [GeneratedRegex(@"<a.*?href=""(.*?)"".*?>(.*?)<\/a>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex HtmlLinkReplacer();
}
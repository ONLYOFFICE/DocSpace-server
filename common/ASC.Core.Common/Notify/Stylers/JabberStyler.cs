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
public class JabberStyler : IPatternStyler
{
    private static readonly Regex _velocityArguments
        = new(NVelocityPatternFormatter.NoStylePreffix + "(?<arg>.*?)" + NVelocityPatternFormatter.NoStyleSuffix,
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex _linkReplacer
        = new(@"""(?<text>[\w\W]+?)"":""(?<link>[^""]+)""",
            RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex _textileReplacer
        = new(@"(h1\.|h2\.|\*|h3\.|\^)",
            RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex _brReplacer
        = new(@"<br\s*\/*>",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace
            | RegexOptions.Compiled | RegexOptions.Singleline);

    private static readonly Regex _closedTagsReplacer
        = new(@"</(p|div)>",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace
            | RegexOptions.Compiled | RegexOptions.Singleline);

    private static readonly Regex _tagReplacer
        = new(@"<(.|\n)*?>",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace
            | RegexOptions.Compiled | RegexOptions.Singleline);

    private static readonly Regex _multiLineBreaksReplacer
        = new(@"(?:\r\n|\r(?!\n)|(?!<\r)\n){3,}",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public Task ApplyFormatingAsync(NoticeMessage message)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrEmpty(message.Subject))
        {
            sb.AppendLine(_velocityArguments.Replace(message.Subject, ArgMatchReplace));
            message.Subject = string.Empty;
        }
        if (string.IsNullOrEmpty(message.Body))
        {
            return Task.CompletedTask;
        }

        var lines = message.Body.Split([Environment.NewLine, "\n"], StringSplitOptions.None);

        for (var i = 0; i < lines.Length - 1; i++)
        {
            ref var line = ref lines[i];
            if (string.IsNullOrEmpty(line))
            {
                sb.AppendLine();
                continue;
            }

            line = _velocityArguments.Replace(line, ArgMatchReplace);
            sb.AppendLine(_linkReplacer.Replace(line, EvalLink));
        }

        ref var lastLine = ref lines[^1];
        lastLine = _velocityArguments.Replace(lastLine, ArgMatchReplace);
        sb.Append(_linkReplacer.Replace(lastLine, EvalLink));
        var body = sb.ToString();
        body = _textileReplacer.Replace(HttpUtility.HtmlDecode(body), ""); //Kill textile markup
        body = _brReplacer.Replace(body, Environment.NewLine);
        body = _closedTagsReplacer.Replace(body, Environment.NewLine);
        body = _tagReplacer.Replace(body, "");
        body = _multiLineBreaksReplacer.Replace(body, Environment.NewLine);
        message.Body = body;
        return Task.CompletedTask;
    }

    private string EvalLink(Match match)
    {
        if (match.Success)
        {
            if (match.Groups["text"].Success && match.Groups["link"].Success)
            {
                if (match.Groups["text"].Value.Equals(match.Groups["link"].Value, StringComparison.OrdinalIgnoreCase))
                {
                    return " " + match.Groups["text"].Value + " ";
                }

                return match.Groups["text"].Value + $" ( {match.Groups["link"].Value} )";
            }
        }

        return match.Value;
    }

    private string ArgMatchReplace(Match match)
    {
        return match.Result("${arg}");
    }
}
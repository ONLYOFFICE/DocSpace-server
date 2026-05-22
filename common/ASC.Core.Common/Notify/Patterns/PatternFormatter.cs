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

namespace ASC.Notify.Patterns;

public abstract class PatternFormatter : IPatternFormatter
{
    private readonly bool _doformat;
    private readonly string _tagSearchPattern;

    protected Regex RegEx { get; private set; }

    protected PatternFormatter() { }

    protected PatternFormatter(string tagSearchRegExp, bool formatMessage = false)
    {
        if (string.IsNullOrEmpty(tagSearchRegExp))
        {
            throw new ArgumentException(nameof(tagSearchRegExp));
        }

        _tagSearchPattern = tagSearchRegExp;
        RegEx = new Regex(_tagSearchPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
        _doformat = formatMessage;
    }

    public List<string> GetTags(IPattern pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);

        var findedTags = new List<string>(SearchTags(pattern.Body()));
        findedTags.AddRange(SearchTags(pattern.Subject()));
        return findedTags.Distinct().ToList();
    }

    public void FormatMessage(INoticeMessage message, ITagValue[] tagsValues)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(message.Pattern);
        ArgumentNullException.ThrowIfNull(tagsValues);

        BeforeFormat(message, tagsValues);

        message.Subject = FormatText(_doformat ? message.Subject : message.Pattern.Subject(), tagsValues);
        message.Body = FormatText(_doformat ? message.Body : message.Pattern.Body(), tagsValues);

        AfterFormat(message);
    }

    protected abstract string FormatText(string text, ITagValue[] tagsValues);

    protected virtual void BeforeFormat(INoticeMessage message, ITagValue[] tagsValues) { }

    protected virtual void AfterFormat(INoticeMessage message) { }

    private List<string> SearchTags(string text)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(_tagSearchPattern))
        {
            return [];
        }

        var maches = RegEx.Matches(text);
        var findedTags = new List<string>(maches.Count);
        foreach (Match mach in maches)
        {
            var tag = mach.Groups["tagName"].Value;
            if (!findedTags.Contains(tag))
            {
                findedTags.Add(tag);
            }
        }

        return findedTags;
    }
}
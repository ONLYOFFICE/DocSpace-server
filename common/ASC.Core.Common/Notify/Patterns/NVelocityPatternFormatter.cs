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

public sealed class NVelocityPatternFormatter() : PatternFormatter(DefaultPattern)
{
    public const string DefaultPattern = @"(^|[^\\])\$[\{]{0,1}(?<tagName>[a-zA-Z0-9_]+)";
    public const string NoStylePreffix = "==";
    public const string NoStyleSuffix = "==";

    private VelocityContext _nvelocityContext;

    protected override void BeforeFormat(INoticeMessage message, ITagValue[] tagsValues)
    {
        _nvelocityContext = new VelocityContext();
        _nvelocityContext.AttachEventCartridge(new EventCartridge());
        _nvelocityContext.EventCartridge.ReferenceInsertion += EventCartridgeReferenceInsertion;
        foreach (var tagValue in tagsValues)
        {
            _nvelocityContext.Put(tagValue.Tag, tagValue.Value);
        }

        base.BeforeFormat(message, tagsValues);
    }

    protected override string FormatText(string text, ITagValue[] tagsValues)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        return VelocityFormatter.FormatText(text, _nvelocityContext);
    }

    protected override void AfterFormat(INoticeMessage message)
    {
        _nvelocityContext = null;
        base.AfterFormat(message);
    }

    private void EventCartridgeReferenceInsertion(object sender, ReferenceInsertionEventArgs e)
    {
        if (e.OriginalValue is not string originalString)
        {
            return;
        }

        var lines = originalString.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0)
        {
            return;
        }

        e.NewValue = string.Empty;
        for (var i = 0; i < lines.Length - 1; i++)
        {
            e.NewValue += $"{NoStylePreffix}{lines[i]}{NoStyleSuffix}\n";
        }

        e.NewValue += $"{NoStylePreffix}{lines[^1]}{NoStyleSuffix}";
    }
}
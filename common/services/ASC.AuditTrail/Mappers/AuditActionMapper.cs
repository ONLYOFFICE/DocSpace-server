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

namespace ASC.AuditTrail.Mappers;

[Singleton]
public class AuditActionMapper(ILogger<AuditActionMapper> logger)
{
    public List<IProductActionMapper> Mappers { get; } =
    [
        new DocumentsActionMapper(),
        new LoginActionsMapper(),
        new PeopleActionMapper(),
        new SettingsActionsMapper()
    ];

    public string GetActionText(MessageMaps action, AuditEvent evt, bool limited)
    {
        if (action == null)
        {
            logger.ErrorThereIsNoActionText(action.ActionTextResourceName);

            return string.Empty;
        }

        try
        {
            var actionText = action.GetActionText();

            if (evt.Action is >= (int)MessageAction.CreateClient and <= 10000 && evt.Target != null)
            {
                return string.Format(actionText, evt.Target.GetItems().ToArray<object>());
            }

            if (evt.Description == null || evt.Description.Count == 0)
            {
                return actionText;
            }

            var description = evt.Description
                .Select(t => t.Split([','], StringSplitOptions.RemoveEmptyEntries))
                .Select(split => string.Join(", ", limited ? split.Select(ToLimitedText) : split))
                .ToArray();

            return string.Format(actionText, description);
        }
        catch
        {
            //log.Error(string.Format("Error while building action text for \"{0}\" type of event", action));
            return string.Empty;
        }
    }

    public string GetActionText(MessageMaps action, LoginEvent evt, bool limited)
    {
        if (action == null)
        {
            //log.Error(string.Format("There is no action text for \"{0}\" type of event", action));
            return string.Empty;
        }

        try
        {
            var actionText = action.GetActionText();

            if (evt.Description == null || evt.Description.Count == 0)
            {
                return actionText;
            }

            var description = evt.Description
                                 .Select(t => t.Split([','], StringSplitOptions.RemoveEmptyEntries))
                                 .Select(split => string.Join(", ", limited ? split.Select(ToLimitedText) : split))
                                 .ToArray();

            return string.Format(actionText, description);
        }
        catch
        {
            //log.Error(string.Format("Error while building action text for \"{0}\" type of event", action));
            return string.Empty;
        }
    }

    public string GetActionTypeText(MessageMaps action)
    {
        return action == null
                   ? string.Empty
                   : action.GetActionTypeText();
    }

    public string GetLocationText(MessageMaps action)
    {
        return action == null
                   ? string.Empty
                   : action.GetLocationText();
    }

    private string ToLimitedText(string text)
    {
        if (text == null)
        {
            return null;
        }

        return text.Length < 50 ? text : $"{text[..47]}...";
    }

    public MessageMaps GetMessageMaps(int actionInt)
    {
        var action = (MessageAction)actionInt;
        var mapper = Mappers.SelectMany(m => m.Mappers).FirstOrDefault(m => m.Actions.ContainsKey(action));
        return mapper?.Actions[action];
    }
}
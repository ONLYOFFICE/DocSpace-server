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

using ASC.Common.IntegrationEvents.Events;

namespace ASC.Web.Core.Notify;

[Scope]
public class StudioNotifyServiceHelper(StudioNotifyHelper studioNotifyHelper,
    AuthContext authContext,
    TenantManager tenantManager,
    CommonLinkUtility commonLinkUtility,
    IEventBus eventBus)
{
    public async Task SendNoticeAsync(INotifyAction action)
    {
        var subscriptionSource = studioNotifyHelper.NotifySource.GetSubscriptionProvider();
        var recipients = await subscriptionSource.GetRecipientsAsync(action, null);

        await SendNoticeToAsync(action, recipients, null, false, null);
    }
    
    public async Task SendNoticeToAsync(INotifyAction action, IRecipient[] recipients, string[] senderNames)
    {
        await SendNoticeToAsync(action, recipients, senderNames, false, null);
    }

    public async Task SendNoticeToAsync(INotifyAction action, IRecipient[] recipients, string[] senderNames, string baseUri)
    {
        await SendNoticeToAsync(action, recipients, senderNames, false, baseUri);
    }
    

    public async Task SendNoticeToAsync(INotifyAction action, IRecipient[] recipients, string[] senderNames, bool checkSubsciption, string baseUri)
    {
        var item = new NotifyItemIntegrationEvent(authContext.CurrentAccount.ID, tenantManager.GetCurrentTenantId())
        {
            Action = (NotifyAction)action,
            CheckSubsciption = checkSubsciption,
            BaseUrl = baseUri ?? commonLinkUtility.GetFullAbsolutePath("")
        };

        if (recipients != null && recipients.Length != 0)
        {
            item.Recipients = [];

            foreach (var r in recipients)
            {
                var recipient = new Recipient { Id = r.ID, Name = r.Name, Addresses = [] };
                if (r is IDirectRecipient d)
                {
                    recipient.Addresses.AddRange(d.Addresses);
                    recipient.CheckActivation = d.CheckActivation;
                }

                if (r is IRecipientsGroup)
                {
                    recipient.IsGroup = true;
                }

                item.Recipients.Add(recipient);
            }
        }

        if (senderNames != null && senderNames.Length != 0)
        {
            item.SenderNames = senderNames.ToList();
        }

        if (action.Tags != null)
        {
            item.Tags = action.Tags.Select(Tag (r) => new Tag{ Key = r.Tag, Value = r.Value?.ToString() }).ToList();
        }

        await eventBus.PublishAsync(item);
    }


}
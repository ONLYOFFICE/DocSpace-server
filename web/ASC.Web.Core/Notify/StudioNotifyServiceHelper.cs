﻿// (c) Copyright Ascensio System SIA 2009-2025
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

using ASC.Common.IntegrationEvents.Events;

namespace ASC.Web.Core.Notify;

[Scope]
public class StudioNotifyServiceHelper(StudioNotifyHelper studioNotifyHelper,
    AuthContext authContext,
    TenantManager tenantManager,
    CommonLinkUtility commonLinkUtility,
    IEventBus eventBus)
{
    public async Task SendNoticeToAsync(INotifyAction action, IRecipient[] recipients, string[] senderNames, params ITagValue[] args)
    {
        await SendNoticeToAsync(action, null, recipients, senderNames, false, null, args);
    }

    public async Task SendNoticeToAsync(INotifyAction action, IRecipient[] recipients, string[] senderNames, string baseUri, params ITagValue[] args)
    {
        await SendNoticeToAsync(action, null, recipients, senderNames, false, baseUri, args);
    }

    public async Task SendNoticeToAsync(INotifyAction action, string objectID, IRecipient[] recipients, string[] senderNames, params ITagValue[] args)
    {
        await SendNoticeToAsync(action, objectID, recipients, senderNames, false, null, args);
    }

    public async Task SendNoticeToAsync(INotifyAction action, string objectID, IRecipient[] recipients, string[] senderNames, bool checkSubsciption, string baseUri, params ITagValue[] args)
    {
        var item = new NotifyItemIntegrationEvent(authContext.CurrentAccount.ID, tenantManager.GetCurrentTenantId())
        {
            Action = (NotifyAction)action,
            CheckSubsciption = checkSubsciption,
            BaseUrl = baseUri ?? commonLinkUtility.GetFullAbsolutePath("")
        };

        if (objectID != null)
        {
            item.ObjectId = objectID;
        }

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

        if (args != null && args.Length != 0)
        {
            item.Tags = args.Where(r => r.Value != null).Select(r => new Tag { Key = r.Tag, Value = r.Value.ToString() }).ToList();
        }

        await eventBus.PublishAsync(item);
    }
    
    public async Task SendNoticeAsync(INotifyAction action, params ITagValue[] args)
    {
        var subscriptionSource = studioNotifyHelper.NotifySource.GetSubscriptionProvider();
        var recipients = await subscriptionSource.GetRecipientsAsync(action, null);

        await SendNoticeToAsync(action, null, recipients, null, false, null, args);
    }
}

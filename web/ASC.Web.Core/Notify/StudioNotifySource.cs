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

namespace ASC.Web.Studio.Core.Notify;

[Scope]
public class StudioNotifySource(UserManager userManager, IRecipientProvider recipientsProvider, SubscriptionManager subscriptionManager, IServiceProvider serviceProvider)
    : NotifySource("asc.web.studio", userManager, recipientsProvider, subscriptionManager)
{
    protected override ISubscriptionProvider CreateSubscriptionProvider()
    {
        return new AdminNotifySubscriptionProvider(base.CreateSubscriptionProvider(), serviceProvider);
    }

    private sealed class AdminNotifySubscriptionProvider(ISubscriptionProvider provider, IServiceProvider serviceProvider) : ISubscriptionProvider
    {
        public async Task<object> GetSubscriptionRecordAsync(INotifyAction action, IRecipient recipient, string objectID)
        {
            return await provider.GetSubscriptionRecordAsync(GetAdminAction(action), recipient, objectID);
        }

        public async Task<string[]> GetSubscriptionsAsync(INotifyAction action, IRecipient recipient, bool checkSubscribe = true)
        {
            return await provider.GetSubscriptionsAsync(GetAdminAction(action), recipient, checkSubscribe);
        }

        public async Task SubscribeAsync(INotifyAction action, string objectID, IRecipient recipient)
        {
            await provider.SubscribeAsync(GetAdminAction(action), objectID, recipient);
        }

        public async Task UnSubscribeAsync(INotifyAction action, IRecipient recipient)
        {
            await provider.UnSubscribeAsync(GetAdminAction(action), recipient);
        }

        public async Task UnSubscribeAsync(INotifyAction action)
        {
            await provider.UnSubscribeAsync(GetAdminAction(action));
        }

        public async Task UnSubscribeAsync(INotifyAction action, string objectID)
        {
            await provider.UnSubscribeAsync(GetAdminAction(action), objectID);
        }

        public async Task UnSubscribeAsync(INotifyAction action, string objectID, IRecipient recipient)
        {
            await provider.UnSubscribeAsync(GetAdminAction(action), objectID, recipient);
        }

        public async Task UpdateSubscriptionMethodAsync(INotifyAction action, IRecipient recipient, params string[] senderNames)
        {
            await provider.UpdateSubscriptionMethodAsync(GetAdminAction(action), recipient, senderNames);
        }

        public async Task<IRecipient[]> GetRecipientsAsync(INotifyAction action, string objectID)
        {
            return await provider.GetRecipientsAsync(GetAdminAction(action), objectID);
        }

        public async Task<string[]> GetSubscriptionMethodAsync(INotifyAction action, IRecipient recipient)
        {
            return await provider.GetSubscriptionMethodAsync(GetAdminAction(action), recipient);
        }

        public async Task<bool> IsUnsubscribeAsync(IDirectRecipient recipient, INotifyAction action, string objectID)
        {
            return await provider.IsUnsubscribeAsync(recipient, action, objectID);
        }

        private INotifyAction GetAdminAction(INotifyAction action)
        {
            if (serviceProvider.GetService<SelfProfileUpdatedNotifyAction>().ID == action.ID ||
                serviceProvider.GetService<UserHasJoinNotifyAction>().ID == action.ID ||
                serviceProvider.GetService<UserMessageToAdminNotifyAction>().ID == action.ID ||
                serviceProvider.GetService<ProfileHasDeletedItselfNotifyAction>().ID == action.ID)
            {
                return serviceProvider.GetService<AdminNotifyAction>();
            }

            return action;
        }
    }
}
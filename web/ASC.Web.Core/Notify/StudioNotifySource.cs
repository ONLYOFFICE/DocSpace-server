// (c) Copyright Ascensio System SIA 2009-2025
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

namespace ASC.Web.Studio.Core.Notify;

[Scope]
public class StudioNotifySource(
    UserManager userManager, 
    IRecipientProvider 
        recipientsProvider, 
    SubscriptionManager subscriptionManager, 
    Actions actions)
    : NotifySource("asc.web.studio", userManager, recipientsProvider, subscriptionManager)
{
    protected override ISubscriptionProvider CreateSubscriptionProvider()
    {
        return new AdminNotifySubscriptionProvider(base.CreateSubscriptionProvider(), actions);
    }

    private sealed class AdminNotifySubscriptionProvider(ISubscriptionProvider provider, Actions actions) : ISubscriptionProvider
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
            if (actions.SelfProfileUpdated.ID == action.ID ||
                actions.UserHasJoin.ID == action.ID ||
                actions.UserMessageToAdmin.ID == action.ID ||
                actions.ProfileHasDeletedItself.ID == action.ID
               )
            {
                return actions.AdminNotify;
            }

            return action;
        }
    }
}
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

namespace ASC.Notify.Model;

public class TopSubscriptionProvider(IRecipientProvider recipientProvider, ISubscriptionProvider directSubscriptionProvider) : ISubscriptionProvider
{
    private readonly string[] _defaultSenderMethods = [];
    private readonly ISubscriptionProvider _subscriptionProvider = directSubscriptionProvider ?? throw new ArgumentNullException(nameof(directSubscriptionProvider));
    private readonly IRecipientProvider _recipientProvider = recipientProvider ?? throw new ArgumentNullException(nameof(recipientProvider));


    public TopSubscriptionProvider(IRecipientProvider recipientProvider, ISubscriptionProvider directSubscriptionProvider, string[] defaultSenderMethods)
        : this(recipientProvider, directSubscriptionProvider)
    {
        _defaultSenderMethods = defaultSenderMethods;
    }


    public async Task<string[]> GetSubscriptionMethodAsync(INotifyAction action, IRecipient recipient)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(recipient);

        var senders = await _subscriptionProvider.GetSubscriptionMethodAsync(action, recipient);
        if (senders == null || senders.Length == 0)
        {
            var parents = await WalkUpAsync(recipient);
            foreach (var parent in parents)
            {
                senders = await _subscriptionProvider.GetSubscriptionMethodAsync(action, parent);
                if (senders != null && senders.Length != 0)
                {
                    break;
                }
            }
        }

        return senders is { Length: > 0 } ? senders : _defaultSenderMethods;
    }

    public async Task<IRecipient[]> GetRecipientsAsync(INotifyAction action, string objectID)
    {
        ArgumentNullException.ThrowIfNull(action);

        var recipents = new List<IRecipient>(5);
        var directRecipients = await _subscriptionProvider.GetRecipientsAsync(action, objectID) ?? [];
        recipents.AddRange(directRecipients);

        return recipents.ToArray();
    }

    public async Task<bool> IsUnsubscribeAsync(IDirectRecipient recipient, INotifyAction action, string objectID)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(recipient);

        return await _subscriptionProvider.IsUnsubscribeAsync(recipient, action, objectID);
    }


    public async Task SubscribeAsync(INotifyAction action, string objectID, IRecipient recipient)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(recipient);

        await _subscriptionProvider.SubscribeAsync(action, objectID, recipient);
    }

    public async Task UnSubscribeAsync(INotifyAction action, string objectID, IRecipient recipient)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(recipient);

        await _subscriptionProvider.UnSubscribeAsync(action, objectID, recipient);
    }

    public async Task UnSubscribeAsync(INotifyAction action, string objectID)
    {
        ArgumentNullException.ThrowIfNull(action);

        await _subscriptionProvider.UnSubscribeAsync(action, objectID);
    }

    public async Task UnSubscribeAsync(INotifyAction action)
    {
        ArgumentNullException.ThrowIfNull(action);

        await _subscriptionProvider.UnSubscribeAsync(action);
    }

    public async Task UnSubscribeAsync(INotifyAction action, IRecipient recipient)
    {
        var objects = await GetSubscriptionsAsync(action, recipient);
        foreach (var objectID in objects)
        {
            await _subscriptionProvider.UnSubscribeAsync(action, objectID, recipient);
        }
    }

    public async Task UpdateSubscriptionMethodAsync(INotifyAction action, IRecipient recipient, params string[] senderNames)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(recipient);
        ArgumentNullException.ThrowIfNull(senderNames);

        await _subscriptionProvider.UpdateSubscriptionMethodAsync(action, recipient, senderNames);
    }

    public async Task<object> GetSubscriptionRecordAsync(INotifyAction action, IRecipient recipient, string objectID)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(recipient);

        var subscriptionRecord = await _subscriptionProvider.GetSubscriptionRecordAsync(action, recipient, objectID);

        if (subscriptionRecord != null)
        {
            return subscriptionRecord;
        }

        var parents = await WalkUpAsync(recipient);

        foreach (var parent in parents)
        {
            subscriptionRecord = await _subscriptionProvider.GetSubscriptionRecordAsync(action, parent, objectID);

            if (subscriptionRecord != null)
            {
                break;
            }
        }

        return subscriptionRecord;
    }

    public async Task<string[]> GetSubscriptionsAsync(INotifyAction action, IRecipient recipient, bool checkSubscribe = true)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(recipient);

        var objects = new List<string>();
        var direct = await _subscriptionProvider.GetSubscriptionsAsync(action, recipient, checkSubscribe) ?? [];
        MergeObjects(objects, direct);
        var parents = await WalkUpAsync(recipient);
        foreach (var parent in parents)
        {
            direct = await _subscriptionProvider.GetSubscriptionsAsync(action, parent, checkSubscribe) ?? [];
            if (recipient is IDirectRecipient directRecipient)
            {
                foreach (var groupsubscr in direct)
                {
                    if (!objects.Contains(groupsubscr) && !await _subscriptionProvider.IsUnsubscribeAsync(directRecipient, action, groupsubscr))
                    {
                        objects.Add(groupsubscr);
                    }
                }
            }
            else
            {
                MergeObjects(objects, direct);
            }
        }

        return objects.ToArray();
    }


    private async Task<List<IRecipient>> WalkUpAsync(IRecipient recipient)
    {
        var parents = new List<IRecipient>();
        var groups = await _recipientProvider.GetGroupsAsync(recipient) ?? [];
        foreach (var group in groups)
        {
            parents.Add(group);
            parents.AddRange(await WalkUpAsync(group));
        }

        return parents;
    }

    private void MergeObjects(List<string> result, IEnumerable<string> additions)
    {
        foreach (var addition in additions)
        {
            if (!result.Contains(addition))
            {
                result.Add(addition);
            }
        }
    }
}
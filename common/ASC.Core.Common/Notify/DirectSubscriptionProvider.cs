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

namespace ASC.Core.Notify;

internal class DirectSubscriptionProvider : ISubscriptionProvider
{
    private readonly IRecipientProvider _recipientProvider;
    private readonly SubscriptionManager _subscriptionManager;
    private readonly string _sourceId;


    public DirectSubscriptionProvider(string sourceID, SubscriptionManager subscriptionManager, IRecipientProvider recipientProvider)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourceID);
        _sourceId = sourceID;
        _subscriptionManager = subscriptionManager ?? throw new ArgumentNullException(nameof(subscriptionManager));
        _recipientProvider = recipientProvider ?? throw new ArgumentNullException(nameof(recipientProvider));
    }


    public async Task<object> GetSubscriptionRecordAsync(INotifyAction action, IRecipient recipient, string objectID)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(recipient);

        return await _subscriptionManager.GetSubscriptionRecordAsync(_sourceId, action.ID, recipient.ID, objectID);
    }

    public async Task<string[]> GetSubscriptionsAsync(INotifyAction action, IRecipient recipient, bool checkSubscribe = true)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(recipient);

        return await _subscriptionManager.GetSubscriptionsAsync(_sourceId, action.ID, recipient.ID, checkSubscribe);
    }

    public async Task<IRecipient[]> GetRecipientsAsync(INotifyAction action, string objectID)
    {
        ArgumentNullException.ThrowIfNull(action);

        return await (await _subscriptionManager.GetRecipientsAsync(_sourceId, action.ID, objectID)).ToAsyncEnumerable()
            .Select(GetRecipientAsync)
            .Where(r => r != null)
            .ToArrayAsync();
    }

    private async ValueTask<IRecipient> GetRecipientAsync(string value, CancellationToken token)
    {
        return await _recipientProvider.GetRecipientAsync(value);
    }

    public async Task<string[]> GetSubscriptionMethodAsync(INotifyAction action, IRecipient recipient)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(recipient);

        return await _subscriptionManager.GetSubscriptionMethodAsync(_sourceId, action.ID, recipient.ID);
    }

    public async Task UpdateSubscriptionMethodAsync(INotifyAction action, IRecipient recipient, params string[] senderNames)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(recipient);

        await _subscriptionManager.UpdateSubscriptionMethodAsync(_sourceId, action.ID, recipient.ID, senderNames);
    }

    public async Task<bool> IsUnsubscribeAsync(IDirectRecipient recipient, INotifyAction action, string objectID)
    {
        ArgumentNullException.ThrowIfNull(recipient);
        ArgumentNullException.ThrowIfNull(action);

        return await _subscriptionManager.IsUnsubscribeAsync(_sourceId, recipient.ID, action.ID, objectID);
    }

    public async Task SubscribeAsync(INotifyAction action, string objectID, IRecipient recipient)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(recipient);

        await _subscriptionManager.SubscribeAsync(_sourceId, action.ID, objectID, recipient.ID);
    }

    public async Task UnSubscribeAsync(INotifyAction action, string objectID, IRecipient recipient)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(recipient);

        await _subscriptionManager.UnsubscribeAsync(_sourceId, action.ID, objectID, recipient.ID);
    }

    public async Task UnSubscribeAsync(INotifyAction action)
    {
        ArgumentNullException.ThrowIfNull(action);

        await _subscriptionManager.UnsubscribeAllAsync(_sourceId, action.ID);
    }

    public async Task UnSubscribeAsync(INotifyAction action, string objectID)
    {
        ArgumentNullException.ThrowIfNull(action);

        await _subscriptionManager.UnsubscribeAllAsync(_sourceId, action.ID, objectID);
    }

    [Obsolete("Use UnSubscribe(INotifyAction, string, IRecipient)", true)]
    public Task UnSubscribeAsync(INotifyAction action, IRecipient recipient)
    {
        throw new NotSupportedException("use UnSubscribe(INotifyAction, string, IRecipient )");
    }
}
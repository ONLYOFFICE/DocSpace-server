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

using System.Threading.Channels;

namespace ASC.Notify.Model;

[Transient]
internal class NotifyClientImpl(
        ILoggerProvider loggerFactory,
        NotifyEngine notifyEngine,
        IServiceProvider serviceProvider,
        ChannelWriter<NotifyRequest> channelWriter)
    : INotifyClient
{
    private readonly InterceptorStorage _interceptors = new();
    private INotifySource _notifySource;

    public void Init(INotifySource notifySource)
    {
        _notifySource = notifySource;
    }

    public async Task SendNoticeToAsync(INotifyAction action, IRecipient recipient, string senderNames)
    {
        await SendNoticeToAsync(action, null, recipient, [senderNames], false);
    }

    public async Task SendNoticeToAsync(INotifyAction action, IRecipient[] recipients, string[] senderNames)
    {
        await SendNoticeToAsync(action, null, recipients, senderNames, false);
    }

    public async Task SendNoticeAsync(INotifyAction action, string objectID, IRecipient recipient)
    {
        await SendNoticeToAsync(action, objectID, [recipient], null, false);
    }

    public async Task SendNoticeAsync(INotifyAction action, string objectID, IRecipient recipient, string sendername)
    {
        await SendNoticeToAsync(action, objectID, [recipient], [sendername], false);
    }

    public async Task SendNoticeAsync(INotifyAction action, string objectID, IRecipient[] recipients, string sendername)
    {
        await SendNoticeToAsync(action, objectID, recipients, [sendername], false);
    }

    public async Task SendNoticeAsync(INotifyAction action, string objectID, IRecipient recipient, bool checkSubscription)
    {
        await SendNoticeToAsync(action, objectID, [recipient], null, checkSubscription);
    }

    public void BeginSingleRecipientEvent(string name)
    {
        _interceptors.Add(new SingleRecipientInterceptor(name));
    }

    public void AddInterceptor(ISendInterceptor interceptor)
    {
        _interceptors.Add(interceptor);
    }

    public async Task SendNoticeToAsync(INotifyAction action, string objectID, IRecipient[] recipients, string[] senderNames, bool checkSubsciption)
    {
        ArgumentNullException.ThrowIfNull(recipients);

        BeginSingleRecipientEvent("__syspreventduplicateinterceptor");

        foreach (var recipient in recipients)
        {
            await SendNoticeToAsync(action, objectID, recipient, senderNames, checkSubsciption);
        }
    }

    public async Task SendNoticeToAsync(INotifyAction action, string objectID, IRecipient recipient, string[] senderNames, bool checkSubsciption)
    {
        ArgumentNullException.ThrowIfNull(recipient);

        BeginSingleRecipientEvent("__syspreventduplicateinterceptor");

        await SendRequest(action, objectID, recipient, senderNames, checkSubsciption);
    }

    private async Task SendRequest(INotifyAction action, string objectID, IRecipient recipient, string[] senderNames, bool checkSubsciption)
    {
        var r = CreateRequest(action, objectID, recipient, senderNames, checkSubsciption);
        r._interceptors = _interceptors.GetAll();
        foreach (var a in notifyEngine.Actions)
        {
            await ((INotifyEngineAction)serviceProvider.GetRequiredService(a)).BeforeTransferRequestAsync(r);
        }

        await channelWriter.WriteAsync(r);
    }

    private NotifyRequest CreateRequest(INotifyAction action, string objectID, IRecipient recipient, string[] senders, bool checkSubsciption)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(recipient);

        var tenantManager = serviceProvider.GetService<TenantManager>();
        var request = new NotifyRequest(loggerFactory, _notifySource, action, objectID, recipient)
        {
            _tenantId = tenantManager.GetCurrentTenant().Id,
            _senderNames = senders,
            _isNeedCheckSubscriptions = checkSubsciption
        };
        
        if (action.Tags != null)
        {
            request.Arguments.AddRange(action.Tags);
        }
        
        return request;
    }
}
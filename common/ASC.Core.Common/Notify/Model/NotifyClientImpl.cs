// (c) Copyright Ascensio System SIA 2009-2024
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

using System.Threading.Channels;

namespace ASC.Notify.Model;

[Transient]
class NotifyClientImpl(
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
    
    public async Task SendNoticeToAsync(INotifyAction action, IRecipient recipient, string senderNames, params ITagValue[] args)
    {
        await SendNoticeToAsync(action, null, recipient, [senderNames], false, args);
    }

    public async Task SendNoticeToAsync(INotifyAction action, IRecipient[] recipients, string[] senderNames, params ITagValue[] args)
    {
        await SendNoticeToAsync(action, null, recipients, senderNames, false, args);
    }

    public async Task SendNoticeAsync(INotifyAction action, string objectID, IRecipient recipient, params ITagValue[] args)
    {
        await SendNoticeToAsync(action, objectID, [recipient], null, false, args);
    }

    public async Task SendNoticeAsync(INotifyAction action, string objectID, IRecipient recipient, string sendername, params ITagValue[] args)
    {
        await SendNoticeToAsync(action, objectID, [recipient], [sendername], false, args);
    }

    public async Task SendNoticeAsync(INotifyAction action, string objectID, IRecipient[] recipients, string sendername, params ITagValue[] args)
    {
        await SendNoticeToAsync(action, objectID, recipients, [sendername], false, args);
    }

    public async Task SendNoticeAsync(INotifyAction action, string objectID, IRecipient recipient, bool checkSubscription, params ITagValue[] args)
    {
        await SendNoticeToAsync(action, objectID, [recipient], null, checkSubscription, args);
    }

    public void BeginSingleRecipientEvent(string name)
    {
        _interceptors.Add(new SingleRecipientInterceptor(name));
    }

    public void AddInterceptor(ISendInterceptor interceptor)
    {
        _interceptors.Add(interceptor);
    }

    public async Task SendNoticeToAsync(INotifyAction action, string objectID, IRecipient[] recipients, string[] senderNames, bool checkSubsciption, params ITagValue[] args)
    {
        ArgumentNullException.ThrowIfNull(recipients);

        BeginSingleRecipientEvent("__syspreventduplicateinterceptor");

        foreach (var recipient in recipients)
        {
            await SendNoticeToAsync(action, objectID, recipient, senderNames, checkSubsciption, args);
        }
    }
    
    public async Task SendNoticeToAsync(INotifyAction action, string objectID, IRecipient recipient, string[] senderNames, bool checkSubsciption, params ITagValue[] args)
    {
        ArgumentNullException.ThrowIfNull(recipient);

        BeginSingleRecipientEvent("__syspreventduplicateinterceptor");
        
        await SendRequest(action, objectID, recipient, senderNames, checkSubsciption, args);
    }
    
    private async Task SendRequest(INotifyAction action, string objectID, IRecipient recipient, string[] senderNames, bool checkSubsciption, params ITagValue[] args)
    {
        var r = CreateRequest(action, objectID, recipient, args, senderNames, checkSubsciption);
        r._interceptors = _interceptors.GetAll();
        foreach (var a in notifyEngine.Actions)
        {
            await ((INotifyEngineAction)serviceProvider.GetRequiredService(a)).BeforeTransferRequestAsync(r);
        }

        await channelWriter.WriteAsync(r);
    }

    private NotifyRequest CreateRequest(INotifyAction action, string objectID, IRecipient recipient, ITagValue[] args, string[] senders, bool checkSubsciption)
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

        if (args != null)
        {
            request.Arguments.AddRange(args);
        }

        return request;
    }
}

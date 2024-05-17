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

using Constants = ASC.Core.Configuration.Constants;
using NotifyContext = ASC.Notify.Context;

namespace ASC.Core;

[Singleton]
public class WorkContext(IConfiguration configuration,
    DispatchEngine dispatchEngine,
    NotifyEngine notifyEngine,
    NotifyContext notifyContext,
    JabberSender notifyJabberSender,
    AWSSender awsSender,
    SmtpSender smtpSender,
    NotifyServiceSender notifyServiceSender,
    TelegramSender notifyTelegramSender,
    PushSender notifyPushSender)
{
    private static readonly object _syncRoot = new();
    private bool _notifyStarted;
    private static bool? _isMono;

    public static string[] DefaultClientSenders => [Constants.NotifyEMailSenderSysName];
    public event Action<NotifyContext, INotifyClient> NotifyClientRegistration;
    public static bool IsMono
    {
        get
        {
            if (_isMono.HasValue)
            {
                return _isMono.Value;
            }

            var monoRuntime = Type.GetType("Mono.Runtime");
            _isMono = monoRuntime != null;

            return _isMono.Value;
        }
    }

    public void NotifyStartUp()
    {
        if (_notifyStarted)
        {
            return;
        }

        lock (_syncRoot)
        {
            if (_notifyStarted)
            {
                return;
            }

            INotifySender jabberSender = notifyServiceSender;
            INotifySender emailSender = notifyServiceSender;
            INotifySender telegramSender = notifyTelegramSender;
            INotifySender pushSender = notifyPushSender;


            var postman = configuration["core:notify:postman"];

            if ("ases".Equals(postman, StringComparison.InvariantCultureIgnoreCase) || "smtp".Equals(postman, StringComparison.InvariantCultureIgnoreCase))
            {
                jabberSender = notifyJabberSender;

                var properties = new Dictionary<string, string>
                {
                    ["useCoreSettings"] = "true"
                };
                if ("ases".Equals(postman, StringComparison.InvariantCultureIgnoreCase))
                {
                    emailSender = awsSender;
                    properties["accessKey"] = configuration["ses:accessKey"];
                    properties["secretKey"] = configuration["ses:secretKey"];
                    properties["refreshTimeout"] = configuration["ses:refreshTimeout"];
                }
                else
                {
                    emailSender = smtpSender;
                }

                emailSender.Init(properties);
            }

            notifyContext.RegisterSender(dispatchEngine, Constants.NotifyEMailSenderSysName, new EmailSenderSink(emailSender));
            notifyContext.RegisterSender(dispatchEngine, Constants.NotifyMessengerSenderSysName, new JabberSenderSink(jabberSender));
            notifyContext.RegisterSender(dispatchEngine, Constants.NotifyTelegramSenderSysName, new TelegramSenderSink(telegramSender));
            notifyContext.RegisterSender(dispatchEngine, Constants.NotifyPushSenderSysName, new PushSenderSink(pushSender));

            notifyEngine.AddAction<NotifyTransferRequest>();

            _notifyStarted = true;
        }
    }

    public void RegisterSendMethod(Func<DateTime, Task> method, string cron)
    {
        notifyEngine.RegisterSendMethod(method, cron);
    }

    public void UnregisterSendMethod(Func<DateTime, Task> method)
    {
        notifyEngine.UnregisterSendMethod(method);
    }

    public INotifyClient RegisterClient(IServiceProvider serviceProvider, INotifySource source)
    {
        //ValidateNotifySource(source);
        var client = serviceProvider.GetService<NotifyClientImpl>();
        client.Init(source);
        NotifyClientRegistration?.Invoke(notifyContext, client);

        return client;
    }
}

[Scope]
public class NotifyTransferRequest(TenantManager tenantManager) : INotifyEngineAction
{
    public void AfterTransferRequest(NotifyRequest request)
    {
        if ((request.Properties.Contains("Tenant") ? request.Properties["Tenant"] : null) is Tenant tenant)
        {
            tenantManager.SetCurrentTenant(tenant);
        }
    }

    public async Task BeforeTransferRequestAsync(NotifyRequest request)
    {
        request.Properties.Add("Tenant", await tenantManager.GetCurrentTenantAsync(false));
    }
}

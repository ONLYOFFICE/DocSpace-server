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
    private static readonly Lock _syncRoot = new();
    private bool _notifyStarted;
    private static bool? _isMono;

    public static string[] DefaultClientSenders => [Constants.NotifyEMailSenderSysName, Constants.NotifyTelegramSenderSysName];
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

    public Task BeforeTransferRequestAsync(NotifyRequest request)
    {
        request.Properties.Add("Tenant", tenantManager.GetCurrentTenant(false));
        return Task.CompletedTask;
    }
}
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

using ASC.Common.IntegrationEvents.Events;

using Constants = ASC.Core.Configuration.Constants;

namespace ASC.Web.Studio.Core.Notify;

[Singleton(Additional = typeof(ServiceLauncherExtension))]
public class StudioNotifyServiceSender(IServiceScopeFactory serviceProvider,
    IConfiguration configuration,
    WorkContext workContext,
    TenantExtraConfig tenantExtraConfig)
{
    private static string EMailSenderName { get { return Constants.NotifyEMailSenderSysName; } }

    public void RegisterSendMethod()
    {
        var cron = configuration["core:notify:cron"] ?? "0 0 5 ? * *"; // 5am every day

        if (configuration["core:notify:tariff"] != "false")
        {
            if (tenantExtraConfig.Enterprise)
            {
                workContext.RegisterSendMethod(SendEnterpriseTariffLettersAsync, cron);
            }
            else if (tenantExtraConfig.Opensource)
            {
                workContext.RegisterSendMethod(SendOpensourceTariffLettersAsync, cron);
            }
            else if (tenantExtraConfig.Saas)
            {
                workContext.RegisterSendMethod(SendSaasTariffLettersAsync, cron);
            }
        }

        workContext.RegisterSendMethod(SendMsgWhatsNewAsync, "0 0 * ? * *"); // every hour
        workContext.RegisterSendMethod(SendRoomsActivityAsync, "0 0 * ? * *"); //every hour
    }

    private async Task SendSaasTariffLettersAsync(DateTime scheduleDate)
    {
        using var scope = serviceProvider.CreateScope();
        await scope.ServiceProvider.GetService<StudioPeriodicNotify>().SendSaasLettersAsync(EMailSenderName, scheduleDate);
    }

    private async Task SendEnterpriseTariffLettersAsync(DateTime scheduleDate)
    {
        using var scope = serviceProvider.CreateScope();
        await scope.ServiceProvider.GetService<StudioPeriodicNotify>().SendEnterpriseLettersAsync(EMailSenderName, scheduleDate);
    }

    private async Task SendOpensourceTariffLettersAsync(DateTime scheduleDate)
    {
        using var scope = serviceProvider.CreateScope();
        await scope.ServiceProvider.GetService<StudioPeriodicNotify>().SendOpensourceLettersAsync(EMailSenderName, scheduleDate);
    }

    private async Task SendMsgWhatsNewAsync(DateTime scheduleDate)
    {
        using var scope = serviceProvider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<StudioWhatsNewNotify>().SendMsgWhatsNewAsync(scheduleDate, WhatsNewType.DailyFeed);
    }

    private async Task SendRoomsActivityAsync(DateTime scheduleDate)
    {
        using var scope = serviceProvider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<StudioWhatsNewNotify>().SendMsgWhatsNewAsync(scheduleDate, WhatsNewType.RoomsActivity);
    }
}

[Scope]
public class StudioNotifyWorker(TenantManager tenantManager,
    StudioNotifyHelper studioNotifyHelper,
    CommonLinkUtility baseCommonLinkUtility,
    WorkContext workContext,
    IServiceProvider serviceProvider)
{
    public async Task OnMessageAsync(NotifyItemIntegrationEvent item)
    {
        baseCommonLinkUtility.ServerUri = item.BaseUrl;
        await tenantManager.SetCurrentTenantAsync(item.TenantId);

        var client = workContext.RegisterClient(serviceProvider, studioNotifyHelper.NotifySource);

        await client.SendNoticeToAsync(
            (NotifyAction)item.Action,
            item.ObjectId,
            item.Recipients?.Select(r => r.IsGroup ? new RecipientsGroup(r.Id, r.Name) : (IRecipient)new DirectRecipient(r.Id, r.Name, r.Addresses?.ToArray(), r.CheckActivation)).ToArray(),
            item.SenderNames is { Count: > 0 } ? item.SenderNames.ToArray() : null,
            item.CheckSubsciption,
            item.Tags?
                .Select(r => (ITagValue)new TagValue(r.Key, r.Value))
                .ToArray());
    }
}

public static class ServiceLauncherExtension
{
    public static void Register(DIHelper services)
    {
        services.TryAdd<StudioNotifyWorker>();
        services.TryAdd<StudioPeriodicNotify>();
        services.TryAdd<StudioWhatsNewNotify>();
    }
}

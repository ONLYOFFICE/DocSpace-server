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

using ASC.Common.Security;

namespace ASC.Api.Core.Webhook;

[Scope]
public class UserWebhookManager(
    IWebhookPublisher webhookPublisher,
    WebhookGroupAccessChecker webhookGroupAccessChecker,
    WebhookUserAccessChecker webhookUserAccessChecker,
    UserWebhookDtoHelper userWebhookDtoHelper,
    GroupWebhookDtoHelper groupWebhookDtoHelper,
    ILogger<UserWebhookManager> logger)
{
    public async Task PublishAsync(WebhookTrigger trigger, UserInfo userInfo)
    {
        var publish = await PrepareAsync(trigger, userInfo);

        await publish();
    }

    public async Task PublishAsync(WebhookTrigger trigger, ASC.Core.Users.GroupInfo groupInfo)
    {
        var publish = await PrepareAsync(trigger, groupInfo);

        await publish();
    }

    public async Task PublishAfterAsync(WebhookTrigger trigger, UserInfo userInfo, Func<Task> action)
    {
        var publish = await TryPrepareAsync(trigger, userInfo.Id, () => PrepareAsync(trigger, userInfo));

        await action();

        await publish();
    }

    public async Task PublishAfterAsync(WebhookTrigger trigger, ASC.Core.Users.GroupInfo groupInfo, Func<Task> action)
    {
        var publish = await TryPrepareAsync(trigger, groupInfo.ID, () => PrepareAsync(trigger, groupInfo));

        await action();

        await publish();
    }

    // The payload is built before the action, which removes what it describes; a failure to build it must not stop
    // the action itself, so the webhook is dropped instead.
    private async Task<Func<Task>> TryPrepareAsync(WebhookTrigger trigger, Guid entityId, Func<Task<Func<Task>>> prepare)
    {
        try
        {
            return await prepare();
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            logger.ErrorPrepareWebhook(trigger, entityId, e);

            return () => Task.CompletedTask;
        }
    }

    private async Task<Func<Task>> PrepareAsync(WebhookTrigger trigger, UserInfo userInfo)
    {
        var webhookConfigs = await webhookPublisher.GetWebhookConfigsAsync(trigger, webhookUserAccessChecker, userInfo);

        if (webhookConfigs.Count == 0)
        {
            return () => Task.CompletedTask;
        }

        var payload = await userWebhookDtoHelper.GetAsync(userInfo);

        var publications = new List<Func<Task>>();

        foreach (var subscriberConfigs in webhookConfigs.GroupBy(r => r.CreatedBy))
        {
            var subscriberPayload = await userWebhookDtoHelper.RestrictForSubscriberAsync(payload, userInfo, subscriberConfigs.Key);

            publications.Add(() => webhookPublisher.PublishAsync(trigger, subscriberConfigs, subscriberPayload, userInfo.Id));
        }

        return async () =>
        {
            foreach (var publish in publications)
            {
                await publish();
            }
        };
    }

    private async Task<Func<Task>> PrepareAsync(WebhookTrigger trigger, ASC.Core.Users.GroupInfo groupInfo)
    {
        var webhookConfigs = await webhookPublisher.GetWebhookConfigsAsync(trigger, webhookGroupAccessChecker, groupInfo);

        if (webhookConfigs.Count == 0)
        {
            return () => Task.CompletedTask;
        }

        var payload = await groupWebhookDtoHelper.GetAsync(groupInfo);

        var publications = new List<Func<Task>>();

        foreach (var subscriberConfigs in webhookConfigs.GroupBy(r => r.CreatedBy))
        {
            var subscriberPayload = await groupWebhookDtoHelper.RestrictForSubscriberAsync(payload, subscriberConfigs.Key);

            publications.Add(() => webhookPublisher.PublishAsync(trigger, subscriberConfigs, subscriberPayload, groupInfo.ID));
        }

        return async () =>
        {
            foreach (var publish in publications)
            {
                await publish();
            }
        };
    }
}

[Scope]
public class WebhookGroupAccessChecker(
    AuthManager authentication,
    UserManager userManager,
    IPermissionResolver permissionResolver) : IWebhookAccessChecker<ASC.Core.Users.GroupInfo>
{
    public bool CheckIsTarget(ASC.Core.Users.GroupInfo data, string targetId)
    {
        return Guid.TryParse(targetId, out var id) && id == data.ID;
    }

    public async Task<bool> CheckAccessAsync(ASC.Core.Users.GroupInfo data, Guid userId)
    {
        if (await userManager.IsDocSpaceAdminAsync(userId))
        {
            return true;
        }
        
        var user = await userManager.GetUsersAsync(userId);
        var account = await authentication.GetAccountByIDAsync(user.TenantId, user.Id);

        return await permissionResolver.CheckAsync(account, Constants.Action_ReadGroups);
    }
}

[Scope]
public class WebhookUserAccessChecker(UserManager userManager) : IWebhookAccessChecker<UserInfo>
{
    public bool CheckIsTarget(UserInfo data, string targetId)
    {
        return Guid.TryParse(targetId, out var id) && id == data.Id;
    }

    public async Task<bool> CheckAccessAsync(UserInfo data, Guid userId)
    {
        return await userManager.CanUserViewAnotherUserAsync(userId, data.Id);
    }
}
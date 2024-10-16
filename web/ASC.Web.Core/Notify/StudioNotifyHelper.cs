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

using Constants = ASC.Core.Users.Constants;

namespace ASC.Web.Studio.Core.Notify;

[Scope]
public class StudioNotifyHelper(
    StudioNotifySource studioNotifySource,
    UserManager userManager,
    SettingsManager settingsManager,
    CommonLinkUtility commonLinkUtility,
    TenantManager tenantManager,
    TenantExtra tenantExtra,
    WebImageSupplier webImageSupplier,
    IConfiguration configuration,
    ILogger<StudioNotifyHelper> logger)
{
    public string SiteLink => commonLinkUtility.GetSiteLink();
    
    private ISubscriptionProvider _subscriptionProvider;
    private ISubscriptionProvider SubscriptionProvider => _subscriptionProvider ??= NotifySource.GetSubscriptionProvider();
    
    private IRecipientProvider _recipientsProvider;
    private IRecipientProvider RecipientsProvider => _recipientsProvider ??= NotifySource.GetRecipientsProvider();
    
    public readonly StudioNotifySource NotifySource = studioNotifySource;

    public async Task<IEnumerable<UserInfo>> GetRecipientsAsync(bool toadmins, bool tousers, bool toguests)
    {
        if (toadmins)
        {
            if (tousers)
            {
                if (toguests)
                {
                    return (await userManager.GetUsersAsync());
                }

                return await userManager.GetUsersAsync(EmployeeStatus.Default, EmployeeType.RoomAdmin);
            }

            if (toguests)
            {
                return (await userManager.GetUsersByGroupAsync(Constants.GroupAdmin.ID))
                               .Concat(await userManager.GetUsersAsync(EmployeeStatus.Default, EmployeeType.Guest));
            }

            return await userManager.GetUsersByGroupAsync(Constants.GroupAdmin.ID);
        }

        if (tousers)
        {
            if (toguests)
            {
                return await (await userManager.GetUsersAsync()).ToAsyncEnumerable()
                                  .WhereAwait(async u => !await userManager.IsUserInGroupAsync(u.Id, Constants.GroupAdmin.ID)).ToListAsync();
            }

            return await (await userManager.GetUsersAsync(EmployeeStatus.Default, EmployeeType.RoomAdmin)).ToAsyncEnumerable()
                              .WhereAwait(async u => !await userManager.IsUserInGroupAsync(u.Id, Constants.GroupAdmin.ID)).ToListAsync();
        }

        if (toguests)
        {
            return await userManager.GetUsersAsync(EmployeeStatus.Default, EmployeeType.Guest);
        }

        return new List<UserInfo>();
    }

    public async Task<IRecipient> ToRecipientAsync(Guid userId)
    {
        return await RecipientsProvider.GetRecipientAsync(userId.ToString());
    }

    public async Task<IRecipient[]> RecipientFromEmailAsync(string email, bool checkActivation)
    {
        return await RecipientFromEmailAsync([email], checkActivation);
    }

    public async Task<IRecipient[]> RecipientFromEmailAsync(List<string> emails, bool checkActivation)
    {
        var res = new List<IRecipient>();

        if (emails == null)
        {
            return res.ToArray();
        }

        res.AddRange(emails.
                         Select(email => email.ToLower()).
                         Select(e => new DirectRecipient(e, null, [e], checkActivation)));

        int.TryParse(configuration["core:notify:countspam"], out var countMailsToNotActivated);
        if (!checkActivation
            && countMailsToNotActivated > 0
            && tenantExtra.Saas)
        {
            var tenant = await tenantManager.GetCurrentTenantAsync();
            var tariff = await tenantManager.GetTenantQuotaAsync(tenant.Id);
            if (tariff.Free || tariff.Trial)
            {
                var spamEmailSettings = await settingsManager.LoadAsync<SpamEmailSettings>();
                var sended = spamEmailSettings.MailsSended;

                var mayTake = Math.Max(0, countMailsToNotActivated - sended);
                var tryCount = res.Count;
                if (mayTake < tryCount)
                {
                    res = res.Take(mayTake).ToList();

                    logger.WarningFreeTenant(tenant.Id, tryCount, mayTake);
                }
                spamEmailSettings.MailsSended = sended + tryCount;
                await settingsManager.SaveAsync(spamEmailSettings);
            }
        }

        return res.ToArray();
    }

    public string GetNotificationImageUrl(string imageFileName)
    { 
        var notificationImagePath = configuration["web:notification:image:path"];
        if (string.IsNullOrEmpty(notificationImagePath))
        {
            return
                commonLinkUtility.GetFullAbsolutePath(
                    webImageSupplier.GetAbsoluteWebPath("notifications/" + imageFileName));
        }

        return notificationImagePath.TrimEnd('/') + "/" + imageFileName;
    }


    public async Task<bool> IsSubscribedToNotifyAsync(Guid userId, INotifyAction notifyAction)
    {
        return await IsSubscribedToNotifyAsync(await ToRecipientAsync(userId), notifyAction);
    }

    public async Task<bool> IsSubscribedToNotifyAsync(IRecipient recipient, INotifyAction notifyAction)
    {
        return recipient != null && await SubscriptionProvider.IsSubscribedAsync(logger, notifyAction, recipient, null);
    }

    public async Task SubscribeToNotifyAsync(Guid userId, INotifyAction notifyAction, bool subscribe)
    {
        await SubscribeToNotifyAsync(await ToRecipientAsync(userId), notifyAction, subscribe);
    }

    public async Task SubscribeToNotifyAsync(IRecipient recipient, INotifyAction notifyAction, bool subscribe)
    {
        if (recipient == null)
        {
            return;
        }

        if (subscribe)
        {
            await SubscriptionProvider.SubscribeAsync(notifyAction, null, recipient);
        }
        else
        {
            await SubscriptionProvider.UnSubscribeAsync(notifyAction, null, recipient);
        }
    }
}

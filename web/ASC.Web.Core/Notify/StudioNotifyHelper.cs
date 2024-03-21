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
public class StudioNotifyHelper
{
    public readonly string SiteLink;
    public readonly StudioNotifySource NotifySource;
    private readonly ISubscriptionProvider _subscriptionProvider;
    private readonly IRecipientProvider _recipientsProvider;

    private readonly UserManager _userManager;
    private readonly SettingsManager _settingsManager;
    private readonly CommonLinkUtility _commonLinkUtility;
    private readonly TenantManager _tenantManager;
    private readonly TenantExtra _tenantExtra;
    private readonly WebImageSupplier _webImageSupplier;
    private readonly IConfiguration _configuration;
    private readonly ILogger<StudioNotifyHelper> _logger;

    public StudioNotifyHelper(
        StudioNotifySource studioNotifySource,
        UserManager userManager,
        SettingsManager settingsManager,
        MailWhiteLabelSettingsHelper mailWhiteLabelSettingsHelper,
        CommonLinkUtility commonLinkUtility,
        TenantManager tenantManager,
        TenantExtra tenantExtra,
        CoreBaseSettings coreBaseSettings,
        WebImageSupplier webImageSupplier,
        IConfiguration configuration,
        ILogger<StudioNotifyHelper> logger)
    {
        SiteLink = commonLinkUtility.GetSiteLink(mailWhiteLabelSettingsHelper);
        NotifySource = studioNotifySource;
        _userManager = userManager;
        _settingsManager = settingsManager;
        _commonLinkUtility = commonLinkUtility;
        _tenantManager = tenantManager;
        _tenantExtra = tenantExtra;
        _webImageSupplier = webImageSupplier;
        _configuration = configuration;
        _subscriptionProvider = NotifySource.GetSubscriptionProvider();//TODO: remove from constructor
        _recipientsProvider = NotifySource.GetRecipientsProvider();
        _logger = logger;
    }


    public async Task<IEnumerable<UserInfo>> GetRecipientsAsync(bool toadmins, bool tousers, bool toguests)
    {
        if (toadmins)
        {
            if (tousers)
            {
                if (toguests)
                {
                    return (await _userManager.GetUsersAsync());
                }

                return await _userManager.GetUsersAsync(EmployeeStatus.Default, EmployeeType.RoomAdmin);
            }

            if (toguests)
            {
                return (await _userManager.GetUsersByGroupAsync(Constants.GroupAdmin.ID))
                               .Concat(await _userManager.GetUsersAsync(EmployeeStatus.Default, EmployeeType.User));
            }

            return await _userManager.GetUsersByGroupAsync(Constants.GroupAdmin.ID);
        }

        if (tousers)
        {
            if (toguests)
            {
                return await (await _userManager.GetUsersAsync()).ToAsyncEnumerable()
                                  .WhereAwait(async u => !await _userManager.IsUserInGroupAsync(u.Id, Constants.GroupAdmin.ID)).ToListAsync();
            }

            return await (await _userManager.GetUsersAsync(EmployeeStatus.Default, EmployeeType.RoomAdmin)).ToAsyncEnumerable()
                              .WhereAwait(async u => !await _userManager.IsUserInGroupAsync(u.Id, Constants.GroupAdmin.ID)).ToListAsync();
        }

        if (toguests)
        {
            return await _userManager.GetUsersAsync(EmployeeStatus.Default, EmployeeType.User);
        }

        return new List<UserInfo>();
    }

    public async Task<IRecipient> ToRecipientAsync(Guid userId)
    {
        return await _recipientsProvider.GetRecipientAsync(userId.ToString());
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

        int.TryParse(_configuration["core:notify:countspam"], out var countMailsToNotActivated);
        if (!checkActivation
            && countMailsToNotActivated > 0
            && _tenantExtra.Saas)
        {
            var tenant = await _tenantManager.GetCurrentTenantAsync();
            var tariff = await _tenantManager.GetTenantQuotaAsync(tenant.Id);
            if (tariff.Free || tariff.Trial)
            {
                var spamEmailSettings = await _settingsManager.LoadAsync<SpamEmailSettings>();
                var sended = spamEmailSettings.MailsSended;

                var mayTake = Math.Max(0, countMailsToNotActivated - sended);
                var tryCount = res.Count;
                if (mayTake < tryCount)
                {
                    res = res.Take(mayTake).ToList();

                    _logger.WarningFreeTenant(tenant.Id, tryCount, mayTake);
                }
                spamEmailSettings.MailsSended = sended + tryCount;
                await _settingsManager.SaveAsync(spamEmailSettings);
            }
        }

        return res.ToArray();
    }

    public string GetNotificationImageUrl(string imageFileName)
    { 
        var notificationImagePath = _configuration["web:notification:image:path"];
        if (string.IsNullOrEmpty(notificationImagePath))
        {
            return
                _commonLinkUtility.GetFullAbsolutePath(
                    _webImageSupplier.GetAbsoluteWebPath("notifications/" + imageFileName));
        }

        return notificationImagePath.TrimEnd('/') + "/" + imageFileName;
    }


    public async Task<bool> IsSubscribedToNotifyAsync(Guid userId, INotifyAction notifyAction)
    {
        return await IsSubscribedToNotifyAsync(await ToRecipientAsync(userId), notifyAction);
    }

    public async Task<bool> IsSubscribedToNotifyAsync(IRecipient recipient, INotifyAction notifyAction)
    {
        return recipient != null && await _subscriptionProvider.IsSubscribedAsync(_logger, notifyAction, recipient, null);
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
            await _subscriptionProvider.SubscribeAsync(notifyAction, null, recipient);
        }
        else
        {
            await _subscriptionProvider.UnSubscribeAsync(notifyAction, null, recipient);
        }
    }
}

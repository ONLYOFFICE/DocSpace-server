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

namespace ASC.Web.Core.Notify;
[Scope]
public class NotificationControllerHelper(
    StudioNotifyHelper studioNotifyHelper,
    AuthContext authContext,
    BadgesSettingsHelper badgesSettingsHelper,
    NotificationChannelsHelper notificationChannelsHelper,
    IServiceProvider serviceProvider)
{
    private readonly Guid _userId = authContext.CurrentAccount.ID;

    public IEnumerable<NotificationChannelStatus> GetNotificationChannels()
    {
        return notificationChannelsHelper.GetNotificationChannels().Select(c => new NotificationChannelStatus { Name = c.Name, IsEnabled = c.IsEnabled });
    }

    public async Task<bool> GetNotificationStatusAsync(NotificationType notificationType)
    {
        bool isEnabled;

        switch (notificationType)
        {
            case NotificationType.Badges:
                return await badgesSettingsHelper.GetEnabledForCurrentUserAsync();
            case NotificationType.RoomsActivity:
                isEnabled = await studioNotifyHelper.IsSubscribedToNotifyAsync(_userId, serviceProvider.GetService<RoomsActivityNotifyAction>());
                return isEnabled;
            case NotificationType.DailyFeed:
                isEnabled = await studioNotifyHelper.IsSubscribedToNotifyAsync(_userId,serviceProvider.GetService<SendWhatsNewNotifyAction>() );
                return isEnabled;
            case NotificationType.UsefullTips:
                isEnabled = await studioNotifyHelper.IsSubscribedToNotifyAsync(_userId, serviceProvider.GetService<PeriodicNotifyAction>());
                return isEnabled;
            default:
                throw new Exception("Incorrect parameters");
        }
    }

    public async Task SetNotificationStatusAsync(NotificationType notificationType, bool isEnabled)
    {
        switch (notificationType)
        {
            case NotificationType.Badges:
                await badgesSettingsHelper.SetEnabledForCurrentUserAsync(isEnabled);
                break;
            case NotificationType.RoomsActivity:
                await studioNotifyHelper.SubscribeToNotifyAsync(_userId,serviceProvider.GetService<RoomsActivityNotifyAction>(), isEnabled);
                break;
            case NotificationType.DailyFeed:
                await studioNotifyHelper.SubscribeToNotifyAsync(_userId, serviceProvider.GetService<SendWhatsNewNotifyAction>(), isEnabled);
                break;
            case NotificationType.UsefullTips:
                await studioNotifyHelper.SubscribeToNotifyAsync(_userId, serviceProvider.GetService<PeriodicNotifyAction>(), isEnabled);
                break;
        }
    }
}

/// <summary>
/// The notification type.
/// </summary>
public enum NotificationType
{
    [Description("Badges")]
    Badges = 0,

    [Description("Rooms activity")]
    RoomsActivity = 1,

    [Description("Daily feed")]
    DailyFeed = 2,

    [Description("Usefull tips")]
    UsefullTips = 3
}
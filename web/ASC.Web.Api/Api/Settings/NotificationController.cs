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

namespace ASC.Web.Api.Controllers.Settings;

[DefaultRoute("notification")]
public class NotificationController(
    IFusionCache fusionCache,
    WebItemManager webItemManager,
    NotificationControllerHelper notificationControllerHelper,
    RoomsNotificationSettingsHelper roomsNotificationSettingsHelper)
: BaseSettingsController(fusionCache, webItemManager)
{
    /// <remarks>
    /// Checks if the notification type specified in the request is enabled or not.
    /// </remarks>
    /// <summary>Check notification availability</summary>
    /// <path>api/2.0/settings/notification/{type}</path>
    [Tags("Settings / Notifications")]
    [SwaggerResponse(200, "Notification settings", typeof(NotificationSettingsDto))]
    [HttpGet("{type}")]
    public async Task<NotificationSettingsDto> GetNotificationSettings(NotificationTypeRequestsDto inDto)
    {
        var isEnabled = await notificationControllerHelper.GetNotificationStatusAsync(inDto.Type);

        return new NotificationSettingsDto { Type = inDto.Type, IsEnabled = isEnabled };
    }

    /// <remarks>
    /// Enables the notification type specified in the request.
    /// </remarks>
    /// <summary>Enable notifications</summary>
    /// <path>api/2.0/settings/notification</path>
    [Tags("Settings / Notifications")]
    [SwaggerResponse(200, "Notification settings", typeof(NotificationSettingsDto))]
    [HttpPost("")]
    public async Task<NotificationSettingsDto> SetNotificationSettings(NotificationSettingsRequestsDto inDto)
    {
        await notificationControllerHelper.SetNotificationStatusAsync(inDto.Type, inDto.IsEnabled);

        return inDto.Map();
    }

    /// <remarks>
    /// Returns a list of rooms with the disabled notifications.
    /// </remarks>
    /// <summary>Get room notification settings</summary>
    /// <path>api/2.0/settings/notification/rooms</path>
    [Tags("Settings / Notifications")]
    [SwaggerResponse(200, "Room notification settings", typeof(RoomsNotificationSettingsDto))]
    [HttpGet("rooms")]
    public async Task<RoomsNotificationSettingsDto> GetRoomsNotificationSettings()
    {
        var settings = await roomsNotificationSettingsHelper.GetSettingsForCurrentUserAsync();
        return settings.Map();
    }

    /// <remarks>
    /// Sets a notification status for a room with the ID specified in the request.
    /// </remarks>
    /// <summary>Set room notification status</summary>
    /// <path>api/2.0/settings/notification/rooms</path>
    [Tags("Settings / Notifications")]
    [SwaggerResponse(200, "Room notification settings", typeof(RoomsNotificationSettingsDto))]
    [HttpPost("rooms")]
    public async Task<RoomsNotificationSettingsDto> SetRoomsNotificationStatus(RoomsNotificationsSettingsRequestDto inDto)
    {
        var settings = await roomsNotificationSettingsHelper.SetForCurrentUserAsync(inDto.RoomsId, inDto.Mute);
        return settings.Map();
    }

    /// <remarks>
    /// Returns a list of notification channels.
    /// </remarks>
    /// <summary>Get notification channels</summary>
    /// <path>api/2.0/settings/notification/channels</path>
    [Tags("Settings / Notifications")]
    [SwaggerResponse(200, "Notification settings", typeof(NotificationChannelStatusDto))]
    [HttpGet("channels")]
    public NotificationChannelStatusDto GetNotificationChannels()
    {
        var channels = notificationControllerHelper.GetNotificationChannels();
        return new NotificationChannelStatusDto
        {
            Channels = [.. channels.Select(c => new NotificationChannelDto { Name = c.Name, IsEnabled = c.IsEnabled })]
        };
    }
}
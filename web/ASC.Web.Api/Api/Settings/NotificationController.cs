﻿// (c) Copyright Ascensio System SIA 2009-2024
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

namespace ASC.Web.Api.Controllers.Settings;

[DefaultRoute("notification")]
public class NotificationController(
    ApiContext apiContext,
    IMemoryCache memoryCache,
    WebItemManager webItemManager,
    NotificationControllerHelper notificationControllerHelper,
    RoomsNotificationSettingsHelper roomsNotificationSettingsHelper,
    IMapper mapper,
    IHttpContextAccessor httpContextAccessor)
: BaseSettingsController(apiContext, memoryCache, webItemManager, httpContextAccessor)
{
    /// <summary>
    /// Checks if the notification type specified in the request is enabled or not.
    /// </summary>
    /// <short>Check notification availability</short>
    /// <path>api/2.0/settings/notification/{type}</path>
    [Tags("Settings / Notifications")]
    [SwaggerResponse(200, "Notification settings", typeof(NotificationSettingsDto))]
    [HttpGet("{type}")]
    public async Task<NotificationSettingsDto> GetNotificationSettingsAsync(NotificationTypeRequestsDto inDto)
    {
        var isEnabled = await notificationControllerHelper.GetNotificationStatusAsync(inDto.Type);

        return new NotificationSettingsDto { Type = inDto.Type, IsEnabled = isEnabled };
    }

    /// <summary>
    /// Enables the notification type specified in the request.
    /// </summary>
    /// <short>Enable notifications</short>
    /// <path>api/2.0/settings/notification</path>
    [Tags("Settings / Notifications")]
    [SwaggerResponse(200, "Notification settings", typeof(NotificationSettingsDto))]
    [HttpPost("")]
    public async Task<NotificationSettingsDto> SetSettingsAsync(NotificationSettingsRequestsDto inDto)
    {
        await notificationControllerHelper.SetNotificationStatusAsync(inDto.Type, inDto.IsEnabled);

        return mapper.Map<NotificationSettingsDto>(inDto);
    }

    /// <summary>
    /// Returns a list of rooms with the disabled notifications
    /// </summary>
    /// <short>Get room notification settings</short>
    /// <path>api/2.0/settings/notification/rooms</path>
    [Tags("Settings / Notifications")]
    [SwaggerResponse(200, "Room notification settings", typeof(RoomsNotificationSettingsDto))]
    [HttpGet("rooms")]
    public async Task<RoomsNotificationSettingsDto> GetRoomsNotificationSettings()
    {
        var  settings = await roomsNotificationSettingsHelper.GetSettingsForCurrentUserAsync();
        return mapper.Map<RoomsNotificationSettingsDto>(settings);
    }

    /// <summary>
    /// Sets a notification status for a room with the ID specified in the request.
    /// </summary>
    /// <short>Set room notification status</short>
    /// <path>api/2.0/settings/notification/rooms</path>
    [Tags("Settings / Notifications")]
    [SwaggerResponse(200, "Room notification settings", typeof(RoomsNotificationSettingsDto))]
    [HttpPost("rooms")]
    public async Task<RoomsNotificationSettingsDto> SetRoomsNotificationStatus(RoomsNotificationsSettingsRequestDto inDto)
    {
        var settings = await roomsNotificationSettingsHelper.SetForCurrentUserAsync(inDto.RoomsId, inDto.Mute);
        return mapper.Map<RoomsNotificationSettingsDto>(settings);
    }
}

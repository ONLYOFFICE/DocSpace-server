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

[ApiEndpoint(Template = "notification")]
public class NotificationController(
    IFusionCache fusionCache,
    WebItemManager webItemManager,
    NotificationControllerHelper notificationControllerHelper,
    RoomsNotificationSettingsHelper roomsNotificationSettingsHelper)
: BaseSettingsController(fusionCache, webItemManager)
{
    /// <remarks>
    /// Returns whether one kind of notification is turned on for the calling user. Set the kind in the route `type`.
    /// Kinds 0 and 1 are the new-item badges and the room activity letters. Kinds 2 and 3 are the daily feed digest
    /// and the tips letters. Any signed-in user can call it, including a guest. The call is read-only and reads only
    /// the caller's own settings. Badges are on by default. Kinds 1, 2, and 3 are off until
    /// `POST api/2.0/settings/notification` turns them on. The call returns 400 if `type` is not one of these kinds.
    /// To list the muted rooms, use `GET api/2.0/settings/notification/rooms`. To list the delivery channels, use
    /// `GET api/2.0/settings/notification/channels`.
    /// </remarks>
    /// <summary>Check notification availability</summary>
    /// <path>api/2.0/settings/notification/{type}</path>
    [Tags("Settings / Notifications")]
    [SwaggerResponse(200, "The notification kind that was asked for together with the flag that says whether it is switched on for the calling user", typeof(NotificationSettingsDto))]
    [SwaggerResponse(400, "The `type` is not one of the notification kinds 0-3")]
    [HttpGet("{type}")]
    public async Task<NotificationSettingsDto> GetNotificationSettings(NotificationTypeRequestsDto inDto)
    {
        var isEnabled = await notificationControllerHelper.GetNotificationStatusAsync(inDto.Type);

        return new NotificationSettingsDto { Type = inDto.Type, IsEnabled = isEnabled };
    }

    /// <remarks>
    /// Turns one kind of notification on or off for the calling user. Kinds 0 and 1 of `type` are the new-item badges
    /// and the room activity letters. Kinds 2 and 3 are the daily feed digest and the tips letters. Any signed-in
    /// user can call it, including a guest. It changes data only in the caller's own settings. You can repeat it
    /// with the same body. With badges off, the Files responses show `new` as 0 and mark files as muted. With room
    /// activity off, the hourly room digest and immediate room letters, such as mentions, stop. Turning off
    /// kind 2 or 3 stops the daily digest or the tips letters. The call returns the request as sent, not the stored
    /// state. If `type` is not one of these kinds, the call stores nothing but still returns it. So check the result
    /// with `GET api/2.0/settings/notification/{type}`. The call returns 400 if the body cannot be read or has no
    /// `type`. To mute a single room, use `POST api/2.0/settings/notification/rooms`.
    /// </remarks>
    /// <summary>Set notification status</summary>
    /// <path>api/2.0/settings/notification</path>
    [Tags("Settings / Notifications")]
    [SwaggerResponse(200, "The notification kind and state as they were sent in the request", typeof(NotificationSettingsDto))]
    [SwaggerResponse(400, "The request body cannot be read or has no `type`")]
    [HttpPost("")]
    public async Task<NotificationSettingsDto> SetNotificationSettings(NotificationSettingsRequestsDto inDto)
    {
        await notificationControllerHelper.SetNotificationStatusAsync(inDto.Type, inDto.IsEnabled);

        return inDto.Map();
    }

    /// <remarks>
    /// Returns the IDs of the rooms the calling user has muted, as `disabledRooms`. Any signed-in user can call it,
    /// including a guest. The call is read-only and reads only the caller's own list. An empty list means no room is
    /// muted. The list is in the order the rooms were added, without paging. It holds the IDs as
    /// `POST api/2.0/settings/notification/rooms` stored them. So it can hold numeric portal room IDs and string IDs
    /// of third-party rooms. An ID stays in the list after its room is deleted. For a muted room, the portal leaves
    /// its activity out of the hourly room digest and the daily feed. It also skips that room's immediate letters.
    /// The Files responses hide its new-item counters. Muting a room changes nothing for its other members. To turn
    /// whole kinds of notification on or off, use `POST api/2.0/settings/notification`.
    /// </remarks>
    /// <summary>Get muted rooms</summary>
    /// <path>api/2.0/settings/notification/rooms</path>
    [Tags("Settings / Notifications")]
    [SwaggerResponse(200, "The identifiers of the rooms the calling user has silenced", typeof(RoomsNotificationSettingsDto))]
    [HttpGet("rooms")]
    public async Task<RoomsNotificationSettingsDto> GetRoomsNotificationSettings()
    {
        var settings = await roomsNotificationSettingsHelper.GetSettingsForCurrentUserAsync();
        return settings.Map();
    }

    /// <remarks>
    /// Mutes or unmutes one room for the calling user. Set `mute` to true to mute the room, or to false to unmute it.
    /// Any signed-in user can call it, including a guest. It changes data only in the caller's own list. One call
    /// changes one room. A repeated call with the same body changes nothing. Name the room in `roomsId`, as a numeric
    /// portal room ID or a string ID of a third-party room. The call checks neither that the room exists nor that the
    /// caller can access it. So it stores a mistyped ID as sent. For a muted room, the portal leaves its activity out
    /// of the hourly room digest and the daily feed. It also skips that room's immediate letters. The Files responses
    /// hide its new-item counters. They stop offering the `mute` action on rooms when badges, room activity, and the
    /// daily feed are all off. This call still works then. It returns the whole updated list, like
    /// `GET api/2.0/settings/notification/rooms`.
    /// </remarks>
    /// <summary>Mute or unmute a room</summary>
    /// <path>api/2.0/settings/notification/rooms</path>
    [Tags("Settings / Notifications")]
    [SwaggerResponse(200, "The identifiers of the rooms the calling user has silenced, as the list stands after the change", typeof(RoomsNotificationSettingsDto))]
    [HttpPost("rooms")]
    public async Task<RoomsNotificationSettingsDto> SetRoomsNotificationStatus(RoomsNotificationsSettingsRequestDto inDto)
    {
        var settings = await roomsNotificationSettingsHelper.SetForCurrentUserAsync(inDto.RoomsId, inDto.Mute);
        return settings.Map();
    }

    /// <remarks>
    /// Returns the channels this installation can use to deliver notifications, each with `isEnabled`. The channel
    /// names are `email.sender` for letters and `telegram.sender` for Telegram messages. The list describes the
    /// installation and the portal, so every user gets the same answer. Any signed-in user can call it, including a
    /// guest. The call is read-only. A channel appears only if the notification service configures a sender with that
    /// name. An empty list means the configuration names no channel this build supports. The call always reports
    /// `email.sender` as enabled when it is listed. It reports `telegram.sender` as enabled only while the portal
    /// stores a Telegram bot name and token. To store them, use `POST api/2.0/settings/authservice`. An enabled
    /// channel says nothing about the caller. To get Telegram messages, a user also connects their own Telegram
    /// account. To get the link, use `GET api/2.0/settings/telegram/link`. To check the result, use
    /// `GET api/2.0/settings/telegram/check`. To read which kinds of notification a user receives, use
    /// `GET api/2.0/settings/notification/{type}`.
    /// </remarks>
    /// <summary>Get notification channels</summary>
    /// <path>api/2.0/settings/notification/channels</path>
    [Tags("Settings / Notifications")]
    [SwaggerResponse(200, "The notification channels this installation can deliver through, each with the flag that says whether it is enabled", typeof(NotificationChannelStatusDto))]
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
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
    /// Reports whether one kind of notification is switched on for the calling user, taking the kind as the integer
    /// `type` in the route: 0 the new-item badges the Files responses carry, 1 the room activity letters, 2 the daily
    /// feed digest, 3 the periodic tips letters. The answer describes the caller's own account only - there is no way
    /// to read another member's settings - and the call is read-only and safe to repeat. Every signed-in member reads
    /// its own settings: the portal owner, a DocSpace administrator, a room administrator, a user and a guest are all
    /// accepted, and no permission is demanded. Badges come back switched on for an account that has not changed
    /// them, while the kinds 1, 2 and 3 come back switched off until they are switched on with
    /// `POST api/2.0/settings/notification`. What comes back is the kind that was asked for together with
    /// `isEnabled`. A `type` outside 0-3 is not recognised and the call fails instead of falling back to a default.
    /// The rooms silenced one by one are listed by `GET api/2.0/settings/notification/rooms`, and the delivery
    /// channels of the installation by `GET api/2.0/settings/notification/channels`.
    /// </remarks>
    /// <summary>Check notification availability</summary>
    /// <path>api/2.0/settings/notification/{type}</path>
    [Tags("Settings / Notifications")]
    [SwaggerResponse(200, "The notification kind that was asked for together with the flag that says whether it is switched on for the calling user", typeof(NotificationSettingsDto))]
    [HttpGet("{type}")]
    public async Task<NotificationSettingsDto> GetNotificationSettings(NotificationTypeRequestsDto inDto)
    {
        var isEnabled = await notificationControllerHelper.GetNotificationStatusAsync(inDto.Type);

        return new NotificationSettingsDto { Type = inDto.Type, IsEnabled = isEnabled };
    }

    /// <remarks>
    /// Switches one kind of notification on or off for the calling user: send the kind as `type` - 0 the new-item
    /// badges, 1 the room activity letters, 2 the daily feed digest, 3 the periodic tips letters - together with
    /// `isEnabled`. The change touches the caller's own account only, and repeating the call with the same pair
    /// leaves the account as it is. Every signed-in member configures its own settings: the portal owner, a DocSpace
    /// administrator, a room administrator, a user and a guest are all accepted, and no permission is demanded. With
    /// 0 switched off the Files responses report `new` as 0 and mark files as muted; with 1 switched off both the
    /// hourly room digest and the letters a room sends at once, such as an editor mention, stop; with 2 switched off
    /// the daily digest stops; with 3 switched off the tips letters stop. What comes back is an echo of the request
    /// rather than a re-read of the stored state, and a `type` outside 0-3 is echoed as well while nothing is stored,
    /// so confirm the result with `GET api/2.0/settings/notification/{type}`. To silence a single room instead of a
    /// whole kind use `POST api/2.0/settings/notification/rooms`.
    /// </remarks>
    /// <summary>Set notification status</summary>
    /// <path>api/2.0/settings/notification</path>
    [Tags("Settings / Notifications")]
    [SwaggerResponse(200, "The notification kind and state as they were sent in the request", typeof(NotificationSettingsDto))]
    [HttpPost("")]
    public async Task<NotificationSettingsDto> SetNotificationSettings(NotificationSettingsRequestsDto inDto)
    {
        await notificationControllerHelper.SetNotificationStatusAsync(inDto.Type, inDto.IsEnabled);

        return inDto.Map();
    }

    /// <remarks>
    /// Returns the rooms the calling user has silenced, as the `disabledRooms` list of their identifiers. The list
    /// describes the caller's own account only, the call is read-only, and an empty list means nothing is silenced.
    /// Every signed-in member reads its own list, whatever its role - owner, administrator, user or guest - and no
    /// permission is demanded. The identifiers come back the way `POST api/2.0/settings/notification/rooms` stored
    /// them, in the order they were added and without paging; they are kept as opaque values, so both the numeric
    /// identifier of a portal room and the string identifier of a room on a connected third-party account appear
    /// here, and an identifier stays in the list after the room itself is deleted. While a room is on this list its
    /// activity is left out of the hourly room digest and of the daily feed, the letters that room would send at once
    /// are not sent, and its new-item counters are hidden from the Files responses. Silencing a room changes nothing
    /// for its other members. The kinds of notification this list is applied to are switched with
    /// `POST api/2.0/settings/notification`.
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
    /// Adds one room to the calling user's silenced list or takes it off again: `mute` true silences the room, false
    /// lets its notifications through. One call carries one room, so several rooms take several calls, and repeating
    /// a call with the same pair changes nothing. The room is named by `roomsId` and kept as an opaque value: the
    /// numeric identifier of a portal room and the string identifier of a room on a connected third-party account are
    /// both accepted, and neither the room's existence nor the caller's access to it is checked, so a mistyped
    /// identifier is stored as sent. Every signed-in member manages its own list, whatever its role, and the list of
    /// another member cannot be touched. While a room is silenced its activity is left out of the hourly room digest
    /// and of the daily feed, the letters it would send at once are not sent, and its new-item counters are hidden.
    /// The Files responses stop offering the `mute` action on a room once badges, room activity and the daily feed
    /// are all switched off, while this call keeps working. What comes back is the whole updated list, the same shape
    /// `GET api/2.0/settings/notification/rooms` returns.
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
    /// Lists the ways this installation can deliver a notification, each as the internal name of the channel together
    /// with `isEnabled`: `email.sender` for letters and `telegram.sender` for Telegram messages. The list describes
    /// the installation and the portal rather than the calling user, so every member gets the same answer, and the
    /// call is read-only. Any signed-in member may ask for it, whatever its role, and no permission is demanded. A
    /// channel appears only when the notification service of the running installation is configured with a sender of
    /// that name, so the list can be shorter than the two names above, and an empty list means that configuration
    /// names no channel this build implements. `email.sender` is reported as enabled whenever it is listed, while
    /// `telegram.sender` is reported as enabled only while the portal has a Telegram bot name and token stored, which
    /// is what `POST api/2.0/settings/authservice` writes. An enabled channel says nothing about the caller: a member
    /// also has to connect their own Telegram account, for which `GET api/2.0/settings/telegram/link` hands out the
    /// link and `GET api/2.0/settings/telegram/check` reports the outcome. Which kinds of notification a member
    /// receives is a separate setting, read with `GET api/2.0/settings/notification/{type}`.
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
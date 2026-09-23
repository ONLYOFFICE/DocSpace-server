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

using ASC.Web.Api.Controllers.Settings;

namespace ASC.Web.Api.Api.Settings;

[ApiEndpoint(Template = "push")]
public class PushController(
    WebItemManager webItemManager,
    IFusionCache fusionCache,
    FirebaseHelper firebaseHelper)
    : BaseSettingsController(fusionCache, webItemManager)
{
    /// <remarks>
    /// Registers one mobile device of the calling user for the push notifications of the Documents application, by
    /// storing the Firebase token that device was issued together with the initial `isSubscribed` state. The token is
    /// handed out by Firebase to the mobile client, so obtain it there before calling: nothing here checks it, and it
    /// is kept as an opaque string of up to 255 characters. Every signed-in member registers its own devices,
    /// whatever its role - owner, administrator, user or guest - and a registration is bound to the caller and the
    /// current portal, so another member's devices cannot be touched. The call is safe to repeat, but it is not an
    /// update: a token already registered comes back as it stands and `isSubscribed` from the request is ignored, so
    /// switch an existing registration on or off with `PUT api/2.0/settings/push/docsubscribe` instead. What comes
    /// back is the stored registration, with `application` always `doc` and `isSubscribed` as stored. Only a
    /// subscribed device is sent the room activity messages, such as an invitation to a room, a role change, an
    /// archived room or a new document in a room, and only while the installation itself is configured with Firebase
    /// credentials.
    /// </remarks>
    /// <summary>Register a push device</summary>
    /// <path>api/2.0/settings/push/docregisterdevice</path>
    [Tags("Security / Firebase")]
    [SwaggerResponse(200, "The stored device registration of the calling user, with the Firebase token, the `doc` application and the subscription state as they are kept", typeof(FireBaseUser))]
    [HttpPost("docregisterdevice")]
    public async Task<FireBaseUser> DocRegisterPusnNotificationDevice(FirebaseRequestsDto inDto)
    {
        return await firebaseHelper.RegisterUserDeviceAsync(inDto.FirebaseDeviceToken, inDto.IsSubscribed, PushConstants.PushDocAppName);
    }

    /// <remarks>
    /// Switches the push notifications of the Documents application on or off for one already registered device of
    /// the calling user: send that device's Firebase token together with `isSubscribed` true to let the messages
    /// through or false to stop them. The device has to be registered first with
    /// `POST api/2.0/settings/push/docregisterdevice`, and only the subscription state is written - the token is
    /// matched, never changed. Every signed-in member manages its own devices, whatever its role - owner,
    /// administrator, user or guest - and a token that belongs to another member or to another portal is not matched
    /// at all, so nothing of theirs can be switched. Repeating the call with the same pair leaves the registration as
    /// it is. What comes back is the updated registration, while an empty response means no registration of the
    /// caller carries that token and nothing was stored - register the device and call again. A device switched off
    /// keeps its token stored but is left out of the delivery, and the other devices of the same member are
    /// unaffected. Which kinds of notification the account receives at all is a separate setting, read with
    /// `GET api/2.0/settings/notification/{type}`.
    /// </remarks>
    /// <summary>Set push subscription</summary>
    /// <path>api/2.0/settings/push/docsubscribe</path>
    [Tags("Security / Firebase")]
    [SwaggerResponse(200, "The device registration as it stands after the change, or an empty response when no registration of the calling user carries the token that was sent", typeof(FireBaseUser))]
    [HttpPut("docsubscribe")]
    public async Task<FireBaseUser> SubscribeDocumentsPushNotification(FirebaseRequestsDto inDto)
    {
        return await firebaseHelper.UpdateUserAsync(inDto.FirebaseDeviceToken, inDto.IsSubscribed, PushConstants.PushDocAppName);
    }
}
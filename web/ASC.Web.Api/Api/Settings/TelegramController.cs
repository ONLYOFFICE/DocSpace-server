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

public class TelegramController(
    AuthContext authContext,
    WebItemManager webItemManager,
    IFusionCache fusionCache,
    TelegramHelper telegramHelper,
    TenantManager tenantManager
    )
    : BaseSettingsController(fusionCache, webItemManager)
{
    /// <remarks>
    /// Returns whether the current user's account is linked to the portal's Telegram bot. Linking needs a bot that
    /// `POST api/2.0/settings/authservice` configures for the portal. Any signed-in user can call it, including a
    /// guest. The call is read-only and covers only the caller's own account. The `status` field is `unlinked`,
    /// `linked`, or `linking`. The value `linking` means the user got a registration link but has not opened it in
    /// Telegram yet. This state ends when the user opens the link or the link expires. The `username` field is
    /// filled only when `status` is `linked`. To start linking, use `GET api/2.0/settings/telegram/link`. To remove
    /// a link, use `DELETE api/2.0/settings/telegram/link`.
    /// </remarks>
    /// <summary>Check the Telegram connection</summary>
    /// <path>api/2.0/settings/telegram/check</path>
    [Tags("Settings / Telegram")]
    [SwaggerResponse(200, "The current user's Telegram link state, with the username filled in only when linked", typeof(TelegramStatusDto))]
    [HttpGet("telegram/check")]
    public async Task<TelegramStatusDto> CheckTelegram()
    {
        var status = await telegramHelper.GetTelegramUserStatus(authContext.CurrentAccount.ID, tenantManager.GetCurrentTenantId());
        return new TelegramStatusDto
        {
            Status = status.Item1,
            Username = status.Item2
        };
    }

    /// <remarks>
    /// Returns the personal `t.me` link that connects the current user's account to the portal's Telegram bot. After
    /// linking, the portal can send the user notifications in Telegram. Linking needs a bot that
    /// `POST api/2.0/settings/authservice` configures for the portal. Without a bot name, the call returns an empty
    /// link. Any signed-in user can call it, including a guest. The call changes data. If the user has no valid registration token, it issues a new one. A repeated call
    /// returns the same link while the token is valid. The token expires in 20 minutes by default. After that, call
    /// this operation again for a new link. The user completes linking by opening the link in Telegram. Then poll
    /// `GET api/2.0/settings/telegram/check` until `status` is `linked`. To remove a link, use
    /// `DELETE api/2.0/settings/telegram/link`.
    /// </remarks>
    /// <summary>Get the Telegram link</summary>
    /// <path>api/2.0/settings/telegram/link</path>
    [Tags("Settings / Telegram")]
    [SwaggerResponse(200, "A `t.me` deep link that connects the caller's account to the portal's Telegram bot", typeof(string))]
    [HttpGet("telegram/link")]
    public async Task<string> LinkTelegram()
    {
        var currentLink = await telegramHelper.CurrentRegistrationLink(authContext.CurrentAccount.ID, tenantManager.GetCurrentTenantId());

        return string.IsNullOrWhiteSpace(currentLink)
            ? await telegramHelper.RegisterUserAsync(authContext.CurrentAccount.ID, tenantManager.GetCurrentTenantId())
            : currentLink;
    }

    /// <remarks>
    /// Deletes the link between the current user's account and the portal's Telegram bot. After this call, the
    /// portal no longer sends the user notifications in Telegram. Any signed-in user can call it, including a guest.
    /// The call deletes only the caller's own link, and other members keep theirs. The call changes data, and you can
    /// repeat it. It returns `true` both when it deleted a link and when there was none. The chat stays in the user's
    /// Telegram. The bot settings of the portal and the other notification channels do not change. To list the channels of the portal, use `GET api/2.0/settings/notification/channels`. To link the
    /// account again, use `GET api/2.0/settings/telegram/link`.
    /// </remarks>
    /// <summary>Unlink Telegram</summary>
    /// <path>api/2.0/settings/telegram/link</path>
    [Tags("Settings / Telegram")]
    [SwaggerResponse(200, "Always `true` once the caller has no link to the Telegram bot", typeof(bool))]
    [HttpDelete("telegram/link")]
    public async Task<bool> UnlinkTelegram()
    {
        await telegramHelper.DisconnectAsync(authContext.CurrentAccount.ID, tenantManager.GetCurrentTenantId());
        return true;
    }
}
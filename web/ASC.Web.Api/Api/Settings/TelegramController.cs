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
    /// Reports whether the current user's account is linked to the portal's Telegram bot, and under which Telegram
    /// username. The bot keys must be configured for the portal beforehand with `POST api/2.0/settings/authservice`;
    /// until a bot is configured, linking cannot be completed and the status never reaches the linked state. Any
    /// authenticated user may call it, and only for their own account: there is no way to read another member's
    /// Telegram status. This is a read-only, idempotent call. The returned `status` is published as a number, where
    /// `0` means the account is not linked, `1` means it is linked, and `2` means a registration link has been issued
    /// and the portal is still waiting for the user to open it in Telegram. The `username` field is filled in only in
    /// state `1` and comes back empty in the other two. Start or resume linking with
    /// `GET api/2.0/settings/telegram/link`, and drop an established link with
    /// `DELETE api/2.0/settings/telegram/link`.
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
    /// Returns the personal `t.me` deep link that connects the current user's account to the portal's Telegram bot,
    /// so that notifications can be delivered to that user in Telegram. The bot keys must be configured for the
    /// portal beforehand with `POST api/2.0/settings/authservice`; without a configured bot name the response comes
    /// back empty. Any authenticated user may call it, and the link always belongs to the caller's own account. The
    /// call mutates state: unless the user still has an outstanding registration token it issues a fresh one, so
    /// calling it twice in a row hands back the same link instead of invalidating the first. That token is
    /// short-lived (20 minutes with the default configuration), and once it has expired the operation has to be
    /// called again for a new link. Linking itself is completed in Telegram, not here, so poll
    /// `GET api/2.0/settings/telegram/check` until its `status` becomes `1`. Remove an established link with
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
    /// Removes the link between the current user's account and the portal's Telegram bot, so that this user stops
    /// receiving notifications in Telegram. Any authenticated user may call it, and only for their own account: one
    /// member cannot unlink another. Nothing has to be linked beforehand, and the call is destructive but idempotent,
    /// returning `true` both when a link was removed and when there was none to remove, so a retry after a timeout is
    /// safe. Only the portal-side link is dropped: the chat itself stays in the user's Telegram, and the portal's bot
    /// configuration is untouched, so the other members keep their own links. Re-linking is not automatic, request a
    /// new link with `GET api/2.0/settings/telegram/link` and confirm the result with
    /// `GET api/2.0/settings/telegram/check`. Delivery over the other notification channels is unaffected; the
    /// channels enabled for the portal are listed by `GET api/2.0/settings/notification/channels`.
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
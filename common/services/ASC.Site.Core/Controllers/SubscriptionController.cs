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

namespace ASC.Site.Core.Controllers;

/// <summary>
/// Subscription and unsubscription by the link of a letter, called by the site in place of its own SQL.
/// </summary>
/// <remarks>
/// An error of a database is not caught here: <see cref="CustomExceptionHandler"/> logs it and the caller gets 500.
/// </remarks>
[Scope]
[ApiController]
[Route("[controller]")]
public class SubscriptionController(
        ValidationKeyProvider validationKeyProvider,
        WorkspaceSubscriptionStore workspaceSubscriptionStore,
        ILogger<SubscriptionController> logger)
    : ControllerBase
{
    /// <summary>Unsubscribes an address, or subscribes it again, by the id of a letter link.</summary>
    /// <remarks>
    /// <c>subscribe = false</c> adds the address to <c>template_unsubscribe</c> of the Workspace database; with
    /// <c>newsOnly = false</c> it also takes the Activated flag off every DocSpace account with the address, on every
    /// portal of the EU and US regions. <c>subscribe = true</c> removes the address from <c>template_unsubscribe</c> and
    /// leaves the accounts as they are. Both flags are required.
    /// Answers 400 when a flag is missing, 404 when the id is too long or does not decrypt to a valid address, and 500
    /// when a database was not updated. A repeated call with the same body changes nothing more, so it can be repeated
    /// after a 500.
    /// </remarks>
    [HttpPost("unsubscribe")]
    [Authorize(AuthenticationSchemes = "auth:allowskip:default")]
    public async Task<ActionResult<UnsubscribeResponseDto>> Unsubscribe(UnsubscribeRequestDto inDto)
    {
        if (inDto?.Subscribe is null || inDto.NewsOnly is null)
        {
            return BadRequest();
        }

        var subscriptionId = await validationKeyProvider.DecodeSubscriptionIdAsync(inDto.UnsubscribeId);

        if (subscriptionId is null)
        {
            logger.WarningSubscriptionIdNotRecognized(inDto.UnsubscribeId?.Length ?? 0);

            return NotFound();
        }

        var action = SubscriptionRules.GetUnsubscribeAction(inDto.Subscribe.Value, inDto.NewsOnly.Value);

        switch (action)
        {
            case UnsubscribeAction.Resubscribe:
                await workspaceSubscriptionStore.RemoveUnsubscribeAsync(subscriptionId.Email);
                break;
            case UnsubscribeAction.UnsubscribeNews:
                await workspaceSubscriptionStore.AddUnsubscribeAsync(subscriptionId.Email);
                break;
            case UnsubscribeAction.UnsubscribeAll:
                // The Workspace row first: if a region fails, the repeated call finds it there and adds nothing.
                await workspaceSubscriptionStore.AddUnsubscribeAsync(subscriptionId.Email);

                // Resolved here only: building it needs the regional connection strings and, on the first build in
                // the process, connects to detect the MySQL version; the other actions must not fail on a region.
                var multiRegionProvider = HttpContext.RequestServices.GetRequiredService<MultiRegionProvider>();
                await multiRegionProvider.DeactivateUsersByEmailAsync(subscriptionId.Email);
                break;
        }

        logger.InformationUnsubscribeDone(action);

        return new UnsubscribeResponseDto(subscriptionId.Email);
    }

    /// <summary>Confirms a subscription request by the id of its confirmation letter.</summary>
    /// <remarks>
    /// Only the id of a subscription request is accepted: the JSON <c>{email, firstname, type}</c> the site encrypts
    /// with <c>/key/generateunsubscribeid</c>. The list is its <c>type</c>, or <c>Common</c> when it names none.
    /// When the address has no subscription to that list, it is written to <c>template_subscribe</c> and the result is
    /// <c>added</c>; when it has one, the address is removed from <c>template_unsubscribe</c> and the result is
    /// <c>updated</c>. So a repeated call answers <c>updated</c> and removes the address from
    /// <c>template_unsubscribe</c>, as the site did.
    /// Answers 404 when the id is too long, does not decrypt to a valid address, holds a plain address instead of a
    /// request, or names an unknown list, and 500 when the database was not updated.
    /// </remarks>
    [HttpPost("subscribe")]
    [Authorize(AuthenticationSchemes = "auth:allowskip:default")]
    public async Task<ActionResult<SubscribeResponseDto>> Subscribe(SubscribeRequestDto inDto)
    {
        var subscriptionId = await validationKeyProvider.DecodeSubscriptionIdAsync(inDto?.UnsubscribeId);

        if (subscriptionId is null)
        {
            logger.WarningSubscriptionIdNotRecognized(inDto?.UnsubscribeId?.Length ?? 0);

            return NotFound();
        }

        var type = SubscriptionRules.GetSubscribeType(subscriptionId);

        if (type is null)
        {
            logger.WarningSubscriptionIdNotARequest(subscriptionId.IsSubscriptionRequest, subscriptionId.HasUnknownType);

            return NotFound();
        }

        var added = await workspaceSubscriptionStore.AddSubscribeAsync(subscriptionId.Email, subscriptionId.FirstName, (sbyte)type.Value);

        var result = added ? SubscribeResult.Added : SubscribeResult.Updated;

        logger.InformationSubscribeDone(result, type.Value);

        return new SubscribeResponseDto(subscriptionId.Email, type.Value, result);
    }
}

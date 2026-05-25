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

[Scope]
public class RadicaleController(
    RadicaleClient radicaleClient,
    DbRadicale dbRadicale,
    CardDavAddressbook cardDavAddressbook,
    TenantManager tenantManager,
    ILogger<RadicaleController> logger,
    InstanceCrypto crypto,
    UserManager userManager,
    AuthContext authContext,
    WebItemSecurity webItemSecurity,
    IFusionCache fusionCache,
    WebItemManager webItemManager)
    : BaseSettingsController(fusionCache, webItemManager)
{
    /// <remarks>
    /// Creates a CardDav address book for a user with all portal users and returns a link to this address book.
    /// </remarks>
    /// <summary>
    /// Get the CardDav address book URL
    /// </summary>
    /// <path>api/2.0/settings/carddavurl</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Settings / CardDav address book")]
    [SwaggerResponse(200, "CardDav response", typeof(DavResponse))]
    [HttpGet("carddavurl")]
    public async Task<DavResponse> GetCardDavUrl()
    {

        if (await WebItemManager[WebItemManager.PeopleProductID].IsDisabledAsync(webItemSecurity, authContext))
        {
            await DeleteCardDavAddressBook().ConfigureAwait(false);
            throw new MethodAccessException("Method not available");
        }

        var myUri = HttpContext.Request.Url();
        var currUser = await userManager.GetUsersAsync(authContext.CurrentAccount.ID);
        var userName = currUser.Email.ToLower();
        var currentAccountPaswd = await crypto.EncryptAsync(userName);
        var cardBuilder = await CardDavAllSerializationAsync();


        var userAuthorization = userName + ":" + currentAccountPaswd;
        var rootAuthorization = await cardDavAddressbook.GetSystemAuthorizationAsync();
        var sharedCardUrl = cardDavAddressbook.GetRadicaleUrl(myUri.ToString(), userName, true, true, true);
        var getResponse = await cardDavAddressbook.GetCollection(sharedCardUrl, userAuthorization, myUri.ToString());
        if (getResponse.Completed)
        {
            return new DavResponse
            {
                Completed = true,
                Data = sharedCardUrl
            };
        }

        if (getResponse.StatusCode == 404)
        {
            var createResponse = await cardDavAddressbook.Create("", "", "", sharedCardUrl, rootAuthorization);
            if (createResponse.Completed)
            {
                try
                {
                    await dbRadicale.SaveCardDavUserAsync(tenantManager.GetCurrentTenantId(), currUser.Id);
                }
                catch (Exception ex)
                {
                    logger.ErrorWithException(ex);
                }

                await cardDavAddressbook.UpdateItem(sharedCardUrl, rootAuthorization, cardBuilder, myUri.ToString()).ConfigureAwait(false);
                return new DavResponse
                {
                    Completed = true,
                    Data = sharedCardUrl
                };
            }

            logger.Error(createResponse.Error);
            throw new RadicaleException(createResponse.Error);
        }

        logger.Error(getResponse.Error);
        throw new RadicaleException(getResponse.Error);

    }

    /// <remarks>
    /// Deletes a CardDav address book with all portal users.
    /// </remarks>
    /// <summary>
    /// Delete a CardDav address book
    /// </summary>
    /// <path>api/2.0/settings/deletebook</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Settings / CardDav address book")]
    [SwaggerResponse(200, "CardDav response", typeof(DavResponse))]
    [HttpDelete("deletebook")]
    public async Task<DavResponse> DeleteCardDavAddressBook()
    {
        var currUser = await userManager.GetUsersAsync(authContext.CurrentAccount.ID);
        var currentUserEmail = currUser.Email;
        var authorization = await cardDavAddressbook.GetSystemAuthorizationAsync();
        var myUri = HttpContext.Request.Url();
        var requestUrlBook = cardDavAddressbook.GetRadicaleUrl(myUri.ToString(), currentUserEmail, true, true);
        var tenant = tenantManager.GetCurrentTenantId();
        var davRequest = new DavRequest
        {
            Url = requestUrlBook,
            Authorization = authorization,
            Header = myUri.ToString()
        };

        await radicaleClient.RemoveAsync(davRequest).ConfigureAwait(false);

        try
        {
            await dbRadicale.RemoveCardDavUserAsync(tenant, currUser.Id);

            return new DavResponse
            {
                Completed = true
            };
        }
        catch (Exception ex)
        {
            logger.ErrorWithException(ex);
            return new DavResponse
            {
                Completed = false,
                Error = ex.Message
            };
        }


    }

    private async Task<string> CardDavAllSerializationAsync()
    {
        var builder = new StringBuilder();
        var users = await userManager.GetUsersAsync();

        foreach (var user in users)
        {
            builder.AppendLine(cardDavAddressbook.GetUserSerialization(ItemFromUserInfo(user)));
        }

        return builder.ToString();
    }

    private static CardDavItem ItemFromUserInfo(UserInfo u)
    {
        return new CardDavItem(u.Id, u.FirstName, u.LastName, u.UserName, u.BirthDate, u.Sex, u.Title, u.Email, u.ContactsList, u.MobilePhone);
    }
}
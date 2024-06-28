// (c) Copyright Ascensio System SIA 2009-2024
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

[Scope]
public class RadicaleController(RadicaleClient radicaleClient,
        DbRadicale dbRadicale,
        CardDavAddressbook cardDavAddressbook,
        TenantManager tenantManager,
        ILogger<RadicaleController> logger,
        InstanceCrypto crypto,
        UserManager userManager,
        AuthContext authContext,
        WebItemSecurity webItemSecurity,
        ApiContext apiContext,
        IMemoryCache memoryCache,
        WebItemManager webItemManager,
        IHttpContextAccessor httpContextAccessor)
    : BaseSettingsController(apiContext, memoryCache, webItemManager, httpContextAccessor)
{
    /// <summary>
    /// Creates a CardDav address book for a user with all portal users and returns a link to this address book.
    /// </summary>
    /// <short>
    /// Get a link to the CardDav address book
    /// </short>
    /// <category>CardDav address book</category>
    /// <returns type="ASC.Common.Radicale.DavResponse, ASC.Common.Radicale">CardDav response</returns>
    /// <path>api/2.0/settings/carddavurl</path>
    /// <httpMethod>GET</httpMethod>
    /// <visible>false</visible>
    [Tags("Settings / CardDav address book")]
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
                    await dbRadicale.SaveCardDavUserAsync(await tenantManager.GetCurrentTenantIdAsync(), currUser.Id);
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

    /// <summary>
    /// Deletes a CardDav address book with all portal users.
    /// </summary>
    /// <short>
    /// Delete a CardDav address book
    /// </short>
    /// <category>CardDav address book</category>
    /// <returns type="ASC.Common.Radicale.DavResponse, ASC.Common.Radicale">CardDav response</returns>
    /// <path>api/2.0/settings/deletebook</path>
    /// <httpMethod>DELETE</httpMethod>
    /// <visible>false</visible>
    [Tags("Settings / CardDav address book")]
    [HttpDelete("deletebook")]
    public async Task<DavResponse> DeleteCardDavAddressBook()
    {
        var currUser = await userManager.GetUsersAsync(authContext.CurrentAccount.ID);
        var currentUserEmail = currUser.Email;
        var authorization = await cardDavAddressbook.GetSystemAuthorizationAsync();
        var myUri = HttpContext.Request.Url();
        var requestUrlBook = cardDavAddressbook.GetRadicaleUrl(myUri.ToString(), currentUserEmail, true, true);
        var tenant = await tenantManager.GetCurrentTenantIdAsync();
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
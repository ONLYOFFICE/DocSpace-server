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

namespace ASC.People.Api;

///<summary>
/// People API.
///</summary>
public abstract class PeopleControllerBase(
    UserManager userManager,
    PermissionContext permissionContext,
    ApiContext apiContext,
    UserPhotoManager userPhotoManager,
    IHttpClientFactory httpClientFactory,
    IHttpContextAccessor httpContextAccessor)
    : ApiControllerBase
{
    protected readonly UserManager _userManager = userManager;
    protected readonly PermissionContext _permissionContext = permissionContext;
    protected readonly ApiContext _apiContext = apiContext;
    protected readonly UserPhotoManager _userPhotoManager = userPhotoManager;
    protected readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    protected readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    protected async Task<UserInfo> GetUserInfoAsync(string userNameOrId)
    {
        UserInfo user;
        try
        {
            var userId = new Guid(userNameOrId);
            user = await _userManager.GetUsersAsync(userId);
        }
        catch (FormatException)
        {
            user = await _userManager.GetUserByUserNameAsync(userNameOrId);
        }

        if (user == null || user.Id == Constants.LostUser.Id)
        {
            throw new ItemNotFoundException(Resource.ErrorUserNotFound);
        }

        return user;
    }

    protected async Task UpdateContactsAsync(IEnumerable<Contact> contacts, UserInfo user, bool checkPermissions = true)
    {
        if (checkPermissions)
        {
            await _permissionContext.DemandPermissionsAsync(new UserSecurityProvider(user.Id), Constants.Action_EditUser);
        }
        
        if (contacts == null)
        {
            return;
        }
        
        var values = contacts.Where(r => !string.IsNullOrEmpty(r.Value)).Select(r => $"{r.Type}|{r.Value}");
        user.Contacts = string.Join('|', values);
    }

    protected async Task UpdatePhotoUrlAsync(string files, UserInfo user)
    {
        if (string.IsNullOrEmpty(files))
        {
            return;
        }

        await _permissionContext.DemandPermissionsAsync(new UserSecurityProvider(user.Id), Constants.Action_EditUser);

        if (!files.StartsWith("http://") && !files.StartsWith("https://"))
        {
            files = new Uri(_httpContextAccessor.HttpContext.Request.GetDisplayUrl()).GetLeftPart(UriPartial.Authority) + "/" + files.TrimStart('/');
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, files);

#pragma warning disable CA2000
        var httpClient = _httpClientFactory.CreateClient();
#pragma warning restore CA2000
        using var response = await httpClient.SendAsync(request);
        var imageByteArray = await response.Content.ReadAsByteArrayAsync();

        await _userPhotoManager.SaveOrUpdatePhoto(user.Id, imageByteArray);
    }
}
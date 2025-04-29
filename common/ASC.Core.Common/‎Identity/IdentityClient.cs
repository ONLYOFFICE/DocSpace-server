// (c) Copyright Ascensio System SIA 2009-2025
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


using System.IdentityModel.Tokens.Jwt;

using Microsoft.IdentityModel.Tokens;

namespace ASC.Core.Common.Identity;

[Scope]
public class IdentityClient(MachinePseudoKeys machinePseudoKeys,
    TenantManager tenantManager,
    SecurityContext securityContext,
    UserManager userManager,
    BaseCommonLinkUtility baseCommonLinkUtility,
    SettingsManager settingsManager,
    UserFormatter userFormatter,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration)
{
    private string Url => configuration["web:identity:url"];

    public Task<string> GenerateJwtTokenAsync()
    {
        return GenerateJwtTokenAsync(null, Guid.Empty);
    }

    private async Task<string> GenerateJwtTokenAsync(bool? isPublic, Guid userId)
    {
        var key = new SymmetricSecurityKey(machinePseudoKeys.GetMachineConstant(256));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var tenant = tenantManager.GetCurrentTenant();
        var currentUserId = securityContext.CurrentAccount.ID;
        var userInfo = await userManager.GetUsersAsync(currentUserId);

        var type = userId == Guid.Empty ? await userManager.GetUserTypeAsync(currentUserId) : await userManager.GetUserTypeAsync(userId);
        var isAdmin = type is EmployeeType.DocSpaceAdmin;
        var isGuest = type is EmployeeType.Guest;

        var serverRootPath = baseCommonLinkUtility.ServerRootPath;

        if (!isPublic.HasValue) 
        {
            var tenantDevToolsAccessSettings = await settingsManager.LoadAsync<TenantDevToolsAccessSettings>();

            if (tenantDevToolsAccessSettings != null)
            {
                isPublic = !tenantDevToolsAccessSettings.LimitedAccessForUsers;
            }
        }

        var token = new JwtSecurityToken(
        issuer: serverRootPath,
            audience: serverRootPath,
        claims: new List<Claim>() {
                new("sub", securityContext.CurrentAccount.ID.ToString()),
                new("user_id", securityContext.CurrentAccount.ID.ToString()),
                new("user_name", userFormatter.GetUserName(userInfo)),
                new("user_email", userInfo.Email),
                new("tenant_id", tenant.Id.ToString()),
                new("tenant_url", serverRootPath),
                new("is_admin", isAdmin.ToString().ToLower()),
                new("is_guest", isGuest.ToString().ToLower()),
                new("is_public", isPublic.ToString().ToLower()) // TODO: check OAuth enable for non-admin users
            },
            expires: DateTime.Now.AddMinutes(5),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task DeleteClientsAsync(Guid userId, string url)
    {
        var jwt = await GenerateJwtTokenAsync(true, userId);
        var httpClient = httpClientFactory.CreateClient();

        var request = new HttpRequestMessage
        {
            RequestUri = new Uri(baseCommonLinkUtility.GetFullAbsolutePath(Url + "/api/2.0/clients")),
            Method = HttpMethod.Delete
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        var response = await httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(response.ReasonPhrase);
        }
    }

    public async Task DeleteTenantClientsAsync()
    {
        var jwt = await GenerateJwtTokenAsync(true, Guid.Empty);
        var httpClient = httpClientFactory.CreateClient();

        var request = new HttpRequestMessage
        {
            RequestUri = new Uri(baseCommonLinkUtility.GetFullAbsolutePath(Url + "/api/2.0/clients/tenant")),
            Method = HttpMethod.Delete
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        var response = await httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(response.ReasonPhrase);
        }
    }
}

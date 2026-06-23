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
    private string Url
    {
        get
        {
            var serverRootPath = baseCommonLinkUtility.ServerRootPath;
            var authority = configuration["core:oidc:authority"];

            if (string.IsNullOrEmpty(authority))
            {
                authority = "/oauth2";
            }

            if (!Uri.IsWellFormedUriString(authority, UriKind.Absolute))
            {
                authority = $"{serverRootPath}/api/2.0{authority}";
            }

            return authority.TrimEnd('/') + "/";
        }
    }

    public Task<string> GenerateJwtTokenAsync()
    {
        return GenerateJwtTokenAsync(null, Guid.Empty);
    }

    public async Task<string> GenerateJwtTokenAsync(bool? isPublic, Guid userId)
    {
        var key = new SymmetricSecurityKey(machinePseudoKeys.GetMachineConstant(256));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var tenant = tenantManager.GetCurrentTenant();

        var effectiveUserId = userId == Guid.Empty
            ? securityContext.CurrentAccount.ID
            : userId;

        var userInfo = await userManager.GetUsersAsync(effectiveUserId);
        var type = await userManager.GetUserTypeAsync(effectiveUserId);
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
        claims: new List<Claim> {
                new("sub", effectiveUserId.ToString()),
                new("user_id", effectiveUserId.ToString()),
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

    public async Task DeleteClientsAsync(Guid userId)
    {
        if (!string.IsNullOrEmpty(Url))
        {

            var jwt = await GenerateJwtTokenAsync(true, userId);
#pragma warning disable CA2000
            var httpClient = httpClientFactory.CreateClient();
#pragma warning restore CA2000

            using var request = new HttpRequestMessage(HttpMethod.Delete, Url + "clients");

            request.Headers.Add("x-signature", jwt);
            using var response = await httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(response.ReasonPhrase);
            }
        }
    }

    public async Task DeleteTenantClientsAsync(bool throwIfNotSuccess = true)
    {
        if (!string.IsNullOrEmpty(Url))
        {
            var jwt = await GenerateJwtTokenAsync(true, Guid.Empty);
#pragma warning disable CA2000
            var httpClient = httpClientFactory.CreateClient();
#pragma warning restore CA2000

            using var request = new HttpRequestMessage(HttpMethod.Delete, (Url + "clients/tenant"));
            request.Headers.Add("x-signature", jwt);
            using var response = await httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode && throwIfNotSuccess)
            {
                throw new InvalidOperationException(response.ReasonPhrase);
            }
        }
    }
}

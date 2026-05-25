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

using SecurityContext = ASC.Core.SecurityContext;

namespace ASC.Api.Core.Auth;

public partial class BasicAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    UserManager userManager,
    SecurityContext securityContext,
    PasswordHasher passwordHasher)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        Response.Headers.Append("WWW-Authenticate", "Basic");

        if (!Request.Headers.ContainsKey("Authorization"))
        {
            return AuthenticateResult.Fail("Authorization header missing.");
        }

        // Get authorization key
        var authorizationHeader = Request.Headers.Authorization.ToString();
        var authHeaderRegex = BasicRegex();

        if (!authHeaderRegex.IsMatch(authorizationHeader))
        {
            return AuthenticateResult.Fail("Authorization code not formatted properly.");
        }

        var authBase64 = Encoding.UTF8.GetString(Convert.FromBase64String(authHeaderRegex.Replace(authorizationHeader, "$1")));
        var authSplit = authBase64.Split(Convert.ToChar(":"), 2);
        var authUsername = authSplit[0];
        var authPassword = authSplit.Length > 1 ? authSplit[1] : throw new Exception("Unable to get password");

        try
        {
            var userInfo = await userManager.GetUserByEmailAsync(authUsername);
            var passwordHash = passwordHasher.GetClientPassword(authPassword);

            var claims = new List<Claim>
            {
                AuthConstants.Claim_ScopeGlobalWrite
            };

            await securityContext.AuthenticateMeAsync(userInfo.Email, passwordHash, null, claims);
        }
        catch (Exception)
        {
            return AuthenticateResult.Fail("The username or password is not correct.");
        }

        return AuthenticateResult.Success(new AuthenticationTicket(Context.User, Scheme.Name));
    }

    [GeneratedRegex(@"Basic (.*)")]
    private static partial Regex BasicRegex();
}
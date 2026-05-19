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

using Constants = ASC.Core.Configuration.Constants;
using Role = ASC.Common.Security.Authorizing.Role;

namespace ASC.Api.Core.Auth;

public class AuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration,
    ILogger<AuthHandler> log,
    ApiSystemHelper apiSystemHelper,
    MachinePseudoKeys machinePseudoKeys,
    IHttpContextAccessor httpContextAccessor)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Convert.ToBoolean(configuration[Scheme.Name] ?? "false"))
        {
            log.LogDebug("Auth for {SchemeName} skipped", Scheme.Name);
            Authenticate();
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(Context.User, new AuthenticationProperties(), Scheme.Name)));
        }

        try
        {
            Context.Request.Headers.TryGetValue("Authorization", out var headers);

            string header = headers;

            if (string.IsNullOrEmpty(header))
            {
                log.LogDebug("Auth header is NULL");

                return Task.FromResult(AuthenticateResult.Fail(new AuthenticationException(nameof(HttpStatusCode.Unauthorized))));
            }

            var substring = "ASC";

            if (header.StartsWith(substring, StringComparison.InvariantCultureIgnoreCase))
            {
                var splitted = header[substring.Length..].Trim().Split(':', StringSplitOptions.RemoveEmptyEntries);

                if (splitted.Length < 3)
                {
                    log.LogDebug("Auth failed: invalid token {Header}", header);

                    return Task.FromResult(AuthenticateResult.Fail(new AuthenticationException(nameof(HttpStatusCode.Unauthorized))));
                }

                var pkey = splitted[0];
                var date = splitted[1];
                var origHash = splitted[2];

                log.LogDebug("Variant of correct auth: {AuthToken}", apiSystemHelper.CreateAuthToken(pkey));

                if (!string.IsNullOrWhiteSpace(date))
                {
                    var timestamp = DateTime.ParseExact(date, "yyyyMMddHHmmss", CultureInfo.InvariantCulture);

                    var trustInterval = TimeSpan.FromMinutes(Convert.ToDouble(configuration["auth:trust-interval"] ?? "5"));

                    if (DateTime.UtcNow > timestamp.Add(trustInterval))
                    {
                        log.LogDebug("Auth failed: invalid timestamp {Timestamp}, now {Date}", timestamp, DateTime.UtcNow);

                        return Task.FromResult(AuthenticateResult.Fail(new AuthenticationException(nameof(HttpStatusCode.Forbidden))));
                    }
                }

                var sKey = machinePseudoKeys.GetMachineConstant();
                using var hasher = new HMACSHA1(sKey);
                var data = string.Join("\n", date, pkey);
                var hash = hasher.ComputeHash(Encoding.UTF8.GetBytes(data));

                if (WebEncoders.Base64UrlEncode(hash) != origHash && Convert.ToBase64String(hash) != origHash)
                {
                    log.LogDebug("Auth failed: invalid token {Token}, expect {Hash} or {Base64Hash}", origHash, WebEncoders.Base64UrlEncode(hash), Convert.ToBase64String(hash));

                    return Task.FromResult(AuthenticateResult.Fail(new AuthenticationException(nameof(HttpStatusCode.Forbidden))));
                }
            }
            else
            {
                log.LogDebug("Auth failed: invalid auth header. Scheme: {SchemeName}, parameter: {Header}", Scheme.Name, header);

                return Task.FromResult(AuthenticateResult.Fail(new AuthenticationException(nameof(HttpStatusCode.Forbidden))));
            }
        }
        catch (Exception ex)
        {
            log.LogError(ex, "auth error");

            return Task.FromResult(AuthenticateResult.Fail(new AuthenticationException(nameof(HttpStatusCode.InternalServerError))));
        }

        var identity = new ClaimsIdentity(Scheme.Name);

        log.LogInformation("Auth success {SchemeName}", Scheme.Name);

        httpContextAccessor?.HttpContext?.User = new CustomClaimsPrincipal(new ClaimsIdentity(Scheme.Name), identity);

        Authenticate();

        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(Context.User, new AuthenticationProperties(), Scheme.Name)));
    }

    private void Authenticate()
    {
        var account = Constants.SystemAccounts.FirstOrDefault(a => a.ID == Constants.CoreSystem.ID);

        if (account == null)
        {
            return;
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Sid, account.ID.ToString()),
            new(ClaimTypes.Name, account.Name),
            new(ClaimTypes.Role, Role.System)
        };


        httpContextAccessor.HttpContext?.User = new CustomClaimsPrincipal(new ClaimsIdentity(account, claims), account);
    }
}
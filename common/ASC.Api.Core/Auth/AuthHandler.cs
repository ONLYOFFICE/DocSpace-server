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

            var header = headers.FirstOrDefault();

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
        
        if (httpContextAccessor?.HttpContext != null)
        {
            httpContextAccessor.HttpContext.User = new CustomClaimsPrincipal(new ClaimsIdentity(Scheme.Name), identity);
        }

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


        if (httpContextAccessor.HttpContext != null)
        {
            httpContextAccessor.HttpContext.User = new CustomClaimsPrincipal(new ClaimsIdentity(account, claims), account);
        }
    }
}

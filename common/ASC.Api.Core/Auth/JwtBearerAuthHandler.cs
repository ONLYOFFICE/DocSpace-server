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

using ASC.Core.Common;

using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

using SecurityContext = ASC.Core.SecurityContext;

namespace ASC.Api.Core.Auth;

[Scope]
public class JwtBearerAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory loggerFactory,
    ILogger<JwtBearerAuthHandler> logger,
    UrlEncoder encoder,
    SecurityContext securityContext,
    BaseCommonLinkUtility baseCommonLinkUtility,
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, loggerFactory, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var serverRootPath = baseCommonLinkUtility.ServerRootPath;
        var authority = configuration["core:oidc:authority"];

        if (string.IsNullOrEmpty(authority))
        {
            authority = "/oauth2";
        }

        if (!Uri.IsWellFormedUriString(authority, UriKind.Absolute))
        {
            authority = $"{serverRootPath}{authority}";
        }

        var httpDocumentRetriever = new HttpDocumentRetriever();

        var showPIIEnable = bool.TryParse(configuration["core:oidc:showPII"], out var showPII) && showPII;
        var requireHttpsEnable = bool.TryParse(configuration["core:oidc:requireHttps"], out var requireHttps) && requireHttps;

        IdentityModelEventSource.ShowPII = showPIIEnable;
        httpDocumentRetriever.RequireHttps = requireHttpsEnable;

        var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(authority + "/.well-known/openid-configuration",
                                                                                        new OpenIdConnectConfigurationRetriever(),
                                                                                        httpDocumentRetriever);

        var accessToken = Request.Headers.Authorization.ToString();

        if (string.IsNullOrEmpty(accessToken))
        {
            throw new AuthenticationException(nameof(HttpStatusCode.Unauthorized));
        }

        accessToken = accessToken.Trim();

        if (0 <= accessToken.IndexOf("Bearer", 0, StringComparison.Ordinal))
        {
            accessToken = accessToken["Bearer ".Length..];
        }

        JwtSecurityToken validatedToken;

        var validationTokenEnable = !(bool.TryParse(configuration["core:oidc:disableValidateToken"], out var disableValidateToken) && disableValidateToken);

        if (validationTokenEnable)
        {
            validatedToken = await ValidateToken(accessToken, authority, serverRootPath, configurationManager);
        }
        else
        {
            logger.WarningDisableTokenValidation();

            validatedToken = new JwtSecurityToken(accessToken);
        }

        if (validatedToken == null)
        {
            return AuthenticateResult.Fail(new AuthenticationException(nameof(HttpStatusCode.Unauthorized)));
        }

        var subject = validatedToken.Subject;

        if (string.IsNullOrEmpty(subject) || !Guid.TryParse(subject, out var userId))
        {
            throw new AuthenticationException("Claim 'sub' is not present in JWT");
        }

        try
        {
            await securityContext.AuthenticateMeWithoutCookieAsync(userId, validatedToken.Claims.ToList());
        }
        catch (Exception ex)
        {
            return AuthenticateResult.Fail(new AuthenticationException(nameof(HttpStatusCode.Unauthorized), ex));
        }

        return AuthenticateResult.Success(new AuthenticationTicket(Context.User, Scheme.Name));
    }

    private async Task<JwtSecurityToken> ValidateToken(string token,
                                                              string issuer,
                                                              string audience,
                                                              ConfigurationManager<OpenIdConnectConfiguration> configurationManager)
    {
        ArgumentNullException.ThrowIfNull(token);
        ArgumentNullException.ThrowIfNull(issuer);
        ArgumentNullException.ThrowIfNull(audience);

        logger.TraceValidateTokenInfo(token, issuer, audience);

        var discoveryDocument = await configurationManager.GetConfigurationAsync();
        var signingKeys = discoveryDocument.SigningKeys;

        var validationParameters = new TokenValidationParameters
        {
            RequireExpirationTime = true,
            ValidateLifetime = true,

            RequireSignedTokens = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = signingKeys,

            RequireAudience = true,
            ValidateAudience = true,
            ValidAudience = audience,

            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidAlgorithms = [SecurityAlgorithms.EcdsaSha256, SecurityAlgorithms.RsaSha256],

            ClockSkew = TimeSpan.FromMinutes(2)
        };

        try
        {
            _ = new JwtSecurityTokenHandler()
                                 .ValidateToken(token, validationParameters, out var rawValidatedToken);
#pragma warning disable CA2000
            var httpClient = httpClientFactory.CreateClient();
#pragma warning restore CA2000

            var response = await httpClient.IntrospectTokenAsync(new TokenIntrospectionRequest
            {
                Address = discoveryDocument.IntrospectionEndpoint,
                Token = token
            });

            if (!response.IsActive)
            {
                throw new SecurityTokenValidationException("Response from IntrospectionEndpoint: 'Token is not active'.");
            }

            return (JwtSecurityToken)rawValidatedToken;
        }
        catch (SecurityTokenValidationException ex)
        {
            logger.InformationTokenValidationException(ex);

            return null;
        }
    }
}
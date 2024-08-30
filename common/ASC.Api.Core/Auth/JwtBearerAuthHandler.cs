// (c) Copyright Ascensio System SIA 2010-2023
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


using System.Net.Http.Json;

using ASC.Core.Common;

using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

using SecurityContext = ASC.Core.SecurityContext;

namespace ASC.Api.Core.Auth;

[Scope]
public class JwtBearerAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly SecurityContext _securityContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;
    private readonly BaseCommonLinkUtility _linkUtility;
    private readonly ILogger<JwtBearerAuthHandler> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public JwtBearerAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        SecurityContext securityContext,
        IHttpContextAccessor httpContextAccessor,
        BaseCommonLinkUtility baseCommonLinkUtility,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
        : base(options, logger, encoder)
    {
        _securityContext = securityContext;
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
        _linkUtility = baseCommonLinkUtility;
        _logger = logger.CreateLogger<JwtBearerAuthHandler>();
        _httpClientFactory = httpClientFactory;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var serverRootPath = _linkUtility.ServerRootPath;
        var authority = _configuration["core:oidc:authority"];

        if (string.IsNullOrEmpty(authority))
        {
            authority = "/oauth2";
        }

        if (!Uri.IsWellFormedUriString(authority, UriKind.Absolute))
        {
            authority = $"{serverRootPath}{authority}";
        }

        var audience = serverRootPath;
        var httpDocumentRetriever = new HttpDocumentRetriever();

        var showPIIEnable = bool.TryParse(_configuration["core:oidc:showPII"], out var showPII) && showPII;
        var requireHttpsEnable = bool.TryParse(_configuration["core:oidc:requireHttps"], out var requireHttps) && requireHttps;

        IdentityModelEventSource.ShowPII = showPIIEnable;
        httpDocumentRetriever.RequireHttps = requireHttpsEnable;

        var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(authority + "/.well-known/openid-configuration",
                                                                                        new OpenIdConnectConfigurationRetriever(),
                                                                                        httpDocumentRetriever);

        var accessToken = _httpContextAccessor?.HttpContext?.Request.Headers["Authorization"].ToString();

        if (string.IsNullOrEmpty(accessToken))
        {
            throw new AuthenticationException(nameof(HttpStatusCode.Unauthorized));
        }

        accessToken = accessToken.Trim();

        if (0 <= accessToken.IndexOf("Bearer", 0))
        {
            accessToken = accessToken["Bearer ".Length..];
        }

        JwtSecurityToken validatedToken;

        var validationTokenEnable = !(bool.TryParse(_configuration["core:oidc:disableValidateToken"], out var disableValidateToken) && disableValidateToken);

        if (validationTokenEnable)
        {
            validatedToken = await ValidateToken(accessToken, authority, audience, configurationManager);
        }
        else
        {
            _logger.WarningDisableTokenValidation();

            validatedToken = new JwtSecurityToken(accessToken);
        }

        if (validatedToken == null)
        {
            return AuthenticateResult.Fail(new AuthenticationException(nameof(HttpStatusCode.Unauthorized)));
        }

        var subject = validatedToken.Subject;

        if (String.IsNullOrEmpty(subject) || !Guid.TryParse(subject, out var userId))
        {
            throw new AuthenticationException($"Claim 'sub' is not present in JWT");
        }

        try
        {
            await _securityContext.AuthenticateMeWithoutCookieAsync(userId, validatedToken.Claims.ToList());
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

        _logger.TraceValidateTokenInfo(token, issuer, audience);

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

            ClockSkew = TimeSpan.FromMinutes(2),
        };

        try
        {

            var principal = new JwtSecurityTokenHandler()
                                 .ValidateToken(token, validationParameters, out var rawValidatedToken);

            var httpClient = _httpClientFactory.CreateClient();

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
            _logger.InformationTokenValidationException(ex);

            return null;
        }
    }  
}
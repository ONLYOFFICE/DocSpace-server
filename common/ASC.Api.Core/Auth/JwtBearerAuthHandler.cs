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


using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Protocols;
using SecurityContext = ASC.Core.SecurityContext;
using Microsoft.IdentityModel.Logging;

namespace ASC.Api.Core.Auth;

[Scope]
public class JwtBearerAuthHandler: AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly SecurityContext _securityContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly TenantManager _tenantManager;
    private readonly CoreSettings _coreSettings;
    private readonly IConfiguration _configuration;

    public JwtBearerAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        SecurityContext securityContext,
        IHttpContextAccessor httpContextAccessor,
        TenantManager tenantManager,
        CoreSettings coreSettings,
        IConfiguration configuration)
        : base(options, logger, encoder)
    {
        _securityContext = securityContext;
        _httpContextAccessor = httpContextAccessor;
        _tenantManager = tenantManager;
        _coreSettings = coreSettings;
        _configuration = configuration;
    }
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var tenant = await _tenantManager.GetCurrentTenantAsync();
        var tenantDomain = tenant.GetTenantDomain(_coreSettings);
        var authority = _configuration["core:oidc:authority"] ?? "/oauth2";


        if (!Uri.IsWellFormedUriString(authority, UriKind.Absolute))
        {
            authority = $"{tenantDomain}{authority}";
        }

        var audience = tenantDomain;
        var httpDocumentRetriever = new HttpDocumentRetriever();
        

#if DEBUG
        IdentityModelEventSource.ShowPII = true;
        httpDocumentRetriever.RequireHttps = false;
#endif

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

        var validatedToken = await ValidateToken(accessToken, authority, audience, configurationManager);

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
        catch (Exception)
        {
            return AuthenticateResult.Fail(new AuthenticationException(nameof(HttpStatusCode.Unauthorized)));
        }


        return AuthenticateResult.Success(new AuthenticationTicket(Context.User, Scheme.Name));
    }

    private static async Task<JwtSecurityToken> ValidateToken(string token,
                                                              string issuer,
                                                              string audience,
                                                              IConfigurationManager<OpenIdConnectConfiguration> configurationManager,
                                                              CancellationToken ct = default(CancellationToken))
    {
        if (string.IsNullOrEmpty(token)) throw new ArgumentNullException(nameof(token));
        if (string.IsNullOrEmpty(issuer)) throw new ArgumentNullException(nameof(issuer));
        if (string.IsNullOrEmpty(audience)) throw new ArgumentNullException(nameof(audience));

        var discoveryDocument = await configurationManager.GetConfigurationAsync(ct);
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
            ValidAlgorithms = new[] { SecurityAlgorithms.EcdsaSha256 },

            ClockSkew = TimeSpan.FromMinutes(2),
        };

        try
        {

            var principal = new JwtSecurityTokenHandler()
                                 .ValidateToken(token, validationParameters, out var rawValidatedToken);

            return (JwtSecurityToken)rawValidatedToken;

        }
        catch (SecurityTokenValidationException)
        {
            return null;
        }
    }

}
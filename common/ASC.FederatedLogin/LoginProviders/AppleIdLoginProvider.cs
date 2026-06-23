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

using Microsoft.Net.Http.Headers;

namespace ASC.FederatedLogin.LoginProviders;

[Scope]
public class AppleIdLoginProvider : BaseLoginProvider<AppleIdLoginProvider>
{
    private const string AppleUrlKeys = "https://appleid.apple.com/auth/keys";
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly RequestHelper _requestHelper;

    public override string AccessTokenUrl => "https://appleid.apple.com/auth/token";
    public override string RedirectUri => this["appleIdRedirectUrl"];
    public override string ClientID => IsApp() ? this["appleIdClientIdMobile"] : this["appleIdClientId"];
    public override string ClientSecret => GenerateSecret();
    public override string CodeUrl => "https://appleid.apple.com/auth/authorize";
    public override string Scopes => "";

    private string TeamId => this["appleIdTeamId"];
    private string KeyId => this["appleIdKeyId"];
    private string PrivateKey => this["appleIdPrivateKey"];

    public AppleIdLoginProvider() { }
    public AppleIdLoginProvider(
        OAuth20TokenHelper oAuth20TokenHelper,
        TenantManager tenantManager,
        CoreBaseSettings coreBaseSettings,
        CoreSettings coreSettings,
        IConfiguration configuration,
        ICacheNotify<ConsumerCacheItem> cache,
        ConsumerFactory consumerFactory,
        IHttpContextAccessor httpContextAccessor,
        RequestHelper requestHelper,
        string name, int order, bool paid, Dictionary<string, string> props, Dictionary<string, string> additional = null)
            : base(oAuth20TokenHelper, tenantManager, coreBaseSettings, coreSettings, configuration, cache, consumerFactory, name, order, paid, props, additional)
    {
        _httpContextAccessor = httpContextAccessor;
        _requestHelper = requestHelper;
    }

    public override LoginProfile ProcessAuthorization(HttpContext context, IDictionary<string, string> @params, IDictionary<string, string> additionalStateArgs)
    {
        try
        {
            var token = Auth(context, out _, @params, additionalStateArgs);
            var claims = ValidateIdToken(JObject.Parse(token.OriginJson).Value<string>("id_token"));
            return GetProfileFromClaims(claims);
        }
        catch (ThreadAbortException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new LoginProfile(ex);
        }
    }

    public override LoginProfile GetLoginProfile(string authCode)
    {
        if (string.IsNullOrEmpty(authCode))
        {
            throw new Exception("Login failed");
        }

        var token = _oAuth20TokenHelper.GetAccessToken<AppleIdLoginProvider>(authCode);
        var claims = ValidateIdToken(JObject.Parse(token.OriginJson).Value<string>("id_token"));
        return GetProfileFromClaims(claims);
    }
    public override LoginProfile GetLoginProfile(OAuth20Token token)
    {
        if (token == null)
        {
            throw new Exception("Login failed");
        }

        var claims = ValidateIdToken(JObject.Parse(token.OriginJson).Value<string>("id_token"));
        return GetProfileFromClaims(claims);
    }

    private LoginProfile GetProfileFromClaims(ClaimsPrincipal claims)
    {
        return new LoginProfile
        {
            Id = claims.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            EMail = claims.FindFirst(ClaimTypes.Email)?.Value,
            Provider = ProviderConstants.AppleId
        };
    }

    private string GenerateSecret()
    {
#pragma warning disable CA2000
        var ecdsa = ECDsa.Create();
#pragma warning restore CA2000
        ecdsa.ImportPkcs8PrivateKey(Convert.FromBase64String(PrivateKey), out _);

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateJwtSecurityToken(
            issuer: TeamId,
            audience: "https://appleid.apple.com",
            subject: new ClaimsIdentity(new List<Claim> { new("sub", ClientID) }),
            issuedAt: DateTime.UtcNow,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(new ECDsaSecurityKey(ecdsa), SecurityAlgorithms.EcdsaSha256)
        );

        token.Header.Add("kid", KeyId);

        return handler.WriteToken(token);
    }

    private ClaimsPrincipal ValidateIdToken(string idToken)
    {
        var handler = new JwtSecurityTokenHandler();
        var claims = handler.ValidateToken(idToken, new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = GetApplePublicKeys(),

            ValidateIssuer = true,
            ValidIssuer = "https://appleid.apple.com",

            ValidateAudience = true,
            ValidAudience = ClientID,

            ValidateLifetime = true

        }, out _);

        return claims;
    }

    private List<SecurityKey> GetApplePublicKeys()
    {
        var applePublicKeys = _requestHelper.PerformRequest(AppleUrlKeys);

        var keys = new List<SecurityKey>();
        foreach (var webKey in JObject.Parse(applePublicKeys).Value<JArray>("keys"))
        {
            var e = Base64UrlEncoder.DecodeBytes(webKey.Value<string>("e"));
            var n = Base64UrlEncoder.DecodeBytes(webKey.Value<string>("n"));

            var key = new RsaSecurityKey(new RSAParameters { Exponent = e, Modulus = n })
            {
                KeyId = webKey.Value<string>("kid")
            };

            keys.Add(key);
        }

        return keys;
    }

    private bool IsApp()
    {
        if (this["appleIdClientIdMobile"] == null || _httpContextAccessor?.HttpContext == null)
        {
            return false;
        }

        var request = _httpContextAccessor.HttpContext.Request;
        return !string.IsNullOrEmpty(request.Headers[HeaderNames.UserAgent])
            && request.Headers[HeaderNames.UserAgent].ToString().Contains(this["appleIdClientIdMobile"], StringComparison.InvariantCultureIgnoreCase);
    }
}

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

using System.Globalization;

namespace ASC.FederatedLogin.LoginProviders;

[Scope]
public class GosUslugiLoginProvider : BaseLoginProvider<GosUslugiLoginProvider>
{
    public string BaseDomain => this["gosUslugiDomain"];
    public override string CodeUrl => BaseDomain + "/aas/oauth2/ac";
    public override string AccessTokenUrl => BaseDomain + "/aas/oauth2/te";
    public override string ClientID => this["gosUslugiClientId"];
    public override string ClientSecret => this["gosUslugiCert"];
    public override string RedirectUri => this["gosUslugiRedirectUrl"];
    public override string Scopes => "fullname birthdate gender email";
    private string GosUslugiProfileUrl => BaseDomain + "/rs/prns/";
    private readonly RequestHelper _requestHelper;

    public GosUslugiLoginProvider() { }

    public GosUslugiLoginProvider(
        OAuth20TokenHelper oAuth20TokenHelper,
        TenantManager tenantManager,
        CoreBaseSettings coreBaseSettings,
        CoreSettings coreSettings,
        IConfiguration configuration,
        ICacheNotify<ConsumerCacheItem> cache,
        ConsumerFactory consumerFactory,
        RequestHelper requestHelper,
        string name, int order, bool paid, Dictionary<string, string> props, Dictionary<string, string> additional = null)
        : base(oAuth20TokenHelper, tenantManager, coreBaseSettings, coreSettings, configuration, cache, consumerFactory, name, order, paid, props, additional)
    {
        _requestHelper = requestHelper;
    }

    public override LoginProfile ProcessAuthorization(HttpContext context, IDictionary<string, string> @params, IDictionary<string, string> additionalStateArgs)
    {
        try
        {
            var token = Auth(context, out var redirect);

            if (redirect)
            {
                return null;
            }

            if (token == null)
            {
                throw new Exception("Login failed");
            }

            return GetLoginProfile(token.AccessToken);
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

    public override LoginProfile GetLoginProfile(string accessToken)
    {
        var tokenPayloadString = JsonWebToken.Decode(accessToken);
        var tokenPayload = JObject.Parse(tokenPayloadString);
        if (tokenPayload == null)
        {
            throw new Exception("Payload is incorrect");
        }

        var oid = tokenPayload.Value<string>("urn:esia:sbj_id");

        var userInfoString = _requestHelper.PerformRequest(GosUslugiProfileUrl + oid, "application/x-www-form-urlencoded", headers: new Dictionary<string, string> { { "Authorization", "Bearer " + accessToken } });
        var userInfo = JObject.Parse(userInfoString);
        if (userInfo == null)
        {
            throw new Exception("userinfo is incorrect");
        }

        var profile = new LoginProfile
        {
            Id = oid,
            FirstName = userInfo.Value<string>("firstName"),
            LastName = userInfo.Value<string>("lastName"),

            Provider = ProviderConstants.GosUslugi
        };

        var userContactsString = _requestHelper.PerformRequest(GosUslugiProfileUrl + oid + "/ctts", "application/x-www-form-urlencoded", headers: new Dictionary<string, string> { { "Authorization", "Bearer " + accessToken } });
        var userContacts = JObject.Parse(userContactsString);
        if (userContacts == null)
        {
            throw new Exception("usercontacts is incorrect");
        }

        var contactElements = userContacts.Value<JArray>("elements");
        if (contactElements == null)
        {
            throw new Exception("usercontacts elements is incorrect");
        }

        foreach (var contactElement in contactElements.ToObject<List<string>>())
        {
            var userContactString = _requestHelper.PerformRequest(contactElement, "application/x-www-form-urlencoded", headers: new Dictionary<string, string> { { "Authorization", "Bearer " + accessToken } });

            var userContact = JObject.Parse(userContactString);
            if (userContact == null)
            {
                throw new Exception("usercontacts is incorrect");
            }

            var type = userContact.Value<string>("type");
            if (type != "EML")
            {
                continue;
            }

            profile.EMail = userContact.Value<string>("value");
            break;
        }

        return profile;
    }

    protected override OAuth20Token Auth(HttpContext context, out bool redirect, IDictionary<string, string> additionalArgs = null, IDictionary<string, string> additionalStateArgs = null)
    {
        var error = context.Request.Query["error"];
        if (!string.IsNullOrEmpty(error))
        {
            if (error == "access_denied")
            {
                error = "Canceled at provider";
            }

            throw new Exception(error);
        }

        var code = context.Request.Query["code"];
        if (string.IsNullOrEmpty(code))
        {
            RequestCode(context);
            redirect = true;

            return null;
        }

        redirect = false;
        var state = context.Request.Query["state"];

        return GetAccessToken(state, code);
    }

    private void RequestCode(HttpContext context)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy.MM.dd HH:mm:ss +0000", CultureInfo.InvariantCulture);
        var state = Guid.NewGuid().ToString();//HttpContext.Current.Request.Url().AbsoluteUri;
        var msg = Scopes + timestamp + ClientID + state;
        var encodedSignature = SignMsg(msg);
        var clientSecret = WebEncoders.Base64UrlEncode(encodedSignature);

        var requestParams = new Dictionary<string, string>
                {
                    { "client_id", ClientID },
                    { "client_secret", clientSecret },
                    { "redirect_uri", RedirectUri },
                    { "scope", Scopes },
                    { "response_type", "code" },
                    { "state", state },
                    { "timestamp", timestamp },
                    { "access_type", "online" },
                    { "display", "popup" }
                };
        var requestQuery = string.Join("&", requestParams.Select(pair => pair.Key + "=" + HttpUtility.UrlEncode(pair.Value)));//.Replace("+", "%2b");

        var redURL = CodeUrl + "?" + requestQuery;
        context.Response.Redirect(redURL, true);
    }

    private OAuth20Token GetAccessToken(string state, string code)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy.MM.dd HH:mm:ss +0000", CultureInfo.InvariantCulture);

        var msg = Scopes + timestamp + ClientID + state;
        var encodedSignature = SignMsg(msg);
        var clientSecret = WebEncoders.Base64UrlEncode(encodedSignature);

        var requestParams = new Dictionary<string, string>
                {
                    { "client_id", ClientID },
                    { "code", code },
                    { "grant_type", "authorization_code" },
                    { "client_secret", clientSecret },
                    { "state", state },
                    { "redirect_uri", RedirectUri },
                    { "scope", Scopes },
                    { "timestamp", timestamp },
                    { "token_type", "Bearer" }
                };
        var requestQuery = string.Join("&", requestParams.Select(pair => pair.Key + "=" + HttpUtility.UrlEncode(pair.Value)));

        var result = _requestHelper.PerformRequest(AccessTokenUrl, "application/x-www-form-urlencoded", "POST", requestQuery);

        return OAuth20Token.FromJson(result);
    }

    private X509Certificate2 GetSignerCert()
    {
        var storeMy = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
        storeMy.Open(OpenFlags.ReadOnly);
        var certColl = storeMy.Certificates.Find(X509FindType.FindBySubjectKeyIdentifier, ClientSecret, false);
        storeMy.Close();
        if (certColl.Count == 0)
        {
            throw new Exception("Certificate not found");
        }

        return certColl[0];
    }

    private byte[] SignMsg(string msg)
    {
        var signerCert = GetSignerCert();

        var msgBytes = Encoding.UTF8.GetBytes(msg);
        var contentInfo = new ContentInfo(msgBytes);
        var signedCms = new SignedCms(contentInfo, true);
        var cmsSigner = new CmsSigner(signerCert);
        signedCms.ComputeSignature(cmsSigner);

        return signedCms.Encode();
    }

    //private static bool VerifyMsg(Byte[] msg, byte[] encodedSignature)
    //{
    //    ContentInfo contentInfo = new ContentInfo(msg);
    //    SignedCms signedCms = new SignedCms(contentInfo, true);
    //    signedCms.Decode(encodedSignature);

    //    try
    //    {
    //        signedCms.CheckSignature(true);
    //    }
    //    catch (System.Security.Cryptography.CryptographicException e)
    //    {
    //        return false;
    //    }

    //    return true;
    //}
}
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

using Tweetinvi;
using Tweetinvi.Auth;
using Tweetinvi.Parameters;

using ZiggyCreatures.Caching.Fusion;

namespace ASC.FederatedLogin.LoginProviders;

public class TwitterLoginProvider : BaseLoginProvider<TwitterLoginProvider>
{
    public override string AccessTokenUrl => "https://api.twitter.com/oauth/access_token";
    public override string RedirectUri => this["twitterRedirectUrl"];
    public override string ClientID => this["twitterKey"];
    public override string ClientSecret => this["twitterSecret"];
    public override string CodeUrl => "https://api.twitter.com/oauth/request_token";

    private static readonly LocalAuthenticationRequestStore _myAuthRequestStore = new();
    private readonly IFusionCache _hybridCache;
    private readonly InstanceCrypto _instanceCrypto;

    public override bool IsEnabled =>
        !string.IsNullOrEmpty(ClientID) &&
        !string.IsNullOrEmpty(ClientSecret);

    public TwitterLoginProvider() { }
    public TwitterLoginProvider(
        OAuth20TokenHelper oAuth20TokenHelper,
        TenantManager tenantManager,
        CoreBaseSettings coreBaseSettings,
        CoreSettings coreSettings,
        IConfiguration configuration,
        ICacheNotify<ConsumerCacheItem> cache,
        ConsumerFactory consumerFactory,
        InstanceCrypto instanceCrypto,
        IFusionCache hybridCache,
        string name, int order, bool paid, Dictionary<string, string> props, Dictionary<string, string> additional = null)
            : base(oAuth20TokenHelper, tenantManager, coreBaseSettings, coreSettings, configuration, cache, consumerFactory, name, order, paid, props, additional)
    {
        _hybridCache = hybridCache;
        _instanceCrypto = instanceCrypto;
    }

    private static string GetCacheKey(string authenticationRequestId)
    {
        return $"twitter_authentication_request:{authenticationRequestId}";
    }

    public override LoginProfile ProcessAuthorization(HttpContext context, IDictionary<string, string> @params, IDictionary<string, string> additionalStateArgs)
    {
        if (!string.IsNullOrEmpty(context.Request.Query["denied"]))
        {
            return new LoginProfile(new Exception("Canceled at provider"));
        }

        var appClient = new TwitterClient(ClientID, ClientSecret);

        if (string.IsNullOrEmpty(context.Request.Query["oauth_token"]))
        {
            var callbackAddress = new UriBuilder(RedirectUri)
            {
                Query = "state=" + HttpUtility.UrlEncode(context.Request.Url().AbsoluteUri)
            };

            var authenticationRequestId = Guid.NewGuid().ToString();

            // Add the user identifier as a query parameters that will be received by `ValidateTwitterAuth`
            var redirectURL = _myAuthRequestStore.AppendAuthenticationRequestIdToCallbackUrl(callbackAddress.ToString(), authenticationRequestId);

            // Initialize the authentication process
            var authenticationRequestToken = appClient.Auth.RequestAuthenticationUrlAsync(redirectURL)
                                   .ConfigureAwait(false)
                                   .GetAwaiter()
                                   .GetResult();

            // Store the token information in the store
            _myAuthRequestStore.AddAuthenticationTokenAsync(authenticationRequestId, authenticationRequestToken)
                                   .ConfigureAwait(false)
                                   .GetAwaiter()
                                   .GetResult();

            var authenticationRequestStr = JsonSerializer.Serialize(authenticationRequestToken);
            var authenticationRequestEncryptedStr = _instanceCrypto.Encrypt(authenticationRequestStr);
            _hybridCache.Set(GetCacheKey(authenticationRequestId), authenticationRequestEncryptedStr, TimeSpan.FromMinutes(5));

            context.Response.Redirect(authenticationRequestToken.AuthorizationURL, true);

            return null;
        }

        // Extract the information from the redirection url
        IRequestCredentialsParameters requestParameters;

        var callback = context.Request.GetDisplayUrl();
        var requestId = _myAuthRequestStore.ExtractAuthenticationRequestIdFromCallbackUrl(callback);
        var cacheKey = GetCacheKey(requestId);

        try
        {
            requestParameters = RequestCredentialsParameters.FromCallbackUrlAsync(callback, _myAuthRequestStore).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            if (ex.Message != "Could not retrieve the authentication token")
            {
                throw;
            }

            var authenticationRequestEncryptedStr = _hybridCache.GetOrDefault<string>(cacheKey);

            if (authenticationRequestEncryptedStr == null)
            {
                throw;
            }

            var authenticationRequestStr = _instanceCrypto.Decrypt(authenticationRequestEncryptedStr);
            var authenticationRequest = JsonSerializer.Deserialize<Tweetinvi.Credentials.Models.AuthenticationRequest>(authenticationRequestStr);

            var verifier = Tweetinvi.Core.Extensions.StringExtension.GetURLParameter(callback, "oauth_verifier");

            if (verifier == null)
            {
                throw new ArgumentException("oauth_verifier query parameter not found, this is required to authenticate the user");
            }

            requestParameters = new RequestCredentialsParameters(verifier, authenticationRequest);
        }
        finally
        {
            _hybridCache.Remove(cacheKey);
        }

        // Request Twitter to generate the credentials.
        var userCreds = appClient.Auth.RequestCredentialsAsync(requestParameters)
                                   .ConfigureAwait(false)
                                   .GetAwaiter()
                                   .GetResult();

        // Congratulations the user is now authenticated!
        var userClient = new TwitterClient(userCreds);

        var user = userClient.Users.GetAuthenticatedUserAsync()
                                   .ConfigureAwait(false)
                                   .GetAwaiter()
                                   .GetResult();

        var userSettings = userClient.AccountSettings.GetAccountSettingsAsync()
                                   .ConfigureAwait(false)
                                   .GetAwaiter()
                                   .GetResult();

        return user == null
                   ? null
                   : new LoginProfile
                   {
                       Name = user.Name,
                       DisplayName = user.ScreenName,
                       Avatar = user.ProfileImageUrl,
                       Locale = userSettings.Language.ToString(),
                       Id = user.Id.ToString(CultureInfo.InvariantCulture),
                       Provider = ProviderConstants.Twitter
                   };

    }

    protected override OAuth20Token Auth(HttpContext context, out bool redirect, IDictionary<string, string> additionalArgs = null, IDictionary<string, string> additionalStateArgs = null)
    {
        throw new NotImplementedException();
    }

    public override LoginProfile GetLoginProfile(string accessToken)
    {
        throw new NotImplementedException();
    }
}
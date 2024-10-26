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

namespace ASC.FederatedLogin.LoginProviders;

[Scope]
public class ZoomLoginProvider : BaseLoginProvider<ZoomLoginProvider>
{
    public override string AccessTokenUrl => "https://zoom.us/oauth/token";
    public override string RedirectUri => this["zoomRedirectUrl"];
    public override string ClientID => this["zoomClientId"];
    public override string ClientSecret => this["zoomClientSecret"];
    public override string CodeUrl => "https://zoom.us/oauth/authorize";
    public override string Scopes => "";

    // used in ZoomService
    public const string ApiUrl = "https://api.zoom.us/v2";
    private const string UserProfileUrl = $"{ApiUrl}/users/me";
    
    private readonly RequestHelper _requestHelper;

    public ZoomLoginProvider() { }
    public ZoomLoginProvider(
        OAuth20TokenHelper oAuth20TokenHelper,
        TenantManager tenantManager,
        CoreBaseSettings coreBaseSettings,
        CoreSettings coreSettings,
        IConfiguration configuration,
        ICacheNotify<ConsumerCacheItem> cache,
        ConsumerFactory consumerFactory,
        RequestHelper requestHelper,
        string name, int order, Dictionary<string, string> props, Dictionary<string, string> additional = null)
            : base(oAuth20TokenHelper, tenantManager, coreBaseSettings, coreSettings, configuration, cache, consumerFactory, name, order, props, additional)
    {
        _requestHelper = requestHelper;
    }

    public override LoginProfile ProcessAuthorization(HttpContext context, IDictionary<string, string> @params, IDictionary<string, string> additionalStateArgs)
    {
        try
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
                context.Response.Redirect(_oAuth20TokenHelper.RequestCode<ZoomLoginProvider>(Scopes, @params, additionalStateArgs));
                return null;
            }

            var token = GetAccessToken(code);
            return GetLoginProfile(token);
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

    // used in ZoomService
    public OAuth20Token GetAccessToken(string code, string redirectUri = null, string codeVerifier = null)
    {
        var clientPair = $"{ClientID}:{ClientSecret}";
        var base64ClientPair = Convert.ToBase64String(Encoding.UTF8.GetBytes(clientPair));

        var body = new Dictionary<string, string>
        {
            { "code", code },
            { "grant_type", "authorization_code" },
            { "redirect_uri", redirectUri ?? RedirectUri }
        };

        if (codeVerifier != null)
        {
            body.Add("code_verifier", codeVerifier);
        }

        var json = _requestHelper.PerformRequest(AccessTokenUrl, "application/x-www-form-urlencoded", "POST",
            body: string.Join("&", body.Select(kv => $"{HttpUtility.UrlEncode(kv.Key)}={HttpUtility.UrlEncode(kv.Value)}" )),
            headers: new Dictionary<string, string> { { "Authorization", $"Basic {base64ClientPair}" } }
        );

        return OAuth20Token.FromJson(json);
    }

    public override LoginProfile GetLoginProfile(string accessToken)
    {
        if (string.IsNullOrEmpty(accessToken))
        {
            throw new Exception("Login failed");
        }

        var (loginProfile, _) = RequestProfile(accessToken);

        return loginProfile;
    }

    public (LoginProfile, ZoomProfile) GetLoginProfileAndRaw(string accessToken)
    {
        if (string.IsNullOrEmpty(accessToken))
        {
            throw new Exception("Login failed");
        }

        var (loginProfile, raw) = RequestProfile(accessToken);

        return (loginProfile, raw);
    }

    public LoginProfile GetMinimalProfile(string uid)
    {
        return new LoginProfile
        {
            Id = uid,
            Provider = ProviderConstants.Zoom
        };
    }

    private (LoginProfile, ZoomProfile) ProfileFromZoom(string zoomProfile)
    {
        var jsonProfile = JsonSerializer.Deserialize<ZoomProfile>(zoomProfile);

        var profile = new LoginProfile
        {
            Id = jsonProfile.Id,
            Avatar = jsonProfile.PicUrl?.ToString(),
            EMail = jsonProfile.Email,
            FirstName = jsonProfile.FirstName,
            LastName = jsonProfile.LastName,
            Locale = jsonProfile.Language,
            TimeZone = jsonProfile.Timezone,
            DisplayName = jsonProfile.DisplayName,
            Provider = ProviderConstants.Zoom
        };

        return (profile, jsonProfile);
    }

    private (LoginProfile, ZoomProfile) RequestProfile(string accessToken)
    {
        var json = _requestHelper.PerformRequest(UserProfileUrl, headers: new Dictionary<string, string> { { "Authorization", "Bearer " + accessToken } });
        var (loginProfile, jsonProfile) = ProfileFromZoom(json);

        return (loginProfile, jsonProfile);
    }

    public class ZoomProfile
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("first_name")]
        public string FirstName { get; set; }

        [JsonPropertyName("last_name")]
        public string LastName { get; set; }

        [JsonPropertyName("display_name")]
        public string DisplayName { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("role_name")]
        public string RoleName { get; set; }

        [JsonPropertyName("pmi")]
        public long Pmi { get; set; }

        [JsonPropertyName("use_pmi")]
        public bool UsePmi { get; set; }

        [JsonPropertyName("personal_meeting_url")]
        public Uri PersonalMeetingUrl { get; set; }

        [JsonPropertyName("timezone")]
        public string Timezone { get; set; }

        [JsonPropertyName("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonPropertyName("last_login_time")]
        public DateTimeOffset LastLoginTime { get; set; }

        [JsonPropertyName("pic_url")]
        public Uri PicUrl { get; set; }

        [JsonPropertyName("jid")]
        public string Jid { get; set; }

        [JsonPropertyName("account_id")]
        public string AccountId { get; set; }

        [JsonPropertyName("language")]
        public string Language { get; set; }

        [JsonPropertyName("phone_country")]
        public string PhoneCountry { get; set; }

        [JsonPropertyName("phone_number")]
        public string PhoneNumber { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("job_title")]
        public string JobTitle { get; set; }

        [JsonPropertyName("location")]
        public string Location { get; set; }

        [JsonPropertyName("account_number")]
        public long AccountNumber { get; set; }

        [JsonPropertyName("cluster")]
        public string Cluster { get; set; }

        [JsonPropertyName("user_created_at")]
        public DateTimeOffset UserCreatedAt { get; set; }
    }
}

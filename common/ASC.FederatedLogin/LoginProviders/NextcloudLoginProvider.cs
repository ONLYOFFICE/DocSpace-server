// (c) Copyright Ascensio System SIA 2009-2025
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

using System.Text.Json;

namespace ASC.FederatedLogin.LoginProviders;

[Scope]
public class NextcloudLoginProvider : BaseLoginProvider<NextcloudLoginProvider>
{
    public Uri BaseUri => new(this["nextcloudBaseUrl"]);
    public override string AccessTokenUrl => new Uri(BaseUri, "/apps/oauth2/api/v1/token").ToString();
    public override string CodeUrl => new Uri(BaseUri, "/apps/oauth2/authorize").ToString();
    public override string RedirectUri => this["nextcloudRedirectUrl"];
    public override string ClientID => this["nextcloudClientId"];
    public override string ClientSecret => this["nextcloudClientSecret"];
    public override string Scopes => "";

    private readonly RequestHelper _requestHelper;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions() { PropertyNameCaseInsensitive = true };

    public NextcloudLoginProvider() { }
    public NextcloudLoginProvider(
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

    public override LoginProfile GetLoginProfile(string accessToken)
    {
        if (string.IsNullOrEmpty(accessToken))
        {
            throw new Exception("Login failed");
        }

        return RequestProfile(accessToken);
    }

    private LoginProfile RequestProfile(string accessToken)
    {
        var responseJson = _requestHelper.PerformRequest(new Uri(BaseUri, "/ocs/v2.php/cloud/user").ToString(), headers: new Dictionary<string, string> {
            { "Authorization", "Bearer " + accessToken },
            { "OCS-APIRequest", "true" },
            { "Accept", "application/json" }
        });

        var response = JsonSerializer.Deserialize<NextcloudApiResponse<NextcloudUser>>(responseJson, _jsonSerializerOptions);

        return ProfileFromNextcloud(response.Ocs.Data);
    }

    private LoginProfile ProfileFromNextcloud(NextcloudUser nextcloudUser)
    {
        var profile = new LoginProfile
        {
            Id = nextcloudUser.Id,
            Provider = ProviderConstants.Nextcloud,
            EMail = nextcloudUser.Email,
            DisplayName = nextcloudUser.DisplayName
        };

        return profile;
    }

    private class NextcloudApiResponse<T>
    {
        public Ocs<T> Ocs { get; set; }
    }

    private class Ocs<T>
    {
        public NextcloudApiResponseMeta Meta { get; set; }
        public T Data { get; set; }
    }

    private class NextcloudUser
    {
        public bool Enabled { get; set; }
        public string StorageLocation { get; set; }
        public string Id { get; set; }
        public long LastLogin { get; set; }
        public string Backend { get; set; }
        public NextcloudUserQuota Quota { get; set; }
        public string Manager { get; set; }
        public string AvatarScope { get; set; }
        public string Email { get; set; }
        public string EmailScope { get; set; }
        public string DisplayName { get; set; }
        public string DisplaynameScope { get; set; }
        public string Phone { get; set; }
        public string PhoneScope { get; set; }
        public string Address { get; set; }
        public string AddressScope { get; set; }
        public string Website { get; set; }
        public string WebsiteScope { get; set; }
        public string Twitter { get; set; }
        public string TwitterScope { get; set; }
        public string Fediverse { get; set; }
        public string FediverseScope { get; set; }
        public string Organisation { get; set; }
        public string OrganisationScope { get; set; }
        public string Role { get; set; }
        public string RoleScope { get; set; }
        public string Headline { get; set; }
        public string HeadlineScope { get; set; }
        public string Biography { get; set; }
        public string BiographyScope { get; set; }
        public long ProfileEnabled { get; set; }
        public string ProfileEnabledScope { get; set; }
        public string[] Groups { get; set; }
        public string Language { get; set; }
        public string Locale { get; set; }
        public string NotifyEmail { get; set; }
        public NextcloudUserBackendCapabilities BackendCapabilities { get; set; }
    }

    private class NextcloudUserBackendCapabilities
    {
        public bool SetDisplayName { get; set; }
        public bool SetPassword { get; set; }
    }

    private class NextcloudUserQuota
    {
        public long Free { get; set; }
        public long Used { get; set; }
        public long Total { get; set; }
        public double Relative { get; set; }
        public long QuotaQuota { get; set; }
    }

    private class NextcloudApiResponseMeta
    {
        public string Status { get; set; }
        public long Statuscode { get; set; }
        public string Message { get; set; }
    }
}

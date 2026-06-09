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

using System.Text.Json;

namespace ASC.FederatedLogin.LoginProviders;

[Scope]
public class NextcloudLoginProvider : BaseLoginProvider<NextcloudLoginProvider>, IDummyEmailProvider
{
    public Uri BaseUrl => new(this["nextcloudBaseUrl"]);
    public override string AccessTokenUrl => new Uri(BaseUrl, "/apps/oauth2/api/v1/token").ToString();
    public override string CodeUrl => new Uri(BaseUrl, "/apps/oauth2/authorize").ToString();
    public override string RedirectUri => this["nextcloudRedirectUrl"];
    public override string ClientID => this["nextcloudClientId"];
    public override string ClientSecret => this["nextcloudClientSecret"];
    public override string Scopes => "";

    public override bool IsEnabled =>
        !string.IsNullOrWhiteSpace(this["nextcloudBaseUrl"]) &&
        base.IsEnabled;

    private readonly RequestHelper _requestHelper;
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

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
        string name, int order, bool paid, Dictionary<string, string> props, Dictionary<string, string> additional = null)
            : base(oAuth20TokenHelper, tenantManager, coreBaseSettings, coreSettings, configuration, cache, consumerFactory, name, order, paid, props, additional)
    {
        _requestHelper = requestHelper;
    }

    public override LoginProfile GetLoginProfile(string accessToken)
    {
        return string.IsNullOrEmpty(accessToken)
            ? throw new Exception("Login failed")
            : RequestProfile(accessToken);
    }

    public LoginProfile RequestProfile(string accessToken)
    {
        try
        {
            var responseJson = _requestHelper.PerformRequest(new Uri(BaseUrl, "/ocs/v2.php/cloud/user").ToString(), headers: new Dictionary<string, string> {
                { "Authorization", "Bearer " + accessToken },
                { "OCS-APIRequest", "true" },
                { "Accept", "application/json" }
            });

            var response = JsonSerializer.Deserialize<NextcloudApiResponse<NextcloudUser>>(responseJson, _jsonSerializerOptions);
            return ProfileFromNextcloud(response.Ocs.Data);
        }
        catch (Exception ex)
        {
            return new LoginProfile(ex);
        }
    }

    public string GenerateEmail(LoginProfile loginProfile)
    {
        return string.IsNullOrWhiteSpace(loginProfile.EMail)
            ? $"{loginProfile.Id}@{BaseUrl.Host}"
            : loginProfile.EMail;
    }

    private static LoginProfile ProfileFromNextcloud(NextcloudUser nextcloudUser)
    {
        return new LoginProfile
        {
            Id = nextcloudUser.Id,
            Provider = ProviderConstants.Nextcloud,
            EMail = nextcloudUser.Email,
            DisplayName = nextcloudUser.DisplayName
        };
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

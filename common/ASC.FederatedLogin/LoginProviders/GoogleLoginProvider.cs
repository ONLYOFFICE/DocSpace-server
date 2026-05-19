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

using System.Collections.Immutable;

namespace ASC.FederatedLogin.LoginProviders;

[Scope]
public class GoogleLoginProvider : BaseLoginProvider<GoogleLoginProvider>
{
    public const string GoogleScopeContacts = "https://www.googleapis.com/auth/contacts.readonly";
    public const string GoogleScopeDrive = "https://www.googleapis.com/auth/drive";
    //https://developers.google.com/gmail/imap/xoauth2-protocol
    public const string GoogleScopeMail = "https://mail.google.com/";
    public const string GoogleUrlContacts = "https://www.google.com/m8/feeds/contacts/default/full/";
    public const string GoogleUrlFile = "https://www.googleapis.com/drive/v3/files/";
    public const string GoogleUrlFileUpload = "https://www.googleapis.com/upload/drive/v3/files";
    public const string GoogleUrlProfile = "https://people.googleapis.com/v1/people/me";
    public static readonly ImmutableDictionary<string, string> GoogleAdditionalArgs = new Dictionary<string, string> { { "access_type", "offline" }, { "prompt", "consent" } }.ToImmutableDictionary();

    public override string AccessTokenUrl => "https://www.googleapis.com/oauth2/v4/token";
    public override string CodeUrl => "https://accounts.google.com/o/oauth2/v2/auth";
    public override string RedirectUri => this["googleRedirectUrl"];
    public override string ClientID => this["googleClientId"];
    public override string ClientSecret => this["googleClientSecret"];
    public override string Scopes => "https://www.googleapis.com/auth/userinfo.profile https://www.googleapis.com/auth/userinfo.email";

    public static readonly string[] GoogleDriveExt = [".gdoc", ".gsheet", ".gslides", ".gdraw"];
    public static readonly string GoogleDriveMimeTypeFolder = "application/vnd.google-apps.folder";
    public static readonly string FilesFields = "id,name,mimeType,parents,createdTime,modifiedTime,owners/displayName,lastModifyingUser/displayName,capabilities/canEdit,size,hasThumbnail";
    public static readonly string ProfileFields = "emailAddresses,genders,names";

    private readonly RequestHelper _requestHelper;

    public GoogleLoginProvider() { }
    public GoogleLoginProvider(
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

    protected override OAuth20Token Auth(HttpContext context, out bool redirect, IDictionary<string, string> additionalArgs = null, IDictionary<string, string> additionalStateArgs = null)
    {
        return base.Auth(context, out redirect, (additionalArgs ?? new Dictionary<string, string>()).Union(GoogleAdditionalArgs).DistinctBy(r => r.Key).ToDictionary(r => r.Key, r => r.Value), additionalStateArgs);
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
        var googleProfile = _requestHelper.PerformRequest(GoogleUrlProfile + "?personFields=" + HttpUtility.UrlEncode(ProfileFields), headers: new Dictionary<string, string> { { "Authorization", "Bearer " + accessToken } });
        var loginProfile = ProfileFromGoogle(googleProfile);

        return loginProfile;
    }

    private LoginProfile ProfileFromGoogle(string googleProfile)
    {
        var jProfile = JObject.Parse(googleProfile);
        if (jProfile == null)
        {
            throw new Exception("Failed to correctly process the response");
        }

        var profile = new LoginProfile
        {
            Id = jProfile.Value<string>("resourceName").Replace("people/", ""),
            Provider = ProviderConstants.Google
        };

        var emailsArr = jProfile.Value<JArray>("emailAddresses");
        if (emailsArr != null)
        {
            var emailsList = emailsArr.ToObject<List<GoogleEmailAddress>>();
            if (emailsList.Count > 0)
            {
                var ind = emailsList.FindIndex(googleEmail => googleEmail.Metadata.Primary);
                profile.EMail = emailsList[ind > -1 ? ind : 0].Value;
            }
        }

        var namesArr = jProfile.Value<JArray>("names");
        if (namesArr != null)
        {
            var namesList = namesArr.ToObject<List<GoogleName>>();
            if (namesList.Count > 0)
            {
                var ind = namesList.FindIndex(googleName => googleName.Metadata.Primary);
                var name = namesList[ind > -1 ? ind : 0];
                profile.DisplayName = name.DisplayName;
                profile.FirstName = name.GivenName;
                profile.LastName = name.FamilyName;
            }
        }

        var gendersArr = jProfile.Value<JArray>("genders");
        if (gendersArr != null)
        {
            var gendersList = gendersArr.ToObject<List<GoogleGender>>();
            if (gendersList.Count > 0)
            {
                var ind = gendersList.FindIndex(googleGender => googleGender.Metadata.Primary);
                profile.Gender = gendersList[ind > -1 ? ind : 0].Value;
            }
        }

        return profile;
    }

    private class GoogleEmailAddress
    {
        public GoogleMetadata Metadata { get; set; } = new();
        public string Value { get; set; }
    }

    private class GoogleGender
    {
        public GoogleMetadata Metadata { get; set; } = new();
        public string Value { get; set; }
    }

    private class GoogleName
    {
        public GoogleMetadata Metadata { get; set; } = new();
        public string DisplayName { get; set; }
        public string FamilyName { get; set; }
        public string GivenName { get; set; }
    }

    private class GoogleMetadata
    {
        public bool Primary { get; set; }
    }
}
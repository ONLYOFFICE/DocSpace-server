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

namespace ASC.Web.Files.Helpers;

[Singleton]
public class ThirdpartyConfigurationData(IConfiguration configuration)
{
    public HashSet<string> ThirdPartyProviders => field ??=
        configuration.GetSection("files:thirdparty:enable").Get<HashSet<string>>() ?? [];
}

[Scope]
public class ThirdpartyConfiguration(ThirdpartyConfigurationData configuration, ConsumerFactory consumerFactory)
{
    private BoxLoginProvider _boxLoginProvider;
    private BoxLoginProvider BoxLoginProvider => _boxLoginProvider ??= consumerFactory.Get<BoxLoginProvider>();

    private DropboxLoginProvider DropboxLoginProvider => field ??= consumerFactory.Get<DropboxLoginProvider>();

    private OneDriveLoginProvider OneDriveLoginProvider => field ??= consumerFactory.Get<OneDriveLoginProvider>();

    private DocuSignLoginProvider DocuSignLoginProvider => field ??= consumerFactory.Get<DocuSignLoginProvider>();

    private GoogleLoginProvider GoogleLoginProvider => field ??= consumerFactory.Get<GoogleLoginProvider>();

    private HashSet<string> ThirdPartyProviders => configuration.ThirdPartyProviders;

    public bool SupportInclusion(IDaoFactory daoFactory)
    {
        var providerDao = daoFactory.ProviderDao;
        if (providerDao == null)
        {
            return false;
        }

        return SupportBoxInclusion || SupportDropboxInclusion || SupportDocuSignInclusion || SupportGoogleDriveInclusion || SupportOneDriveInclusion || SupportSharePointInclusion || SupportWebDavInclusion || SupportNextcloudInclusion || SupportOwncloudInclusion || SupportkDriveInclusion || SupportYandexInclusion;
    }

    public bool SupportBoxInclusion => ThirdPartyProviders.Contains(BoxKey) && BoxLoginProvider.IsEnabled;

    public bool SupportDropboxInclusion => ThirdPartyProviders.Contains(DropboxKey) && DropboxLoginProvider.IsEnabled;

    public bool SupportOneDriveInclusion => ThirdPartyProviders.Contains(OneDriveKey) && OneDriveLoginProvider.IsEnabled;

    public bool SupportSharePointInclusion => ThirdPartyProviders.Contains(SharePointKey);

    public bool SupportWebDavInclusion => ThirdPartyProviders.Contains(WebDavKey);

    public bool SupportNextcloudInclusion => ThirdPartyProviders.Contains(NextcloudKey);

    public bool SupportOwncloudInclusion => ThirdPartyProviders.Contains(OwncloudKey);

    public bool SupportkDriveInclusion => ThirdPartyProviders.Contains(KDriveKey);

    public bool SupportYandexInclusion => ThirdPartyProviders.Contains(YandexKey);

    public bool SupportDocuSignInclusion => ThirdPartyProviders.Contains(DocuSignKey) && DocuSignLoginProvider.IsEnabled;

    public bool SupportGoogleDriveInclusion => ThirdPartyProviders.Contains(GoogleDriveKey) && GoogleLoginProvider.IsEnabled;

    private static string BoxKey => "box";
    private static string DropboxKey => "dropboxv2";
    private static string GoogleDriveKey => "google";
    private static string OneDriveKey => "onedrive";
    private static string SharePointKey => "sharepoint";
    private static string WebDavKey => "webdav";
    private static string NextcloudKey => "nextcloud";
    private static string OwncloudKey => "owncloud";
    private static string KDriveKey => "kdrive";
    private static string YandexKey => "yandex";
    private static string DocuSignKey => "docusign";

    public List<ProviderDto> GetAllProviders(bool excludeWebDav)
    {
        var webDavKey = ProviderTypes.WebDav.ToStringFast();

        var providers = new List<ProviderDto>(ThirdPartyProviders.Count);

        if (ThirdPartyProviders.Contains(BoxKey))
        {
            providers.Add(new ProviderDto("Box", ProviderTypes.Box.ToStringFast(), BoxLoginProvider.IsEnabled, true, BoxLoginProvider.RedirectUri,
                ClientId: BoxLoginProvider.ClientID));
        }

        if (ThirdPartyProviders.Contains(DropboxKey))
        {
            providers.Add(new ProviderDto("Dropbox", ProviderTypes.DropboxV2.ToStringFast(), DropboxLoginProvider.IsEnabled, true, DropboxLoginProvider.RedirectUri,
                ClientId: DropboxLoginProvider.ClientID));
        }

        if (ThirdPartyProviders.Contains(GoogleDriveKey))
        {
            providers.Add(new ProviderDto("GoogleDrive", ProviderTypes.GoogleDrive.ToStringFast(), GoogleLoginProvider.IsEnabled, true, GoogleLoginProvider.RedirectUri,
                ClientId: GoogleLoginProvider.ClientID));
        }

        if (ThirdPartyProviders.Contains(OneDriveKey))
        {
            providers.Add(new ProviderDto("OneDrive", ProviderTypes.OneDrive.ToStringFast(), OneDriveLoginProvider.IsEnabled, true, OneDriveLoginProvider.RedirectUri,
                ClientId: OneDriveLoginProvider.ClientID));
        }

        if (excludeWebDav)
        {
            return providers;
        }

        if (ThirdPartyProviders.Contains(KDriveKey))
        {
            providers.Add(new ProviderDto("kDrive", webDavKey, true));
        }

        if (ThirdPartyProviders.Contains(YandexKey))
        {
            providers.Add(new ProviderDto("Yandex", webDavKey, true));
        }

        if (ThirdPartyProviders.Contains(WebDavKey))
        {
            providers.Add(new ProviderDto("WebDav", webDavKey, true, RequiredConnectionUrl: true));
        }

        if (ThirdPartyProviders.Contains(NextcloudKey))
        {
            providers.Add(new ProviderDto("Nextcloud", webDavKey, true, RequiredConnectionUrl: true));
        }

        if (ThirdPartyProviders.Contains(OwncloudKey))
        {
            providers.Add(new ProviderDto("ownCloud", webDavKey, true, RequiredConnectionUrl: true));
        }

        return providers;
    }

    public List<List<string>> GetProviders()
    {
        var result = new List<List<string>>();

        if (SupportBoxInclusion)
        {
            result.Add(["Box", BoxLoginProvider.ClientID, _boxLoginProvider.RedirectUri]);
        }

        if (SupportDropboxInclusion)
        {
            result.Add(["DropboxV2", DropboxLoginProvider.ClientID, DropboxLoginProvider.RedirectUri]);
        }

        if (SupportGoogleDriveInclusion)
        {
            result.Add(["GoogleDrive", GoogleLoginProvider.ClientID, GoogleLoginProvider.RedirectUri]);
        }

        if (SupportOneDriveInclusion)
        {
            result.Add(["OneDrive", OneDriveLoginProvider.ClientID, OneDriveLoginProvider.RedirectUri]);
        }

        if (SupportSharePointInclusion)
        {
            result.Add(["SharePoint"]);
        }

        if (SupportkDriveInclusion)
        {
            result.Add(["kDrive"]);
        }

        if (SupportYandexInclusion)
        {
            result.Add(["Yandex"]);
        }

        if (SupportWebDavInclusion)
        {
            result.Add(["WebDav"]);
        }

        //Obsolete BoxNet, DropBox, Google, SkyDrive,

        return result;
    }
}
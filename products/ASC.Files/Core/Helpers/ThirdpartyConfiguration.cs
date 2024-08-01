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

namespace ASC.Web.Files.Helpers;

[Singleton]
public class ThirdpartyConfigurationData(IConfiguration configuration)
{
    private HashSet<string> _thirdPartyProviders;
    public HashSet<string> ThirdPartyProviders => _thirdPartyProviders ??= 
        configuration.GetSection("files:thirdparty:enable").Get<HashSet<string>>() ?? [];
}

[Scope]
public class ThirdpartyConfiguration(ThirdpartyConfigurationData configuration, ConsumerFactory consumerFactory)
{
    private BoxLoginProvider _boxLoginProvider;
    private BoxLoginProvider BoxLoginProvider => _boxLoginProvider ??= consumerFactory.Get<BoxLoginProvider>();
    
    private DropboxLoginProvider _dropboxLoginProvider;
    private DropboxLoginProvider DropboxLoginProvider =>  _dropboxLoginProvider ??= consumerFactory.Get<DropboxLoginProvider>();
    
    private OneDriveLoginProvider _oneDriveLoginProvider;
    private OneDriveLoginProvider OneDriveLoginProvider =>  _oneDriveLoginProvider ??= consumerFactory.Get<OneDriveLoginProvider>();
    
    private DocuSignLoginProvider _docuSignLoginProvider;
    private DocuSignLoginProvider DocuSignLoginProvider =>  _docuSignLoginProvider ??= consumerFactory.Get<DocuSignLoginProvider>();
    
    private GoogleLoginProvider _googleLoginProvider;
    private GoogleLoginProvider GoogleLoginProvider =>  _googleLoginProvider ??= consumerFactory.Get<GoogleLoginProvider>();

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

    public bool SupportBoxInclusion => ThirdPartyProviders.Contains("box") && BoxLoginProvider.IsEnabled;

    public bool SupportDropboxInclusion => ThirdPartyProviders.Contains("dropboxv2") && DropboxLoginProvider.IsEnabled;

    public bool SupportOneDriveInclusion => ThirdPartyProviders.Contains("onedrive") && OneDriveLoginProvider.IsEnabled;

    public bool SupportSharePointInclusion => ThirdPartyProviders.Contains("sharepoint");

    public bool SupportWebDavInclusion => ThirdPartyProviders.Contains("webdav");

    public bool SupportNextcloudInclusion => ThirdPartyProviders.Contains("nextcloud");

    public bool SupportOwncloudInclusion => ThirdPartyProviders.Contains("owncloud");

    public bool SupportkDriveInclusion => ThirdPartyProviders.Contains("kdrive");

    public bool SupportYandexInclusion => ThirdPartyProviders.Contains("yandex");
    
    public bool SupportDocuSignInclusion => ThirdPartyProviders.Contains("docusign") && DocuSignLoginProvider.IsEnabled;

    public bool SupportGoogleDriveInclusion => ThirdPartyProviders.Contains("google") && GoogleLoginProvider.IsEnabled;
    
    public List<ProviderDto> GetAllProviders()
    {
        var webDavKey = ProviderTypes.WebDav.ToStringFast();

        var providers = new List<ProviderDto>
        {
            new("Box", ProviderTypes.Box.ToStringFast(), BoxLoginProvider.IsEnabled, true, BoxLoginProvider.RedirectUri),
            new("Dropbox", ProviderTypes.DropboxV2.ToStringFast(), DropboxLoginProvider.IsEnabled, true, DropboxLoginProvider.RedirectUri),
            new("GoogleDrive", ProviderTypes.GoogleDrive.ToStringFast(), GoogleLoginProvider.IsEnabled, true, GoogleLoginProvider.RedirectUri),
            new("OneDrive", ProviderTypes.OneDrive.ToStringFast(), OneDriveLoginProvider.IsEnabled, true, OneDriveLoginProvider.RedirectUri),
            new("kDrive", webDavKey, true),
            new("WebDav", webDavKey, true, RequiredConnectionUrl: true),
            new("Nextcloud", webDavKey, true, RequiredConnectionUrl: true),
            new("ownCloud", webDavKey, true, RequiredConnectionUrl: true)
        };

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

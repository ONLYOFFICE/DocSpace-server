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

using ASC.Data.Backup.Storage;

namespace ASC.Files.Api;

public class ThirdpartyController(
        FilesSettingsHelper filesSettingsHelper,
        FileStorageService fileStorageService,
        GlobalFolderHelper globalFolderHelper,
        SecurityContext securityContext,
        ThirdpartyConfiguration thirdPartyConfiguration,
        UserManager userManager,
        WordpressHelper wordpressHelper,
        WordpressToken wordpressToken,
        RequestHelper requestHelper,
        FolderDtoHelper folderDtoHelper,
        FileDtoHelper fileDtoHelper,
        FileSecurityCommon fileSecurityCommon,
        BackupRepository backupRepository,
        TenantManager tenantManager)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
{
    /// <remarks>
    /// Lists the third-party storage services this portal is able to connect, in the compact form a connection dialog
    /// needs. Every element is itself an array whose first item is the provider key accepted as `providerKey` by
    /// `POST api/2.0/files/thirdparty`. For the services that authenticate through OAuth 2.0 (`Box`, `DropboxV2`,
    /// `GoogleDrive`, `OneDrive`) the second and third items are the OAuth client ID and the redirect URL this portal
    /// is registered with, so the caller can build the consent screen URL itself; the services that authenticate by
    /// login and password (`SharePoint`, `WebDav`, `kDrive`, `Yandex`) contribute a single-item array. Only the
    /// services enabled in the portal configuration are listed, and an OAuth service whose application is not
    /// configured is left out. The call is read-only. An empty array is a normal answer rather than a failure: it is
    /// what a guest gets, and what everyone gets while the portal-wide third-party switch is off
    /// (`PUT api/2.0/files/thirdparty`). For display names, the WebDAV presets and the flags a connection form needs,
    /// use `GET api/2.0/files/thirdparty/providers` instead.
    /// </remarks>
    /// <summary>Get third-party provider capabilities</summary>
    /// <path>api/2.0/files/thirdparty/capabilities</path>
    /// <collection>list</collection>
    [Tags("Files / Third-party integration")]
    [SwaggerResponse(200, "The provider keys, each with the OAuth client ID and redirect URL where the service uses OAuth", typeof(List<List<string>>))]
    [HttpGet("thirdparty/capabilities")]
    public async Task<List<List<string>>> GetCapabilities()
    {
        if (!await CheckAccessAsync())
        {
            return [];
        }

        return thirdPartyConfiguration.GetProviders();
    }

    /// <remarks>
    /// Creates a WordPress post with the parameters specified in the request.
    /// </remarks>
    /// <summary>Create a WordPress post</summary>
    /// <path>api/2.0/files/wordpress</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / WordPress")]
    [SwaggerResponse(200, "Boolean value: true if the operation is successful", typeof(bool))]
    [HttpPost("wordpress")]
    public async Task<bool> CreateWordpressPost(CreateWordpressPostRequestDto inDto)
    {
        try
        {
            var token = await wordpressToken.GetTokenAsync();
            var meInfo = wordpressHelper.GetWordpressMeInfo(token.AccessToken);

            if (!string.IsNullOrEmpty(meInfo.TokenSiteId))
            {
                var createPost = wordpressHelper.CreateWordpressPost(inDto.Title, inDto.Content, inDto.Status, meInfo.TokenSiteId, token);

                return createPost;
            }

            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <remarks>
    /// Disconnects a third-party storage account from the portal and returns the ID of the folder that stood for it,
    /// in the `provider-accountId` form the Files operations use for third-party entries. Take `providerId` from
    /// `GET api/2.0/files/thirdparty`: it is the numeric account ID, not that composed folder ID. The member who
    /// connected the account can remove it; another member's request is refused unless they hold delete rights on the
    /// folder it stands for. Nothing is deleted at the storage service: the files stay with the provider, and what
    /// goes away is the portal's link to them together with the stored credentials, the sharing records and the tags
    /// kept for its entries. A room that was created on this account stops being available. When the account being
    /// removed is the one connected for backups by `POST api/2.0/files/thirdparty/backup`, its backup schedule is
    /// deleted as well. The removal cannot be repeated: once the account is gone the same ID is refused rather than
    /// confirmed, so treat the first successful answer as the record of it.
    /// </remarks>
    /// <summary>Remove a third-party account</summary>
    /// <path>api/2.0/files/thirdparty/{providerId}</path>
    /// <exception cref="ArgumentException"></exception>
    [Tags("Files / Third-party integration")]
    [SwaggerResponse(200, "The ID of the folder that stood for the removed account", typeof(string))]
    [HttpDelete("thirdparty/{providerId:int}")]
    public async Task<string> DeleteThirdParty(ProviderIdRequestDto inDto)
    {
        var providerInfo = await fileStorageService.DeleteThirdPartyAsync(inDto.ProviderId.ToString(CultureInfo.InvariantCulture));

        if (providerInfo.RootFolderType == FolderType.ThirdpartyBackup)
        {
            await backupRepository.DeleteBackupScheduleAsync(tenantManager.GetCurrentTenantId(), providerInfo.RootFolderId);
        }

        return providerInfo.RootFolderId;
    }

    /// <remarks>
    /// Deletes the WordPress plugin information.
    /// </remarks>
    /// <summary>Delete the WordPress information</summary>
    /// <path>api/2.0/files/wordpress-delete</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / WordPress")]
    [SwaggerResponse(200, "Object with the \"success\" field: true if the operation is successful", typeof(DeleteWordpressInfoResponse))]
    [HttpGet("wordpress-delete")]
    public async Task<DeleteWordpressInfoResponse> DeleteWordpressInfo()
    {
        var token = await wordpressToken.GetTokenAsync();
        if (token != null)
        {
            await wordpressToken.DeleteTokenAsync(token);
            return DeleteWordpressInfoResponse.Succeeded();
        }

        return DeleteWordpressInfoResponse.Failed();
    }

    /// <remarks>
    /// Lists the third-party storage accounts attached to the legacy Common section, as folder entries that can be
    /// browsed with the usual folder operations. Each entry stands for a whole connected account: its title is the
    /// account title, and `providerId` and `providerKey` identify the account behind it. Only accounts whose owner
    /// the caller may read are included, so the answer differs from one member to another. The call is read-only and
    /// returns a plain array with no paging. An empty array is the expected answer in most portals and does not mean
    /// an error: accounts connected by `POST api/2.0/files/thirdparty` are attached to the Rooms section, not to
    /// Common, so only accounts inherited from an older portal appear here. The list is also empty while the
    /// portal-wide third-party switch is off (`PUT api/2.0/files/thirdparty`) and when no storage service is
    /// configured. For the accounts the caller owns, regardless of where they are attached, use
    /// `GET api/2.0/files/thirdparty`.
    /// </remarks>
    /// <summary>Get common third-party folders</summary>
    /// <path>api/2.0/files/thirdparty/common</path>
    /// <collection>list</collection>
    [Tags("Files / Third-party integration")]
    [SwaggerResponse(200, "The third-party accounts attached to the Common section, as folder entries", typeof(IAsyncEnumerable<FolderDto<string>>))]
    [HttpGet("thirdparty/common")]
    public async IAsyncEnumerable<FolderDto<string>> GetCommonThirdPartyFolders([FromServices] EntryManager entryManager)
    {
        var parent = await fileStorageService.GetFolderAsync(await globalFolderHelper.FolderCommonAsync);
        var thirdpartyFolders = entryManager.GetThirdPartyFoldersAsync(parent);

        await foreach (var r in thirdpartyFolders)
        {
            yield return await _folderDtoHelper.GetAsync(r);
        }
    }

    /// <remarks>
    /// Lists the third-party storage accounts the caller has connected, one element per account, with the title it
    /// was saved under, the storage service behind it and the portal section it is attached to. Accounts connected by
    /// other members are not included, and neither is the portal backup account of
    /// `GET api/2.0/files/thirdparty/backup`, even for an administrator. The `providerId` of an element is the value
    /// to send to `DELETE api/2.0/files/thirdparty/{providerId}` and, as `providerId` in
    /// `POST api/2.0/files/thirdparty`, the way to re-authenticate that same account instead of connecting a new one.
    /// Credentials are never disclosed: `auth_data` comes back empty for every element. An element with
    /// `roomsStorage` set is available as storage for a room, while `corporate` marks an account inherited from the
    /// legacy Common section. The call is read-only, returns a plain array with no paging and no contractual
    /// ordering, and answers with an empty array when the caller has connected nothing. To browse the content of an
    /// account, take the folder ID from the answer of the operation that connected it or from
    /// `GET api/2.0/files/@root`.
    /// </remarks>
    /// <summary>Get the third-party accounts</summary>
    /// <path>api/2.0/files/thirdparty</path>
    /// <collection>list</collection>
    [Tags("Files / Third-party integration")]
    [SwaggerResponse(200, "The third-party accounts the caller has connected", typeof(IAsyncEnumerable<ThirdPartyParams>))]
    [HttpGet("thirdparty")]
    public IAsyncEnumerable<ThirdPartyParams> GetThirdPartyAccounts()
    {
        return fileStorageService.GetThirdPartyAsync();
    }

    /// <remarks>
    /// Returns the folder of the third-party storage account the portal keeps for backups, so a caller can check
    /// where scheduled and manual backups are written. There is at most one such account per portal, connected by an
    /// administrator through `POST api/2.0/files/thirdparty/backup`, and it is deliberately kept out of the personal
    /// list of `GET api/2.0/files/thirdparty`. Any authenticated member may ask, and the call is read-only. The body
    /// is `null`, with a successful status, in two situations the answer does not distinguish: no backup account has
    /// been connected, and the caller has no read access to the folder of the one that is. When a folder does come
    /// back, its `id` is the string ID of a third-party folder and can be used with the folder operations that accept
    /// one, and its `title` is the title the account was saved under. Connecting a different account through the
    /// backup operation replaces this one rather than adding a second, and
    /// `DELETE api/2.0/files/thirdparty/{providerId}` removes it.
    /// </remarks>
    /// <summary>Get the third-party backup folder</summary>
    /// <path>api/2.0/files/thirdparty/backup</path>
    [Tags("Files / Third-party integration")]
    [SwaggerResponse(200, "The root folder of the backup storage account, or null when none is connected", typeof(FolderDto<string>))]
    [HttpGet("thirdparty/backup")]
    public async Task<FolderDto<string>> GetBackupThirdPartyAccount()
    {
        var folder = await fileStorageService.GetBackupThirdPartyAsync();
        if (folder != null)
        {

            return await _folderDtoHelper.GetAsync(folder);
        }

        return null;
    }

    /// <remarks>
    /// Returns the WordPress plugin information.
    /// </remarks>
    /// <summary>Get the WordPress information</summary>
    /// <path>api/2.0/files/wordpress-info</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / WordPress")]
    [SwaggerResponse(200, "Object with the following parameters: \"success\" - specifies if the operation is successful or not, \"data\" - blog information", typeof(WordpressInfoResponse))]
    [HttpGet("wordpress-info")]
    public async Task<WordpressInfoResponse> GetWordpressInfo()
    {
        var token = await wordpressToken.GetTokenAsync();
        if (token != null)
        {
            var meInfo = wordpressHelper.GetWordpressMeInfo(token.AccessToken);
            var blogId = meInfo.TokenSiteId;
            var wordpressUserName = meInfo.UserName;

            var blogInfo = requestHelper.PerformRequest(WordpressLoginProvider.WordpressSites + blogId);
            var jsonBlogInfo = JObject.Parse(blogInfo);
            jsonBlogInfo.Add("username", wordpressUserName);

            return new WordpressInfoResponse { Success = true, Data = jsonBlogInfo.ToString() };
        }

        return new WordpressInfoResponse { Success = false, Data = null };
    }

    /// <remarks>
    /// Connects an account at a third-party storage service to the portal, or re-authenticates one that is already
    /// connected, and returns the folder that now stands for its root. Send `providerId` to update an existing
    /// account and omit it to connect a new one; the accepted `providerKey` values come from
    /// `GET api/2.0/files/thirdparty/providers`. The credentials to send depend on the service: the OAuth services
    /// take `token`, which is the authorization code from their consent screen and not an access token, while the
    /// WebDAV family and SharePoint take `login` with `password`, plus `url` where the server address is not fixed.
    /// Credentials are verified against the service before anything is stored, so a wrong password is refused and
    /// nothing is saved. The caller needs the rights to create rooms, and the portal-wide third-party switch has to
    /// be on, otherwise the call is refused. A new account is attached to the Rooms section and becomes available as
    /// room storage for `POST api/2.0/files/rooms/thirdparty/{id}`. Connecting twice with the same title creates two
    /// separate accounts.
    /// </remarks>
    /// <summary>Connect a third-party account</summary>
    /// <path>api/2.0/files/thirdparty</path>
    /// <exception cref="ArgumentException"></exception>
    [Tags("Files / Third-party integration")]
    [SwaggerResponse(200, "The root folder of the connected account", typeof(FolderDto<string>))]
    [HttpPost("thirdparty")]
    public async Task<FolderDto<string>> SaveThirdParty(ThirdPartyRequestDto inDto)
    {
        var thirdPartyParams = new ThirdPartyParams
        {
            AuthData = new AuthData(inDto.Url, inDto.Login, inDto.Password, inDto.Token),
            RoomsStorage = true,
            CustomerTitle = inDto.CustomerTitle,
            ProviderId = inDto.ProviderId,
            ProviderKey = inDto.ProviderKey
        };

        var folder = await fileStorageService.SaveThirdPartyAsync(thirdPartyParams);

        return await _folderDtoHelper.GetAsync(folder);
    }

    /// <remarks>
    /// Connects the third-party storage account the portal writes its backups to, and returns the folder that stands
    /// for its root. Only a portal administrator may call it, and the portal-wide third-party switch has to be on;
    /// other callers are refused. The account is portal-wide and single: a second call does not add another one but
    /// re-authenticates and retitles the existing one, which makes the operation safe to repeat with the same body.
    /// The credentials follow the same rules as in `POST api/2.0/files/thirdparty` - an authorization code in `token`
    /// for the OAuth services, `login` with `password` and, where the server address is not fixed, `url` for the
    /// WebDAV family and SharePoint - and are verified against the service before anything is stored, so a wrong
    /// password leaves the previous account untouched. The account is deliberately absent from
    /// `GET api/2.0/files/thirdparty`; read it back with `GET api/2.0/files/thirdparty/backup` and remove it with
    /// `DELETE api/2.0/files/thirdparty/{providerId}`.
    /// </remarks>
    /// <summary>Connect the third-party backup storage</summary>
    /// <path>api/2.0/files/thirdparty/backup</path>
    /// <exception cref="ArgumentException"></exception>
    [Tags("Files / Third-party integration")]
    [SwaggerResponse(200, "The root folder of the backup storage account", typeof(FolderDto<string>))]
    [HttpPost("thirdparty/backup")]
    public async Task<FolderDto<string>> SaveThirdPartyBackup(ThirdPartyBackupRequestDto inDto)
    {
        if (!await fileSecurityCommon.IsDocSpaceAdministratorAsync(securityContext.CurrentAccount.ID))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_Create);
        }

        var thirdPartyParams = new ThirdPartyParams
        {
            AuthData = new AuthData(inDto.Url, inDto.Login, inDto.Password, inDto.Token),
            CustomerTitle = inDto.CustomerTitle,
            ProviderKey = inDto.ProviderKey
        };

        var folder = await fileStorageService.SaveThirdPartyBackupAsync(thirdPartyParams);

        return await _folderDtoHelper.GetAsync(folder);
    }

    /// <remarks>
    /// Saves the user WordPress information when logging in.
    /// </remarks>
    /// <summary>Save the user WordPress information</summary>
    /// <path>api/2.0/files/wordpress-save</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / WordPress")]
    [SwaggerResponse(200, "Object with the following parameters: \"success\" - specifies if the operation is successful or not, \"data\" - blog information", typeof(WordpressInfoResponse))]
    [HttpPost("wordpress-save")]
    public async Task<WordpressInfoResponse> WordpressSave(WordpressSaveRequestDto inDto)
    {
        if (inDto.Code.Length == 0)
        {
            return new WordpressInfoResponse();
        }
        try
        {
            var token = await wordpressToken.SaveTokenFromCodeAsync(inDto.Code);
            var meInfo = wordpressHelper.GetWordpressMeInfo(token.AccessToken);
            var blogId = meInfo.TokenSiteId;

            var wordpressUserName = meInfo.UserName;

            var blogInfo = requestHelper.PerformRequest(WordpressLoginProvider.WordpressSites + blogId);
            var jsonBlogInfo = JObject.Parse(blogInfo);
            jsonBlogInfo.Add("username", wordpressUserName);

            blogInfo = jsonBlogInfo.ToString();
            return new WordpressInfoResponse
            {
                Success = true,
                Data = blogInfo
            };
        }
        catch (Exception)
        {
            return new WordpressInfoResponse();
        }
    }

    /// <remarks>
    /// Lists the third-party storage services this portal can connect, with everything a connection form needs: the
    /// display name, the key to send as `providerKey`, whether the service authenticates through OAuth 2.0, the OAuth
    /// client ID and redirect URL where it does, and whether the caller has to supply the server address. Several
    /// WebDAV presets share the key `WebDav` and are told apart by their names, so keep the name the caller chose
    /// next to the key when building the request. Pass `excludewebdav=true` to drop the whole WebDAV family,
    /// including the kDrive and Yandex presets, and keep only the OAuth services. The call is read-only. An empty
    /// array is a normal answer: it is what a guest gets, and what everyone gets while the portal-wide third-party
    /// switch is off (`PUT api/2.0/files/thirdparty`). The `connected` flag of an element says the service is
    /// available on this portal, not that an account of it exists - the caller's own accounts are listed by
    /// `GET api/2.0/files/thirdparty`.
    /// </remarks>
    /// <summary>Get all third-party providers</summary>
    /// <path>api/2.0/files/thirdparty/providers</path>
    /// <collection>list</collection>
    [Tags("Files / Third-party integration")]
    [SwaggerResponse(200, "The storage services this portal can connect", typeof(List<ProviderDto>))]
    [HttpGet("thirdparty/providers")]
    public async Task<List<ProviderDto>> GetAllProviders(GetProvidersRequestDto inDto)
    {
        if (!await CheckAccessAsync())
        {
            return [];
        }

        return thirdPartyConfiguration.GetAllProviders(inDto.ExcludeWebDav);
    }

    private async Task<bool> CheckAccessAsync()
    {
        return !await userManager.IsGuestAsync(securityContext.CurrentAccount.ID) && await filesSettingsHelper.GetEnableThirdParty();
    }
}
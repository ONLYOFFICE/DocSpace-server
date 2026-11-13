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
    /// Returns the third-party storage services that the portal can connect, in a short form. Each element is an array.
    /// Its first item is the value to send as `providerKey` to `POST api/2.0/files/thirdparty`. The OAuth services are
    /// `Box`, `DropboxV2`, `GoogleDrive`, and `OneDrive`. Their second and third items are the OAuth client ID and redirect
    /// URL. With them, you can build the address of the consent screen. A service that uses a login and password
    /// (`SharePoint`, `WebDav`, `kDrive`, or `Yandex`) has a one-item array. The list has only the services turned on in
    /// the portal configuration. It also skips an OAuth service whose app is not set up. Any signed-in user can call it.
    /// A guest gets an empty array instead of an error. The call is read-only. The array is also empty while third-party storage is
    /// turned off for the portal. To turn it on, use `PUT api/2.0/files/thirdparty`. For display names, WebDAV presets,
    /// and the flags of a connection form, use `GET api/2.0/files/thirdparty/providers`.
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
    /// Creates a post on the WordPress blog that the caller connected with `POST api/2.0/files/wordpress-save`. Any
    /// signed-in user can call it. The call changes data. Do not repeat it. Each call creates another post. It returns
    /// true if WordPress created the post. It returns false if the caller has no saved WordPress token, the token has no
    /// blog, or WordPress fails.
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
    /// Disconnects a third-party storage account from the portal and returns the ID of its folder. The ID has the
    /// `provider-accountId` form of third-party entries, not the numeric `providerId`. Requires the right to delete the
    /// account folder, which the user who connected the account has. Other callers get 403. The call changes data and
    /// cannot be undone. It deletes nothing at the storage service, and the files stay there. The portal deletes its
    /// link to them, the stored credentials, and the sharing records and tags of its entries. If the account is the
    /// backup storage of `POST api/2.0/files/thirdparty/backup`, the call also deletes the backup schedule. If a room
    /// stands on the account, the call returns 403. To disconnect such an account, delete the room. If no account has
    /// this `providerId`, the call returns 404. So a repeated call also returns 404.
    /// </remarks>
    /// <summary>Remove a third-party account</summary>
    /// <path>api/2.0/files/thirdparty/{providerId}</path>
    /// <exception cref="ArgumentException"></exception>
    [Tags("Files / Third-party integration")]
    [SwaggerResponse(200, "The ID of the folder that stood for the removed account", typeof(string))]
    [SwaggerResponse(403, "The caller neither connected the account nor has the right to delete the folder it stands for")]
    [SwaggerResponse(404, "No third-party account with this ID exists on the portal")]
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
    /// Deletes the WordPress token that the caller saved with `POST api/2.0/files/wordpress-save`. The operation uses
    /// GET but changes data. Any signed-in user can call it. The `success` field is true if the call deleted a token. It
    /// is false if the caller had no token, so a repeated call returns false.
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
    /// Returns the third-party storage accounts of the legacy Common section, as folder entries. Each entry stands for
    /// one account, and the folder operations can browse it. Any signed-in user can call it. The call skips accounts
    /// whose owner the caller cannot read, so the list differs between users. The call is read-only. It returns a plain
    /// array without paging. In most portals the array is empty, and that is not an error. The reason is that
    /// `POST api/2.0/files/thirdparty` attaches accounts to the Rooms section, not to Common. So only accounts from an
    /// older portal appear here. The array is also empty if third-party storage is turned off or no storage service is
    /// set up. For the caller's own accounts in any section, use `GET api/2.0/files/thirdparty`.
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
    /// Returns the third-party storage accounts that the caller connected, one element per account. Any signed-in user
    /// can call it. The list skips the accounts of other users. It also skips the backup account of
    /// `GET api/2.0/files/thirdparty/backup`, even for a DocSpace admin. The call is read-only. It returns a plain array
    /// without paging and in no fixed order. An empty array means that the caller connected nothing. The response has
    /// no credentials. To browse an account, take its folder ID from `GET api/2.0/files/@root`. The answer of
    /// `POST api/2.0/files/thirdparty` also has it.
    /// </remarks>
    /// <summary>Get the third-party accounts</summary>
    /// <path>api/2.0/files/thirdparty</path>
    /// <collection>list</collection>
    [Tags("Files / Third-party integration")]
    [SwaggerResponse(200, "The third-party accounts the caller has connected", typeof(IAsyncEnumerable<ThirdPartyAccountDto>))]
    [HttpGet("thirdparty")]
    public IAsyncEnumerable<ThirdPartyAccountDto> GetThirdPartyAccounts()
    {
        return fileStorageService.GetThirdPartyAsync().Select(r => r.Map());
    }

    /// <remarks>
    /// Returns the root folder of the third-party storage account where the portal saves scheduled and manual backups.
    /// A portal has at most one such account. A DocSpace admin connects it with `POST api/2.0/files/thirdparty/backup`.
    /// Any signed-in user can call it. The call is read-only. The body is `null` with status 200 if no backup account is
    /// connected or the caller cannot read its folder. The answer does not tell these cases apart. The folder operations
    /// accept the returned `id`. The `title` is the title of the account. The list of `GET api/2.0/files/thirdparty`
    /// does not have this account. To remove it, use `DELETE api/2.0/files/thirdparty/{providerId}`.
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
    /// Returns the WordPress blog that the caller connected with `POST api/2.0/files/wordpress-save`. Any signed-in user
    /// can call it. The call is read-only. The `data` field is a JSON string with the blog data from WordPress. It also
    /// has the `username` of the caller at WordPress. If the caller has no saved token, `success` is false and `data` is
    /// null.
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
    /// Connects a third-party storage account as room storage, or re-authenticates the caller's account with
    /// `providerId`. It returns the root folder of the account. Third-party storage must be turned on with
    /// `PUT api/2.0/files/thirdparty`. Requires the right to create rooms. Other callers get 403. The call changes data.
    /// It checks the credentials at the service before it saves anything. Without `providerId`, each call adds a new
    /// account, so do not repeat it. You can repeat a re-authentication with the same body if it has no `token`. A
    /// `token` is a one-time code of Google Drive, Box, Dropbox, or OneDrive. So the service rejects a repeat with the
    /// same `token`. A re-authenticated room account keeps its server address. The call returns 400 if `customerTitle`
    /// or `providerKey` is missing, or `providerKey` names no known service. For a new account, it also returns 400 if a
    /// needed `token`, `password`, or `url` is missing. The call returns 403 if third-party storage is turned off or the
    /// service rejects the credentials. It also returns 403 if `providerId` names the account of another user. For a new
    /// account, a title that is empty after the call removes invalid characters also returns 403. If no account has the
    /// `providerId`, the call returns 404. To create a room on the account, use
    /// `POST api/2.0/files/rooms/thirdparty/{id}`. To connect backup storage, use
    /// `POST api/2.0/files/thirdparty/backup`.
    /// </remarks>
    /// <summary>Connect a third-party account</summary>
    /// <path>api/2.0/files/thirdparty</path>
    /// <exception cref="ArgumentException"></exception>
    [Tags("Files / Third-party integration")]
    [SwaggerResponse(200, "The root folder of the connected account", typeof(FolderDto<string>))]
    [SwaggerResponse(400, "The request body cannot be read or has no `customerTitle` or `providerKey`, `providerId` is neither a number nor a numeric string, or, for a new account, `providerKey` names no known storage service, the `token` or `password` the service needs is missing, or `url` is missing or is not an HTTP address for a service that needs it")]
    [SwaggerResponse(403, "The caller cannot create rooms, the portal-wide third-party switch is off or no storage service is enabled, the title of a new account is empty once invalid characters are removed, the service rejects the credentials, or `providerId` names an account connected by another member")]
    [SwaggerResponse(404, "`providerId` names no third-party account on the portal")]
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
    /// Connects the third-party storage account where the portal saves backups, and returns its root folder. Third-party
    /// storage must be turned on with `PUT api/2.0/files/thirdparty`. Requires a DocSpace admin. Other callers get 403.
    /// The call changes data. A portal has one backup account. A second call re-authenticates and renames that account
    /// instead of adding one. So you can repeat the call with the same body if it has no `token`. A `token` is a
    /// one-time code of Google Drive, Box, Dropbox, or OneDrive. So the service rejects a repeat with the same `token`.
    /// The credentials follow the rules of `POST api/2.0/files/thirdparty`. The call checks them at the service before
    /// it saves anything. So wrong credentials leave the current account unchanged. The call returns 400 if
    /// `providerKey` names no known service or a needed `url` is missing. While no backup account exists, a missing
    /// `token` or `password` also returns 400. The call returns 403 if third-party storage is turned off or the service
    /// rejects the credentials. While no backup account exists, a title that is empty after the call removes invalid
    /// characters also returns 403. The list of `GET api/2.0/files/thirdparty` does not have this account. To read it,
    /// use `GET api/2.0/files/thirdparty/backup`. To remove it, use `DELETE api/2.0/files/thirdparty/{providerId}`.
    /// </remarks>
    /// <summary>Connect the third-party backup storage</summary>
    /// <path>api/2.0/files/thirdparty/backup</path>
    /// <exception cref="ArgumentException"></exception>
    [Tags("Files / Third-party integration")]
    [SwaggerResponse(200, "The root folder of the backup storage account", typeof(FolderDto<string>))]
    [SwaggerResponse(400, "The request body cannot be read, `providerKey` names no known storage service, `url` is missing or is not an HTTP address for a service that needs it, or, while no backup account is connected yet, the `token` or `password` the service needs is missing")]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator, the portal-wide third-party switch is off or no storage service is enabled, the service rejects the credentials, or, while no backup account is connected yet, the title is empty once invalid characters are removed")]
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
    /// Exchanges a WordPress authorization code for a token and saves the token for the caller. It returns the connected
    /// blog in the form of `GET api/2.0/files/wordpress-info`. Any signed-in user can call it. The call changes data. A
    /// new token replaces the saved one. Do not repeat the call with the same code. WordPress accepts a code only once.
    /// If `code` is empty or any step fails, `success` is false.
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
    /// Returns the third-party storage services that the portal configuration lists, with the data of a connection form.
    /// With `excludewebdav=true`, the list has only the OAuth services. Any signed-in user can call it. A guest gets an
    /// empty array instead of an error. The call is read-only. The array is also empty while third-party storage is turned off for
    /// the portal. To turn it on, use `PUT api/2.0/files/thirdparty`. The list also has OAuth services whose app is not
    /// set up, with `connected` false. To list the accounts of the caller, use `GET api/2.0/files/thirdparty`.
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
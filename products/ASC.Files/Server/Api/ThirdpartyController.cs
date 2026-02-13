// (c) Copyright Ascensio System SIA 2009-2026
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
    /// Returns the list of the available providers.
    /// </remarks>
    /// <summary>Get providers</summary>
    /// <remarks>Available provider keys: DropboxV2, Box, WebDav, Yandex, OneDrive, SharePoint, GoogleDrive, kDrive.</remarks>
    /// <path>api/2.0/files/thirdparty/capabilities</path>
    /// <collection>list</collection>
    [Tags("Files / Third-party integration")]
    [SwaggerResponse(200, "List of provider keys", typeof(List<List<string>>))]
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
    /// Removes the third-party storage service account with the ID specified in the request.
    /// </remarks>
    /// <summary>Remove a third-party account</summary>
    /// <path>api/2.0/files/thirdparty/{providerId}</path>
    /// <exception cref="ArgumentException"></exception>
    [Tags("Files / Third-party integration")]
    [SwaggerResponse(200, "Third-party folder ID", typeof(string))]
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
    /// Returns a list of the third-party services connected to the "Common" section.
    /// </remarks>
    /// <summary>Get the common third-party services</summary>
    /// <path>api/2.0/files/thirdparty/common</path>
    /// <collection>list</collection>
    [Tags("Files / Third-party integration")]
    [SwaggerResponse(200, "List of common third-party folderst", typeof(IAsyncEnumerable<FolderDto<string>>))]
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
    /// Returns a list of all the connected third-party accounts.
    /// </remarks>
    /// <summary>Get the third-party accounts</summary>
    /// <path>api/2.0/files/thirdparty</path>
    /// <collection>list</collection>
    [Tags("Files / Third-party integration")]
    [SwaggerResponse(200, "List of connected providers information", typeof(IAsyncEnumerable<ThirdPartyParams>))]
    [HttpGet("thirdparty")]
    public IAsyncEnumerable<ThirdPartyParams> GetThirdPartyAccounts()
    {
        return fileStorageService.GetThirdPartyAsync();
    }

    /// <remarks>
    /// Returns a backup of the connected third-party account.
    /// </remarks>
    /// <summary>Get a third-party account backup</summary>
    /// <path>api/2.0/files/thirdparty/backup</path>
    [Tags("Files / Third-party integration")]
    [SwaggerResponse(200, "Folder for the third-party account backup", typeof(FolderDto<string>))]
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
    /// Saves the third-party storage service account. For WebDav, Yandex, kDrive and SharePoint, the login and password are used for authentication. For other providers, the authentication is performed using a token received via OAuth 2.0.
    /// </remarks>
    /// <summary>Save a third-party account</summary>
    /// <remarks>List of provider keys: DropboxV2, Box, WebDav, Yandex, OneDrive, SharePoint, GoogleDrive, kDrive.</remarks>
    /// <path>api/2.0/files/thirdparty</path>
    /// <exception cref="ArgumentException"></exception>
    [Tags("Files / Third-party integration")]
    [SwaggerResponse(200, "Connected provider folder", typeof(FolderDto<string>))]
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
    /// Saves a backup of the connected third-party account.
    /// </remarks>
    /// <summary>Save a third-party account backup</summary>
    /// <remarks>List of provider keys: DropboxV2, Box, WebDav, Yandex, OneDrive, SharePoint, GoogleDrive, kDrive.</remarks>
    /// <path>api/2.0/files/thirdparty/backup</path>
    /// <exception cref="ArgumentException"></exception>
    [Tags("Files / Third-party integration")]
    [SwaggerResponse(200, "Folder for the third-party account backup", typeof(FolderDto<string>))]
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
    /// Returns a list of all providers.
    /// </remarks>
    /// <summary>Get all providers</summary>
    /// <remarks>Available provider keys: Dropbox, Box, WebDav, OneDrive, GoogleDrive, kDrive, ownCloud, Nextcloud.</remarks>
    /// <path>api/2.0/files/thirdparty/providers</path>
    /// <collection>list</collection>
    [Tags("Files / Third-party integration")]
    [SwaggerResponse(200, "List of provider", typeof(List<ProviderDto>))]
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
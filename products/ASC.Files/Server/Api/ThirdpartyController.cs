﻿// (c) Copyright Ascensio System SIA 2009-2024
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
        FileSecurityCommon fileSecurityCommon)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
{
    /// <summary>
    /// Returns a list of the available providers.
    /// </summary>
    /// <short>Get providers</short>
    /// <remarks>Available provider keys: DropboxV2, Box, WebDav, Yandex, OneDrive, SharePoint, GoogleDrive, kDrive.</remarks>
    /// <path>api/2.0/files/thirdparty/capabilities</path>
    /// <collection>list</collection>
    [Tags("Files / Third-party integration")]
    [EndpointSummary("Get providers")]
    [EndpointDescription("Returns a list of the available providers.\n\n **Note**: Available provider keys: DropboxV2, Box, WebDav, Yandex, OneDrive, SharePoint, GoogleDrive, kDrive.")]
    [OpenApiResponse(typeof(List<List<string>>), 200, "List of provider keys")]
    [HttpGet("thirdparty/capabilities")]
    public async Task<List<List<string>>> CapabilitiesAsync()
    {
        if (!await CheckAccessAsync())
        {
            return [];
        }

        return thirdPartyConfiguration.GetProviders();
    }

    /// <summary>
    /// Creates a WordPress post with the parameters specified in the request.
    /// </summary>
    /// <short>Create a WordPress post</short>
    /// <path>api/2.0/files/wordpress</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / WordPress")]
    [EndpointSummary("Create a WordPress post")]
    [EndpointDescription("Creates a WordPress post with the parameters specified in the request.")]
    [OpenApiResponse(typeof(bool), 200, "Boolean value: true if the operation is successful")]
    [HttpPost("wordpress")]
    public async Task<bool> CreateWordpressPostAsync(CreateWordpressPostRequestDto inDto)
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

    /// <summary>
    /// Removes the third-party storage service account with the ID specified in the request.
    /// </summary>
    /// <short>Remove a third-party account</short>
    /// <path>api/2.0/files/thirdparty/{providerId}</path>
    /// <exception cref="ArgumentException"></exception>
    [Tags("Files / Third-party integration")]
    [EndpointSummary("Remove a third-party account")]
    [EndpointDescription("Removes the third-party storage service account with the ID specified in the request.")]
    [OpenApiResponse(typeof(string), 200, "Third-party folder ID")]
    [HttpDelete("thirdparty/{providerId}")]
    public async Task<string> DeleteThirdPartyAsync(ProviderIdRequestDto inDto)
    {
        return await fileStorageService.DeleteThirdPartyAsync(inDto.ProviderId.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Deletes the WordPress plugin information.
    /// </summary>
    /// <short>Delete the WordPress information</short>
    /// <path>api/2.0/files/wordpress-delete</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / WordPress")]
    [EndpointSummary("Delete the WordPress information")]
    [EndpointDescription("Deletes the WordPress plugin information.")]
    [OpenApiResponse(typeof(DeleteWordpressInfoResponse), 200, "Object with the \"success\" field: true if the operation is successful")]
    [HttpGet("wordpress-delete")]
    public async Task<DeleteWordpressInfoResponse> DeleteWordpressInfoAsync()
    {
        var token = await wordpressToken.GetTokenAsync();
        if (token != null)
        {
            await wordpressToken.DeleteTokenAsync(token);
            return DeleteWordpressInfoResponse.Succeeded();
        }

        return DeleteWordpressInfoResponse.Failed();
    }

    /// <summary>
    /// Returns a list of the third-party services connected to the "Common" section.
    /// </summary>
    /// <short>Get common third-party services</short>
    /// <path>api/2.0/files/thirdparty/common</path>
    /// <collection>list</collection>
    [Tags("Files / Third-party integration")]
    [EndpointSummary("Get common third-party services")]
    [EndpointDescription("Returns a list of the third-party services connected to the \"Common\" section.")]
    [OpenApiResponse(typeof(IAsyncEnumerable<FolderDto<string>>), 200, "List of common third-party folders")]
    [HttpGet("thirdparty/common")]
    public async IAsyncEnumerable<FolderDto<string>> GetCommonThirdPartyFoldersAsync([FromServices] EntryManager entryManager)
    {
        var parent = await fileStorageService.GetFolderAsync(await globalFolderHelper.FolderCommonAsync);
        var thirdpartyFolders = entryManager.GetThirdPartyFoldersAsync(parent);

        await foreach (var r in thirdpartyFolders)
        {
            yield return await _folderDtoHelper.GetAsync(r);
        }
    }

    /// <summary>
    /// Returns a list of all the connected third-party accounts.
    /// </summary>
    /// <short>Get third-party accounts</short>
    /// <path>api/2.0/files/thirdparty</path>
    /// <collection>list</collection>
    [Tags("Files / Third-party integration")]
    [EndpointSummary("Get third-party accounts")]
    [EndpointDescription("Returns a list of all the connected third-party accounts.")]
    [OpenApiResponse(typeof(IAsyncEnumerable<ThirdPartyParams>), 200, "List of connected providers information")]
    [HttpGet("thirdparty")]
    public IAsyncEnumerable<ThirdPartyParams> GetThirdPartyAccountsAsync()
    {
        return fileStorageService.GetThirdPartyAsync();
    }

    /// <summary>
    /// Return a backup of the connected third-party account.
    /// </summary>
    /// <short>Get a third-party account backup</short>
    /// <path>api/2.0/files/thirdparty/backup</path>
    [Tags("Files / Third-party integration")]
    [EndpointSummary("Get a third-party account backup")]
    [EndpointDescription("Return a backup of the connected third-party account.")]
    [OpenApiResponse(typeof(FolderDto<string>), 200, "Folder for the third-party account backup")]
    [HttpGet("thirdparty/backup")]
    public async Task<FolderDto<string>> GetBackupThirdPartyAccountAsync()
    {
        var folder = await fileStorageService.GetBackupThirdPartyAsync();
        if (folder != null)
        {

            return await _folderDtoHelper.GetAsync(folder);
        }

        return null;
    }

    /// <summary>
    /// Returns the WordPress plugin information.
    /// </summary>
    /// <short>Get the WordPress information</short>
    /// <path>api/2.0/files/wordpress-info</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / WordPress")]
    [EndpointSummary("Get the WordPress information")]
    [EndpointDescription("Returns the WordPress plugin information.")]
    [OpenApiResponse(typeof(WordpressInfoResponse), 200, "Object with the following parameters: \"success\" - specifies if the operation is successful or not, \"data\" - blog information")]
    [HttpGet("wordpress-info")]
    public async Task<WordpressInfoResponse> GetWordpressInfoAsync()
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

    /// <summary>
    /// Saves the third-party storage service account. For WebDav, Yandex, kDrive and SharePoint, the login and password are used for authentication. For other providers, the authentication is performed using a token received via OAuth 2.0.
    /// </summary>
    /// <short>Save a third-party account</short>
    /// <remarks>List of provider keys: DropboxV2, Box, WebDav, Yandex, OneDrive, SharePoint, GoogleDrive, kDrive.</remarks>
    /// <path>api/2.0/files/thirdparty</path>
    /// <exception cref="ArgumentException"></exception>
    [Tags("Files / Third-party integration")]
    [EndpointSummary("Save a third-party account")]
    [EndpointDescription("Saves the third-party storage service account. For WebDav, Yandex, kDrive and SharePoint, the login and password are used for authentication. For other providers, the authentication is performed using a token received via OAuth 2.0.\n\n **Note**: List of provider keys: DropboxV2, Box, WebDav, Yandex, OneDrive, SharePoint, GoogleDrive, kDrive.")]
    [OpenApiResponse(typeof(FolderDto<string>), 200, "Connected provider folder")]
    [HttpPost("thirdparty")]
    public async Task<FolderDto<string>> SaveThirdPartyAsync(ThirdPartyRequestDto inDto)
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

    /// <summary>
    /// Saves a backup of the connected third-party account.
    /// </summary>
    /// <short>Save a third-party account backup</short>
    /// <remarks>List of provider key: DropboxV2, Box, WebDav, Yandex, OneDrive, SharePoint, GoogleDrive, kDrive</remarks>
    /// <path>api/2.0/files/thirdparty/backup</path>
    /// <exception cref="ArgumentException"></exception>
    [Tags("Files / Third-party integration")]
    [EndpointSummary("Save a third-party account backup")]
    [EndpointDescription("Saves a backup of the connected third-party account.\n\n **Note**: List of provider key: DropboxV2, Box, WebDav, Yandex, OneDrive, SharePoint, GoogleDrive, kDrive")]
    [OpenApiResponse(typeof(FolderDto<string>), 200, "Folder for the third-party account backup")]
    [HttpPost("thirdparty/backup")]
    public async Task<FolderDto<string>> SaveThirdPartyBackupAsync(ThirdPartyBackupRequestDto inDto)
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

    /// <summary>
    /// Saves the user WordPress information when logging in.
    /// </summary>
    /// <short>Save the user WordPress information</short>
    /// <path>api/2.0/files/wordpress-save</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / WordPress")]
    [EndpointSummary("Save the user WordPress information")]
    [EndpointDescription("Saves the user WordPress information when logging in.")]
    [OpenApiResponse(typeof(WordpressInfoResponse), 200, "Object with the following parameters: \"success\" - specifies if the operation is successful or not, \"data\" - blog information")]
    [HttpPost("wordpress-save")]
    public async Task<WordpressInfoResponse> WordpressSaveAsync(WordpressSaveRequestDto inDto)
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

    /// <summary>
    /// Returns a list of the all providers.
    /// </summary>
    /// <short>Get all providers</short>
    /// <remarks>Available provider keys: Dropbox, Box, WebDav, OneDrive, GoogleDrive, kDrive, ownCloud, Nextcloud</remarks>
    /// <path>api/2.0/files/thirdparty/providers</path>
    /// <collection>list</collection>
    [Tags("Files / Third-party integration")]
    [EndpointSummary("Get all providers")]
    [EndpointDescription("Returns a list of the all providers.\n\n **Note**: Available provider keys: Dropbox, Box, WebDav, OneDrive, GoogleDrive, kDrive, ownCloud, Nextcloud")]
    [OpenApiResponse(typeof(List<ProviderDto>), 200, "List of provider")]
    [HttpGet("thirdparty/providers")]
    public async Task<List<ProviderDto>> GetAllProvidersAsync()
    {
        if (!await CheckAccessAsync())
        {
            return [];
        }
        
        return thirdPartyConfiguration.GetAllProviders();
    }
    
    private async Task<bool> CheckAccessAsync()
    {
        return !await userManager.IsGuestAsync(securityContext.CurrentAccount.ID) && await filesSettingsHelper.GetEnableThirdParty();
    }
}

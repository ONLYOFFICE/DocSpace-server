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
    [SwaggerResponse(200, "List of provider keys", typeof(List<List<string>>))]
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
    [SwaggerResponse(200, "Boolean value: true if the operation is successful", typeof(bool))]
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
    [SwaggerResponse(200, "Third-party folder ID", typeof(object))]
    [HttpDelete("thirdparty/{providerId:int}")]
    public async Task<object> DeleteThirdPartyAsync(ProviderIdRequestDto inDto)
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
    [SwaggerResponse(200, "Object with the \"success\" field: true if the operation is successful", typeof(object))]
    [HttpGet("wordpress-delete")]
    public async Task<object> DeleteWordpressInfoAsync()
    {
        var token = await wordpressToken.GetTokenAsync();
        if (token != null)
        {
            await wordpressToken.DeleteTokenAsync(token);
            return new
            {
                success = true
            };
        }
        return new
        {
            success = false
        };
    }

    /// <summary>
    /// Returns a list of the third-party services connected to the "Common" section.
    /// </summary>
    /// <short>Get common third-party services</short>
    /// <path>api/2.0/files/thirdparty/common</path>
    /// <collection>list</collection>
    [Tags("Files / Third-party integration")]
    [SwaggerResponse(200, "List of common third-party folderst", typeof(IAsyncEnumerable<FolderDto<string>>))]
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
    [SwaggerResponse(200, "List of connected providers information", typeof(IAsyncEnumerable<ThirdPartyParams>))]
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
    [SwaggerResponse(200, "Folder for the third-party account backup", typeof(FolderDto<string>))]
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
    [SwaggerResponse(200, "Object with the following parameters: \"success\" - specifies if the operation is successful or not, \"data\" - blog information", typeof(object))]
    [HttpGet("wordpress-info")]
    public async Task<object> GetWordpressInfoAsync()
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

            blogInfo = jsonBlogInfo.ToString();
            return new
            {
                success = true,
                data = blogInfo
            };
        }
        return new
        {
            success = false
        };
    }

    /// <summary>
    /// Saves the third-party storage service account. For WebDav, Yandex, kDrive and SharePoint, the login and password are used for authentication. For other providers, the authentication is performed using a token received via OAuth 2.0.
    /// </summary>
    /// <short>Save a third-party account</short>
    /// <remarks>List of provider keys: DropboxV2, Box, WebDav, Yandex, OneDrive, SharePoint, GoogleDrive, kDrive.</remarks>
    /// <path>api/2.0/files/thirdparty</path>
    /// <exception cref="ArgumentException"></exception>
    [Tags("Files / Third-party integration")]
    [SwaggerResponse(200, "Connected provider folder", typeof(FolderDto<string>))]
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
    [SwaggerResponse(200, "Folder for the third-party account backup", typeof(FolderDto<string>))]
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
    [SwaggerResponse(200, "Object with the following parameters: \"success\" - specifies if the operation is successful or not, \"data\" - blog information", typeof(object))]
    [HttpPost("wordpress-save")]
    public async Task<object> WordpressSaveAsync(WordpressSaveRequestDto inDto)
    {
        if (inDto.Code.Length == 0)
        {
            return new
            {
                success = false
            };
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
            return new
            {
                success = true,
                data = blogInfo
            };
        }
        catch (Exception)
        {
            return new
            {
                success = false
            };
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
    [SwaggerResponse(200, "List of provider", typeof(List<ProviderDto>))]
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

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

[ConstraintRoute("int")]
public class FoldersControllerInternal(
    BreadCrumbsManager breadCrumbsManager,
    FolderContentDtoHelper folderContentDtoHelper,
    FileStorageService fileStorageService,
    FileOperationsManager fileOperationsManager,
    FileOperationDtoHelper fileOperationDtoHelper,
    FolderDtoHelper folderDtoHelper,
    FileDtoHelper fileDtoHelper,
    PermissionContext permissionContext,
    FileShareDtoHelper fileShareDtoHelper,
    HistoryApiHelper historyApiHelper)
    : FoldersController<int>(breadCrumbsManager,
        folderContentDtoHelper,
        fileStorageService,
        fileOperationsManager,
        fileOperationDtoHelper,
        folderDtoHelper,
        fileDtoHelper,
        permissionContext,
        fileShareDtoHelper)
{
    /// <summary>
    /// Get the activity history of a folder with a specified identifier
    /// </summary>
    /// <short>
    /// Get folder history
    /// </short>
    /// <path>api/2.0/files/folder/{folderId}/log</path>
    /// <collection>list</collection>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "List of actions in the folder", typeof(HistoryDto))]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [SwaggerResponse(404, "The required folder was not found")]
    [HttpGet("folder/{folderId:int}/log")]
    public IAsyncEnumerable<HistoryDto> GetHistoryAsync(HistoryFolderRequestDto inDto)
    {
        return historyApiHelper.GetFolderHistoryAsync(inDto.FolderId, inDto.FromDate, inDto.ToDate);
    }
}

public class FoldersControllerThirdparty(
    BreadCrumbsManager breadCrumbsManager,
    FolderContentDtoHelper folderContentDtoHelper,
    FileStorageService fileStorageService,
    FileOperationsManager fileOperationsManager,
    FileOperationDtoHelper fileOperationDtoHelper,
    FolderDtoHelper folderDtoHelper,
    FileDtoHelper fileDtoHelper,
    PermissionContext permissionContext,
    FileShareDtoHelper fileShareDtoHelper)
    : FoldersController<string>(breadCrumbsManager,
        folderContentDtoHelper,
        fileStorageService,
        fileOperationsManager,
        fileOperationDtoHelper,
        folderDtoHelper,
        fileDtoHelper,
        permissionContext,
        fileShareDtoHelper);

public abstract class FoldersController<T>(
    BreadCrumbsManager breadCrumbsManager,
    FolderContentDtoHelper folderContentDtoHelper,
    FileStorageService fileStorageService,
    FileOperationsManager fileOperationsManager,
    FileOperationDtoHelper fileOperationDtoHelper,
    FolderDtoHelper folderDtoHelper,
    FileDtoHelper fileDtoHelper,
    PermissionContext permissionContext,
    FileShareDtoHelper fileShareDtoHelper)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
{
    /// <summary>
    /// Creates a new folder with the title specified in the request. The parent folder ID can be also specified.
    /// </summary>
    /// <short>Create a folder</short>
    /// <path>api/2.0/files/folder/{folderId}</path>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "New folder parameters", typeof(FolderDto<int>))]
    [HttpPost("folder/{folderId}")]
    public async Task<FolderDto<T>> CreateFolderAsync(CreateFolderRequestDto<T> inDto)
    {
        var folder = await fileStorageService.CreateFolderAsync(inDto.FolderId, inDto.Folder.Title);

        return await _folderDtoHelper.GetAsync(folder);
    }

    /// <summary>
    /// Deletes a folder with the ID specified in the request.
    /// </summary>
    /// <short>Delete a folder</short>
    /// <path>api/2.0/files/folder/{folderId}</path>
    /// <collection>list</collection>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "List of file operations", typeof(FileOperationDto))]
    [HttpDelete("folder/{folderId}")]
    public async IAsyncEnumerable<FileOperationDto> DeleteFolder(DeleteFolder<T> inDto)
    {
        await fileOperationsManager.PublishDelete(new List<T> { inDto.FolderId }, new List<T>(), false, !inDto.Delete.DeleteAfter, inDto.Delete.Immediately);
        
        foreach (var e in await fileOperationsManager.GetOperationResults())
        {
            yield return await fileOperationDtoHelper.GetAsync(e);
        }
    }

    /// <summary>
    /// Sets file order in the folder with ID specified in the request
    /// </summary>
    /// <path>api/2.0/files/folder/{folderId}/order</path>
    [Tags("Files / Folders")]
    [HttpPut("folder/{folderId}/order")]
    public async Task SetFileOrder(OrderFolderRequestDto<T> inDto)
    {
        await fileStorageService.SetFolderOrder(inDto.FolderId, inDto.Order.Order);
    }

    /// <summary>
    /// Returns the detailed list of files and folders located in the folder with the ID specified in the request.
    /// </summary>
    /// <short>
    /// Get a folder by ID
    /// </short>
    /// <path>api/2.0/files/{folderId}</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "Folder contents", typeof(FolderContentDto<int>))]
    [SwaggerResponse(403, "You don't have enough permission to view the folder content")]
    [SwaggerResponse(404, "The required folder was not found")]
    [AllowAnonymous]
    [HttpGet("{folderId}")]
    public async Task<FolderContentDto<T>> GetFolderAsync(GetFolderRequestDto<T> inDto)
    {

        var split = inDto.Extension == null ? [] : inDto.Extension.Split(",");
        var folder = await folderContentDtoHelper.GetAsync(inDto.FolderId, inDto.UserIdOrGroupId, inDto.FilterType, inDto.RoomId, true, true, inDto.ExcludeSubject, inDto.ApplyFilterOption, inDto.SearchArea, split);

        return folder.NotFoundIfNull();
    }

    /// <summary>
    /// Returns the detailed information about a folder with the ID specified in the request.
    /// </summary>
    /// <short>Get folder information</short>
    /// <path>api/2.0/files/folder/{folderId}</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "Folder parameters", typeof(FolderDto<int>))]
    [AllowAnonymous]
    [HttpGet("folder/{folderId}")]
    public async Task<FolderDto<T>> GetFolderInfoAsync(FolderIdRequestDto<T> inDto)
    {        
        var folder = await fileStorageService.GetFolderAsync(inDto.FolderId).NotFoundIfNull("Folder not found");

        return await _folderDtoHelper.GetAsync(folder);
    }

    /// <summary>
    /// Returns a path to the folder with the ID specified in the request.
    /// </summary>
    /// <short>Get the folder path</short>
    /// <path>api/2.0/files/folder/{folderId}/path</path>
    /// <collection>list</collection>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "List of file entry information", typeof(FileEntryDto))]
    [SwaggerResponse(403, "You don't have enough permission to view the folder content")]
    [HttpGet("folder/{folderId}/path")]
    public async IAsyncEnumerable<FileEntryDto> GetFolderPathAsync(FolderIdRequestDto<T> inDto)
    {
        var breadCrumbs = await breadCrumbsManager.GetBreadCrumbsAsync(inDto.FolderId);

        foreach (var e in breadCrumbs)
        {
            yield return await GetFileEntryWrapperAsync(e);
        }
    }

    /// <summary>
    /// Returns a list of all the subfolders from a folder with the ID specified in the request.
    /// </summary>
    /// <short>Get subfolders</short>
    /// <path>api/2.0/files/{folderId}/subfolders</path>
    /// <collection>list</collection>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "List of file entry information", typeof(FileEntryDto))]
    [SwaggerResponse(403, "You don't have enough permission to view the folder content")]
    [HttpGet("{folderId}/subfolders")]
    public async IAsyncEnumerable<FileEntryDto> GetFoldersAsync(FolderIdRequestDto<T> inDto)
    {
        var folders = await fileStorageService.GetFoldersAsync(inDto.FolderId);
        foreach (var folder in folders)
        {
            yield return await GetFileEntryWrapperAsync(folder);
        }
    }

    /// <summary>
    /// Returns a list of all the new items from a folder with the ID specified in the request.
    /// </summary>
    /// <short>Get new folder items</short>
    /// <path>api/2.0/files/{folderId}/news</path>
    /// <collection>list</collection>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "List of file entry information", typeof(FileEntryDto))]
    [SwaggerResponse(403, "You don't have enough permission to view the folder content")]
    [HttpGet("{folderId}/news")]
    public async IAsyncEnumerable<FileEntryDto> GetNewItemsAsync(FolderIdRequestDto<T> inDto)
    {
        var newItems = await fileStorageService.GetNewItemsAsync(inDto.FolderId);

        foreach (var e in newItems)
        {
            yield return await GetFileEntryWrapperAsync(e);
        }
    }

    /// <summary>
    /// Renames the selected folder with a new title specified in the request.
    /// </summary>
    /// <short>Rename a folder</short>
    /// <path>api/2.0/files/folder/{folderId}</path>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "Folder parameters", typeof(FolderDto<int>))]
    [SwaggerResponse(403, "You don't have enough permission to rename the folder")]
    [HttpPut("folder/{folderId}")]
    public async Task<FolderDto<T>> RenameFolderAsync(CreateFolderRequestDto<T> inDto)
    {        
        var folder = await fileStorageService.FolderRenameAsync(inDto.FolderId, inDto.Folder.Title);

        return await _folderDtoHelper.GetAsync(folder);
    }

    /// <summary>
    /// Returns the used space of files in the root folders.
    /// </summary>
    /// <short>Get used space of files</short>
    /// <path>api/2.0/files/filesusedspace</path>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "Used space of files in the root folders", typeof(FilesStatisticsResultDto))]
    [HttpGet("filesusedspace")]
    public async Task<FilesStatisticsResultDto> GetFilesUsedSpace()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        return await fileStorageService.GetFilesUsedSpace();
    }

    /// <summary>
    /// Returns the primary external link by the identifier specified in the request.
    /// </summary>
    /// <short>Get primary external link</short>
    /// <path>api/2.0/files/folder/{id}/link</path>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "Folder security information", typeof(FileShareDto))]
    [SwaggerResponse(404, "Not Found")]
    [AllowAnonymous]
    [HttpGet("folder/{id}/link")]
    public async Task<FileShareDto> GetFolderPrimaryExternalLinkAsync(FolderPrimaryIdRequestDto<T> inDto)
    {
        var linkAce = await fileStorageService.GetPrimaryExternalLinkAsync(inDto.Id, FileEntryType.Folder);

        return await fileShareDtoHelper.Get(linkAce);
    }
}

public class FoldersControllerCommon(
    GlobalFolderHelper globalFolderHelper,
    FolderContentDtoHelper folderContentDtoHelper,
    FolderDtoHelper folderDtoHelper,
    FileDtoHelper fileDtoHelper,
    UserManager userManager,
    SecurityContext securityContext)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
{
    /// <summary>
    /// Returns the detailed list of files and folders located in the "Common" section.
    /// </summary>
    /// <short>Get the "Common" section</short>
    /// <path>api/2.0/files/@common</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The \"Common\" section contents", typeof(FolderContentDto<int>))]
    [SwaggerResponse(403, "You don't have enough permission to view the folder content")]
    [SwaggerResponse(404, "The required folder was not found")]
    [HttpGet("@common")]
    public async Task<FolderContentDto<int>> GetCommonFolderAsync(GetCommonFolderRequestDto inDto)
    {
        return await folderContentDtoHelper.GetAsync(await globalFolderHelper.FolderCommonAsync, inDto.UserIdOrGroupId, inDto.FilterType, 0, true, true, false, ApplyFilterOption.All, null);
    }

    /// <summary>
    /// Returns the detailed list of files and folders located in the "Favorites" section.
    /// </summary>
    /// <short>Get the "Favorites" section</short>
    /// <path>api/2.0/files/@favorites</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The \"Favorites\" section contents", typeof(FolderContentDto<int>))]
    [SwaggerResponse(403, "You don't have enough permission to view the folder content")]
    [SwaggerResponse(404, "The required folder was not found")]
    [HttpGet("@favorites")]
    public async Task<FolderContentDto<int>> GetFavoritesFolderAsync(GetCommonFolderRequestDto inDto)
    {
        return await folderContentDtoHelper.GetAsync(await globalFolderHelper.FolderFavoritesAsync, inDto.UserIdOrGroupId, inDto.FilterType, 0, true, true, false, ApplyFilterOption.All, null);
    }

    /// <summary>
    /// Returns the detailed list of files and folders located in the "My documents" section.
    /// </summary>
    /// <short>Get the "My documents" section</short>
    /// <path>api/2.0/files/@my</path>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The \"My documents\" section contents", typeof(FolderContentDto<int>))]
    [SwaggerResponse(403, "You don't have enough permission to view the folder content")]
    [SwaggerResponse(404, "The required folder was not found")]
    [HttpGet("@my")]
    public async Task<FolderContentDto<int>> GetMyFolderAsync(GetMyTrashFolderRequestDto inDto)
    {
        return await folderContentDtoHelper.GetAsync(await globalFolderHelper.FolderMyAsync, inDto.UserIdOrGroupId, inDto.FilterType, 0, true, true, false, inDto.ApplyFilterOption, null);
    }

    /// <summary>
    /// Returns the detailed list of files and folders located in the "Private Room" section.
    /// </summary>
    /// <short>Get the "Private Room" section</short>
    /// <path>api/2.0/files/@privacy</path>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The \"Private Room\" section contents", typeof(FolderContentDto<int>))]
    [SwaggerResponse(403, "You don't have enough permission to view the folder content")]
    [SwaggerResponse(404, "The required folder was not found")]
    [HttpGet("@privacy")]
    public async Task<FolderContentDto<int>> GetPrivacyFolderAsync(GetCommonFolderRequestDto inDto)
    {
        if (PrivacyRoomSettings.IsAvailable())
        {
            throw new SecurityException();
        }

        return await folderContentDtoHelper.GetAsync(await globalFolderHelper.FolderPrivacyAsync, inDto.UserIdOrGroupId, inDto.FilterType, 0, true, true, false, ApplyFilterOption.All, null);
    }

    /// <summary>
    /// Returns the detailed list of files and folders located in the "In projects" section.
    /// </summary>
    /// <short>Get the "In projects" section</short>
    /// <path>api/2.0/files/@projects</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The \"In projects\" section contents", typeof(FolderContentDto<string>))]
    [SwaggerResponse(403, "You don't have enough permission to view the folder content")]
    [SwaggerResponse(404, "The required folder was not found")]
    [HttpGet("@projects")]
    public async Task<FolderContentDto<string>> GetProjectsFolderAsync(GetCommonFolderRequestDto inDto)
    {
        return await folderContentDtoHelper.GetAsync(await globalFolderHelper.GetFolderProjectsAsync<string>(), inDto.UserIdOrGroupId, inDto.FilterType, null, true, true, false, ApplyFilterOption.All, null);
    }

    /// <summary>
    /// Returns the detailed list of files located in the "Recent" section.
    /// </summary>
    /// <short>Get the "Recent" section</short>
    /// <path>api/2.0/files/@recent</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The \"Recent\" section contents", typeof(FolderContentDto<int>))]
    [SwaggerResponse(403, "You don't have enough permission to view the folder content")]
    [SwaggerResponse(404, "The required folder was not found")]
    [HttpGet("recent")]
    public async Task<FolderContentDto<int>> GetRecentFolderAsync(GetRecentFolderRequestDto inDto)
    {
        return await folderContentDtoHelper.GetAsync(await globalFolderHelper.FolderRecentAsync, inDto.UserIdOrGroupId, inDto.FilterType, 0, true, true, inDto.ExcludeSubject, inDto.ApplyFilterOption, inDto.SearchArea, inDto.Extension);
    }

    /// <summary>
    /// Returns all the sections matching the parameters specified in the request.
    /// </summary>
    /// <short>Get filtered sections</short>
    /// <path>api/2.0/files/@root</path>
    /// <collection>list</collection>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "List of section contents with the following parameters", typeof(FolderContentDto<int>))]
    [SwaggerResponse(403, "You don't have enough permission to view the folder content")]
    [SwaggerResponse(404, "The required folder was not found")]
    [HttpGet("@root")]
    public async IAsyncEnumerable<FolderContentDto<int>> GetRootFoldersAsync(GetRootFolderRequestDto inDto)
    {
        var foldersIds = GetRootFoldersIdsAsync(inDto.WithoutTrash ?? false);

        await foreach (var folder in foldersIds)
        {
            yield return await folderContentDtoHelper.GetAsync(folder, inDto.UserIdOrGroupId, inDto.FilterType, 0, true, true, false, ApplyFilterOption.All, null);
        }
    }

    /// <summary>
    /// Returns the detailed list of files and folders located in the "Shared with me" section.
    /// </summary>
    /// <short>Get the "Shared with me" section</short>
    /// <path>api/2.0/files/@share</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The \"Shared with me\" section contents", typeof(FolderContentDto<int>))]
    [SwaggerResponse(403, "You don't have enough permission to view the folder content")]
    [SwaggerResponse(404, "The required folder was not found")]
    [HttpGet("@share")]
    public async Task<FolderContentDto<int>> GetShareFolderAsync(GetCommonFolderRequestDto inDto)
    {
        return await folderContentDtoHelper.GetAsync(await globalFolderHelper.FolderShareAsync, inDto.UserIdOrGroupId, inDto.FilterType, 0, true, true, false, ApplyFilterOption.All, null);
    }

    /// <summary>
    /// Returns the detailed list of files located in the "Templates" section.
    /// </summary>
    /// <short>Get the "Templates" section</short>
    /// <path>api/2.0/files/@templates</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The \"Templates\" section contents", typeof(FolderContentDto<int>))]
    [SwaggerResponse(403, "You don't have enough permission to view the folder content")]
    [SwaggerResponse(404, "The required folder was not found")]
    [HttpGet("@templates")]
    public async Task<FolderContentDto<int>> GetTemplatesFolderAsync(GetCommonFolderRequestDto inDto)
    {
        return await folderContentDtoHelper.GetAsync(await globalFolderHelper.FolderTemplatesAsync, inDto.UserIdOrGroupId, inDto.FilterType, 0, true, true, false, ApplyFilterOption.All, null);
    }

    /// <summary>
    /// Returns the detailed list of files and folders located in the "Trash" section.
    /// </summary>
    /// <short>Get the "Trash" section</short>
    /// <path>api/2.0/files/@trash</path>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The \"Trash\" section contents", typeof(FolderContentDto<int>))]
    [SwaggerResponse(403, "You don't have enough permission to view the folder content")]
    [SwaggerResponse(404, "The required folder was not found")]
    [HttpGet("@trash")]
    public async Task<FolderContentDto<int>> GetTrashFolderAsync(GetMyTrashFolderRequestDto inDto)
    {
        return await folderContentDtoHelper.GetAsync(Convert.ToInt32(await globalFolderHelper.FolderTrashAsync), inDto.UserIdOrGroupId, inDto.FilterType, 0, true, true, false, inDto.ApplyFilterOption, null);
    }
    
    private async IAsyncEnumerable<int> GetRootFoldersIdsAsync(bool withoutTrash)
    {
        var user = await userManager.GetUsersAsync(securityContext.CurrentAccount.ID);
        var isGuest = await userManager.IsGuestAsync(user);
        var isOutsider = await userManager.IsOutsiderAsync(user);

        if (isOutsider)
        {
            withoutTrash = true;
        }

        if (!isGuest)
        {
            yield return await globalFolderHelper.FolderMyAsync;
        }

        if (!withoutTrash)
        {
            yield return await globalFolderHelper.FolderTrashAsync;
        }

        yield return await globalFolderHelper.FolderVirtualRoomsAsync;
        yield return await globalFolderHelper.FolderArchiveAsync;
    }
}
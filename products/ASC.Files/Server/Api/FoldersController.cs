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
    /// <category>Folders</category>
    /// <param type="System.Int32, System" name="folderId">Folder ID</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.HistoryDto, ASC.Files.Core">List of actions in the folder</returns>
    /// <path>api/2.0/files/folder/{folderId}/log</path>
    /// <httpMethod>GET</httpMethod>
    /// <collection>list</collection>
    [HttpGet("folder/{folderId:int}/log")]
    public IAsyncEnumerable<HistoryDto> GetHistoryAsync(int folderId)
    {
        return historyApiHelper.GetFolderHistoryAsync(folderId);
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
    /// <short>
    /// Create a folder
    /// </short>
    /// <category>Folders</category>
    /// <param type="System.Int32, System" method="url" name="folderId">Parent folder ID</param>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.CreateFolderRequestDto, ASC.Files.Core" name="inDto">Request parameters for creating a folder</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FolderDto, ASC.Files.Core">New folder parameters</returns>
    /// <path>api/2.0/files/folder/{folderId}</path>
    /// <httpMethod>POST</httpMethod>
    [HttpPost("folder/{folderId}")]
    public async Task<FolderDto<T>> CreateFolderAsync(T folderId, CreateFolderRequestDto inDto)
    {
        var folder = await fileStorageService.CreateFolderAsync(folderId, inDto.Title);

        return await _folderDtoHelper.GetAsync(folder);
    }

    /// <summary>
    /// Deletes a folder with the ID specified in the request.
    /// </summary>
    /// <short>Delete a folder</short>
    /// <category>Folders</category>
    /// <param type="System.Int32, System" method="url" name="folderId">Folder ID</param>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.DeleteFolderDto, ASC.Files.Core" name="inDto">Request parameters for deleting a folder</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FileOperationDto, ASC.Files.Core">List of file operations</returns>
    /// <path>api/2.0/files/folder/{folderId}</path>
    /// <httpMethod>DELETE</httpMethod>
    /// <collection>list</collection>
    [HttpDelete("folder/{folderId}")]
    public async IAsyncEnumerable<FileOperationDto> DeleteFolder(T folderId, DeleteFolderDto inDto)
    {
        await fileOperationsManager.PublishDelete(new List<T> { folderId }, new List<T>(), false, !inDto.DeleteAfter, inDto.Immediately);
        
        foreach (var e in await fileOperationsManager.GetOperationResults())
        {
            yield return await fileOperationDtoHelper.GetAsync(e);
        }
    }

    [HttpPut("folder/{folderId}/order")]
    public async Task SetOrder(T folderId, OrderRequestDto inDto)
    {
        await fileStorageService.SetFolderOrder(folderId, inDto.Order);
    }

    /// <summary>
    /// Returns the detailed list of files and folders located in the folder with the ID specified in the request.
    /// </summary>
    /// <short>
    /// Get a folder by ID
    /// </short>
    /// <category>Folders</category>
    /// <param type="System.Int32, System" method="url" name="folderId">Folder ID</param>
    /// <param type="System.Nullable{System.Guid}, System" name="userIdOrGroupId" optional="true">User or group ID</param>
    /// <param type="System.Nullable{ASC.Files.Core.FilterType}, System" name="filterType" optional="true" remark="Allowed values: None (0), FilesOnly (1), FoldersOnly (2), DocumentsOnly (3), PresentationsOnly (4), SpreadsheetsOnly (5), ImagesOnly (7), ByUser (8), ByDepartment (9), ArchiveOnly (10), ByExtension (11), MediaOnly (12), EditingRooms (14), CustomRooms (17), OFormTemplateOnly (18), OFormOnly (19)">Filter type</param>
    /// <param type="System.Int32, System" name="roomId">Room ID</param>
    /// <param type="System.Nullable{System.Boolean}, System" name="searchInContent">Specifies whether to search within the section contents or not</param>
    /// <param type="System.Nullable{System.Boolean}, System" name="withsubfolders">Specifies whether to return sections with or without subfolders</param>
    /// <param type="System.Nullable{System.Boolean}, System" name="excludeSubject">Specifies whether to exclude a subject or not</param>
    /// <param type="System.Nullable{ASC.Files.Core.Core.ApplyFilterOption}, System" name="applyFilterOption">Specifies whether to return only files, only folders or all elements from the specified folder</param>
    /// <param type="System.String, System" name="extension">Specifies whether to search for a specific file extension</param>
    /// <param type="ASC.Files.Core.VirtualRooms.SearchArea, ASC.Files.Core" name="searchArea" optional="true" remark="Allowed values: Active (0), Archive (1), Any (2), RecentByLinks (3)">Search area</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FolderContentDto, ASC.Files.Core">Folder contents</returns>
    /// <path>api/2.0/files/{folderId}</path>
    /// <httpMethod>GET</httpMethod>
    /// <requiresAuthorization>false</requiresAuthorization>
    [AllowAnonymous]
    [HttpGet("{folderId}")]
    public async Task<FolderContentDto<T>> GetFolderAsync(T folderId, Guid? userIdOrGroupId, FilterType? filterType, T roomId, bool? searchInContent, bool? withsubfolders, bool? excludeSubject,
        ApplyFilterOption? applyFilterOption, string extension, SearchArea searchArea)
    {

        var split = extension == null ? [] : extension.Split(",");
        var folder = await folderContentDtoHelper.GetAsync(folderId, userIdOrGroupId, filterType, roomId, searchInContent, withsubfolders, excludeSubject, applyFilterOption, searchArea, split);

        return folder.NotFoundIfNull();
    }

    /// <summary>
    /// Returns the detailed information about a folder with the ID specified in the request.
    /// </summary>
    /// <short>Get folder information</short>
    /// <param type="System.Int32, System" method="url" name="folderId">Folder ID</param>
    /// <category>Folders</category>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FolderDto, ASC.Files.Core">Folder parameters</returns>
    /// <path>api/2.0/files/folder/{folderId}</path>
    /// <httpMethod>GET</httpMethod>
    /// <requiresAuthorization>false</requiresAuthorization>
    [AllowAnonymous]
    [HttpGet("folder/{folderId}")]
    public async Task<FolderDto<T>> GetFolderInfoAsync(T folderId)
    {        
        var folder = await fileStorageService.GetFolderAsync(folderId).NotFoundIfNull("Folder not found");

        return await _folderDtoHelper.GetAsync(folder);
    }

    /// <summary>
    /// Returns a path to the folder with the ID specified in the request.
    /// </summary>
    /// <short>Get the folder path</short>
    /// <param type="System.Int32, System" method="url" name="folderId">Folder ID</param>
    /// <category>Folders</category>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FileEntryDto, ASC.Files.Core">List of file entry information</returns>
    /// <path>api/2.0/files/folder/{folderId}/path</path>
    /// <httpMethod>GET</httpMethod>
    /// <collection>list</collection>
    [HttpGet("folder/{folderId}/path")]
    public async IAsyncEnumerable<FileEntryDto> GetFolderPathAsync(T folderId)
    {
        var breadCrumbs = await breadCrumbsManager.GetBreadCrumbsAsync(folderId);

        foreach (var e in breadCrumbs)
        {
            yield return await GetFileEntryWrapperAsync(e);
        }
    }

    /// <summary>
    /// Returns a list of all the subfolders from a folder with the ID specified in the request.
    /// </summary>
    /// <short>Get subfolders</short>
    /// <param type="System.Int32, System" method="url" name="folderId">Folder ID</param>
    /// <category>Folders</category>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FileEntryDto, ASC.Files.Core">List of file entry information</returns>
    /// <path>api/2.0/files/{folderId}/subfolders</path>
    /// <httpMethod>GET</httpMethod>
    /// <collection>list</collection>
    [HttpGet("{folderId}/subfolders")]
    public async IAsyncEnumerable<FileEntryDto> GetFoldersAsync(T folderId)
    {
        var folders = await fileStorageService.GetFoldersAsync(folderId);
        foreach (var folder in folders)
        {
            yield return await GetFileEntryWrapperAsync(folder);
        }
    }

    /// <summary>
    /// Returns a list of all the new items from a folder with the ID specified in the request.
    /// </summary>
    /// <short>Get new folder items</short>
    /// <param type="System.Int32, System" method="url" name="folderId">Folder ID</param>
    /// <category>Folders</category>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FileEntryDto, ASC.Files.Core">List of file entry information</returns>
    /// <path>api/2.0/files/{folderId}/news</path>
    /// <httpMethod>GET</httpMethod>
    /// <collection>list</collection>
    [HttpGet("{folderId}/news")]
    public async IAsyncEnumerable<FileEntryDto> GetNewItemsAsync(T folderId)
    {
        var newItems = await fileStorageService.GetNewItemsAsync(folderId);

        foreach (var e in newItems)
        {
            yield return await GetFileEntryWrapperAsync(e);
        }
    }

    /// <summary>
    /// Renames the selected folder with a new title specified in the request.
    /// </summary>
    /// <short>
    /// Rename a folder
    /// </short>
    /// <category>Folders</category>
    /// <param type="System.Int32, System" method="url" name="folderId">Folder ID</param>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.CreateFolderRequestDto, ASC.Files.Core" name="inDto">Request parameters for creating a folder: Title (string) - new folder title</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FolderDto, ASC.Files.Core">Folder parameters</returns>
    /// <path>api/2.0/files/folder/{folderId}</path>
    /// <httpMethod>PUT</httpMethod>
    [HttpPut("folder/{folderId}")]
    public async Task<FolderDto<T>> RenameFolderAsync(T folderId, CreateFolderRequestDto inDto)
    {        
        var folder = await fileStorageService.FolderRenameAsync(folderId, inDto.Title);

        return await _folderDtoHelper.GetAsync(folder);
    }

    /// <summary>
    /// Returns the used space of files in the root folders.
    /// </summary>
    /// <short>Get used space of files</short>
    /// <category>Folders</category>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FilesStatisticsResultDto, ASC.Files.Core">Used space of files in the root folders</returns>
    /// <path>api/2.0/files/filesusedspace</path>
    /// <httpMethod>GET</httpMethod>
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
    /// <category>Folders</category>
    /// <param type="System.Int32, System" method="url" name="id">Folder Id</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FileShareDto, ASC.Files.Core">Folder security information</returns>
    /// <path>api/2.0/files/folder/{id}/link</path>
    /// <httpMethod>GET</httpMethod>
    [AllowAnonymous]
    [HttpGet("folder/{id}/link")]
    public async Task<FileShareDto> GetPrimaryExternalLinkAsync(T id)
    {
        var linkAce = await fileStorageService.GetPrimaryExternalLinkAsync(id, FileEntryType.Folder);

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
    /// <category>Folders</category>
    /// <param type="System.Nullable{System.Guid}, System" name="userIdOrGroupId" optional="true">User or group ID</param>
    /// <param type="System.Nullable{ASC.Files.Core.FilterType}, System" name="filterType" optional="true" remark="Allowed values: None (0), FilesOnly (1), FoldersOnly (2), DocumentsOnly (3), PresentationsOnly (4), SpreadsheetsOnly (5), ImagesOnly (7), ByUser (8), ByDepartment (9), ArchiveOnly (10), ByExtension (11), MediaOnly (12), EditingRooms (14), CustomRooms (17), OFormTemplateOnly (18), OFormOnly (19)">Filter type</param>
    /// <param type="System.Nullable{System.Boolean}, System" name="searchInContent">Specifies whether to search within the section contents or not</param>
    /// <param type="System.Nullable{System.Boolean}, System" name="withsubfolders">Specifies whether to return sections with or without subfolders</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FolderContentDto, ASC.Files.Core">The "Common" section contents</returns>
    /// <path>api/2.0/files/@common</path>
    /// <httpMethod>GET</httpMethod>
    /// <visible>false</visible>
    [HttpGet("@common")]
    public async Task<FolderContentDto<int>> GetCommonFolderAsync(Guid? userIdOrGroupId, FilterType? filterType, bool? searchInContent, bool? withsubfolders)
    {
        return await folderContentDtoHelper.GetAsync(await globalFolderHelper.FolderCommonAsync, userIdOrGroupId, filterType, default, searchInContent, withsubfolders,
            false, ApplyFilterOption.All, null);
    }

    /// <summary>
    /// Returns the detailed list of files and folders located in the "Favorites" section.
    /// </summary>
    /// <short>Get the "Favorites" section</short>
    /// <category>Folders</category>
    /// <param type="System.Nullable{System.Guid}, System" name="userIdOrGroupId" optional="true">User or group ID</param>
    /// <param type="System.Nullable{ASC.Files.Core.FilterType}, System" name="filterType" optional="true" remark="Allowed values: None (0), FilesOnly (1), FoldersOnly (2), DocumentsOnly (3), PresentationsOnly (4), SpreadsheetsOnly (5), ImagesOnly (7), ByUser (8), ByDepartment (9), ArchiveOnly (10), ByExtension (11), MediaOnly (12), EditingRooms (14), CustomRooms (17), OFormTemplateOnly (18), OFormOnly (19)">Filter type</param>
    /// <param type="System.Nullable{System.Boolean}, System" name="searchInContent">Specifies whether to search within the section contents or not</param>
    /// <param type="System.Nullable{System.Boolean}, System" name="withsubfolders">Specifies whether to return sections with or without subfolders</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FolderContentDto, ASC.Files.Core">The "Favorites" section contents</returns>
    /// <path>api/2.0/files/@favorites</path>
    /// <httpMethod>GET</httpMethod>
    /// <visible>false</visible>
    [HttpGet("@favorites")]
    public async Task<FolderContentDto<int>> GetFavoritesFolderAsync(Guid? userIdOrGroupId, FilterType? filterType, bool? searchInContent, bool? withsubfolders)
    {
        return await folderContentDtoHelper.GetAsync(await globalFolderHelper.FolderFavoritesAsync, userIdOrGroupId, filterType, default, searchInContent, withsubfolders,
            false, ApplyFilterOption.All, null);
    }

    /// <summary>
    /// Returns the detailed list of files and folders located in the "My documents" section.
    /// </summary>
    /// <short>Get the "My documents" section</short>
    /// <category>Folders</category>
    /// <param type="System.Nullable{System.Guid}, System" name="userIdOrGroupId" optional="true">User or group ID</param>
    /// <param type="System.Nullable{ASC.Files.Core.FilterType}, System" name="filterType" optional="true" remark="Allowed values: None (0), FilesOnly (1), FoldersOnly (2), DocumentsOnly (3), PresentationsOnly (4), SpreadsheetsOnly (5), ImagesOnly (7), ByUser (8), ByDepartment (9), ArchiveOnly (10), ByExtension (11), MediaOnly (12), EditingRooms (14), CustomRooms (17), OFormTemplateOnly (18), OFormOnly (19)">Filter type</param>
    /// <param type="System.Nullable{System.Boolean}, System" name="searchInContent">Specifies whether to search within the section contents or not</param>
    /// <param type="System.Nullable{System.Boolean}, System" name="withsubfolders">Specifies whether to return sections with or without subfolders</param>
    /// <param type="System.Nullable{ASC.Files.Core.Core.ApplyFilterOption}, System" name="applyFilterOption">Specifies whether to return only files, only folders or all elements from the specified folder</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FolderContentDto, ASC.Files.Core">The "My documents" section contents</returns>
    /// <path>api/2.0/files/@my</path>
    /// <httpMethod>GET</httpMethod>
    [HttpGet("@my")]
    public async Task<FolderContentDto<int>> GetMyFolderAsync(Guid? userIdOrGroupId, FilterType? filterType, bool? searchInContent, bool? withsubfolders, ApplyFilterOption? applyFilterOption)
    {
        return await folderContentDtoHelper.GetAsync(await globalFolderHelper.FolderMyAsync, userIdOrGroupId, filterType, default, searchInContent, withsubfolders,
            false, applyFilterOption, null);
    }

    /// <summary>
    /// Returns the detailed list of files and folders located in the "Private Room" section.
    /// </summary>
    /// <short>Get the "Private Room" section</short>
    /// <category>Folders</category>
    /// <param type="System.Nullable{System.Guid}, System" name="userIdOrGroupId" optional="true">User or group ID</param>
    /// <param type="System.Nullable{ASC.Files.Core.FilterType}, System" name="filterType" optional="true" remark="Allowed values: None (0), FilesOnly (1), FoldersOnly (2), DocumentsOnly (3), PresentationsOnly (4), SpreadsheetsOnly (5), ImagesOnly (7), ByUser (8), ByDepartment (9), ArchiveOnly (10), ByExtension (11), MediaOnly (12), EditingRooms (14), CustomRooms (17), OFormTemplateOnly (18), OFormOnly (19)">Filter type</param>
    /// <param type="System.Nullable{System.Boolean}, System" name="searchInContent">Specifies whether to search within the section contents or not</param>
    /// <param type="System.Nullable{System.Boolean}, System" name="withsubfolders">Specifies whether to return sections with or without subfolders</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FolderContentDto, ASC.Files.Core">The "Private Room" section contents</returns>
    /// <path>api/2.0/files/@privacy</path>
    /// <httpMethod>GET</httpMethod>
    [HttpGet("@privacy")]
    public async Task<FolderContentDto<int>> GetPrivacyFolderAsync(Guid? userIdOrGroupId, FilterType? filterType, bool? searchInContent, bool? withsubfolders)
    {
        if (PrivacyRoomSettings.IsAvailable())
        {
            throw new SecurityException();
        }

        return await folderContentDtoHelper.GetAsync(await globalFolderHelper.FolderPrivacyAsync, userIdOrGroupId, filterType, default, searchInContent, withsubfolders,
            false, ApplyFilterOption.All, null);
    }

    /// <summary>
    /// Returns the detailed list of files and folders located in the "In projects" section.
    /// </summary>
    /// <short>Get the "In projects" section</short>
    /// <category>Folders</category>
    /// <param type="System.Nullable{System.Guid}, System" name="userIdOrGroupId" optional="true">User or group ID</param>
    /// <param type="System.Nullable{ASC.Files.Core.FilterType}, System" name="filterType" optional="true" remark="Allowed values: None (0), FilesOnly (1), FoldersOnly (2), DocumentsOnly (3), PresentationsOnly (4), SpreadsheetsOnly (5), ImagesOnly (7), ByUser (8), ByDepartment (9), ArchiveOnly (10), ByExtension (11), MediaOnly (12), EditingRooms (14), CustomRooms (17), OFormTemplateOnly (18), OFormOnly (19)">Filter type</param>
    /// <param type="System.Nullable{System.Boolean}, System" name="searchInContent">Specifies whether to search within the section contents or not</param>
    /// <param type="System.Nullable{System.Boolean}, System" name="withsubfolders">Specifies whether to return sections with or without subfolders</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FolderContentDto, ASC.Files.Core">The "In projects" section contents</returns>
    /// <path>api/2.0/files/@projects</path>
    /// <httpMethod>GET</httpMethod>
    /// <visible>false</visible>
    [HttpGet("@projects")]
    public async Task<FolderContentDto<string>> GetProjectsFolderAsync(Guid? userIdOrGroupId, FilterType? filterType, bool? searchInContent, bool? withsubfolders)
    {
        return await folderContentDtoHelper.GetAsync(await globalFolderHelper.GetFolderProjectsAsync<string>(), userIdOrGroupId, filterType, default, searchInContent, withsubfolders,
            false, ApplyFilterOption.All, null);
    }

    /// <summary>
    /// Returns the detailed list of files located in the "Recent" section.
    /// </summary>
    /// <short>Get the "Recent" section</short>
    /// <category>Folders</category>
    /// <param type="System.Nullable{System.Guid}, System" name="userIdOrGroupId" optional="true">User or group ID</param>
    /// <param type="System.Nullable{ASC.Files.Core.FilterType}, ASC.Files.Core" name="filterType" optional="true" remark="Allowed values: None (0), FilesOnly (1), FoldersOnly (2), DocumentsOnly (3), PresentationsOnly (4), SpreadsheetsOnly (5), ImagesOnly (7), ByUser (8), ByDepartment (9), ArchiveOnly (10), ByExtension (11), MediaOnly (12), EditingRooms (14), CustomRooms (17), OFormTemplateOnly (18), OFormOnly (19)">Filter type</param>
    /// <param type="System.Nullable{System.Boolean}, System" name="searchInContent">Specifies whether to search within the section contents or not</param>
    /// <param type="System.Nullable{System.Boolean}, System" name="withsubfolders">Specifies whether to return sections with or without subfolders</param>
    /// <param type="System.Nullable{System.Boolean}, System" name="excludeSubject">Exclude a subject from the search</param>
    /// <param type="System.Nullable{ASC.Files.Core.Core.ApplyFilterOption}, ASC.Files.Core" name="applyFilterOption" optional="true" remark="Allowed values: All (0), Files (1), Folders (2)">Scope of filters</param>
    /// <param type="System.Nullable{ASC.Files.Core.VirtualRooms.SearchArea}, ASC.Files.Core" name="searchArea" optional="true" remark="Allowed values: Any (2), RecentByLinks (3)">Search area</param>
    /// <param type="System.String, System" name="extension">Specifies whether to search for a specific file extension</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FolderContentDto, ASC.Files.Core">The "Recent" section contents</returns>
    /// <path>api/2.0/files/@recent</path>
    /// <httpMethod>GET</httpMethod>
    /// <visible>false</visible>
    [HttpGet("recent")]
    public async Task<FolderContentDto<int>> GetRecentFolderAsync(Guid? userIdOrGroupId, FilterType? filterType, bool? searchInContent, bool? withsubfolders, bool? excludeSubject, 
        ApplyFilterOption? applyFilterOption, SearchArea? searchArea, string[] extension)
    {
        return await folderContentDtoHelper.GetAsync(await globalFolderHelper.FolderRecentAsync, userIdOrGroupId, filterType, default, searchInContent, withsubfolders,
            excludeSubject, applyFilterOption, searchArea, extension);
    }

    /// <summary>
    /// Returns all the sections matching the parameters specified in the request.
    /// </summary>
    /// <short>Get filtered sections</short>
    /// <param type="System.Nullable{System.Guid}, System" name="userIdOrGroupId" optional="true">User or group ID</param>
    /// <param type="System.Nullable{ASC.Files.Core.FilterType}, System" name="filterType" optional="true" remark="Allowed values: None (0), FilesOnly (1), FoldersOnly (2), DocumentsOnly (3), PresentationsOnly (4), SpreadsheetsOnly (5), ImagesOnly (7), ByUser (8), ByDepartment (9), ArchiveOnly (10), ByExtension (11), MediaOnly (12), EditingRooms (14), CustomRooms (17), OFormTemplateOnly (18), OFormOnly (19)">Filter type</param>
    /// <param type="System.Nullable{System.Boolean}, System" name="withsubfolders">Specifies whether to return sections with or without subfolders</param>
    /// <param type="System.Nullable{System.Boolean}, System" name="withoutTrash">Specifies whether to return the "Trash" section or not</param>
    /// <param type="System.Nullable{System.Boolean}, System" name="searchInContent">Specifies whether to search within the section contents or not</param>
    /// <category>Folders</category>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FolderContentDto, ASC.Files.Core">List of section contents with the following parameters</returns>
    /// <path>api/2.0/files/@root</path>
    /// <httpMethod>GET</httpMethod>
    /// <collection>list</collection>
    [HttpGet("@root")]
    public async IAsyncEnumerable<FolderContentDto<int>> GetRootFoldersAsync(Guid? userIdOrGroupId, FilterType? filterType, bool? withsubfolders, bool? withoutTrash, bool? searchInContent)
    {
        var foldersIds = GetRootFoldersIdsAsync(withoutTrash ?? false);

        await foreach (var folder in foldersIds)
        {
            yield return await folderContentDtoHelper.GetAsync(folder, userIdOrGroupId, filterType, default, searchInContent, withsubfolders, false, ApplyFilterOption.All, null);
        }
    }

    /// <summary>
    /// Returns the detailed list of files and folders located in the "Shared with me" section.
    /// </summary>
    /// <short>Get the "Shared with me" section</short>
    /// <category>Folders</category>
    /// <param type="System.Nullable{System.Guid}, System" name="userIdOrGroupId" optional="true">User or group ID</param>
    /// <param type="System.Nullable{ASC.Files.Core.FilterType}, System" name="filterType" optional="true" remark="Allowed values: None (0), FilesOnly (1), FoldersOnly (2), DocumentsOnly (3), PresentationsOnly (4), SpreadsheetsOnly (5), ImagesOnly (7), ByUser (8), ByDepartment (9), ArchiveOnly (10), ByExtension (11), MediaOnly (12), EditingRooms (14), CustomRooms (17), OFormTemplateOnly (18), OFormOnly (19)">Filter type</param>
    /// <param type="System.Nullable{System.Boolean}, System" name="searchInContent">Specifies whether to search within the section contents or not</param>
    /// <param type="System.Nullable{System.Boolean}, System" name="withsubfolders">Specifies whether to return sections with or without subfolders</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FolderContentDto, ASC.Files.Core">The "Shared with me" section contents</returns>
    /// <path>api/2.0/files/@share</path>
    /// <httpMethod>GET</httpMethod>
    /// <visible>false</visible>
    [HttpGet("@share")]
    public async Task<FolderContentDto<int>> GetShareFolderAsync(Guid? userIdOrGroupId, FilterType? filterType, bool? searchInContent, bool? withsubfolders)
    {
        return await folderContentDtoHelper.GetAsync(await globalFolderHelper.FolderShareAsync, userIdOrGroupId, filterType, default, searchInContent, withsubfolders,
            false, ApplyFilterOption.All, null);
    }

    /// <summary>
    /// Returns the detailed list of files located in the "Templates" section.
    /// </summary>
    /// <short>Get the "Templates" section</short>
    /// <category>Folders</category>
    /// <param type="System.Nullable{System.Guid}, System" name="userIdOrGroupId" optional="true">User or group ID</param>
    /// <param type="System.Nullable{ASC.Files.Core.FilterType}, System" name="filterType" optional="true" remark="Allowed values: None (0), FilesOnly (1), FoldersOnly (2), DocumentsOnly (3), PresentationsOnly (4), SpreadsheetsOnly (5), ImagesOnly (7), ByUser (8), ByDepartment (9), ArchiveOnly (10), ByExtension (11), MediaOnly (12), EditingRooms (14), CustomRooms (17), OFormTemplateOnly (18), OFormOnly (19)">Filter type</param>
    /// <param type="System.Nullable{System.Boolean}, System" name="searchInContent">Specifies whether to search within the section contents or not</param>
    /// <param type="System.Nullable{System.Boolean}, System" name="withsubfolders">Specifies whether to return sections with or without subfolders</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FolderContentDto, ASC.Files.Core">The "Templates" section contents</returns>
    /// <path>api/2.0/files/@templates</path>
    /// <httpMethod>GET</httpMethod>
    /// <visible>false</visible>
    [HttpGet("@templates")]
    public async Task<FolderContentDto<int>> GetTemplatesFolderAsync(Guid? userIdOrGroupId, FilterType? filterType, bool? searchInContent, bool? withsubfolders)
    {
        return await folderContentDtoHelper.GetAsync(await globalFolderHelper.FolderTemplatesAsync, userIdOrGroupId, filterType, default, searchInContent, withsubfolders,
            false, ApplyFilterOption.All, null);
    }

    /// <summary>
    /// Returns the detailed list of files and folders located in the "Trash" section.
    /// </summary>
    /// <short>Get the "Trash" section</short>
    /// <category>Folders</category>
    /// <param type="System.Nullable{System.Guid}, System" name="userIdOrGroupId" optional="true">User or group ID</param>
    /// <param type="System.Nullable{ASC.Files.Core.FilterType}, System" name="filterType" optional="true" remark="Allowed values: None (0), FilesOnly (1), FoldersOnly (2), DocumentsOnly (3), PresentationsOnly (4), SpreadsheetsOnly (5), ImagesOnly (7), ByUser (8), ByDepartment (9), ArchiveOnly (10), ByExtension (11), MediaOnly (12), EditingRooms (14), CustomRooms (17), OFormTemplateOnly (18), OFormOnly (19)">Filter type</param>
    /// <param type="System.Nullable{System.Boolean}, System" name="searchInContent">Specifies whether to search within the section contents or not</param>
    /// <param type="System.Nullable{System.Boolean}, System" name="withsubfolders">Specifies whether to return sections with or without subfolders</param>
    /// <param type="System.Nullable{ASC.Files.Core.Core.ApplyFilterOption}, System" name="applyFilterOption">Specifies whether to return only files, only folders or all elements from the specified folder</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FolderContentDto, ASC.Files.Core">The "Trash" section contents</returns>
    /// <path>api/2.0/files/@trash</path>
    /// <httpMethod>GET</httpMethod>
    [HttpGet("@trash")]
    public async Task<FolderContentDto<int>> GetTrashFolderAsync(Guid? userIdOrGroupId, FilterType? filterType, bool? searchInContent, bool? withsubfolders, ApplyFilterOption? applyFilterOption)
    {
        return await folderContentDtoHelper.GetAsync(Convert.ToInt32(await globalFolderHelper.FolderTrashAsync), userIdOrGroupId, filterType, default, searchInContent, withsubfolders,
            false, applyFilterOption, null);
    }
    
    private async IAsyncEnumerable<int> GetRootFoldersIdsAsync(bool withoutTrash)
    {
        var user = await userManager.GetUsersAsync(securityContext.CurrentAccount.ID);
        var isUser = await userManager.IsUserAsync(user);
        var isOutsider = await userManager.IsOutsiderAsync(user);

        if (isOutsider)
        {
            withoutTrash = true;
        }

        if (!isUser)
        {
            yield return await globalFolderHelper.FolderMyAsync;
        }

        if (!withoutTrash && !isUser)
        {
            yield return await globalFolderHelper.FolderTrashAsync;
        }

        yield return await globalFolderHelper.FolderVirtualRoomsAsync;
        yield return await globalFolderHelper.FolderArchiveAsync;
    }
}
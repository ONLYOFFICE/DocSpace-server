// (c) Copyright Ascensio System SIA 2010-2023
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

namespace ASC.Files.Helpers;

public class FoldersControllerHelper(FilesSettingsHelper filesSettingsHelper,
        FileUploader fileUploader,
        SocketManager socketManager,
        FileDtoHelper fileDtoHelper,
        ApiContext apiContext,
        FileStorageService fileStorageService,
        FolderContentDtoHelper folderContentDtoHelper,
        IHttpContextAccessor httpContextAccessor,
        FolderDtoHelper folderDtoHelper,
        UserManager userManager,
        SecurityContext securityContext,
        GlobalFolderHelper globalFolderHelper,
        CoreBaseSettings coreBaseSettings,
        FileUtility fileUtility)
    : FilesHelperBase(filesSettingsHelper,
    fileUploader,
    socketManager,
    fileDtoHelper,
    apiContext,
    fileStorageService,
    folderContentDtoHelper,
    httpContextAccessor,
    folderDtoHelper)
{
    public async Task<FolderDto<T>> CreateFolderAsync<T>(T folderId, string title)
    {
        var folder = await _fileStorageService.CreateNewFolderAsync(folderId, title);

        return await _folderDtoHelper.GetAsync(folder);
    }

    public async Task<FolderContentDto<T>> GetFolderAsync<T>(T folderId, Guid? userIdOrGroupId, FilterType? filterType, T roomId, bool? searchInContent, bool? withSubFolders, bool? excludeSubject, ApplyFilterOption? applyFilterOption, SearchArea? searchArea, string[] extension = null)
    {
        var folderContentWrapper = await ToFolderContentWrapperAsync(folderId, userIdOrGroupId ?? Guid.Empty, filterType ?? FilterType.None, roomId, searchInContent ?? false, withSubFolders ?? false, excludeSubject ?? false, applyFilterOption ?? ApplyFilterOption.All, extension, searchArea ?? SearchArea.Active);

        return folderContentWrapper.NotFoundIfNull();
    }

    public async Task<FolderDto<T>> GetFolderInfoAsync<T>(T folderId)
    {
        var folder = await _fileStorageService.GetFolderAsync(folderId).NotFoundIfNull("Folder not found");

        return await _folderDtoHelper.GetAsync(folder);
    }

    public async IAsyncEnumerable<int> GetRootFoldersIdsAsync(bool withoutTrash, bool withoutAdditionalFolder)
    {
        var user = await userManager.GetUsersAsync(securityContext.CurrentAccount.ID);
        var IsUser = await userManager.IsUserAsync(user);
        var IsOutsider = await userManager.IsOutsiderAsync(user);

        if (IsOutsider)
        {
            withoutTrash = true;
            withoutAdditionalFolder = true;
        }

        if (!IsUser)
        {
            yield return await globalFolderHelper.FolderMyAsync;
        }

        if (coreBaseSettings.DisableDocSpace)
        {
            if (!coreBaseSettings.Personal &&
                !await userManager.IsOutsiderAsync(user))
            {
                yield return await globalFolderHelper.FolderShareAsync;
            }

            if (!withoutAdditionalFolder)
            {
                if (_filesSettingsHelper.FavoritesSection)
                {
                    yield return await globalFolderHelper.FolderFavoritesAsync;
                }

                if (_filesSettingsHelper.RecentSection)
                {
                    yield return await globalFolderHelper.FolderRecentAsync;
                }

                if (!IsUser &&
                    !coreBaseSettings.Personal &&
                    PrivacyRoomSettings.IsAvailable())
                {
                    yield return await globalFolderHelper.FolderPrivacyAsync;
                }
            }

            if (!coreBaseSettings.Personal)
            {
                yield return await globalFolderHelper.FolderCommonAsync;
            }

            if (!IsUser &&
                !withoutAdditionalFolder &&
                fileUtility.ExtsWebTemplate.Count > 0 &&
                _filesSettingsHelper.TemplatesSection)
            {
                yield return await globalFolderHelper.FolderTemplatesAsync;
            }
        }

        if (!withoutTrash && !IsUser)
        {
            yield return await globalFolderHelper.FolderTrashAsync;
        }

        if (!coreBaseSettings.DisableDocSpace)
        {
            yield return await globalFolderHelper.FolderVirtualRoomsAsync;
            yield return await globalFolderHelper.FolderArchiveAsync;
        }
    }

    public async Task<FolderDto<T>> RenameFolderAsync<T>(T folderId, string title)
    {
        var folder = await _fileStorageService.FolderRenameAsync(folderId, title);

        return await _folderDtoHelper.GetAsync(folder);
    }

    private async Task<FolderContentDto<T>> ToFolderContentWrapperAsync<T>(T folderId, Guid userIdOrGroupId, FilterType filterType, T roomId, bool searchInContent, bool withSubFolders, bool excludeSubject, ApplyFilterOption applyFilterOption, string[] extension, SearchArea searchArea)
    {
        OrderBy orderBy = null;
        if (SortedByTypeExtensions.TryParse(_apiContext.SortBy, true, out var sortBy))
        {
            orderBy = new OrderBy(sortBy, !_apiContext.SortDescending);
        }

        var startIndex = Convert.ToInt32(_apiContext.StartIndex);
        var items = await _fileStorageService.GetFolderItemsAsync(folderId, startIndex, Convert.ToInt32(_apiContext.Count), filterType, filterType == FilterType.ByUser, userIdOrGroupId.ToString(), _apiContext.FilterValue, extension, searchInContent, withSubFolders, orderBy, excludeSubject: excludeSubject,
            roomId: roomId, applyFilterOption: applyFilterOption, searchArea: searchArea);

        return await _folderContentDtoHelper.GetAsync(folderId, items, startIndex);
    }
}
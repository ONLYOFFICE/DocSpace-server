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

namespace ASC.Files.Core.ApiModels.ResponseDto;

/// <summary>
/// </summary>
public class FolderContentDto<T>
{
    /// <summary>List of files</summary>
    /// <type>System.Collections.Generic.List{ASC.Files.Core.ApiModels.ResponseDto.FileEntryDto}, System.Collections.Generic</type>
    public List<FileEntryDto> Files { get; set; }

    /// <summary>List of folders</summary>
    /// <type>System.Collections.Generic.List{ASC.Files.Core.ApiModels.ResponseDto.FileEntryDto}, System.Collections.Generic</type>
    public List<FileEntryDto> Folders { get; set; }

    /// <summary>Current folder information</summary>
    /// <type>ASC.Files.Core.ApiModels.ResponseDto.FolderDto, ASC.Files.Core</type>
    public FolderDto<T> Current { get; set; }

    /// <summary>Folder path</summary>
    /// <type>System.Object, System</type>
    public object PathParts { get; set; }

    /// <summary>Folder start index</summary>
    /// <type>System.Int32, System</type>
    public int StartIndex { get; set; }

    /// <summary>Number of folder elements</summary>
    /// <type>System.Int32, System</type>
    public int Count { get; set; }

    /// <summary>Total number of elements in the folder</summary>
    /// <type>System.Int32, System</type>
    public int Total { get; set; }

    /// <summary>New element index</summary>
    /// <type>System.Int32, System</type>
    public int New { get; set; }

    public static FolderContentDto<int> GetSample()
    {
        return new FolderContentDto<int>
        {
            Current = FolderDto<int>.GetSample(),
            //Files = new List<FileEntryDto>(new[] { FileDto<int>.GetSample(), FileDto<int>.GetSample() }),
            //Folders = new List<FileEntryDto>(new[] { FolderDto<int>.GetSample(), FolderDto<int>.GetSample() }),
            PathParts = new
            {
                key = "Key",
                path = "//path//to//folder"
            },

            StartIndex = 0,
            Count = 4,
            Total = 4
        };
    }
}

[Scope]
public class FolderContentDtoHelper(
    FileStorageService fileStorageService,
    ApiContext apiContext,
    FileSecurity fileSecurity,
    FileDtoHelper fileWrapperHelper,
    FolderDtoHelper folderWrapperHelper,
    BadgesSettingsHelper badgesSettingsHelper,
    FileSecurityCommon fileSecurityCommon,
    AuthContext authContext,
    BreadCrumbsManager breadCrumbsManager)
{
    public async Task<FolderContentDto<T>> GetAsync<T>(T folderId, Guid? userIdOrGroupId, FilterType? filterType, T roomId, bool? searchInContent, bool? withSubFolders, bool? excludeSubject, ApplyFilterOption? applyFilterOption, SearchArea? searchArea, string[] extension = null)
    {
        var folderContentWrapper = await ToFolderContentWrapperAsync(folderId, userIdOrGroupId ?? Guid.Empty, filterType ?? FilterType.None, roomId, searchInContent ?? false, withSubFolders ?? false, excludeSubject ?? false, applyFilterOption ?? ApplyFilterOption.All, extension, searchArea ?? SearchArea.Active);

        return folderContentWrapper.NotFoundIfNull();
    }
    
    public async Task<FolderContentDto<T>> GetAsync<T>(T parentId, DataWrapper<T> folderItems, int startIndex)
    {
        var files = new List<FileEntry>();
        var folders = new List<FileEntry>();

        foreach (var e in folderItems.Entries)
        {
            switch (e.FileEntryType)
            {
                case FileEntryType.File:
                    files.Add(e);
                    break;
                case FileEntryType.Folder:
                    folders.Add(e);
                    break;
            }
        }

        var order = await breadCrumbsManager.GetBreadCrumbsOrderAsync(parentId);

        var foldersTask = await GetFoldersDto(folders, order).ToListAsync();
        var filesTask = await GetFilesDto(files, foldersTask.Count, order).ToListAsync();
        var currentTask = GetFoldersDto(new [] { folderItems.FolderInfo }, order).FirstOrDefaultAsync();

        var isEnableBadges = await badgesSettingsHelper.GetEnabledForCurrentUserAsync();

        var result = new FolderContentDto<T>
        {
            PathParts = folderItems.FolderPathParts,
            StartIndex = startIndex,
            Total = folderItems.Total,
            New = isEnableBadges ? folderItems.New : 0,
            Count = folderItems.Entries.Count,
            Current = (FolderDto<T>)(await currentTask),
            Files = filesTask,
            Folders = foldersTask
        };

        return result;

        async IAsyncEnumerable<FileEntryDto> GetFilesDto(IEnumerable<FileEntry> fileEntries, int foldersCount, string entriesOrder)
        {
            foreach (var r in fileEntries)
            {
                switch (r)
                {
                    case File<int> fol1:
                        yield return await fileWrapperHelper.GetAsync(fol1, foldersCount, entriesOrder);
                        break;
                    case File<string> fol2:
                    yield return await fileWrapperHelper.GetAsync(fol2, foldersCount, entriesOrder);
                        break;
                }
            }
        }

        async IAsyncEnumerable<FileEntryDto> GetFoldersDto(IEnumerable<FileEntry> folderEntries, string entriesOrder)
        {
            List<FileShareRecord<string>> currentUsersRecords = null;

            foreach (var r in folderEntries)
            {
                switch (r)
                {
                    case Folder<int> fol1:
                        if (currentUsersRecords == null && 
                            DocSpaceHelper.IsRoom(fol1.FolderType) && 
                            await fileSecurityCommon.IsDocSpaceAdministratorAsync(authContext.CurrentAccount.ID))
                        {
                            currentUsersRecords = await fileSecurity.GetUserRecordsAsync().ToListAsync();
                        }
                
                        yield return await folderWrapperHelper.GetAsync(fol1, currentUsersRecords, entriesOrder);
                        break;
                    case Folder<string> fol2:
                        if (currentUsersRecords == null && 
                            DocSpaceHelper.IsRoom(fol2.FolderType) && 
                            await fileSecurityCommon.IsDocSpaceAdministratorAsync(authContext.CurrentAccount.ID))
                        {
                            currentUsersRecords = await fileSecurity.GetUserRecordsAsync().ToListAsync();
                        }
                
                        yield return await folderWrapperHelper.GetAsync(fol2, currentUsersRecords, entriesOrder);
                        break;
                }
            }
        }
    }
    
    private async Task<FolderContentDto<T>> ToFolderContentWrapperAsync<T>(T folderId, Guid userIdOrGroupId, FilterType filterType, T roomId, bool searchInContent, bool withSubFolders, bool excludeSubject, ApplyFilterOption applyFilterOption, string[] extension, SearchArea searchArea)
    {
        OrderBy orderBy = null;
        if (SortedByTypeExtensions.TryParse(apiContext.SortBy, true, out var sortBy))
        {
            orderBy = new OrderBy(sortBy, !apiContext.SortDescending);
        }

        var startIndex = Convert.ToInt32(apiContext.StartIndex);
        var items = await fileStorageService.GetFolderItemsAsync(folderId, startIndex, Convert.ToInt32(apiContext.Count), filterType, filterType == FilterType.ByUser, userIdOrGroupId.ToString(), apiContext.FilterValue, extension, searchInContent, withSubFolders, orderBy, excludeSubject: excludeSubject,
            roomId: roomId, applyFilterOption: applyFilterOption, searchArea: searchArea);

        return await GetAsync(folderId, items, startIndex);
    }
}

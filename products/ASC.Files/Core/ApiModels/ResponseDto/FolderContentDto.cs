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

public class FolderContentDto<T>
{
    /// <summary>
    /// List of files
    /// </summary>
    public List<FileEntryDto> Files { get; set; }

    /// <summary>
    /// List of folders
    /// </summary>
    public List<FileEntryDto> Folders { get; set; }

    /// <summary>
    /// Current folder information
    /// </summary>
    public FolderDto<T> Current { get; set; }

    /// <summary>
    /// Folder path
    /// </summary>
    [SwaggerSchemaCustom(Example = "{key = \"Key\", path = \"//path//to//folder\"}")]
    public object PathParts { get; set; }

    /// <summary>
    /// Folder start index
    /// </summary>
    [SwaggerSchemaCustom(Example = 0)]
    public int StartIndex { get; set; }

    /// <summary>
    /// Number of folder elements
    /// </summary>
    [SwaggerSchemaCustom(Example = 4)]
    public int Count { get; set; }

    /// <summary>
    /// Total number of elements in the folder
    /// </summary>
    [SwaggerSchemaCustom(Example = 4)]
    public int Total { get; set; }

    /// <summary>
    /// New element index
    /// </summary>
    public int New { get; set; }
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
        var types = filterType.HasValue ? new[] { filterType.Value } : null;
        
        var folderContentWrapper = await ToFolderContentWrapperAsync(folderId, userIdOrGroupId ?? Guid.Empty, types, roomId, searchInContent ?? false, withSubFolders ?? false, excludeSubject ?? false, applyFilterOption ?? ApplyFilterOption.All, extension, searchArea ?? SearchArea.Active);

        return folderContentWrapper.NotFoundIfNull();
    }

    public async Task<FolderContentDto<T>> GetAsync<T>(T parentId, DataWrapper<T> folderItems, int startIndex)
    {
        var result = new FolderContentDto<T> { PathParts = folderItems.FolderPathParts, StartIndex = startIndex, Total = folderItems.Total, Count = folderItems.Entries.Count };
        
        var expiration = TimeSpan.MaxValue;
        if (folderItems.ParentRoom is { SettingsLifetime: not null })
        {
            expiration = DateTime.UtcNow - folderItems.ParentRoom.SettingsLifetime.GetExpirationUtc();
        }
        
        List<FileShareRecord<string>> currentUsersRecords = null;
        if (await fileSecurityCommon.IsDocSpaceAdministratorAsync(authContext.CurrentAccount.ID) && 
            folderItems.FolderInfo is { FolderType: FolderType.VirtualRooms or FolderType.Archive })
        {
            currentUsersRecords = await fileSecurity.GetUserRecordsAsync().ToListAsync();
        }
        
        if (folderItems.ParentRoom is { FolderType: FolderType.VirtualDataRoom, SettingsIndexing: true })
        {
            var order = await breadCrumbsManager.GetBreadCrumbsOrderAsync(parentId);
            var entries = await GetEntriesDto(folderItems.Entries, order, folderItems.FolderInfo).ToListAsync();

            result.Files = entries.Where(r => r.FileEntryType == FileEntryType.File).ToList();
            result.Folders = entries.Where(r => r.FileEntryType == FileEntryType.Folder).ToList();
        }
        else
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

            var foldersTask = GetFoldersDto(folders).ToListAsync();
            var filesTask = GetFilesDto(files).ToListAsync();
            result.Files = await filesTask;
            result.Folders = await foldersTask;
        }

        var currentTask = GetFolderDto(folderItems.FolderInfo);
        var isEnableBadges = badgesSettingsHelper.GetEnabledForCurrentUserAsync();

        result.PathParts = folderItems.FolderPathParts;
        result.StartIndex = startIndex;
        result.Total = folderItems.Total;
        result.New = (await isEnableBadges) ? folderItems.New : 0;
        result.Current = (FolderDto<T>)(await currentTask);

        return result;

        async IAsyncEnumerable<FileEntryDto> GetEntriesDto(IEnumerable<FileEntry> fileEntries, string entriesOrder = null, IFolder contextFolder = null)
        {
            foreach (var e in fileEntries)
            {
                if (e.FileEntryType == FileEntryType.File)
                {
                    yield return await GetFileDto(e, entriesOrder, contextFolder);
                }
                else
                {
                    yield return await GetFolderDto(e, entriesOrder, contextFolder);
                }
            }
        }

        async IAsyncEnumerable<FileEntryDto> GetFilesDto(IEnumerable<FileEntry> fileEntries, string entriesOrder = null)
        {
            foreach (var r in fileEntries)
            {
                yield return await GetFileDto(r, entriesOrder);
            }
        }

        async Task<FileEntryDto> GetFileDto(FileEntry fileEntry, string entriesOrder = null, IFolder contextFolder = null)
        {
            return fileEntry switch
            {
                File<int> fol1 => await fileWrapperHelper.GetAsync(fol1, entriesOrder, expiration, contextFolder),
                File<string> fol2 => await fileWrapperHelper.GetAsync(fol2, entriesOrder, expiration, contextFolder),
                _ => null
            };
        }

        async IAsyncEnumerable<FileEntryDto> GetFoldersDto(IEnumerable<FileEntry> folderEntries, string entriesOrder = null)
        {
            foreach (var r in folderEntries)
            {
                yield return await GetFolderDto(r, entriesOrder);
            }
        }
        
        async Task<FileEntryDto> GetFolderDto(FileEntry folderEntry, string entriesOrder = null, IFolder contextFolder = null)
        {
            switch (folderEntry)
            {
                case Folder<int> fol1:
                    if (currentUsersRecords == null &&
                        DocSpaceHelper.IsRoom(fol1.FolderType) &&
                        await fileSecurityCommon.IsDocSpaceAdministratorAsync(authContext.CurrentAccount.ID))
                    {
                        currentUsersRecords = await fileSecurity.GetUserRecordsAsync().ToListAsync();
                    }
                    return await folderWrapperHelper.GetAsync(fol1, currentUsersRecords, entriesOrder, contextFolder);
                case Folder<string> fol2:
                    if (currentUsersRecords == null &&
                        DocSpaceHelper.IsRoom(fol2.FolderType) &&
                        await fileSecurityCommon.IsDocSpaceAdministratorAsync(authContext.CurrentAccount.ID))
                    {
                        currentUsersRecords = await fileSecurity.GetUserRecordsAsync().ToListAsync();
                    }
                    return await folderWrapperHelper.GetAsync(fol2, currentUsersRecords, entriesOrder, contextFolder);
            }

            return null;
        }
    }
    
    private async Task<FolderContentDto<T>> ToFolderContentWrapperAsync<T>(T folderId, Guid userIdOrGroupId, IEnumerable<FilterType> filterTypes, T roomId, bool searchInContent, bool withSubFolders, bool excludeSubject, ApplyFilterOption applyFilterOption, string[] extension, SearchArea searchArea)
    {
        OrderBy orderBy = null;
        if (SortedByTypeExtensions.TryParse(apiContext.SortBy, true, out var sortBy))
        {
            orderBy = new OrderBy(sortBy, !apiContext.SortDescending);
        }

        var startIndex = Convert.ToInt32(apiContext.StartIndex);
        var items = await fileStorageService.GetFolderItemsAsync(folderId, startIndex, Convert.ToInt32(apiContext.Count), filterTypes, filterTypes?.FirstOrDefault() == FilterType.ByUser, userIdOrGroupId.ToString(), apiContext.FilterValue, extension, searchInContent, withSubFolders, orderBy, excludeSubject: excludeSubject,
            roomId: roomId, applyFilterOption: applyFilterOption, searchArea: searchArea);

        return await GetAsync(folderId, items, startIndex);
    }
}

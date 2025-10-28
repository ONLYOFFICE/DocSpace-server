// (c) Copyright Ascensio System SIA 2009-2025
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
/// The folder content information.
/// </summary>
public class FolderContentDto<T>
{
    /// <summary>
    /// The list of files in the folder.
    /// </summary>
    public List<FileEntryBaseDto> Files { get; set; }

    /// <summary>
    /// The list of folders in the folder. 
    /// </summary>
    public List<FileEntryBaseDto> Folders { get; set; }

    /// <summary>
    /// The current folder information.
    /// </summary>
    public FolderDto<T> Current { get; set; }

    /// <summary>
    /// The folder path.
    /// </summary>
    [SwaggerSchemaCustom(Example = "{key = \"Key\", path = \"//path//to//folder\"}")]
    public required object PathParts { get; set; }

    /// <summary>
    /// The folder start index.
    /// </summary>
    [SwaggerSchemaCustom(Example = 0)]
    public int StartIndex { get; set; }

    /// <summary>
    /// The number of folder elements.
    /// </summary>
    [SwaggerSchemaCustom(Example = 4)]
    public int Count { get; set; }

    /// <summary>
    /// The total number of elements in the folder.
    /// </summary>
    [SwaggerSchemaCustom(Example = 4)]
    public required int Total { get; set; }

    /// <summary>
    /// The new element index in the folder.
    /// </summary>
    public int New { get; set; }
}

[Scope]
public class FolderContentDtoHelper(
    FileStorageService fileStorageService,
    FileSecurity fileSecurity,
    FileDtoHelper fileWrapperHelper,
    FolderDtoHelper folderWrapperHelper,
    BadgesSettingsHelper badgesSettingsHelper,
    FileSecurityCommon fileSecurityCommon,
    AuthContext authContext,
    BreadCrumbsManager breadCrumbsManager)
{
    public async Task<FolderContentDto<T>> GetAsync<T>(T folderId, Guid? userIdOrGroupId, FilterType? filterType, T roomId, bool? searchInContent, bool? withSubFolders, bool? excludeSubject, ApplyFilterOption? applyFilterOption, SearchArea? searchArea, string sortByFilter, SortOrder sortOrder, int startIndex, int limit, string text, string[] extension = null, FormsItemDto formsItemDto = null, Location? location = null)
    {
        var types = filterType.HasValue ? new[] { filterType.Value } : null;

        var folderContentWrapper = await ToFolderContentWrapperAsync(folderId, userIdOrGroupId ?? Guid.Empty, types, roomId, searchInContent ?? false, withSubFolders ?? false, excludeSubject ?? false, applyFilterOption ?? ApplyFilterOption.All, text, extension, searchArea ?? SearchArea.Active, formsItemDto, location, sortByFilter, sortOrder, startIndex, limit);

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
            folderItems.FolderInfo is { FolderType: FolderType.VirtualRooms or FolderType.Archive or FolderType.RoomTemplates })
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

            var foldersTask = GetFoldersDto(folders, contextFolder: folderItems.FolderInfo).ToListAsync().AsTask();
            var filesTask = GetFilesDto(files, contextFolder: folderItems.FolderInfo).ToListAsync().AsTask();

            await Task.WhenAll(foldersTask, filesTask);

            result.Files = filesTask.Result;
            result.Folders = foldersTask.Result;
        }
        
        
        var currentTask = GetFolderDto(folderItems.FolderInfo, contextFolder: folderItems.FolderInfo);
        var isEnableBadges = badgesSettingsHelper.GetEnabledForCurrentUserAsync();

        await Task.WhenAll(currentTask, isEnableBadges);

        result.PathParts = folderItems.FolderPathParts;
        result.StartIndex = startIndex;
        result.Total = folderItems.Total;
        result.New = (isEnableBadges.Result) ? folderItems.New : 0;
        result.Current = (FolderDto<T>)(currentTask.Result);

        if (folderItems.ParentRoom is { FolderType: FolderType.AiRoom })
        {
            result.Current.RootRoomType = DocSpaceHelper.MapToRoomType(folderItems.ParentRoom.FolderType);
        }

        return result;

        async IAsyncEnumerable<FileEntryBaseDto> GetEntriesDto(IEnumerable<FileEntry> fileEntries, string entriesOrder = null, IFolder contextFolder = null)
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

        async IAsyncEnumerable<FileEntryBaseDto> GetFilesDto(IEnumerable<FileEntry> fileEntries, string entriesOrder = null, IFolder contextFolder = null)
        {
            foreach (var r in fileEntries)
            {
                yield return await GetFileDto(r, entriesOrder, contextFolder);
            }
        }

        async Task<FileEntryBaseDto> GetFileDto(FileEntry fileEntry, string entriesOrder = null, IFolder contextFolder = null)
        {
            return fileEntry switch
            {
                File<int> fol1 => await fileWrapperHelper.GetAsync(fol1, entriesOrder, expiration, contextFolder),
                File<string> fol2 => await fileWrapperHelper.GetAsync(fol2, entriesOrder, expiration, contextFolder),
                _ => null
            };
        }

        async IAsyncEnumerable<FileEntryBaseDto> GetFoldersDto(IEnumerable<FileEntry> folderEntries, string entriesOrder = null, IFolder contextFolder = null)
        {
            foreach (var r in folderEntries)
            {
                yield return await GetFolderDto(r, entriesOrder, contextFolder);
            }
        }
        
        async Task<FileEntryBaseDto> GetFolderDto(FileEntry folderEntry, string entriesOrder = null, IFolder contextFolder = null)
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
    
    private async Task<FolderContentDto<T>> ToFolderContentWrapperAsync<T>(
        T folderId, 
        Guid userIdOrGroupId, 
        IEnumerable<FilterType> filterTypes, 
        T roomId, 
        bool searchInContent, 
        bool withSubFolders, 
        bool excludeSubject, 
        ApplyFilterOption applyFilterOption, 
        string text,
        string[] extension, 
        SearchArea searchArea, 
        FormsItemDto formsItemDto,
        Location? location,
        string sortByFilter,
        SortOrder sortOrder,
        int startIndex,
        int count)
    {
        OrderBy orderBy = null;
        if (SortedByTypeExtensions.TryParse(sortByFilter, true, out var sortBy))
        {
            orderBy = new OrderBy(sortBy, sortOrder == SortOrder.Ascending);
        }

        var items = await fileStorageService.GetFolderItemsAsync(
            folderId, 
            startIndex, 
            count, 
            filterTypes, 
            filterTypes?.FirstOrDefault() == FilterType.ByUser, 
            userIdOrGroupId.ToString(), 
            text,
            extension, 
            searchInContent, 
            withSubFolders, 
            orderBy, 
            excludeSubject: excludeSubject,
            roomId: roomId, 
            applyFilterOption: applyFilterOption, 
            searchArea: searchArea, 
            formsItemDto: formsItemDto,
            location: location);

        return await GetAsync(folderId, items, startIndex);
    }
}
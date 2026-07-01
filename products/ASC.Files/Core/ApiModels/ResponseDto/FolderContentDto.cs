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

namespace ASC.Files.Core.ApiModels.ResponseDto;

 /// <summary>
/// The folder content information.
/// </summary>
public class FolderContentDto<T>
{
    /// <summary>
    /// The list of files in the folder.
    /// </summary>
    /// <example>[{"id": 10, "title": "document.docx"}]</example>
    public List<FileEntryBaseDto> Files { get; set; }

    /// <summary>
    /// The list of folders in the folder.
    /// </summary>
    /// <example>[{"id": 20, "title": "My Folder"}]</example>
    public List<FileEntryBaseDto> Folders { get; set; }

    /// <summary>
    /// The current folder information.
    /// </summary>
    /// <example>{"id": 10, "title": "My Documents"}</example>
    public FolderDto<T> Current { get; set; }

    /// <summary>
    /// The folder path.
    /// </summary>
    /// <example>{key = "Key", path = "//path//to//folder"}</example>
    public required object PathParts { get; set; }

    /// <summary>
    /// The folder start index.
    /// </summary>
    /// <example>0</example>
    public int StartIndex { get; set; }

    /// <summary>
    /// The number of folder elements.
    /// </summary>
    /// <example>4</example>
    public int Count { get; set; }

    /// <summary>
    /// The total number of elements in the folder.
    /// </summary>
    /// <example>4</example>
    public required int Total { get; set; }

    /// <summary>
    /// The new element index in the folder.
    /// </summary>
    /// <example>0</example>
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
    BreadCrumbsManager breadCrumbsManager,
    AiAccessibility accessibility,
    AiModelSettingsLoader modelSettingsLoader,
    IDaoFactory daoFactory,
    IConfiguration configuration)
{
    private readonly int _foldersDtoParallelism = int.TryParse(configuration["files:folders-dto:parallelism"], out var parallelism) ? parallelism : 5;

    public async Task<FolderContentDto<T>> GetAsync<T>(T folderId, Guid? userIdOrGroupId, Guid? sharedBy, FilterType? filterType, T roomId, bool? searchInContent, bool? withSubFolders, bool? excludeSubject, ApplyFilterOption? applyFilterOption, SearchArea? searchArea, string sortByFilter, SortOrder sortOrder, int startIndex, int limit, string text, string[] extension = null, FormsItemDto formsItemDto = null, Location? location = null, T parentId = default, List<FolderType> folderType = null)
    {
        var types = filterType.HasValue ? new[] { filterType.Value } : null;

        var folderContentWrapper = await ToFolderContentWrapperAsync(folderId, userIdOrGroupId ?? Guid.Empty, sharedBy ?? Guid.Empty,types, roomId, searchInContent ?? false, withSubFolders ?? false, excludeSubject ?? false, applyFilterOption ?? ApplyFilterOption.All, text, extension, searchArea ?? SearchArea.Active, formsItemDto, location, sortByFilter, sortOrder, startIndex, limit, parentId, folderType);

        return folderContentWrapper.NotFoundIfNull();
    }

    public async Task<FolderContentDto<T>> GetAsync<T>(T parentId, DataWrapper<T> folderItems, int startIndex)
    {
        var result = new FolderContentDto<T>
        {
            PathParts = folderItems.FolderPathParts,
            StartIndex = startIndex,
            Total = folderItems.Total,
            Count = folderItems.Entries.Count
        };

        var expiration = TimeSpan.MaxValue;
        if (folderItems.ParentRoom is { SettingsLifetime: not null })
        {
            expiration = DateTime.UtcNow - folderItems.ParentRoom.SettingsLifetime.GetExpirationUtc();
        }

        List<FileShareRecord<string>> currentUsersRecords = null;
        if (folderItems.FolderInfo is { FolderType: FolderType.VirtualRooms or FolderType.Archive or FolderType.RoomTemplates or FolderType.DefaultTemplates })
        {
            currentUsersRecords = await fileSecurity.GetUserRecordsAsync().ToListAsync();
        }

        if (folderItems.FolderInfo is { FolderType: FolderType.AiAgents })
        {
            await SetAgentsChatSettingsAsync(folderItems);
        }

        var aiStatusTask = accessibility.GetStatusAsync();
        var modelSettingsResultTask = modelSettingsLoader.LoadForEntriesAsync(folderItems.Entries, folderItems.FolderInfo);

        await Task.WhenAll(aiStatusTask, modelSettingsResultTask);

        var aiStatus = await aiStatusTask;
        var modelSettingsResult = await modelSettingsResultTask;

        if (folderItems.ParentRoom is { FolderType: FolderType.VirtualDataRoom, SettingsIndexing: true })
        {
            var order = await breadCrumbsManager.GetBreadCrumbsOrderAsync(parentId);

            if (currentUsersRecords == null &&
                folderItems.Entries.Exists(f => f is IFolder { IsRoom: true }) &&
                await fileSecurityCommon.IsDocSpaceAdministratorAsync(authContext.CurrentAccount.ID))
            {
                currentUsersRecords = await fileSecurity.GetUserRecordsAsync().ToListAsync();
            }

            var entries = (await GetEntriesDto(folderItems.Entries, order, folderItems.FolderInfo)).ToList();

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

            if (currentUsersRecords == null &&
                folders.Exists(f => f is IFolder { IsRoom: true }) &&
                await fileSecurityCommon.IsDocSpaceAdministratorAsync(authContext.CurrentAccount.ID))
            {
                currentUsersRecords = await fileSecurity.GetUserRecordsAsync().ToListAsync();
            }

            var foldersTask = GetFoldersDto(folders, contextFolder: folderItems.FolderInfo);
            var filesTask = GetFilesDto(files, contextFolder: folderItems.FolderInfo);

            await Task.WhenAll(foldersTask, filesTask);

            result.Files = [.. filesTask.Result];
            result.Folders = [.. foldersTask.Result];
        }


        var currentTask = GetFolderDto(folderItems.FolderInfo, contextFolder: folderItems.FolderInfo);
        var isEnableBadges = badgesSettingsHelper.GetEnabledForCurrentUserAsync();

        await Task.WhenAll(currentTask, isEnableBadges);

        result.PathParts = folderItems.FolderPathParts;
        result.StartIndex = startIndex;
        result.Total = folderItems.Total;
        result.New = isEnableBadges.Result ? folderItems.New : 0;
        result.Current = (FolderDto<T>)currentTask.Result;

        if (folderItems.ParentRoom is { FolderType: FolderType.AiRoom })
        {
            result.Current.RootRoomType = DocSpaceHelper.MapToRoomType(folderItems.ParentRoom.FolderType);
        }

        return result;

        async Task<IEnumerable<FileEntryBaseDto>> GetEntriesDto(IEnumerable<FileEntry> fileEntries, string entriesOrder = null, IFolder contextFolder = null)
        {
            var count = fileEntries.Count();
            var entryDtos = new FileEntryBaseDto[count];
            await Parallel.ForEachAsync(Enumerable.Range(0, count),
                new ParallelOptions { MaxDegreeOfParallelism = _foldersDtoParallelism },
                async (i, _) =>
                {
                    var e = fileEntries.ElementAt(i);
                    entryDtos[i] = e.FileEntryType == FileEntryType.File
                        ? await GetFileDto(e, entriesOrder, contextFolder)
                        : await GetFolderDto(e, entriesOrder, contextFolder);
                });
            return entryDtos;
        }

        async Task<IEnumerable<FileEntryBaseDto>> GetFilesDto(IEnumerable<FileEntry> fileEntries, string entriesOrder = null, IFolder contextFolder = null)
        {
            var count = fileEntries.Count();
            var fileDtos = new FileEntryBaseDto[count];
            await Parallel.ForEachAsync(Enumerable.Range(0, count),
                new ParallelOptions { MaxDegreeOfParallelism = _foldersDtoParallelism },
                async (i, _) => fileDtos[i] = await GetFileDto(fileEntries.ElementAt(i), entriesOrder, contextFolder));
            return fileDtos;
        }

        async Task<FileEntryBaseDto> GetFileDto(FileEntry fileEntry, string entriesOrder = null, IFolder contextFolder = null)
        {
            return fileEntry switch
            {
                File<int> fol1 => await fileWrapperHelper.GetAsync(fol1, entriesOrder, expiration, contextFolder, aiStatus),
                File<string> fol2 => await fileWrapperHelper.GetAsync(fol2, entriesOrder, expiration, contextFolder, aiStatus),
                _ => null
            };
        }

        async Task<IEnumerable<FileEntryBaseDto>> GetFoldersDto(IEnumerable<FileEntry> folderEntries, string entriesOrder = null, IFolder contextFolder = null)
        {
            var count = folderEntries.Count();
            var folderDtos = new FileEntryBaseDto[count];
            await Parallel.ForEachAsync(Enumerable.Range(0, count),
                new ParallelOptions { MaxDegreeOfParallelism = _foldersDtoParallelism },
                async (i, _) => folderDtos[i] = await GetFolderDto(folderEntries.ElementAt(i), contextFolder: folderItems.FolderInfo));
            return folderDtos;
        }

        async Task<FileEntryBaseDto> GetFolderDto(FileEntry folderEntry, string entriesOrder = null, IFolder contextFolder = null)
        {
            switch (folderEntry)
            {
                case Folder<int> fol1:
                    if (currentUsersRecords == null &&
                        fol1.IsRoom &&
                        await fileSecurityCommon.IsDocSpaceAdministratorAsync(authContext.CurrentAccount.ID))
                    {
                        currentUsersRecords = await fileSecurity.GetUserRecordsAsync().ToListAsync();
                    }
                    return await folderWrapperHelper.GetAsync(fol1, currentUsersRecords, entriesOrder, contextFolder);
                case Folder<string> fol2:
                    if (currentUsersRecords == null &&
                        fol2.IsRoom &&
                        await fileSecurityCommon.IsDocSpaceAdministratorAsync(authContext.CurrentAccount.ID))
                    {
                        currentUsersRecords = await fileSecurity.GetUserRecordsAsync().ToListAsync();
                    }
                    return await folderWrapperHelper.GetAsync(fol2, currentUsersRecords, entriesOrder, contextFolder);
            }

            return null;
        }
    }

    private async Task SetAgentsChatSettingsAsync<T>(DataWrapper<T> folderItems)
    {
        var agentFolders = folderItems.Entries.OfType<Folder<int>>().Where(f => f.IsAgent).ToList();

        var ids = agentFolders.Where(f => f.ChatSettings == null).Select(f => f.Id).Distinct().ToList();
        if (ids.Count == 0)
        {
            return;
        }

        var chatSettings = await daoFactory.GetFolderDao<int>().GetChatSettingsAsync(ids);
        foreach (var folder in agentFolders)
        {
            if (folder.ChatSettings == null && chatSettings.TryGetValue(folder.Id, out var settings))
            {
                folder.ChatSettings = settings;
            }
        }
    }

    private async Task<FolderContentDto<T>> ToFolderContentWrapperAsync<T>(
        T folderId,
        Guid userIdOrGroupId,
        Guid sharedBy,
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
        int count,
        T parentId = default,
        List<FolderType> folderType = null)
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
            userIdOrGroupId,
            sharedBy,
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
            location: location,
            parentFolderId: parentId,
            folderType: folderType);

        return await GetAsync(folderId, items, startIndex);
    }

}

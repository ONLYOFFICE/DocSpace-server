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

public record HistoryDto
{
    public HistoryAction Action { get; init; }
    public EmployeeDto Initiator { get; init; }
    public ApiDateTime Date { get; init; }
    public HistoryData Data { get; init; }
    public List<HistoryDto> Related { get; set; }
}

[Scope]
public class HistoryDtoHelper(EmployeeFullDtoHelper employeeFullDtoHelper, UserManager userManager, ApiDateTimeHelper apiDateTimeHelper)
{
    public async Task<HistoryDto> GetAsync(HistoryEntry entry)
    {
        EmployeeDto initiator;
        
        if (string.IsNullOrEmpty(entry.InitiatorName))
        {
            initiator = await employeeFullDtoHelper.GetAsync(await userManager.GetUsersAsync(entry.InitiatorId));
        }
        else
        {
            initiator = new EmployeeDto
            {
                DisplayName = entry.InitiatorName
            };
        }
        
        return new HistoryDto
        {
            Action = entry.Action,
            Initiator = initiator,
            Date = apiDateTimeHelper.Get(entry.Date),
            Data = entry.Data
        };
    }
}

[Scope]
public class HistoryApiHelper(HistoryService historyService, HistoryDtoHelper historyDtoHelper, ApiContext apiContext, IDaoFactory daoFactory, FileSecurity fileSecurity)
{
    public IAsyncEnumerable<HistoryDto> GetFileHistoryAsync(int fileId)
    {
        return GetEntryHistoryAsync(fileId, FileEntryType.File);
    }

    public IAsyncEnumerable<HistoryDto> GetFolderHistoryAsync(int folderId)
    {
        return GetEntryHistoryAsync(folderId, FileEntryType.Folder);
    }
    
    private async IAsyncEnumerable<HistoryDto> GetEntryHistoryAsync(int entryId, FileEntryType entryType)
    {
        var offset = Convert.ToInt32(apiContext.StartIndex);
        var count = Convert.ToInt32(apiContext.Count);
        
        var filterFolderIds = new List<int>();
        var filterFileIds = new List<int>();
        var needFiltering = false;
        FileEntry<int> entry = entryType switch
        {
            FileEntryType.File => await daoFactory.GetFileDao<int>().GetFileAsync(entryId),
            FileEntryType.Folder => await daoFactory.GetFolderDao<int>().GetFolderAsync(entryId),
            _ => throw new ArgumentOutOfRangeException(nameof(entryType), entryType, null)
        };

        if (entry == null)
        {
            throw new ItemNotFoundException(entryType == FileEntryType.File
                ? FilesCommonResource.ErrorMessage_FileNotFound
                : FilesCommonResource.ErrorMessage_FolderNotFound
                );
        }

        if (!await fileSecurity.CanReadAsync(entry))
        {
            throw new SecurityException(entryType == FileEntryType.File
                ? FilesCommonResource.ErrorMessage_SecurityException_ReadFile
                : FilesCommonResource.ErrorMessage_SecurityException_ReadFolder
                );
        }

        if (entryType == FileEntryType.Folder &&
            DocSpaceHelper.IsFormsFillingFolder(entry) &&
            entry.ShareRecord is { Share: FileShare.FillForms })
        {
            needFiltering = true;
            var folderDao = daoFactory.GetFolderDao<int>();
            var fileDao = daoFactory.GetFileDao<int>();

            var f = entry as Folder<int>;
            filterFolderIds = await folderDao.GetFoldersAsync(entryId, new OrderBy(SortedByType.DateAndTime, false), FilterType.None, false, Guid.Empty, null, true, false, 0, -1, default, true, f.FolderType).Select(r => r.Id).ToListAsync();
            filterFileIds = await fileDao.GetFilesAsync(entryId, new OrderBy(SortedByType.DateAndTime, false), FilterType.None, false, Guid.Empty, null, null, false, true, false, 0, -1, default, false, true, f.FolderType).Select(r => r.Id).ToListAsync();
        }

        var totalCountTask = historyService.GetHistoryCountAsync(entryId, entryType, needFiltering, filterFolderIds, filterFileIds);

        var histories = historyService.GetHistoryAsync(entryId, entryType, offset, count, needFiltering, filterFolderIds, filterFileIds)
            .GroupByAwait(x => ValueTask.FromResult(x.GetGroupId()),
                async (_, group) =>
                {
                    var first = await historyDtoHelper.GetAsync(await group.FirstAsync());
                    first.Related = await group.Skip(1).SelectAwait(async x => await historyDtoHelper.GetAsync(x)).ToListAsync();
                    return first;
                })
            .OrderByDescending(x => x.Date);

        var totalCount = await totalCountTask;
        
        apiContext.SetCount(Math.Min(Math.Max(totalCount - offset, 0), count)).SetTotalCount(totalCount);
        
        await foreach (var history in histories)
        {
            yield return history;
        }
    }
}
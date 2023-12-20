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

namespace ASC.Files.Api;

[DefaultRoute("fileops")]
public class OperationController(FileOperationDtoHelper fileOperationDtoHelper,
        FolderDtoHelper folderDtoHelper,
        FileDtoHelper fileDtoHelper,
        FileStorageService fileStorageService,
        IEventBus eventBus,
        TenantManager tenantManager, 
        AuthContext authContext)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
    {
    /// <summary>
    /// Starts the download process of files and folders with the IDs specified in the request.
    /// </summary>
    /// <short>Bulk download</short>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.DownloadRequestDto, ASC.Files.Core" name="inDto">Request parameters for downloading files</param>
    /// <category>Operations</category>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FileOperationDto, ASC.Files.Core">List of file operations</returns>
    /// <path>api/2.0/files/fileops/bulkdownload</path>
    /// <httpMethod>PUT</httpMethod>
    /// <collection>list</collection>
    [AllowAnonymous]
    [HttpPut("bulkdownload")]
    public async IAsyncEnumerable<FileOperationDto> BulkDownload(DownloadRequestDto inDto)
    {
        var folders = new Dictionary<JsonElement, string>();
        var files = new Dictionary<JsonElement, string>();

        foreach (var fileId in inDto.FileConvertIds.Where(fileId => !files.ContainsKey(fileId.Key)))
        {
            files.Add(fileId.Key, fileId.Value);
        }

        foreach (var fileId in inDto.FileIds.Where(fileId => !files.ContainsKey(fileId)))
        {
            files.Add(fileId, string.Empty);
        }

        foreach (var folderId in inDto.FolderIds.Where(folderId => !folders.ContainsKey(folderId)))
        {
            folders.Add(folderId, string.Empty);
        }

        var (tasks, currentTaskId, headers) = await fileStorageService.PublishBulkDownloadAsync(folders, files);

        var tenantId = await tenantManager.GetCurrentTenantIdAsync();

        eventBus.Publish(new BulkDownloadIntegrationEvent(authContext.CurrentAccount.ID, tenantId)
        {
            FileStringIds = files.ToDictionary(x => JsonSerializer.Serialize(x.Key), x => x.Value),
            FolderStringIds = folders.ToDictionary(x => JsonSerializer.Serialize(x.Key), x => x.Value),
            TaskId = currentTaskId,
            Headers = headers?.ToDictionary(x => x.Key, x => x.Value.ToString())
        });

        foreach (var e in tasks)
        {
            yield return await fileOperationDtoHelper.GetAsync(e);
        }
    }

    /// <summary>
    /// Copies all the selected files and folders to the folder with the ID specified in the request.
    /// </summary>
    /// <short>Copy to a folder</short>
    /// <category>Operations</category>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.BatchRequestDto, ASC.Files.Core" name="inDto">Request parameters for copying files</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FileOperationDto, ASC.Files.Core">List of file operations</returns>
    /// <path>api/2.0/files/fileops/copy</path>
    /// <httpMethod>PUT</httpMethod>
    /// <collection>list</collection>
    [HttpPut("copy")]
    public async IAsyncEnumerable<FileOperationDto> CopyBatchItems(BatchRequestDto inDto)
    {
        var (folderIntIds, folderStringIds) = FileOperationsManager.GetIds(inDto.FolderIds);
        var (fileIntIds, fileStringIds) = FileOperationsManager.GetIds(inDto.FileIds);

        var (tasks, currentTaskId, headers) = await fileStorageService.PublishMoveOrCopyItemsAsync(folderStringIds, fileStringIds, folderIntIds, fileIntIds, inDto.DestFolderId, inDto.ConflictResolveType, true, inDto.DeleteAfter, inDto.Content);

        var tenantId = await tenantManager.GetCurrentTenantIdAsync();

        eventBus.Publish(new MoveOrCopyIntegrationEvent(authContext.CurrentAccount.ID, tenantId)
        {
            DeleteAfter = inDto.DeleteAfter,
            Ic = true,
            FileStringIds = fileStringIds,
            FolderStringIds = folderStringIds,
            FileIntIds = fileIntIds,
            FolderIntIds = folderIntIds,
            Content = inDto.Content,
            ConflictResolveType = inDto.ConflictResolveType,
            DestFolderId = JsonSerializer.Serialize(inDto.DestFolderId),
            TaskId = currentTaskId,
            Headers = headers?.ToDictionary(x => x.Key, x => x.Value.ToString())
        });

        foreach (var e in tasks)
        {
            yield return await fileOperationDtoHelper.GetAsync(e);
        }
    }

    /// <summary>
    /// Deletes the files and folders with the IDs specified in the request.
    /// </summary>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.DeleteBatchRequestDto, ASC.Files.Core" name="inDto">Request parameters for deleting files</param>
    /// <short>Delete files and folders</short>
    /// <category>Operations</category>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FileOperationDto}, ASC.Files.Core">List of file operations</returns>
    /// <path>api/2.0/files/fileops/delete</path>
    /// <httpMethod>PUT</httpMethod>
    /// <collection>list</collection>
    [HttpPut("delete")]
    public async IAsyncEnumerable<FileOperationDto> DeleteBatchItems(DeleteBatchRequestDto inDto)
    {
        var (folderIntIds, folderStringIds) = FileOperationsManager.GetIds(inDto.FolderIds);
        var (fileIntIds, fileStringIds) = FileOperationsManager.GetIds(inDto.FileIds);

        var (tasks, currentTaskId, headers) = await fileStorageService.PublishDeleteItemsAsync(folderStringIds, fileStringIds, folderIntIds, fileIntIds, false, inDto.DeleteAfter, inDto.Immediately);

        var tenantId = await tenantManager.GetCurrentTenantIdAsync();

        eventBus.Publish(new DeleteIntegrationEvent(authContext.CurrentAccount.ID, tenantId)
        {
            DeleteAfter = inDto.DeleteAfter,
            Immediately = inDto.Immediately,
            FolderStringIds = folderStringIds,
            FileStringIds = fileStringIds,
            FolderIntIds = folderIntIds,
            FileIntIds = fileIntIds,
            TaskId = currentTaskId,
            Headers = headers?.ToDictionary(x => x.Key, x => x.Value.ToString())
        });

        foreach (var e in tasks)
        {
            yield return await fileOperationDtoHelper.GetAsync(e);
        }
    }

    /// <summary>
    /// Deletes all the files and folders from the "Trash" folder.
    /// </summary>
    /// <short>Empty the "Trash" folder</short>
    /// <category>Operations</category>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FileOperationDto, ASC.Files.Core">List of file operations</returns>
    /// <path>api/2.0/files/fileops/emptytrash</path>
    /// <httpMethod>PUT</httpMethod>
    /// <collection>list</collection>
    [HttpPut("emptytrash")]
    public async IAsyncEnumerable<FileOperationDto> EmptyTrashAsync()
    {
        var (tasks, currentTaskId, headers) = await fileStorageService.PublishEmptyTrashAsync();

        var tenantId = await tenantManager.GetCurrentTenantIdAsync();

        eventBus.Publish(new EmptyTrashIntegrationEvent(authContext.CurrentAccount.ID, tenantId)
        {
            TaskId = currentTaskId,
            Headers = headers?.ToDictionary(x => x.Key, x => x.Value.ToString())
        });

        foreach (var e in tasks)
        {
            yield return await fileOperationDtoHelper.GetAsync(e);
        }
    }

    /// <summary>
    ///  Returns a list of all the active operations.
    /// </summary>
    /// <short>Get active operations</short>
    /// <category>Operations</category>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FileOperationDto, ASC.Files.Core">List of file operations</returns>
    /// <path>api/2.0/files/fileops</path>
    /// <httpMethod>GET</httpMethod>
    /// <collection>list</collection>
    [AllowAnonymous]
    [HttpGet("")]
    public async IAsyncEnumerable<FileOperationDto> GetOperationStatuses()
    {
        foreach (var e in fileStorageService.GetTasksStatuses())
        {
            yield return await fileOperationDtoHelper.GetAsync(e);
        }
    }

    /// <summary>
    /// Marks the files and folders with the IDs specified in the request as read.
    /// </summary>
    /// <short>Mark as read</short>
    /// <category>Operations</category>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.BaseBatchRequestDto, ASC.Files.Core" name="inDto">Base batch request parameters</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FileOperationDto, ASC.Files.Core">List of file operations</returns>
    /// <path>api/2.0/files/fileops/markasread</path>
    /// <httpMethod>PUT</httpMethod>
    /// <collection>list</collection>
    [HttpPut("markasread")]
    public async IAsyncEnumerable<FileOperationDto> MarkAsRead(BaseBatchRequestDto inDto)
    {
        var (folderIntIds, folderStringIds) = FileOperationsManager.GetIds(inDto.FolderIds);
        var (fileIntIds, fileStringIds) = FileOperationsManager.GetIds(inDto.FileIds);

        var (tasks, currentTaskId, headers) = await fileStorageService.PublishMarkAsRead(folderStringIds, fileStringIds, folderIntIds, fileIntIds);

        var tenantId = await tenantManager.GetCurrentTenantIdAsync();

        eventBus.Publish(new MarkAsReadIntegrationEvent(authContext.CurrentAccount.ID, tenantId)
        {
            FolderStringIds = folderStringIds,
            FileStringIds = fileStringIds,
            FolderIntIds = folderIntIds,
            FileIntIds = fileIntIds,
            TaskId = currentTaskId,
            Headers = headers?.ToDictionary(x => x.Key, x => x.Value.ToString())
        });

        foreach (var e in tasks)
        {
            yield return await fileOperationDtoHelper.GetAsync(e);
        }
    }

    /// <summary>
    /// Moves all the selected files and folders to the folder with the ID specified in the request.
    /// </summary>
    /// <short>Move to a folder</short>
    /// <category>Operations</category>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.BatchRequestDto, ASC.Files.Core" name="inDto">Request parameters for moving files and folders</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FileOperationDto, ASC.Files.Core">List of file operations</returns>
    /// <path>api/2.0/files/fileops/move</path>
    /// <httpMethod>PUT</httpMethod>
    /// <collection>list</collection>
    [HttpPut("move")]
    public async IAsyncEnumerable<FileOperationDto> MoveBatchItems(BatchRequestDto inDto)
    {
        var (folderIntIds, folderStringIds) = FileOperationsManager.GetIds(inDto.FolderIds);
        var (fileIntIds, fileStringIds) = FileOperationsManager.GetIds(inDto.FileIds);

        var (tasks, currentTaskId, headers) = await fileStorageService.PublishMoveOrCopyItemsAsync(folderStringIds, fileStringIds, folderIntIds, fileIntIds, inDto.DestFolderId, inDto.ConflictResolveType, false, inDto.DeleteAfter, inDto.Content);

        var tenantId = await tenantManager.GetCurrentTenantIdAsync();

        eventBus.Publish(new MoveOrCopyIntegrationEvent(authContext.CurrentAccount.ID, tenantId)
        {
            DeleteAfter = inDto.DeleteAfter,
            Ic = false,
            FileStringIds = fileStringIds,
            FolderStringIds = folderStringIds,
            FileIntIds = fileIntIds, 
            FolderIntIds = folderIntIds,
            Content = inDto.Content,
            ConflictResolveType = inDto.ConflictResolveType,
            DestFolderId = JsonSerializer.Serialize(inDto.DestFolderId),
            TaskId = currentTaskId,
            Headers = headers?.ToDictionary(x => x.Key, x => x.Value.ToString())
        });

        foreach (var e in tasks)
        {
            yield return await fileOperationDtoHelper.GetAsync(e);
        }
    }

    /// <summary>
    /// Checks a batch of files and folders for conflicts when moving or copying them to the folder with the ID specified in the request.
    /// </summary>
    /// <short>Check files and folders for conflicts</short>
    /// <category>Operations</category>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.BatchRequestDto, ASC.Files.Core" name="inDto">Request parameters for checking files and folders for conflicts</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FileEntryDto, ASC.Files.Core">List of file entry information</returns>
    /// <path>api/2.0/files/fileops/move</path>
    /// <httpMethod>GET</httpMethod>
    /// <collection>list</collection>
    [HttpGet("move")]
    public async IAsyncEnumerable<FileEntryDto> MoveOrCopyBatchCheckAsync([ModelBinder(BinderType = typeof(BatchModelBinder))] BatchRequestDto inDto)
    {
        List<object> checkedFiles;

        if (inDto.DestFolderId.ValueKind == JsonValueKind.Number)
        {
            (checkedFiles, _) = await fileStorageService.MoveOrCopyFilesCheckAsync(inDto.FileIds.ToList(), inDto.FolderIds.ToList(), inDto.DestFolderId.GetInt32());
        }
        else
        {
            (checkedFiles, _) = await fileStorageService.MoveOrCopyFilesCheckAsync(inDto.FileIds.ToList(), inDto.FolderIds.ToList(), inDto.DestFolderId.GetString());
        }

        var entries = await fileStorageService.GetItemsAsync(checkedFiles.OfType<int>().Select(Convert.ToInt32), checkedFiles.OfType<int>().Select(Convert.ToInt32), FilterType.FilesOnly, false, "", "");

        entries.AddRange(await fileStorageService.GetItemsAsync(checkedFiles.OfType<string>(), checkedFiles.OfType<string>(), FilterType.FilesOnly, false, "", ""));

        foreach (var e in entries)
        {
            yield return await GetFileEntryWrapperAsync(e);
        }
    }
    /// <summary>
    /// Finishes an operation with the ID specified in the request or all the active operations.
    /// </summary>
    /// <short>Finish active operations</short>
    /// <category>Operations</category>
    /// <param type="System.String, System" name="id" method="url">Operation ID</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FileOperationDto, ASC.Files.Core">List of file operations</returns>
    /// <path>api/2.0/files/fileops/terminate/{id?}</path>
    /// <httpMethod>PUT</httpMethod>
    /// <collection>list</collection>
    [AllowAnonymous]
    [HttpPut("terminate/{id?}")]
    public async IAsyncEnumerable<FileOperationDto> TerminateTasks(string id = null)
    {
        var tasks = fileStorageService.TerminateTasks(id);

        foreach (var e in tasks)
        {
            yield return await fileOperationDtoHelper.GetAsync(e);
        }
    }
}
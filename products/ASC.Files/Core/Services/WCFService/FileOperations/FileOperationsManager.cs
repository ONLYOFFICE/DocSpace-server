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

namespace ASC.Web.Files.Services.WCFService.FileOperations;

[Singleton(Additional = typeof(FileOperationsManagerHelperExtention))]
public class FileOperationsManager(TempStream tempStream,
        IDistributedTaskQueueFactory queueFactory,
        IServiceProvider serviceProvider,
        ThumbnailSettings thumbnailSettings)
{
    public const string CUSTOM_DISTRIBUTED_TASK_QUEUE_NAME = "files_operation";
    private readonly DistributedTaskQueue _tasks = queueFactory.CreateQueue(CUSTOM_DISTRIBUTED_TASK_QUEUE_NAME);

    public List<FileOperationResult> GetOperationResults(Guid userId)
    {
        userId = ProcessUserId(userId);

        var operations = _tasks.GetAllTasks();
        var processlist = Process.GetProcesses();

        //TODO: replace with distributed cache
        if (processlist.Length > 0)
        {
            foreach (var o in operations.Where(o => processlist.All(p => p.Id != o.InstanceId)))
            {
                o[FileOperation.Progress] = 100;
                _tasks.DequeueTask(o.Id);
            }
        }

        operations = operations.Where(t => new Guid(t[FileOperation.Owner]) == userId).ToList();
        foreach (var o in operations.Where(o => o.Status > DistributedTaskStatus.Running))
        {
            o[FileOperation.Progress] = 100;

            _tasks.DequeueTask(o.Id);
        }

        var results = operations
            .Where(o => o[FileOperation.Hold] || o[FileOperation.Progress] != 100)
            .Select(o => new FileOperationResult
            {
                Id = o.Id,
                OperationType = (FileOperationType)o[FileOperation.OpType],
                Source = o[FileOperation.Src],
                Progress = o[FileOperation.Progress],
                Processed = Convert.ToString(o[FileOperation.Process]),
                Result = o[FileOperation.Res],
                Error = o[FileOperation.Err],
                Finished = o[FileOperation.Finish]
            })
            .ToList();

        return results;
    }

    public List<FileOperationResult> CancelOperations(Guid userId, string id = null)
    {
        var operations = _tasks.GetAllTasks()
            .Where(t => (string.IsNullOrEmpty(id) || t.Id == id) && new Guid(t[FileOperation.Owner]) == userId);

        foreach (var o in operations)
        {
            _tasks.DequeueTask(o.Id);
        }

        return GetOperationResults(userId);
    }

    #region MarkAsRead

    public void MarkAsRead(Guid userId, Tenant tenant, IEnumerable<JsonElement> folderIds, IEnumerable<JsonElement> fileIds,
        IDictionary<string, StringValues> headers, bool enqueueTask = true, string taskId = null)
    {
        var (folderIntIds, folderStringIds) = GetIds(folderIds);
        var (fileIntIds, fileStringIds) = GetIds(fileIds);

        MarkAsRead(userId, tenant, folderStringIds, fileStringIds, folderIntIds, fileIntIds, headers, enqueueTask, taskId);
    }

    public (List<FileOperationResult>, string) MarkAsRead(Guid userId, Tenant tenant, IEnumerable<string> folderIdsString, IEnumerable<string> fileIdsString, IEnumerable<int> folderIdsInt, IEnumerable<int> fileIdsInt,
        IDictionary<string, StringValues> headers, bool enqueueTask = true, string taskId = null)
    {
        var op1 = new FileMarkAsReadOperation<int>(serviceProvider, new FileMarkAsReadOperationData<int>(folderIdsInt, fileIdsInt, tenant, headers));
        var op2 = new FileMarkAsReadOperation<string>(serviceProvider, new FileMarkAsReadOperationData<string>(folderIdsString, fileIdsString, tenant, headers));
        var op = new FileMarkAsReadOperation(serviceProvider, op2, op1);

        if (!string.IsNullOrEmpty(taskId))
        {
            op.Id = taskId;
        }

        return (QueueTask(userId, op, enqueueTask), op.Id);
    }

    #endregion

    #region Download

    public (List<FileOperationResult>, string) Download(Guid userId, Tenant tenant, Dictionary<JsonElement, string> folders, Dictionary<JsonElement, string> files,
        IDictionary<string, StringValues> headers, bool enqueueTask = true, string taskId = null, string baseUri = null)
    {
        var operations = _tasks.GetAllTasks()
            .Where(t => new Guid(t[FileOperation.Owner]) == ProcessUserId(userId))
            .Where(t => (FileOperationType)t[FileOperation.OpType] == FileOperationType.Download);

        if (operations.Any(o => o.Status <= DistributedTaskStatus.Running && o.Id != taskId))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_ManyDownloads);
        }

        var (folderIntIds, folderStringIds) = GetIds(folders);
        var (fileIntIds, fileStringIds) = GetIds(files);

        var op1 = new FileDownloadOperation<int>(serviceProvider, new FileDownloadOperationData<int>(folderIntIds, fileIntIds, tenant, headers));
        var op2 = new FileDownloadOperation<string>(serviceProvider, new FileDownloadOperationData<string>(folderStringIds, fileStringIds, tenant, headers));
        var op = new FileDownloadOperation(serviceProvider, tempStream, baseUri, op2, op1);

        if (!string.IsNullOrEmpty(taskId))
        {
            op.Id = taskId;
        }

        return (QueueTask(userId, op, enqueueTask), op.Id);
    }

    #endregion

    #region MoveOrCopy

    public async Task<(List<FileOperationResult>, string)> MoveOrCopyAsync(Guid userId, Tenant tenant, IEnumerable<JsonElement> folders, IEnumerable<JsonElement> files, JsonElement destFolderId, bool copy, FileConflictResolveType resolveType, bool holdResult,
        IDictionary<string, StringValues> headers, bool content)
    {
        var (folderIntIds, folderStringIds) = GetIds(folders);
        var (fileIntIds, fileStringIds) = GetIds(files);

        return await MoveOrCopyAsync(userId, tenant, folderStringIds, fileStringIds, folderIntIds, fileIntIds, destFolderId, copy, resolveType, holdResult, headers, content, enqueueTask: true);
    }

    public async Task<(List<FileOperationResult>, string)> MoveOrCopyAsync(Guid userId, Tenant tenant, List<string> folderStringIds, List<string> fileStringIds, List<int> folderIntIds, List<int> fileIntIds, JsonElement destFolderId, bool copy, FileConflictResolveType resolveType, bool holdResult, IDictionary<string, StringValues> headers, 
        bool content = false, bool enqueueTask = true, string taskId = null)
    {
        if (content)
        {
            await GetContent(folderIntIds, fileIntIds);
            await GetContent(folderStringIds, fileStringIds);
        }

        var op1 = new FileMoveCopyOperation<int>(serviceProvider, new FileMoveCopyOperationData<int>(folderIntIds, fileIntIds, tenant, destFolderId, copy, resolveType, holdResult, headers), thumbnailSettings);
        var op2 = new FileMoveCopyOperation<string>(serviceProvider, new FileMoveCopyOperationData<string>(folderStringIds, fileStringIds, tenant, destFolderId, copy, resolveType, holdResult, headers), thumbnailSettings);
        var op = new FileMoveCopyOperation(serviceProvider, op2, op1);

        if (!string.IsNullOrEmpty(taskId))
        {
            op.Id = taskId;
        }

        return (QueueTask(userId, op, enqueueTask), op.Id);

        async Task GetContent<T1>(List<T1> folderIds, List<T1> fileIds)
        {
            var copyFolderIds = folderIds.ToList();
            folderIds.Clear();

            using var scope = serviceProvider.CreateScope();
            var daoFactory = scope.ServiceProvider.GetService<IDaoFactory>();
            var fileDao = daoFactory.GetFileDao<T1>();
            var folderDao = daoFactory.GetFolderDao<T1>();

            foreach (var folderId in copyFolderIds)
            {
                folderIds.AddRange(await folderDao.GetFoldersAsync(folderId).Select(r => r.Id).ToListAsync());
                fileIds.AddRange(await fileDao.GetFilesAsync(folderId).ToListAsync());
            }
        }
    }

    #endregion

    #region Delete

    public (List<FileOperationResult>, string) Delete<T>(Guid userId, Tenant tenant, IEnumerable<T> folders, IEnumerable<T> files, bool ignoreException, bool holdResult, bool immediately,
        IDictionary<string, StringValues> headers, bool isEmptyTrash = false, bool enqueueTask = true, string taskId = null)
    {
        var op = new FileDeleteOperation<T>(serviceProvider, new FileDeleteOperationData<T>(folders, files, tenant, holdResult, ignoreException, immediately, headers, isEmptyTrash));

        if (!string.IsNullOrEmpty(taskId))
        {
            op.Id = taskId;
        }

        return (QueueTask(userId, op, enqueueTask), op.Id);
    }

    public (List<FileOperationResult>, string) Delete(Guid userId, Tenant tenant, IEnumerable<string> foldersIdString, IEnumerable<string> filesIdString, IEnumerable<int> foldersIdInt, IEnumerable<int> filesIdInt, bool ignoreException, bool holdResult, bool immediately,
        IDictionary<string, StringValues> headers, bool isEmptyTrash = false, bool enqueueTask = true, string taskId = null)
    {
        var op1 = new FileDeleteOperation<int>(serviceProvider, new FileDeleteOperationData<int>(foldersIdInt, filesIdInt, tenant, holdResult, ignoreException, immediately, headers, isEmptyTrash));
        var op2 = new FileDeleteOperation<string>(serviceProvider, new FileDeleteOperationData<string>(foldersIdString, filesIdString, tenant, holdResult, ignoreException, immediately, headers, isEmptyTrash));
        var op = new FileDeleteOperation(serviceProvider, op2, op1);

        if (!string.IsNullOrEmpty(taskId))
        {
            op.Id = taskId;
        }

        return (QueueTask(userId, op, enqueueTask), op.Id);
    }

    #endregion

    private List<FileOperationResult> QueueTask(Guid userId, DistributedTaskProgress op, bool enqueueTask)
    {
        if (enqueueTask)
        {
            _tasks.EnqueueTask(op);
        }
        else
        {
            _tasks.PublishTask(op);
        }

        return GetOperationResults(userId);
    }

    public static (List<int>, List<string>) GetIds(IEnumerable<JsonElement> items)
    {
        var (resultInt, resultString) = (new List<int>(), new List<string>());

        foreach (var item in items)
        {
            if (item.ValueKind == JsonValueKind.Number)
            {
                resultInt.Add(item.GetInt32());
            }
            else if (item.ValueKind == JsonValueKind.String)
            {
                var val = item.GetString();
                if (int.TryParse(val, out var i))
                {
                    resultInt.Add(i);
                }
                else
                {
                    resultString.Add(val);
                }
            }
        }

        return (resultInt, resultString);
    }

    public static (Dictionary<int, string>, Dictionary<string, string>) GetIds(Dictionary<JsonElement, string> items)
    {
        var (resultInt, resultString) = (new Dictionary<int, string>(), new Dictionary<string, string>());

        foreach (var item in items)
        {
            if (item.Key.ValueKind == JsonValueKind.Number)
            {
                resultInt.Add(item.Key.GetInt32(), item.Value);
            }
            else if (item.Key.ValueKind == JsonValueKind.String)
            {
                var val = item.Key.GetString();
                if (int.TryParse(val, out var i))
                {
                    resultInt.Add(i, item.Value);
                }
                else
                {
                    resultString.Add(val, item.Value);
                }
            }
            else if (item.Key.ValueKind == JsonValueKind.Object)
            {
                var key = item.Key.GetProperty("key");

                var val = item.Key.GetProperty("value").GetString();

                if (key.ValueKind == JsonValueKind.Number)
                {
                    resultInt.Add(key.GetInt32(), val);
                }
                else
                {
                    resultString.Add(key.GetString(), val);
                }
            }
        }

        return (resultInt, resultString);
    }

    private Guid ProcessUserId(Guid userId)
    {
        var securityContext = serviceProvider.GetRequiredService<SecurityContext>();

        if (securityContext.IsAuthenticated)
        {
            return userId;
        }

        var externalShare = serviceProvider.GetRequiredService<ExternalShare>();

        return externalShare.GetSessionId();
    }
}

public static class FileOperationsManagerHelperExtention
{
    public static void Register(DIHelper services)
    {
        services.TryAdd<FileDeleteOperationScope>();
        services.TryAdd<FileMarkAsReadOperationScope>();
        services.TryAdd<FileMoveCopyOperationScope>();
        services.TryAdd<FileOperationScope>();
        services.TryAdd<CompressToArchive>();
    }
}

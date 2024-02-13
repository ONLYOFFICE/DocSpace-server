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

[Singleton]
public class FileOperationsManagerHolder(IDistributedTaskQueueFactory queueFactory, IServiceProvider serviceProvider)
{
    internal const string CUSTOM_DISTRIBUTED_TASK_QUEUE_NAME = "files_operation";
    private readonly DistributedTaskQueue _tasks = queueFactory.CreateQueue(CUSTOM_DISTRIBUTED_TASK_QUEUE_NAME);

    public List<FileOperationResult> GetOperationResults(Guid userId)
    {
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

    public DistributedTask FindById(string taskId)
    {
        return _tasks.GetAllTasks().FirstOrDefault(r => r.Id == taskId);
    }

    public void Enqueue(DistributedTaskProgress task)
    {
        _tasks.EnqueueTask(task);
    }

    public string Publish(DistributedTaskProgress task)
    {
        return _tasks.PublishTask(task);
    }

    public void CheckRunning(Guid userId, FileOperationType fileOperationType)
    {
        var operations = _tasks.GetAllTasks()
            .Where(t => new Guid(t[FileOperation.Owner]) == userId)
            .Where(t => (FileOperationType)t[FileOperation.OpType] == fileOperationType);
        
        if (operations.Any(o => o.Status <= DistributedTaskStatus.Running))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_ManyDownloads);
        }
    }
    
    internal T GetService<T>() 
    {
        return serviceProvider.GetService<T>();
    }
}

[Scope(Additional = typeof(FileOperationsManagerExtension))]
public class FileOperationsManager(
    IHttpContextAccessor httpContextAccessor,
    IEventBus eventBus,
    AuthContext authContext,
    TenantManager tenantManager,
    UserManager userManager,
    FileOperationsManagerHolder fileOperationsManagerHolder,
    ExternalShare externalShare,
    IServiceProvider serviceProvider)
{
    public List<FileOperationResult> GetOperationResults()
    {
        return fileOperationsManagerHolder.GetOperationResults(ProcessUserId());
    }

    public List<FileOperationResult> CancelOperations(string id = null)
    {
        return fileOperationsManagerHolder.CancelOperations(ProcessUserId(), id);
    }

    #region MarkAsRead

    public void EnqueueMarkAsRead(string taskId)
    {
        var op = fileOperationsManagerHolder.FindById(taskId);

        if (op is DistributedTaskProgress task)
        {
            var operation = fileOperationsManagerHolder.GetService<FileMarkAsReadOperation>();
            operation.Init<FileMarkAsReadOperationData<JsonElement>>((string)task[FileOperation.Data], taskId);
            fileOperationsManagerHolder.Enqueue(operation);
        }
    }

    public async Task PublishMarkAsRead(IEnumerable<JsonElement> folderIds, IEnumerable<JsonElement> fileIds)
    {
        var tenantId = await tenantManager.GetCurrentTenantIdAsync();
        
        var op = fileOperationsManagerHolder.GetService<FileMarkAsReadOperation>();
        op.Init(new FileMarkAsReadOperationData<JsonElement>(folderIds, fileIds, tenantId, GetHttpHeaders()));
        
        var taskId = fileOperationsManagerHolder.Publish(op);
        
        eventBus.Publish(new MarkAsReadIntegrationEvent(authContext.CurrentAccount.ID, tenantId)
        {
            TaskId = taskId
        });
    }

    #endregion

    #region Download
    
    public void EnqueueDownload(string taskId)
    {
        var op = fileOperationsManagerHolder.FindById(taskId);

        if (op is DistributedTaskProgress task)
        {
            var operation = fileOperationsManagerHolder.GetService<FileDownloadOperation>();
            operation.Init<FileDownloadOperationData<JsonElement>>((string)task[FileOperation.Data], taskId);
            fileOperationsManagerHolder.Enqueue(operation);
        }
    }
    
    public async Task PublishDownload(IEnumerable<JsonElement> folders, IEnumerable<FilesDownloadOperationItem<JsonElement>> files, string baseUri)
    {
        fileOperationsManagerHolder.CheckRunning(authContext.CurrentAccount.ID, FileOperationType.Download);
        
        var tenantId = await tenantManager.GetCurrentTenantIdAsync();
        
        var op = fileOperationsManagerHolder.GetService<FileDownloadOperation>();
        op.Init(new FileDownloadOperationData<JsonElement>(folders, files, tenantId, GetHttpHeaders(), baseUri));
        
        var taskId = fileOperationsManagerHolder.Publish(op);
        
        eventBus.Publish(new BulkDownloadIntegrationEvent(authContext.CurrentAccount.ID, tenantId)
        {
            TaskId = taskId
        });
    }

    #endregion

    #region MoveOrCopy

    public void EnqueueMoveOrCopy(string taskId)
    {
        var op = fileOperationsManagerHolder.FindById(taskId);

        if (op is DistributedTaskProgress task)
        {
            var operation = fileOperationsManagerHolder.GetService<FileMoveCopyOperation>();
            operation.Init<FileMoveCopyOperationData<JsonElement>>((string)task[FileOperation.Data], taskId);
            fileOperationsManagerHolder.Enqueue(operation);
        }
    }

    public async Task PublishMoveOrCopyAsync(
        IEnumerable<JsonElement> folderIds,
        IEnumerable<JsonElement> fileIds,
        JsonElement destFolderId,
        bool copy,
        FileConflictResolveType resolveType,
        bool holdResult, 
        bool content = false)
    {        
        if (resolveType == FileConflictResolveType.Overwrite && await userManager.IsUserAsync(authContext.CurrentAccount.ID))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException);
        }
        
        var tenantId = await tenantManager.GetCurrentTenantIdAsync();
        
        var toCopyFolderIds = folderIds;
        var toCopyFilesIds = fileIds;
        
        if (content)
        {        
            var (folderIntIds, folderStringIds) = GetIds(folderIds);
            var (fileIntIds, fileStringIds) = GetIds(fileIds);
            await GetContent(folderIntIds, fileIntIds);
            await GetContent(folderStringIds, fileStringIds);

            toCopyFilesIds = fileIntIds.Select(r => JsonSerializer.SerializeToElement(r))
                .Concat(fileStringIds.Select(r => JsonSerializer.SerializeToElement(r)))
                .ToList();
            
            toCopyFolderIds = folderIntIds.Select(r => JsonSerializer.SerializeToElement(r))
                .Concat(folderStringIds.Select(r => JsonSerializer.SerializeToElement(r)))
                .ToList();
        }
        
        var op = fileOperationsManagerHolder.GetService<FileMoveCopyOperation>();
        op.Init(new FileMoveCopyOperationData<JsonElement>(toCopyFolderIds, toCopyFilesIds, tenantId, destFolderId, copy, resolveType, holdResult, GetHttpHeaders()));
        
        var taskId = fileOperationsManagerHolder.Publish(op);
        
        eventBus.Publish(new MoveOrCopyIntegrationEvent(authContext.CurrentAccount.ID, tenantId)
        {
            TaskId = taskId
        });
        
        async Task GetContent<T1>(List<T1> folderForContentIds, List<T1> fileForContentIds)
        {
            var copyFolderIds = folderForContentIds.ToList();
            folderForContentIds.Clear();

            using var scope = serviceProvider.CreateScope();
            var daoFactory = scope.ServiceProvider.GetService<IDaoFactory>();
            var fileDao = daoFactory.GetFileDao<T1>();
            var folderDao = daoFactory.GetFolderDao<T1>();

            foreach (var folderId in copyFolderIds)
            {
                folderForContentIds.AddRange(await folderDao.GetFoldersAsync(folderId).Select(r => r.Id).ToListAsync());
                fileForContentIds.AddRange(await fileDao.GetFilesAsync(folderId).ToListAsync());
            }
        }
    }

    #endregion

    #region Delete

    public void EnqueueDelete(string taskId)
    {
        var op = fileOperationsManagerHolder.FindById(taskId);

        if (op is DistributedTaskProgress task)
        {
            var operation = fileOperationsManagerHolder.GetService<FileDeleteOperation>();
            operation.Init<FileDeleteOperationData<JsonElement>>((string)task[FileOperation.Data], taskId);
            fileOperationsManagerHolder.Enqueue(operation);
        }
    }

    public async Task PublishDelete<T>(
        IEnumerable<T> folders, 
        IEnumerable<T> files, 
        bool ignoreException, 
        bool holdResult,
        bool immediately,
        bool isEmptyTrash = false)
    {        
        var jsonFolders = folders.Select(r => JsonSerializer.SerializeToElement(r));
        var jsonFiles = files.Select(r => JsonSerializer.SerializeToElement(r));
        await PublishDelete(jsonFolders, jsonFiles, ignoreException, holdResult, immediately, isEmptyTrash);
    }
    
    public async Task PublishDelete(
        IEnumerable<JsonElement> folderIds, 
        IEnumerable<JsonElement> fileIds, 
        bool ignoreException, 
        bool holdResult, 
        bool immediately,
        bool isEmptyTrash = false)
    {        
        var tenantId = await tenantManager.GetCurrentTenantIdAsync();
        
        var op = fileOperationsManagerHolder.GetService<FileDeleteOperation>();
        op.Init(new FileDeleteOperationData<JsonElement>(folderIds, fileIds, tenantId, GetHttpHeaders(), holdResult, ignoreException, immediately, isEmptyTrash));
        
        var taskId = fileOperationsManagerHolder.Publish(op);

        IntegrationEvent toPublish;
        if (isEmptyTrash)
        {
            toPublish = new EmptyTrashIntegrationEvent(authContext.CurrentAccount.ID, tenantId) { TaskId = taskId };
        }
        else
        {
            toPublish = new DeleteIntegrationEvent(authContext.CurrentAccount.ID, tenantId) { TaskId = taskId };
        }
        
        eventBus.Publish(toPublish);
    }

    #endregion

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

    public static (IEnumerable<FilesDownloadOperationItem<int>>, IEnumerable<FilesDownloadOperationItem<string>>) GetIds(IEnumerable<FilesDownloadOperationItem<JsonElement>> items)
    {
        var (resultInt, resultString) = (new List<FilesDownloadOperationItem<int>>(), new List<FilesDownloadOperationItem<string>>());

        foreach (var item in items)
        {
            if (item.Id.ValueKind == JsonValueKind.Number)
            {
                resultInt.Add(new FilesDownloadOperationItem<int>(item.Id.GetInt32(), item.Ext));
            }
            else if (item.Id.ValueKind == JsonValueKind.String)
            {
                var val = item.Id.GetString();
                if (int.TryParse(val, out var i))
                {
                    resultInt.Add(new FilesDownloadOperationItem<int>(i, item.Ext));
                }
                else
                {
                    resultString.Add(new FilesDownloadOperationItem<string>(val, item.Ext));
                }
            }
            else if (item.Id.ValueKind == JsonValueKind.Object)
            {
                var key = item.Id.GetProperty("key");

                var val = item.Id.GetProperty("value").GetString();

                if (key.ValueKind == JsonValueKind.Number)
                {
                    resultInt.Add(new FilesDownloadOperationItem<int>(key.GetInt32(), val));
                }
                else
                {
                    resultString.Add(new FilesDownloadOperationItem<string>(key.GetString(), val));
                }
            }
        }

        return (resultInt, resultString);
    }

    private Guid ProcessUserId()
    {
        if (authContext.IsAuthenticated)
        {
            return authContext.CurrentAccount.ID;
        }

        return externalShare.GetSessionId();
    }
    
    private IDictionary<string, string> GetHttpHeaders()
    {
        var request = httpContextAccessor?.HttpContext?.Request;

        return MessageSettings.GetHttpHeaders(request).ToDictionary(x => x.Key, x => x.Value.ToString());
    }
}

public static class FileOperationsManagerExtension
{
    public static void Register(DIHelper services)
    {
        services.TryAdd<FileDeleteOperationScope>();
        services.TryAdd<FileMarkAsReadOperationScope>();
        services.TryAdd<FileMoveCopyOperationScope>();
        services.TryAdd<FileOperationScope>();
        services.TryAdd<CompressToArchive>();
        services.TryAdd<FileDownloadOperation>();
        services.TryAdd<FileDeleteOperation>();
        services.TryAdd<FileMarkAsReadOperation>();
        services.TryAdd<FileMoveCopyOperation>();
    }

    public static void RegisterQueue(this IServiceCollection services, int threadCount = 10)
    {
        services.Configure<DistributedTaskQueueFactoryOptions>(FileOperationsManagerHolder.CUSTOM_DISTRIBUTED_TASK_QUEUE_NAME, x =>
        {
            x.MaxThreadsCount = threadCount;
        });
    }
}

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

using Serializer = ProtoBuf.Serializer;

namespace ASC.Web.Files.Services.WCFService.FileOperations;

[Singleton]
public class FileOperationsManagerHolder(
    IDistributedTaskQueueFactory queueFactory, 
    IServiceProvider serviceProvider)
{
    internal const string CUSTOM_DISTRIBUTED_TASK_QUEUE_NAME = "files_operation";
    private readonly DistributedTaskQueue _tasks = queueFactory.CreateQueue(CUSTOM_DISTRIBUTED_TASK_QUEUE_NAME);

    public async Task<List<FileOperationResult>> GetOperationResults(Guid userId)
    {
        var operations = (await _tasks
            .GetAllTasks())
            .Where(t => new Guid(t[FileOperation.Owner]) == userId)
            .ToList();
        
        foreach (var o in operations.Where(o => o.Status > DistributedTaskStatus.Running))
        {
            o[FileOperation.Progress] = 100;

            await _tasks.DequeueTask(o.Id);
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

    public async Task<List<FileOperationResult>> CancelOperations(Guid userId, string id = null)
    {
        var operations = (await _tasks.GetAllTasks())
            .Where(t => (string.IsNullOrEmpty(id) || t.Id == id) && new Guid(t[FileOperation.Owner]) == userId);

        foreach (var o in operations)
        {
            await _tasks.DequeueTask(o.Id);
        }

        return await GetOperationResults(userId);
    }

    public async Task Enqueue(DistributedTaskProgress task)
    {
        await _tasks.EnqueueTask(task);
    }

    public Task<string> Publish(DistributedTaskProgress task)
    {
        return _tasks.PublishTask(task);
    }

    public async Task CheckRunning(Guid userId, FileOperationType fileOperationType)
    {
        var operations = (await _tasks.GetAllTasks())
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
    public async Task<List<FileOperationResult>> GetOperationResults()
    {
        return await fileOperationsManagerHolder.GetOperationResults(GetUserId());
    }

    public async Task<List<FileOperationResult>> CancelOperations(string id = null)
    {
        return await fileOperationsManagerHolder.CancelOperations(GetUserId(), id);
    }
    
    public async Task Enqueue<T, T1, T2>(string taskId, T1 thirdPartyData, T2 data) 
        where T: ComposeFileOperation<T1, T2>
        where T1 : FileOperationData<string>
        where T2 : FileOperationData<int>
    {
        var operation = fileOperationsManagerHolder.GetService<T>();
        operation.Init(data, thirdPartyData, taskId);
        await fileOperationsManagerHolder.Enqueue(operation);
    }
    
    public async Task PublishMarkAsRead(IEnumerable<JsonElement> folderIds, IEnumerable<JsonElement> fileIds)
    {
        if ((folderIds == null || !folderIds.Any()) && (fileIds == null || !fileIds.Any()))
        {
            return;
        }
        
        var tenantId = await tenantManager.GetCurrentTenantIdAsync();
        var sessionSnapshot = await externalShare.TakeSessionSnapshotAsync();
        
        var op = fileOperationsManagerHolder.GetService<FileMarkAsReadOperation>();
        op.Init(true);
        var taskId = await fileOperationsManagerHolder.Publish(op);
        
        var (folderIntIds, folderStringIds) = GetIds(folderIds);
        var (fileIntIds, fileStringIds) = GetIds(fileIds);
        var data = new FileMarkAsReadOperationData<int>(folderIntIds, fileIntIds, tenantId, GetHttpHeaders(), sessionSnapshot);
        var thirdPartyData = new FileMarkAsReadOperationData<string>(folderStringIds, fileStringIds, tenantId, GetHttpHeaders(), sessionSnapshot);
        
        eventBus.Publish(new MarkAsReadIntegrationEvent(authContext.CurrentAccount.ID, tenantId)
        {
            TaskId = taskId,
            Data = data,
            ThirdPartyData = thirdPartyData
        });
    }
    
    public async Task PublishDownload(IEnumerable<JsonElement> folders, IEnumerable<FilesDownloadOperationItem<JsonElement>> files, string baseUri)
    {
        await fileOperationsManagerHolder.CheckRunning(GetUserId(), FileOperationType.Download);
        if ((folders == null || !folders.Any()) && (files == null || !files.Any()))
        {
            return;
        }
        
        var tenantId = await tenantManager.GetCurrentTenantIdAsync();
        var sessionSnapshot = await externalShare.TakeSessionSnapshotAsync();
        
        var op = fileOperationsManagerHolder.GetService<FileDownloadOperation>();
        op.Init(true);
        var taskId = await fileOperationsManagerHolder.Publish(op);
        
        var (folderIntIds, folderStringIds) = GetIds(folders);
        var (fileIntIds, fileStringIds) = GetIds(files);
        
        var data = new FileDownloadOperationData<int>(folderIntIds, fileIntIds, tenantId, GetHttpHeaders(), sessionSnapshot, baseUri);
        var thirdPartyData = new FileDownloadOperationData<string>(folderStringIds, fileStringIds, tenantId, GetHttpHeaders(), sessionSnapshot, baseUri);
        
        eventBus.Publish(new BulkDownloadIntegrationEvent(GetUserId(), tenantId)
        {
            TaskId = taskId,
            Data = data,
            ThirdPartyData = thirdPartyData
        });
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
        
        if ((folderIds == null || !folderIds.Any()) && (fileIds == null || !fileIds.Any()))
        {
            return;
        }
        
        var tenantId = await tenantManager.GetCurrentTenantIdAsync();
        var sessionSnapshot = await externalShare.TakeSessionSnapshotAsync();
        
        var (folderIntIds, folderStringIds) = GetIds(folderIds);
        var (fileIntIds, fileStringIds) = GetIds(fileIds);
        
        if (content)
        {        
            await GetContent(folderIntIds, fileIntIds);
            await GetContent(folderStringIds, fileStringIds);
        }
        
        var op = fileOperationsManagerHolder.GetService<FileMoveCopyOperation>();
        op.Init(holdResult, copy);
        var taskId = await fileOperationsManagerHolder.Publish(op);
        
        var data = new FileMoveCopyOperationData<int>(folderIntIds, fileIntIds, tenantId, destFolderId, copy, resolveType, holdResult, GetHttpHeaders(), sessionSnapshot); 
        var thirdPartyData = new FileMoveCopyOperationData<string>(folderStringIds, fileStringIds, tenantId, destFolderId, copy, resolveType, holdResult, GetHttpHeaders(), sessionSnapshot);
        
        eventBus.Publish(new MoveOrCopyIntegrationEvent(authContext.CurrentAccount.ID, tenantId)
        {
            TaskId = taskId,
            Data = data,
            ThirdPartyData = thirdPartyData
        });
        
        return;

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

    public Task PublishDelete<T>(
        IEnumerable<T> folders, 
        IEnumerable<T> files, 
        bool ignoreException, 
        bool holdResult,
        bool immediately,
        bool isEmptyTrash = false)
    {        
        if ((folders == null || !folders.Any()) && (files == null || !files.Any()))
        {
            return Task.CompletedTask;
        }
        
        var folderIds = (folders.OfType<int>(), folders.OfType<string>());
        var fileIds = (files.OfType<int>(), files.OfType<string>());
        
        return PublishDelete(folderIds, fileIds, ignoreException, holdResult, immediately, isEmptyTrash);
    }

    public Task PublishDelete(
        IEnumerable<JsonElement> folders, 
        IEnumerable<JsonElement> files, 
        bool ignoreException, 
        bool holdResult,
        bool immediately,
        bool isEmptyTrash = false)
    {        
        if ((folders == null || !folders.Any()) && (files == null || !files.Any()))
        {
            return Task.CompletedTask;
        }
        
        var folderIds = GetIds(folders);
        var fileIds = GetIds(files);

        return PublishDelete(folderIds, fileIds, ignoreException, holdResult, immediately, isEmptyTrash);
    }

    private async Task PublishDelete(
        (IEnumerable<int>, IEnumerable<string>) folders, 
        (IEnumerable<int>, IEnumerable<string>) files, 
        bool ignoreException, 
        bool holdResult,
        bool immediately,
        bool isEmptyTrash = false)
    {        
        
        var tenantId = await tenantManager.GetCurrentTenantIdAsync();
        var sessionSnapshot = await externalShare.TakeSessionSnapshotAsync();
        
        var op = fileOperationsManagerHolder.GetService<FileDeleteOperation>();
        op.Init(holdResult);
        var taskId = await fileOperationsManagerHolder.Publish(op);
        
        
        var data = new FileDeleteOperationData<int>(folders.Item1, files.Item1, tenantId, GetHttpHeaders(), sessionSnapshot, holdResult, ignoreException, immediately, isEmptyTrash); 
        var thirdPartyData = new FileDeleteOperationData<string>(folders.Item2, files.Item2, tenantId, GetHttpHeaders(), sessionSnapshot, holdResult, ignoreException, immediately, isEmptyTrash);
        
        IntegrationEvent toPublish;
        if (isEmptyTrash)
        {
            toPublish = new EmptyTrashIntegrationEvent(authContext.CurrentAccount.ID, tenantId)
            {
                TaskId = taskId, 
                Data = data,
                ThirdPartyData = thirdPartyData
            };
        }
        else
        {
            toPublish = new DeleteIntegrationEvent(authContext.CurrentAccount.ID, tenantId)
            {
                TaskId = taskId, 
                Data = data,
                ThirdPartyData = thirdPartyData
            };
        }
        
        eventBus.Publish(toPublish);
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

    private static (IEnumerable<FilesDownloadOperationItem<int>>, IEnumerable<FilesDownloadOperationItem<string>>) GetIds(IEnumerable<FilesDownloadOperationItem<JsonElement>> items)
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

    private Guid GetUserId()
    {
        return authContext.IsAuthenticated ? authContext.CurrentAccount.ID : externalShare.GetSessionId();
    }
    
    private Dictionary<string, string> GetHttpHeaders()
    {
        var request = httpContextAccessor?.HttpContext?.Request;
        var headers = MessageSettings.GetHttpHeaders(request);
        
        return headers == null 
            ? new Dictionary<string, string>() 
            : headers.ToDictionary(x => x.Key, x => x.Value.ToString());
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

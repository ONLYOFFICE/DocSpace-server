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

namespace ASC.Web.Files.Services.WCFService.FileOperations;

[Singleton(GenericArguments = [typeof(FileDeleteOperation)])]
[Singleton(GenericArguments = [typeof(FileMoveCopyOperation)])]
[Singleton(GenericArguments = [typeof(FileMarkAsReadOperation)])]
[Singleton(GenericArguments = [typeof(FileDuplicateOperation)])]
[Singleton(GenericArguments = [typeof(FileDownloadOperation)])]
public class FileOperationsManagerHolder<T> where T : FileOperation
{
    private readonly IServiceProvider _serviceProvider;
    private readonly DistributedTaskQueue<T> _tasks;

    public FileOperationsManagerHolder(IDistributedTaskQueueFactory queueFactory, NotifyConfiguration notifyConfiguration, IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _tasks = queueFactory.CreateQueue<T>();
        notifyConfiguration.Configure();
    }

    public async Task<List<FileOperationResult>> GetOperationResults(Guid userId, string id = null)
    {
        List<FileOperationResult> results = [];

        var operations = await _tasks.GetAllTasks();

        var userOperations = operations.Where(t => t.Owner == userId);
        if (!string.IsNullOrEmpty(id))
        {
            userOperations = userOperations.Where(t => t.Id == id);
        }

        foreach (var o in userOperations)
        {
            if (o.Status > DistributedTaskStatus.Running)
            {
                o.Progress = 100;

                await _tasks.DequeueTask(o.Id);
            }

            if (o.Hold || o.Progress != 100)
            {
                results.Add(new FileOperationResult
                {
                    Id = o.Id,
                    OperationType = o.FileOperationType,
                    Source = o.Src,
                    Progress = o.Progress,
                    Processed = Convert.ToString(o.Process),
                    Result = o.Result,
                    Error = o.Err,
                    Finished = o.Finish
                });
            }
        }

        return results;
    }

    public async Task<List<FileOperationResult>> CancelOperations(Guid userId, string id = null)
    {
        var operations = (await _tasks.GetAllTasks())
            .Where(t => (string.IsNullOrEmpty(id) || t.Id == id) && t.Owner == userId);

        foreach (var o in operations)
        {
            await _tasks.DequeueTask(o.Id);
        }

        return await GetOperationResults(userId);
    }

    public async Task Enqueue(T task)
    {
        await _tasks.EnqueueTask(task);
    }

    public async Task<string> Publish(T task)
    {
        return await _tasks.PublishTask(task);
    }

    public async Task CheckRunning(Guid userId, FileOperationType fileOperationType)
    {
        var operations = (await _tasks.GetAllTasks())
            .Where(t => t.Owner == userId)
            .Where(t => t.FileOperationType == fileOperationType);

        if (operations.Any(o => o.Status <= DistributedTaskStatus.Running))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_ManyDownloads);
        }
    }

    internal T GetService()
    {
        return _serviceProvider.GetService<T>();
    }
}

public static class FileOperationsManager
{
    public static (List<int>, List<string>) GetIds(IEnumerable<JsonElement> items)
    {
        var (resultInt, resultString) = (new List<int>(), new List<string>());

        foreach (var item in items)
        {
            switch (item.ValueKind)
            {
                case JsonValueKind.Number:
                    resultInt.Add(item.GetInt32());
                    break;
                case JsonValueKind.String:
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

                        break;
                    }
            }
        }

        return (resultInt, resultString);
    }

    public static (List<FilesDownloadOperationItem<int>>, List<FilesDownloadOperationItem<string>>) GetIds(IEnumerable<FilesDownloadOperationItem<JsonElement>> items)
    {
        var (resultInt, resultString) = (new List<FilesDownloadOperationItem<int>>(), new List<FilesDownloadOperationItem<string>>());

        foreach (var item in items)
        {
            if (item.Id.ValueKind == JsonValueKind.Number)
            {
                resultInt.Add(new FilesDownloadOperationItem<int>(item.Id.GetInt32(), item.Ext, item.Password));
            }
            else if (item.Id.ValueKind == JsonValueKind.String)
            {
                var val = item.Id.GetString();
                if (int.TryParse(val, out var i))
                {
                    resultInt.Add(new FilesDownloadOperationItem<int>(i, item.Ext, item.Password));
                }
                else
                {
                    resultString.Add(new FilesDownloadOperationItem<string>(val, item.Ext, item.Password));
                }
            }
            else if (item.Id.ValueKind == JsonValueKind.Object)
            {
                var key = item.Id.GetProperty("key");
                var val = item.Id.GetProperty("value").GetString();
                var password = "";
                if (item.Id.TryGetProperty("password", out var p))
                {
                    password = p.GetString();
                }

                if (key.ValueKind == JsonValueKind.Number)
                {
                    resultInt.Add(new FilesDownloadOperationItem<int>(key.GetInt32(), val, password));
                }
                else
                {
                    resultString.Add(new FilesDownloadOperationItem<string>(key.GetString(), val, password));
                }
            }
        }

        return (resultInt, resultString);
    }
}

public interface IFileOperationsManager
{
    Task<List<FileOperationResult>> GetOperationResults(string id = null);
    Task<List<FileOperationResult>> CancelOperations(string id = null);
}

public abstract class FileOperationsManager<T>(
    IHttpContextAccessor httpContextAccessor,
    IEventBus eventBus,
    AuthContext authContext,
    FileOperationsManagerHolder<T> fileOperationsManagerHolder,
    ExternalShare externalShare,
    IServiceProvider serviceProvider) : IFileOperationsManager where T : FileOperation
{
    protected readonly IEventBus _eventBus = eventBus;
    protected readonly AuthContext _authContext = authContext;
    protected readonly FileOperationsManagerHolder<T> _fileOperationsManagerHolder = fileOperationsManagerHolder;
    protected readonly ExternalShare _externalShare = externalShare;
    protected readonly IServiceProvider _serviceProvider = serviceProvider;

    public async Task<List<FileOperationResult>> GetOperationResults(string id = null)
    {
        return await _fileOperationsManagerHolder.GetOperationResults(await GetUserIdAsync(), id);
    }

    public async Task<List<FileOperationResult>> CancelOperations(string id = null)
    {
        return await _fileOperationsManagerHolder.CancelOperations(await GetUserIdAsync(), id);
    }

    public async Task Enqueue<T1, T2>(string taskId, T1 thirdPartyData, T2 data)
        where T1 : FileOperationData<string>
        where T2 : FileOperationData<int>
    {
        var operation = _fileOperationsManagerHolder.GetService();
        (operation as ComposeFileOperation<T1, T2>)?.Init(data, thirdPartyData, taskId);
        await _fileOperationsManagerHolder.Enqueue(operation);
    }


    protected async Task<Guid> GetUserIdAsync()
    {
        return _authContext.IsAuthenticated ? _authContext.CurrentAccount.ID : await _externalShare.GetSessionIdAsync();
    }

    protected Dictionary<string, string> GetHttpHeaders()
    {
        var request = httpContextAccessor?.HttpContext?.Request;
        var headers = MessageSettings.GetHttpHeaders(request);

        return headers == null
            ? new Dictionary<string, string>()
            : headers.ToDictionary(x => x.Key, x => x.Value.ToString());
    }
}

[Scope(typeof(FileOperationsManager<FileMarkAsReadOperation>))]
public class FileMarkAsReadOperationsManager(
    IHttpContextAccessor httpContextAccessor,
    IEventBus eventBus,
    AuthContext authContext,
    TenantManager tenantManager,
    FileOperationsManagerHolder<FileMarkAsReadOperation> fileOperationsManagerHolder,
    ExternalShare externalShare,
    IServiceProvider serviceProvider) : FileOperationsManager<FileMarkAsReadOperation>(httpContextAccessor, eventBus, authContext, fileOperationsManagerHolder, externalShare, serviceProvider)
{
    public async Task<string> Publish(List<JsonElement> folderIds, List<JsonElement> fileIds)
    {
        if ((folderIds == null || folderIds.Count == 0) && (fileIds == null || fileIds.Count == 0))
        {
            return null;
        }

        var tenantId = tenantManager.GetCurrentTenantId();
        var userId = _authContext.CurrentAccount.ID;
        var sessionSnapshot = await _externalShare.TakeSessionSnapshotAsync();

        var (folderIntIds, folderStringIds) = FileOperationsManager.GetIds(folderIds);
        var (fileIntIds, fileStringIds) = FileOperationsManager.GetIds(fileIds);
        if (folderIntIds.Count == 0 && folderStringIds.Count == 0 && fileIntIds.Count == 0 && fileStringIds.Count == 0)
        {
            return null;
        }

        var op = _serviceProvider.GetService<FileMarkAsReadOperation>();
        op.Init(true);
        var taskId = await _fileOperationsManagerHolder.Publish(op);

        var data = new FileMarkAsReadOperationData<int>(folderIntIds, fileIntIds, tenantId, userId, GetHttpHeaders(), sessionSnapshot);
        var thirdPartyData = new FileMarkAsReadOperationData<string>(folderStringIds, fileStringIds, tenantId, userId, GetHttpHeaders(), sessionSnapshot);

        await _eventBus.PublishAsync(new MarkAsReadIntegrationEvent(_authContext.CurrentAccount.ID, tenantId)
        {
            TaskId = taskId,
            Data = data,
            ThirdPartyData = thirdPartyData
        });

        return taskId;
    }
}

[Scope(typeof(FileOperationsManager<FileDownloadOperation>))]
public class FileDownloadOperationsManager(
    IHttpContextAccessor httpContextAccessor,
    IEventBus eventBus,
    AuthContext authContext,
    TenantManager tenantManager,
    FileOperationsManagerHolder<FileDownloadOperation> fileOperationsManagerHolder,
    ExternalShare externalShare,
    IServiceProvider serviceProvider) : FileOperationsManager<FileDownloadOperation>(httpContextAccessor, eventBus, authContext, fileOperationsManagerHolder, externalShare, serviceProvider)
{
    public async Task<string> Publish(List<JsonElement> folders, List<FilesDownloadOperationItem<JsonElement>> files, string baseUri)
    {
        await _fileOperationsManagerHolder.CheckRunning(await GetUserIdAsync(), FileOperationType.Download);
        if ((folders == null || folders.Count == 0) && (files == null || files.Count == 0))
        {
            return null;
        }

        var tenantId = tenantManager.GetCurrentTenantId();
        var userId = _authContext.CurrentAccount.ID;
        var sessionSnapshot = await _externalShare.TakeSessionSnapshotAsync();

        var (folderIntIds, folderStringIds) = FileOperationsManager.GetIds(folders);
        var (fileIntIds, fileStringIds) = FileOperationsManager.GetIds(files);
        if (folderIntIds.Count == 0 && folderStringIds.Count == 0 && fileIntIds.Count == 0 && fileStringIds.Count == 0)
        {
            return null;
        }

        var op = _serviceProvider.GetService<FileDownloadOperation>();
        op.Init(true);
        var taskId = await _fileOperationsManagerHolder.Publish(op);

        var data = new FileDownloadOperationData<int>(folderIntIds, fileIntIds, tenantId, userId, GetHttpHeaders(), sessionSnapshot, baseUri);
        var thirdPartyData = new FileDownloadOperationData<string>(folderStringIds, fileStringIds, tenantId, userId, GetHttpHeaders(), sessionSnapshot, baseUri);

        await _eventBus.PublishAsync(new BulkDownloadIntegrationEvent(await GetUserIdAsync(), tenantId)
        {
            TaskId = taskId,
            Data = data,
            ThirdPartyData = thirdPartyData
        });

        return taskId;
    }
}

[Scope(typeof(FileOperationsManager<FileMoveCopyOperation>))]
public class FileMoveCopyOperationsManager(
    IHttpContextAccessor httpContextAccessor,
    IEventBus eventBus,
    AuthContext authContext,
    TenantManager tenantManager,
    FileOperationsManagerHolder<FileMoveCopyOperation> fileOperationsManagerHolder,
    ExternalShare externalShare,
    IServiceProvider serviceProvider) : FileOperationsManager<FileMoveCopyOperation>(httpContextAccessor, eventBus, authContext, fileOperationsManagerHolder, externalShare, serviceProvider)
{
    public async Task<string> Publish(
        List<JsonElement> folderIds,
        List<JsonElement> fileIds,
        JsonElement destFolderId,
        bool copy,
        FileConflictResolveType resolveType,
        bool holdResult,
        bool toFillOut,
        bool content = false)
    {

        if ((folderIds == null || folderIds.Count == 0) && (fileIds == null || fileIds.Count == 0))
        {
            return null;
        }

        var tenantId = tenantManager.GetCurrentTenantId();
        var userId = _authContext.CurrentAccount.ID;
        var sessionSnapshot = await _externalShare.TakeSessionSnapshotAsync();

        var (folderIntIds, folderStringIds) = FileOperationsManager.GetIds(folderIds);
        var (fileIntIds, fileStringIds) = FileOperationsManager.GetIds(fileIds);
        if (folderIntIds.Count == 0 && folderStringIds.Count == 0 && fileIntIds.Count == 0 && fileStringIds.Count == 0)
        {
            return null;
        }

        if (content)
        {
            await GetContent(folderIntIds, fileIntIds);
            await GetContent(folderStringIds, fileStringIds);
        }

        var op = _serviceProvider.GetService<FileMoveCopyOperation>();
        op.Init(holdResult, copy);
        var taskId = await _fileOperationsManagerHolder.Publish(op);

        var data = new FileMoveCopyOperationData<int>(folderIntIds, fileIntIds, tenantId, userId, destFolderId, copy, resolveType, toFillOut, holdResult, GetHttpHeaders(), sessionSnapshot);
        var thirdPartyData = new FileMoveCopyOperationData<string>(folderStringIds, fileStringIds, tenantId, userId, destFolderId, copy, resolveType, toFillOut, holdResult, GetHttpHeaders(), sessionSnapshot);

        await _eventBus.PublishAsync(new MoveOrCopyIntegrationEvent(_authContext.CurrentAccount.ID, tenantId)
        {
            TaskId = taskId,
            Data = data,
            ThirdPartyData = thirdPartyData
        });

        return taskId;

        async Task GetContent<T1>(List<T1> folderForContentIds, List<T1> fileForContentIds)
        {
            var copyFolderIds = folderForContentIds.ToList();
            folderForContentIds.Clear();

            using var scope = _serviceProvider.CreateScope();

            var scopedTenantManager = scope.ServiceProvider.GetService<TenantManager>();
            scopedTenantManager.SetCurrentTenant(new Tenant(tenantId, String.Empty));
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
}

[Scope(typeof(FileOperationsManager<FileDuplicateOperation>))]
public class FileDuplicateOperationsManager(
    IHttpContextAccessor httpContextAccessor,
    IEventBus eventBus,
    AuthContext authContext,
    TenantManager tenantManager,
    FileOperationsManagerHolder<FileDuplicateOperation> fileOperationsManagerHolder,
    ExternalShare externalShare,
    IServiceProvider serviceProvider) : FileOperationsManager<FileDuplicateOperation>(httpContextAccessor, eventBus, authContext, fileOperationsManagerHolder, externalShare, serviceProvider)
{
    public async Task<string> Publish(
        List<JsonElement> folderIds,
        List<JsonElement> fileIds)
    {
        if ((folderIds == null || folderIds.Count == 0) && (fileIds == null || fileIds.Count == 0))
        {
            return null;
        }

        var tenantId = tenantManager.GetCurrentTenantId();
        var userId = _authContext.CurrentAccount.ID;
        var sessionSnapshot = await _externalShare.TakeSessionSnapshotAsync();

        var (folderIntIds, folderStringIds) = FileOperationsManager.GetIds(folderIds);
        var (fileIntIds, fileStringIds) = FileOperationsManager.GetIds(fileIds);
        if (folderIntIds.Count == 0 && folderStringIds.Count == 0 && fileIntIds.Count == 0 && fileStringIds.Count == 0)
        {
            return null;
        }

        var op = _serviceProvider.GetService<FileDuplicateOperation>();
        op.Init(true);
        var taskId = await _fileOperationsManagerHolder.Publish(op);

        var data = new FileOperationData<int>(folderIntIds, fileIntIds, tenantId, userId, GetHttpHeaders(), sessionSnapshot);
        var thirdPartyData = new FileOperationData<string>(folderStringIds, fileStringIds, tenantId, userId, GetHttpHeaders(), sessionSnapshot);

        await _eventBus.PublishAsync(new DuplicateIntegrationEvent(_authContext.CurrentAccount.ID, tenantId)
        {
            TaskId = taskId,
            Data = data,
            ThirdPartyData = thirdPartyData
        });

        return taskId;
    }
}

[Scope(typeof(FileOperationsManager<FileDeleteOperation>))]
public class FileDeleteOperationsManager(
    IHttpContextAccessor httpContextAccessor,
    IEventBus eventBus,
    AuthContext authContext,
    TenantManager tenantManager,
    FileOperationsManagerHolder<FileDeleteOperation> fileOperationsManagerHolder,
    ExternalShare externalShare,
    IServiceProvider serviceProvider) : FileOperationsManager<FileDeleteOperation>(httpContextAccessor, eventBus, authContext, fileOperationsManagerHolder, externalShare, serviceProvider)
{
    public Task<string> Publish<T>(
        List<T> folders,
        List<T> files,
        bool ignoreException,
        bool holdResult,
        bool immediately,
        bool isEmptyTrash = false,
        List<int> versions = null)
    {
        if ((folders == null || folders.Count == 0) && (files == null || files.Count == 0))
        {
            return Task.FromResult<string>(null);
        }

        var folderIds = (folders.OfType<int>().ToList(), folders.OfType<string>().ToList());
        var fileIds = (files.OfType<int>().ToList(), files.OfType<string>().ToList());

        return Publish(folderIds, fileIds, ignoreException, holdResult, immediately, isEmptyTrash, versions);
    }

    public Task<string> Publish(
        List<JsonElement> folders,
        List<JsonElement> files,
        bool ignoreException,
        bool holdResult,
        bool immediately,
        bool isEmptyTrash = false,
        List<int> versions = null)
    {
        if ((folders == null || folders.Count == 0) && (files == null || files.Count == 0))
        {
            return Task.FromResult<string>(null);
        }

        var folderIds = FileOperationsManager.GetIds(folders);
        var fileIds = FileOperationsManager.GetIds(files);

        return Publish(folderIds, fileIds, ignoreException, holdResult, immediately, isEmptyTrash, versions);
    }

    private async Task<string> Publish(
        (List<int>, List<string>) folders,
        (List<int>, List<string>) files,
        bool ignoreException,
        bool holdResult,
        bool immediately,
        bool isEmptyTrash = false,
        List<int> versions = null)
    {
        if (folders.Item1.Count == 0 && folders.Item2.Count == 0 && files.Item1.Count == 0 && files.Item2.Count == 0)
        {
            return null;
        }

        var tenantId = tenantManager.GetCurrentTenantId();
        var userId = _authContext.CurrentAccount.ID;
        var sessionSnapshot = await _externalShare.TakeSessionSnapshotAsync();

        var op = _serviceProvider.GetService<FileDeleteOperation>();
        op.Init(holdResult);
        var taskId = await _fileOperationsManagerHolder.Publish(op);

        var data = new FileDeleteOperationData<int>(folders.Item1, files.Item1, versions, tenantId, userId, GetHttpHeaders(), sessionSnapshot, holdResult, ignoreException, immediately, isEmptyTrash);
        var thirdPartyData = new FileDeleteOperationData<string>(folders.Item2, files.Item2, versions, tenantId, userId, GetHttpHeaders(), sessionSnapshot, holdResult, ignoreException, immediately, isEmptyTrash);
        IntegrationEvent toPublish;
        if (isEmptyTrash)
        {
            toPublish = new EmptyTrashIntegrationEvent(_authContext.CurrentAccount.ID, tenantId)
            {
                TaskId = taskId,
                Data = data,
                ThirdPartyData = thirdPartyData
            };
        }
        else
        {
            toPublish = new DeleteIntegrationEvent(_authContext.CurrentAccount.ID, tenantId)
            {
                TaskId = taskId,
                Data = data,
                ThirdPartyData = thirdPartyData
            };
        }

        await _eventBus.PublishAsync(toPublish);

        return taskId;
    }
}
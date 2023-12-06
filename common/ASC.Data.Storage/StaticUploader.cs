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

namespace ASC.Data.Storage;

[Scope]
public class StaticUploader(IServiceProvider serviceProvider,
    TenantManager tenantManager,
    SettingsManager settingsManager,
    StorageSettingsHelper storageSettingsHelper,
    UploadOperation uploadOperation,
    ICache cache,
    IDistributedTaskQueueFactory queueFactory,
    IDistributedLockProvider distributedLockProvider)
{
    protected readonly DistributedTaskQueue _queue = queueFactory.CreateQueue(CUSTOM_DISTRIBUTED_TASK_QUEUE_NAME);
    public const string CUSTOM_DISTRIBUTED_TASK_QUEUE_NAME = "static_upload"; 
    private static readonly CancellationTokenSource _tokenSource;
    private static readonly object _locker;

    static StaticUploader()
    {
        _locker = new object();
        _tokenSource = new CancellationTokenSource();
    }

    public async Task<string> UploadFileAsync(string relativePath, string mappedPath, Action<string> onComplete = null)
    {
        if (_tokenSource.Token.IsCancellationRequested)
        {
            return null;
        }

        if (!await CanUploadAsync())
        {
            return null;
        }

        if (!File.Exists(mappedPath))
        {
            return null;
        }

        var tenantId = await tenantManager.GetCurrentTenantIdAsync();
        var key = GetCacheKey(tenantId.ToString(), relativePath);

        lock (_locker)
        {
            var result = cache.Get<string>(key);
            if (!string.IsNullOrEmpty(result))
            {
                return result;
            }
        }

        await uploadOperation.DoJobAsync(tenantId, relativePath, mappedPath);
        onComplete?.Invoke(uploadOperation.Result);

        lock (_locker)
        {
            cache.Insert(key, uploadOperation.Result, DateTime.MaxValue);
        }

        return uploadOperation.Result;
    }

    public async Task UploadDirAsync(string relativePath, string mappedPath)
    {
        if (!await CanUploadAsync())
        {
            return;
        }

        if (!Directory.Exists(mappedPath))
        {
            return;
        }

        var tenant = await tenantManager.GetCurrentTenantAsync();
        var key = typeof(UploadOperationProgress).FullName + tenant.Id;

        await using (await distributedLockProvider.TryAcquireLockAsync($"lock_{CUSTOM_DISTRIBUTED_TASK_QUEUE_NAME}", TimeSpan.FromMinutes(1)))
        {
            if (_queue.GetAllTasks().Any(x => x.Id != key))
            {
                return;
            }

            var uploadOperation = new UploadOperationProgress(serviceProvider, key, tenant.Id, relativePath, mappedPath);

            _queue.EnqueueTask(uploadOperation);
        }
    }

    public async Task<bool> CanUploadAsync()
    {
        var current = storageSettingsHelper.DataStoreConsumer(await settingsManager.LoadAsync<CdnStorageSettings>());
        if (current == null || !current.IsSet || (string.IsNullOrEmpty(current["cnamessl"]) && string.IsNullOrEmpty(current["cname"])))
        {
            return false;
        }

        return true;
    }

    public static void Stop()
    {
        _tokenSource.Cancel();
    }

    public async Task<UploadOperationProgress> GetProgressAsync(int tenantId)
    {
        await using (await distributedLockProvider.TryAcquireLockAsync($"lock_{CUSTOM_DISTRIBUTED_TASK_QUEUE_NAME}", TimeSpan.FromMinutes(1)))
        {
            var key = typeof(UploadOperationProgress).FullName + tenantId;

            return _queue.PeekTask<UploadOperationProgress>(key);
        }
    }

    private static string GetCacheKey(string tenantId, string path)
    {
        return typeof(UploadOperation).FullName + tenantId + path;
    }
}

[Scope]
public class UploadOperation(ILogger<UploadOperation> logger,
        TenantManager tenantManager,
        SecurityContext securityContext,
        SettingsManager settingsManager,
        StorageSettingsHelper storageSettingsHelper)
    {
    public string Result { get; private set; } = string.Empty;

    public async Task<string> DoJobAsync(int tenantId, string path, string mappedPath)
    {
        try
        {
            path = path.TrimStart('/');
            var tenant = await tenantManager.GetTenantAsync(tenantId);
            tenantManager.SetCurrentTenant(tenant);
            await securityContext.AuthenticateMeWithoutCookieAsync(tenant.OwnerId);

            var dataStore = await storageSettingsHelper.DataStoreAsync(await settingsManager.LoadAsync<CdnStorageSettings>());

            if (File.Exists(mappedPath))
            {
                if (!await dataStore.IsFileAsync(path))
                {
                    await using var stream = File.OpenRead(mappedPath);
                    await dataStore.SaveAsync(path, stream);
                }
                var uri = await dataStore.GetInternalUriAsync("", path, TimeSpan.Zero, null);
                Result = uri.AbsoluteUri.ToLower();
                logger.DebugUploadFile(Result);
                return Result;
            }
        }
        catch (Exception e)
        {
            logger.ErrorUploadOperation(e);
        }

        return null;
    }
}

[Transient]
public class UploadOperationProgress : DistributedTaskProgress
{
    public int TenantId { get; }

    private readonly string _relativePath;
    private readonly string _mappedPath;
    private readonly IEnumerable<string> _directoryFiles;
    private readonly IServiceProvider _serviceProvider;

    public UploadOperationProgress(IServiceProvider serviceProvider, string key, int tenantId, string relativePath, string mappedPath)
    {
        _serviceProvider = serviceProvider;

        Id = key;
        Status = DistributedTaskStatus.Created;

        TenantId = tenantId;
        _relativePath = relativePath;
        _mappedPath = mappedPath;

        const string extensions = ".png|.jpeg|.jpg|.gif|.ico|.swf|.mp3|.ogg|.eot|.svg|.ttf|.woff|.woff2|.css|.less|.js";
        var extensionsArray = extensions.Split('|');

        _directoryFiles = Directory.GetFiles(mappedPath, "*", SearchOption.AllDirectories)
            .Where(r => extensionsArray.Contains(Path.GetExtension(r)))
            .ToList();

        StepCount = _directoryFiles.Count();
    }

    protected override async Task DoJob()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
        var staticUploader = scope.ServiceProvider.GetService<StaticUploader>();
        var tenant = await tenantManager.GetTenantAsync(TenantId);
        tenantManager.SetCurrentTenant(tenant);

        tenant.SetStatus(TenantStatus.Migrating);
        await tenantManager.SaveTenantAsync(tenant);
        PublishChanges();

        foreach (var file in _directoryFiles)
        {
            var filePath = file[_mappedPath.TrimEnd('/').Length..];
            await staticUploader.UploadFileAsync(CrossPlatform.PathCombine(_relativePath, filePath), file, _ => StepDone());
        }

        tenant.SetStatus(TenantStatus.Active);
        await tenantManager.SaveTenantAsync(tenant);
    }

    public object Clone()
    {
        return MemberwiseClone();
    }
}
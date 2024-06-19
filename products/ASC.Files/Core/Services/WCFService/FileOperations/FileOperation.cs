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

namespace ASC.Web.Files.Services.WCFService.FileOperations;

public abstract class FileOperation : DistributedTaskProgress
{
    protected readonly IServiceProvider _serviceProvider;
    public const string SplitChar = ":";
    public const string Owner = "Owner";
    public const string OpType = "OperationType";
    public const string Src = "Source";
    public const string Progress = "Progress";
    public const string Res = "Result";
    public const string Err = "Error";
    public const string Process = "Processed";
    public const string Finish = "Finished";
    public const string Hold = "Hold";

    protected readonly IPrincipal _principal;
    protected readonly string _culture;

    protected int _processed;
    public int Total { get; set; }

    protected FileOperation(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _principal = serviceProvider.GetService<IHttpContextAccessor>()?.HttpContext?.User ?? CustomSynchronizationContext.CurrentContext.CurrentPrincipal;
        _culture = CultureInfo.CurrentCulture.Name;

        if (_principal is { Identity: IAccount { IsAuthenticated: true } account })
        {
            this[Owner] = account.ID.ToString();
        }
        else
        {
            var externalShare = serviceProvider.GetRequiredService<ExternalShare>();
            this[Owner] = externalShare.GetSessionId().ToString();
        }

        this[Src] = _props.ContainsValue(Src) ? this[Src] : "";
        this[Progress] = 0;
        this[Res] = "";
        this[Err] = "";
        this[Process] = 0;
        this[Finish] = false;
    }

    protected void IncrementProgress()
    {
        _processed++;
        var progress = Total != 0 ? 100 * _processed / Total : 0;
        this[Progress] = progress < 100 ? progress : 100;
    }

    protected abstract Task DoJob(IServiceScope serviceScope);
}

public abstract class ComposeFileOperation<T1, T2>(IServiceProvider serviceProvider) : FileOperation(serviceProvider)
    where T1 : FileOperationData<string>
    where T2 : FileOperationData<int>
{
    protected abstract FileOperationType FileOperationType { get; }
    protected FileOperation<T1, string> ThirdPartyOperation { get; set; }
    protected FileOperation<T2, int> DaoOperation { get; set; }
    
    protected  T1 ThirdPartyData { get; set; }
    protected  T2 Data { get; set; }


    public void Init(bool holdResult)
    {
        this[OpType] = (int)FileOperationType;
        this[Hold] = holdResult;
    }

    public virtual void Init(T2 data, T1 thirdPartyData, string taskId)
    {
        Data = data;
        ThirdPartyData = thirdPartyData;
        Id = taskId;
        Init(data.HoldResult);
    }

    public override async Task RunJob(DistributedTask distributedTask, CancellationToken cancellationToken)
    {
        var daoOperation = DaoOperation.Files.Count != 0 || DaoOperation.Folders.Count != 0;
        var thirdPartyOperation = ThirdPartyOperation.Files.Count != 0 || ThirdPartyOperation.Folders.Count != 0;

        DaoOperation[Finish] = !daoOperation;
        ThirdPartyOperation[Finish] = !thirdPartyOperation;

        if (daoOperation)
        {
            DaoOperation.Publication = PublishChanges;
            await DaoOperation.RunJob(distributedTask, cancellationToken);
        }

        if (thirdPartyOperation)
        {
            ThirdPartyOperation.Publication = PublishChanges;
            await ThirdPartyOperation.RunJob(distributedTask, cancellationToken);
        }
    }

    protected virtual async Task PublishChanges(DistributedTask task)
    {
        var thirdpartyTask = ThirdPartyOperation;
        var daoTask = DaoOperation;

        var error1 = thirdpartyTask[Err];
        var error2 = daoTask[Err];

        if (!string.IsNullOrEmpty(error1))
        {
            this[Err] = error1;
        }
        else if (!string.IsNullOrEmpty(error2))
        {
            this[Err] = error2;
        }

        var status1 = thirdpartyTask[Res];
        var status2 = daoTask[Res];

        if (!string.IsNullOrEmpty(status1))
        {
            this[Res] = status1;
        }
        else if (!string.IsNullOrEmpty(status2))
        {
            this[Res] = status2;
        }

        bool finished1 = thirdpartyTask[Finish];
        bool finished2 = daoTask[Finish];

        if (finished1 && finished2)
        {
            this[Finish] = true;
        }

        this[Process] = thirdpartyTask[Process] + daoTask[Process];

        var progress = 0;

        if (ThirdPartyOperation.Total != 0)
        {
            progress += thirdpartyTask[Progress];
        }

        if (DaoOperation.Total != 0)
        {
            progress += daoTask[Progress];
        }

        if (ThirdPartyOperation.Total != 0 && DaoOperation.Total != 0)
        {
            progress /= 2;
        }

        this[Progress] = progress < 100 ? progress : 100;
        await PublishChanges();
    }

    protected override Task DoJob(IServiceScope serviceScope)
    {
        throw new NotImplementedException();
    }
}

[ProtoContract]
[ProtoInclude(100, typeof(FileDeleteOperationData<string>))]
[ProtoInclude(101, typeof(FileDeleteOperationData<int>))]
[ProtoInclude(102, typeof(FileMoveCopyOperationData<int>))]
[ProtoInclude(103, typeof(FileMoveCopyOperationData<string>))]
[ProtoInclude(104, typeof(FileMarkAsReadOperationData<int>))]
[ProtoInclude(105, typeof(FileMarkAsReadOperationData<string>))]
[ProtoInclude(106, typeof(FileDownloadOperationData<int>))]
[ProtoInclude(107, typeof(FileDownloadOperationData<string>))]
public record FileOperationData<T>
{
    [ProtoMember(1)]
    public IEnumerable<T> Folders { get; set; }
    
    [ProtoMember(2)]
    public IEnumerable<T> Files { get; set; }
    
    [ProtoMember(3)]
    public int TenantId { get; set; }
    
    [ProtoMember(4)]
    public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
    
    [ProtoMember(5)]
    public ExternalSessionSnapshot SessionSnapshot { get; set; }
    
    [ProtoMember(6)]
    public bool HoldResult { get; set; }

    public FileOperationData()
    {
        
    }

    public FileOperationData(IEnumerable<T> folders, IEnumerable<T> files, int tenantId, IDictionary<string, string> headers, ExternalSessionSnapshot sessionSnapshot, bool holdResult = true)
    {
        Folders = folders;
        Files = files;
        TenantId = tenantId;
        Headers = headers;
        SessionSnapshot = sessionSnapshot;
        HoldResult = holdResult;
    }
}

public abstract class FileOperation<T, TId> : FileOperation where T : FileOperationData<TId>
{
    protected int CurrentTenantId { get; }
    protected FileSecurity FilesSecurity { get; private set; }
    protected IFolderDao<TId> FolderDao { get; private set; }
    protected IFileDao<TId> FileDao { get; private set; }
    protected ITagDao<TId> TagDao { get; private set; }
    protected ILinkDao LinkDao { get; private set; }
    protected IProviderDao ProviderDao { get; private set; }
    protected ILogger Logger { get; private set; }
    protected internal List<TId> Folders { get; }
    protected internal List<TId> Files { get; }
    protected IDictionary<string, StringValues> Headers { get; } = new Dictionary<string, StringValues>();  
    protected ExternalSessionSnapshot SessionSnapshot { get; }

    protected FileOperation(IServiceProvider serviceProvider, T fileOperationData) : base(serviceProvider)
    {
        Files = fileOperationData.Files?.ToList() ?? [];
        Folders = fileOperationData.Folders?.ToList() ?? [];
        this[Hold] = fileOperationData.HoldResult;
        CurrentTenantId = fileOperationData.TenantId;
        Headers = fileOperationData.Headers.ToDictionary(x => x.Key, x => new StringValues(x.Value));
        SessionSnapshot = fileOperationData.SessionSnapshot;

        using var scope = _serviceProvider.CreateScope();
        var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
        tenantManager.SetCurrentTenantAsync(CurrentTenantId).Wait();

        var externalShare = scope.ServiceProvider.GetRequiredService<ExternalShare>();
        externalShare.Initialize(SessionSnapshot);

        var daoFactory = scope.ServiceProvider.GetService<IDaoFactory>();
        FolderDao = daoFactory.GetFolderDao<TId>();

        Total = InitTotalProgressSteps();
        this[Src] = string.Join(SplitChar, Folders.Select(f => "folder_" + f).Concat(Files.Select(f => "file_" + f)).ToArray());
    }

    public override async Task RunJob(DistributedTask distributedTask, CancellationToken cancellationToken)
    {
        try
        {
            CancellationToken = cancellationToken;

            await using var scope = _serviceProvider.CreateAsyncScope();
            var scopeClass = scope.ServiceProvider.GetService<FileOperationScope>();
            var (tenantManager, daoFactory, fileSecurity, logger) = scopeClass;
            await tenantManager.SetCurrentTenantAsync(CurrentTenantId);

            var externalShare = scope.ServiceProvider.GetRequiredService<ExternalShare>();
            externalShare.Initialize(SessionSnapshot);

            CustomSynchronizationContext.CurrentContext.CurrentPrincipal = _principal;
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(_culture);
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(_culture);

            FolderDao = daoFactory.GetFolderDao<TId>();
            FileDao = daoFactory.GetFileDao<TId>();
            TagDao = daoFactory.GetTagDao<TId>();
            LinkDao = daoFactory.GetLinkDao();
            ProviderDao = daoFactory.ProviderDao;
            FilesSecurity = fileSecurity;

            Logger = logger;

            await DoJob(scope);
        }
        catch (AuthorizingException authError)
        {
            this[Err] = FilesCommonResource.ErrorMessage_SecurityException;
            Logger.ErrorWithException(new SecurityException(this[Err], authError));
        }
        catch (AggregateException ae)
        {
            ae.Flatten().Handle(e => e is TaskCanceledException or OperationCanceledException);
        }
        catch (Exception error)
        {
            Logger.ErrorWithException(error);
        }
        finally
        {
            try
            {
                this[Finish] = true;
                await PublishChanges();
            }
            catch
            {
                /* ignore */
            }
        }
    }

    public async Task<AsyncServiceScope> CreateScopeAsync()
    {
        var scope = _serviceProvider.CreateAsyncScope();
        var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
        await tenantManager.SetCurrentTenantAsync(CurrentTenantId);
        var externalShare = scope.ServiceProvider.GetRequiredService<ExternalShare>();
        externalShare.Initialize(SessionSnapshot);

        return scope;
    }

    protected virtual int InitTotalProgressSteps()
    {
        var count = Files.Count;
        Folders.ForEach(f => count += 1 + (FolderDao.CanCalculateSubitems(f) ? FolderDao.GetItemsCountAsync(f).Result : 0));

        return count;
    }

    protected async Task ProgressStep(TId folderId = default, TId fileId = default)
    {
        if (Equals(folderId, default(TId)) && Equals(fileId, default(TId))
            || !Equals(folderId, default(TId)) && Folders.Contains(folderId)
            || !Equals(fileId, default(TId)) && Files.Contains(fileId))
        {
            IncrementProgress();
            await PublishChanges();
        }
    }

    protected bool ProcessedFolder(TId folderId)
    {
        this[Process]++;
        if (Folders.Contains(folderId))
        {
            this[Res] += $"folder_{folderId}{SplitChar}";

            return true;
        }

        return false;
    }

    protected bool ProcessedFile(TId fileId)
    {
        this[Process]++;
        if (Files.Contains(fileId))
        {
            this[Res] += $"file_{fileId}{SplitChar}";

            return true;
        }

        return false;
    }
}

[Scope]
public record FileOperationScope(
    TenantManager TenantManager,
    IDaoFactory DaoFactory,
    FileSecurity FileSecurity,
    ILogger<FileOperationScope> Options);
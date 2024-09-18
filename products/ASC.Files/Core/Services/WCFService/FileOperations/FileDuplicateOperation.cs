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

[Transient]
public class FileDuplicateOperation(IServiceProvider serviceProvider) : ComposeFileOperation<FileOperationData<string>, FileOperationData<int>>(serviceProvider)
{
    protected override FileOperationType FileOperationType => FileOperationType.Duplicate;

    public override Task RunJob(DistributedTask distributedTask, CancellationToken cancellationToken)
    {
        DaoOperation = new FileDuplicateOperation<int>(_serviceProvider, Data);
        ThirdPartyOperation = new FileDuplicateOperation<string>(_serviceProvider, ThirdPartyData);

        return base.RunJob(distributedTask, cancellationToken);

    }
}

class FileDuplicateOperation<T>(IServiceProvider serviceProvider, FileOperationData<T> data) : FileOperation<FileOperationData<T>, T>(serviceProvider, data)
{
    private readonly IDictionary<string, string> _headers = data.Headers;
    private DistributedTask _distributedTask;
    private CancellationToken _cancellationToken;
    public override Task RunJob(DistributedTask distributedTask, CancellationToken cancellationToken)
    {
        _distributedTask = distributedTask;
        _cancellationToken = cancellationToken;
        return base.RunJob(distributedTask, cancellationToken);
    }

    protected override async Task DoJob(IServiceScope serviceScope)
    {
        foreach (var file in Files)
        {
            await DoFileAsync(serviceScope, file);
        }
        foreach (var folder in Folders)
        {
            await DoFolderAsync(serviceScope, folder);
        }
    }

    private async Task DoFileAsync(IServiceScope scope, T id)
    {
        var fileDao = scope.ServiceProvider.GetService<IFileDao<T>>();
        var file = await fileDao.GetFilesAsync([id]).FirstOrDefaultAsync(cancellationToken: _cancellationToken);
        var copyOperationData = new FileMoveCopyOperationData<T>([], [id], CurrentTenantId, JsonSerializer.SerializeToElement(file.ParentId), true, FileConflictResolveType.Duplicate, true, _headers, SessionSnapshot);
        var copyOperation = new FileMoveCopyOperation<T>(scope.ServiceProvider, copyOperationData) 
        { 
            Publication = FileMoveCopyOperationPublishChanges
        };
        await copyOperation.RunJob(_distributedTask, _cancellationToken);
    }

    private async Task DoFolderAsync(IServiceScope scope, T id)
    {             
        var folderDao = scope.ServiceProvider.GetService<IFolderDao<T>>();   
        var folder = await folderDao.GetFolderAsync(id);
        var copyOperationData = new FileMoveCopyOperationData<T>([id], [], CurrentTenantId,  JsonSerializer.SerializeToElement(folder.ParentId), true, FileConflictResolveType.Duplicate, true, _headers, SessionSnapshot);
        var copyOperation = new FileMoveCopyOperation<T>(scope.ServiceProvider, copyOperationData)        
        { 
            Publication = FileMoveCopyOperationPublishChanges
        };
        await copyOperation.RunJob(_distributedTask, _cancellationToken);
    }

    private readonly Dictionary<string, Dictionary<string, dynamic>> _tasksProps = new();
    private async Task FileMoveCopyOperationPublishChanges(DistributedTask task)
    {
        if (!_tasksProps.TryGetValue(task.Id, out var value))
        {
            value = new Dictionary<string, dynamic>();
            _tasksProps.Add(task.Id, value);
        }
        
        value[Process] = task[Process];
        value[Res] = task[Res];
        value[Err] = task[Err];

        this[Process] = 0;
        this[Res] = "";
        
        foreach (var data in _tasksProps)
        {
            this[Process] += data.Value[Process];
            this[Res] += data.Value[Res];
            var err = data.Value[Err];
            if (!string.IsNullOrEmpty(err))
            {
                this[Err] = err;
            }
        }
        
        var progressSteps = Total;

        var progress = (int)(this[Process] / (double)progressSteps * 100);

        this[Progress] = progress < 100 ? progress : 100;
        
        await PublishChanges();
    }
}

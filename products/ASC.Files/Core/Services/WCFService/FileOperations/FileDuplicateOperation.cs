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

[Transient]
public class FileDuplicateOperation : ComposeFileOperation<FileOperationData<string>, FileOperationData<int>>
{    
    public override FileOperationType FileOperationType { get; set; } = FileOperationType.Duplicate;
    public FileDuplicateOperation() { }
    public FileDuplicateOperation(IServiceProvider serviceProvider) : base(serviceProvider) { }
    
    public override Task RunJob(CancellationToken cancellationToken)
    {
        DaoOperation = new FileDuplicateOperation<int>(_serviceProvider, Data);
        ThirdPartyOperation = new FileDuplicateOperation<string>(_serviceProvider, ThirdPartyData);

        return base.RunJob(cancellationToken);

    }
}

class FileDuplicateOperation<T>(IServiceProvider serviceProvider, FileOperationData<T> data) : FileOperation<FileOperationData<T>, T>(serviceProvider, data)
{
    private readonly IDictionary<string, string> _headers = data.Headers;
    private CancellationToken _cancellationToken;
    public override FileOperationType FileOperationType { get; set; } = FileOperationType.Duplicate;
    
    public override Task RunJob(CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
        return base.RunJob(cancellationToken);
    }

    protected override async Task DoJob(AsyncServiceScope serviceScope)
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

    private async Task DoFileAsync(AsyncServiceScope scope, T id)
    {
        var fileDao = scope.ServiceProvider.GetService<IFileDao<T>>();
        var file = await fileDao.GetFilesAsync([id]).FirstOrDefaultAsync(cancellationToken: _cancellationToken);
        var copyOperationData = new FileMoveCopyOperationData<T>([], [id], CurrentTenantId, CurrentUserId, JsonSerializer.SerializeToElement(file.ParentId), true, FileConflictResolveType.Duplicate, true, false, _headers, SessionSnapshot);
        var copyOperation = new FileMoveCopyOperation<T>(scope.ServiceProvider, copyOperationData) 
        { 
            Publication = FileMoveCopyOperationPublishChanges
        };
        await copyOperation.RunJob(_cancellationToken);
    }

    private async Task DoFolderAsync(AsyncServiceScope scope, T id)
    {             
        var folderDao = scope.ServiceProvider.GetService<IFolderDao<T>>();   
        var folder = await folderDao.GetFolderAsync(id);
        var copyOperationData = new FileMoveCopyOperationData<T>([id], [], CurrentTenantId, CurrentUserId, JsonSerializer.SerializeToElement(folder.ParentId), true, FileConflictResolveType.Duplicate, true, false, _headers, SessionSnapshot);
        var copyOperation = new FileMoveCopyOperation<T>(scope.ServiceProvider, copyOperationData)        
        { 
            Publication = FileMoveCopyOperationPublishChanges
        };
        await copyOperation.RunJob(_cancellationToken);
    }

    private readonly Dictionary<string, FileOperation> _tasksProps = new();
    private async Task FileMoveCopyOperationPublishChanges(DistributedTask task)
    {
        _tasksProps[task.Id] = (FileOperation)task;
        
        Process = 0;
        Result = "";
        
        foreach (var data in _tasksProps)
        {
            Process += data.Value.Process;
            Result += data.Value.Result;
            var err = data.Value.Err;
            if (!string.IsNullOrEmpty(err))
            {
                Err = err;
            }
        }
        
        var progressSteps = Total;

        var progress = (int)(Process / (double)progressSteps * 100);

        Progress = progress < 100 ? progress : 100;
        
        await PublishChanges();
    }
}

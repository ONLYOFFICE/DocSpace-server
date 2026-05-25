// Copyright (C) Ascensio System SIA, 2009-2026
// 
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied
// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For
// details, see the GNU AGPL at: https://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA by email at info@onlyoffice.com
// or by postal mail at 20A-6 Ernesta Birznieka-Upisha Street, Riga,
// LV-1050, Latvia, European Union.
// 
// The interactive user interfaces in modified versions of the Program
// are required to display Appropriate Legal Notices in accordance with
// Section 5 of the GNU AGPL version 3.
// 
// No trademark rights are granted under this License.
// 
// All non-code elements of the Product, including illustrations,
// icon sets, and technical writing content, are licensed under the
// Creative Commons Attribution-ShareAlike 4.0 International License:
// https://creativecommons.org/licenses/by-sa/4.0/legalcode
// 
// This license applies only to such non-code elements and does not
// modify or replace the licensing terms applicable to the Program's
// source code, which remains licensed under the GNU Affero General
// Public License v3.
// 
// SPDX-License-Identifier: AGPL-3.0-only

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

internal class FileDuplicateOperation<T>(IServiceProvider serviceProvider, FileOperationData<T> data) : FileOperation<FileOperationData<T>, T>(serviceProvider, data)
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
        var copyOperationData = new FileMoveCopyOperationData<T>([], [id], CurrentTenantId, CurrentUserId, JsonSerializer.SerializeToElement(file.ParentId), true, FileConflictResolveType.Duplicate, false, true, _headers, SessionSnapshot);
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
        var copyOperationData = new FileMoveCopyOperationData<T>([id], [], CurrentTenantId, CurrentUserId, JsonSerializer.SerializeToElement(folder.ParentId), true, FileConflictResolveType.Duplicate, false, true, _headers, SessionSnapshot);
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
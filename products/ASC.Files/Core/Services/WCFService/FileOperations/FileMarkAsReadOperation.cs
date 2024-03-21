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

record FileMarkAsReadOperationData<T>(
    IEnumerable<T> Folders,
    IEnumerable<T> Files,
    int TenantId,
    IDictionary<string, string> Headers,
    bool HoldResult = true)
    : FileOperationData<T>(Folders, Files, TenantId, Headers, HoldResult);

[Transient]
class FileMarkAsReadOperation(IServiceProvider serviceProvider) : 
    ComposeFileOperation<FileMarkAsReadOperationData<string>, FileMarkAsReadOperationData<int>>(serviceProvider)
{
    protected override FileOperationType FileOperationType { get => FileOperationType.MarkAsRead; }
    
    public override Task RunJob(DistributedTask distributedTask, CancellationToken cancellationToken)
    {
        var data = JsonSerializer.Deserialize<FileMarkAsReadOperationData<JsonElement>>((string)this[Data]);
        var (folderIntIds, folderStringIds) = FileOperationsManager.GetIds(data.Folders);
        var (fileIntIds, fileStringIds) = FileOperationsManager.GetIds(data.Files);
        DaoOperation = new FileMarkAsReadOperation<int>(_serviceProvider, new FileMarkAsReadOperationData<int>(folderIntIds, fileIntIds, data.TenantId, data.Headers, data.HoldResult));
        ThirdPartyOperation = new FileMarkAsReadOperation<string>(_serviceProvider, new FileMarkAsReadOperationData<string>(folderStringIds, fileStringIds, data.TenantId, data.Headers, data.HoldResult));

        return base.RunJob(distributedTask, cancellationToken);
    }
}

class FileMarkAsReadOperation<T> : FileOperation<FileMarkAsReadOperationData<T>, T>
{
    private readonly IDictionary<string, StringValues> _headers;

    public FileMarkAsReadOperation(IServiceProvider serviceProvider, FileMarkAsReadOperationData<T> fileOperationData)
        : base(serviceProvider, fileOperationData)
    {
        _headers = fileOperationData.Headers.ToDictionary(x => x.Key, x => new StringValues(x.Value));
        this[OpType] = (int)FileOperationType.MarkAsRead;
    }

    protected override int InitTotalProgressSteps()
    {
        return Files.Count + Folders.Count;
    }

    protected override async Task DoJob(IServiceScope serviceScope)
    {
        var scopeClass = serviceScope.ServiceProvider.GetService<FileMarkAsReadOperationScope>();
        var filesMessageService = serviceScope.ServiceProvider.GetRequiredService<FilesMessageService>();
        var fileSecurity = serviceScope.ServiceProvider.GetRequiredService<FileSecurity>();
        var (fileMarker, globalFolder, daoFactory, settingsManager) = scopeClass;
        var entries = Enumerable.Empty<FileEntry<T>>();
        if (Folders.Count > 0)
        {
            entries = entries.Concat(await fileSecurity.CanReadAsync(FolderDao.GetFoldersAsync(Folders)).Where(r => r.Item2).Select(r => r.Item1).ToListAsync());
        }
        if (Files.Count > 0)
        {
            entries = entries.Concat(await fileSecurity.CanReadAsync(FileDao.GetFilesAsync(Files)).Where(r => r.Item2).Select(r => r.Item1).ToListAsync());
        }

        foreach (var entry in entries)
        {
            CancellationToken.ThrowIfCancellationRequested();

            await fileMarker.RemoveMarkAsNewAsync(entry, ((IAccount)(_principal ?? CustomSynchronizationContext.CurrentContext.CurrentPrincipal).Identity).ID);

            if (entry.FileEntryType == FileEntryType.File)
            {
                ProcessedFile(((File<T>)entry).Id);
                await filesMessageService.SendAsync(MessageAction.FileMarkedAsRead, entry, _headers, entry.Title);
            }
            else
            {
                ProcessedFolder(((Folder<T>)entry).Id);
                await filesMessageService.SendAsync(MessageAction.FolderMarkedAsRead, entry, _headers, entry.Title);
            }

            ProgressStep();
        }


        var rootIds = new List<int>
            {
                await globalFolder.GetFolderMyAsync(daoFactory),
                await globalFolder.GetFolderCommonAsync(daoFactory),
                await globalFolder.GetFolderShareAsync(daoFactory),
                await globalFolder.GetFolderProjectsAsync(daoFactory),
                await globalFolder.GetFolderVirtualRoomsAsync(daoFactory)
            };

        if (await PrivacyRoomSettings.GetEnabledAsync(settingsManager))
        {
            rootIds.Add(await globalFolder.GetFolderPrivacyAsync(daoFactory));
        }

        var newrootfolder = new List<string>();

        foreach (var r in rootIds.Where(id => id != 0))
        {
            var item = new KeyValuePair<int, int>(r, await fileMarker.GetRootFoldersIdMarkedAsNewAsync(r));
            newrootfolder.Add($"new_{{\"key\"? \"{item.Key}\", \"value\"? \"{item.Value}\"}}");
        }

        this[Res] += string.Join(SplitChar, newrootfolder.ToArray());
    }
}

[Scope]
public record FileMarkAsReadOperationScope(
    FileMarker FileMarker,
    GlobalFolder GlobalFolder,
    IDaoFactory DaoFactory,
    SettingsManager SettingsManager);

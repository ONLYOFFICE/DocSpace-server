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

using System.Threading.Channels;
using ASC.Core.Billing;

namespace ASC.Web.Files.Utils;

public class SocketManager(
    ITariffService tariffService,
    TenantManager tenantManager,
    ChannelWriter<SocketData> channelWriter,
        MachinePseudoKeys machinePseudoKeys,
        IConfiguration configuration,
        FileDtoHelper filesWrapperHelper,
        FolderDtoHelper folderDtoHelper,
        FileSecurity fileSecurity,
        UserManager userManager)
    : SocketServiceClient(tariffService, tenantManager, channelWriter, machinePseudoKeys, configuration)
{
    protected override string Hub => "files";

    public async Task StartEditAsync<T>(T fileId)
    {
        var room = FileRoom(fileId);
        await MakeRequest("start-edit", new { room, fileId });
    }

    public async Task StopEditAsync<T>(T fileId)
    {
        var room = FileRoom(fileId);
        await MakeRequest("stop-edit", new { room, fileId });
    }

    public async Task CreateFileAsync<T>(File<T> file, IEnumerable<Guid> users = null)
    {
        await MakeRequest("create-file", file, true, users);
    }

    public async Task CreateFormAsync<T>(File<T> file, Guid user, bool isOneMember)
    {
        await MakeCreateFormRequest("create-form", file, new List<Guid> { user }, isOneMember);
    }

    public async Task CreateFolderAsync<T>(Folder<T> folder, IEnumerable<Guid> users = null)
    {
        await MakeRequest("create-folder", folder, true, users);
    }

    public async Task UpdateFileAsync<T>(File<T> file)
    {
        await MakeRequest("update-file", file, true);
    }

    public async Task UpdateFolderAsync<T>(Folder<T> folder, IEnumerable<Guid> users = null)
    {
        await MakeRequest("update-folder", folder, true, users: users);
    }

    public async Task DeleteFileAsync<T>(File<T> file, Func<Task> action = null, IEnumerable<Guid> users = null)
    {
        await MakeRequest("delete-file", file, users: users, action: action);
    }

    public async Task DeleteFolder<T>(Folder<T> folder, IEnumerable<Guid> users = null, Func<Task> action = null)
    {
        await MakeRequest("delete-folder", folder, users: users, action: action);
    }

    public async Task ExecMarkAsNewFilesAsync(IEnumerable<Tag> tags)
    {
        var result = new List<object>();
        
        foreach (var g in tags.GroupBy(r => r.EntryId))
        {
            var room = FileRoom(g.Key);
            result.Add(new { room, fileId = g.Key });
        }
        
        await MakeRequest("mark-as-new-file", result);
    }

    public async Task ExecMarkAsNewFoldersAsync(IEnumerable<Tag> tags)
    { 
        var result = new List<object>();
        
        foreach (var g in tags.GroupBy(r => r.EntryId))
        {
            var room = FolderRoom(g.Key);
            result.Add(             
                new {
                    room,
                    folderId = g.Key,
                    userIds = g.Select(r=> new { owner = r.Owner, count = r.Count}).ToList()
                });
        }
        
        await MakeRequest("mark-as-new-folder", result);
    }

    public async Task BackupProgressAsync(double percentage)
    {
        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();
        await MakeRequest("backup-progress", new { tenantId, percentage });
    }

    public async Task RestoreProgressAsync(int tenantId, double percentage)
    {
        await MakeRequest("restore-progress", new { tenantId, percentage });
    }

    private async Task MakeCreateFormRequest<T>(string method, FileEntry<T> entry, IEnumerable<Guid> userIds, bool isOneMember)
    {
        var room = FolderRoom(entry.FolderIdDisplay);
        var data = await Serialize(entry);

        await base.MakeRequest(method, new
        {
            room,
            entry.Id,
            data,
            userIds,
            isOneMember
        });
    }
    private async Task MakeRequest<T>(string method, FileEntry<T> entry, bool withData = false, IEnumerable<Guid> users = null, Func<Task> action = null)
    {
        var room = FolderRoom(entry.FolderIdDisplay);
        var whoCanRead = users ?? await WhoCanRead(entry);

        if (action != null)
        {
            await action();
        }

        var data = "";

        if (withData)
        {
            data = await Serialize(entry);
        }

        foreach (var userIds in whoCanRead.Chunk(1000))
        {
            await base.MakeRequest(method, new
            {
                room,
                entry.Id,
                data,
                userIds
            });
        }
    }

    private string FileRoom<T>(T fileId)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();

        return $"{tenantId}-FILE-{fileId}";
    }

    private string FolderRoom<T>(T folderId)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();

        return $"{tenantId}-DIR-{folderId}";
    }

    private async Task<string> Serialize<T>(FileEntry<T> entry)
    {
        return entry switch
        {
            File<T> file => JsonSerializer.Serialize(await filesWrapperHelper.GetAsync(file), typeof(FileDto<T>), FileEntryDtoContext.Default),
            Folder<T> folder => JsonSerializer.Serialize(await folderDtoHelper.GetAsync(folder), typeof(FolderDto<T>), FileEntryDtoContext.Default),
            _ => string.Empty
        };
    }

    private async Task<List<Guid>> WhoCanRead<T>(FileEntry<T> entry)
    {
        var whoCanReadTask = fileSecurity.WhoCanReadAsync(entry, true);
        var adminsTask = Admins();

        var whoCanRead = await Task.WhenAll(whoCanReadTask, adminsTask);
        
        var userIds = whoCanRead
            .SelectMany(r => r)
            .Concat([entry.CreateBy])
            .Distinct()
            .ToList();

        return userIds;
    }
    
    private List<Guid> _admins;
    private Task<IEnumerable<Guid>> Admins()
    {
        return _admins != null 
            ? Task.FromResult<IEnumerable<Guid>>(_admins) 
            : AdminsFromDb();
    }
    
    private async Task<IEnumerable<Guid>> AdminsFromDb()
    {
        _admins = (await userManager.GetUsersByGroupAsync(Constants.GroupAdmin.ID))
            .Select(x => x.Id)
            .ToList();
        
        _admins.Add((_tenantManager.GetCurrentTenant()).OwnerId);

        return _admins;
    }
}

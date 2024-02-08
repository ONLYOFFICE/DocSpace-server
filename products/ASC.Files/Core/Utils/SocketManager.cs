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

namespace ASC.Web.Files.Utils;

public class SocketManager(ILogger<SocketServiceClient> logger,
        IHttpClientFactory clientFactory,
        MachinePseudoKeys machinePseudoKeys,
        IConfiguration configuration,
        FileDtoHelper filesWrapperHelper,
        TenantManager tenantManager,
        FolderDtoHelper folderDtoHelper,
        FileSecurity fileSecurity,
        UserManager userManager)
    : SocketServiceClient(logger, clientFactory, machinePseudoKeys, configuration)
{
    protected override string Hub => "files";

    public async Task StartEditAsync<T>(T fileId)
    {
        var room = await GetFileRoomAsync(fileId);
        await MakeRequest("start-edit", new { room, fileId });
    }

    public async Task StopEditAsync<T>(T fileId)
    {
        var room = await GetFileRoomAsync(fileId);
        await MakeRequest("stop-edit", new { room, fileId });
    }

    public async Task CreateFileAsync<T>(File<T> file)
    {
        await MakeRequest("create-file", file, true);
    }

    public async Task CreateFolderAsync<T>(Folder<T> folder, IEnumerable<Guid> users = null)
    {
        await MakeRequest("create-folder", folder, true, users);
    }

    public async Task UpdateFileAsync<T>(File<T> file)
    {
        await MakeRequest("update-file", file, true);
    }

    public async Task UpdateFolderAsync<T>(Folder<T> folder)
    {
        await MakeRequest("update-folder", folder, true);
    }

    public async Task DeleteFileAsync<T>(File<T> file, Func<Task> action = null)
    {
        await MakeRequest("delete-file", file, action: action);
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
            var room = await GetFileRoomAsync(g.Key);
            result.Add(new { room, fileId = g.Key });
        }
        
        SendNotAwaitableRequest("mark-as-new-file", result);
    }

    public async Task ExecMarkAsNewFoldersAsync(IEnumerable<Tag> tags)
    { 
        var result = new List<object>();
        
        foreach (var g in tags.GroupBy(r => r.EntryId))
        {
            var room = await GetFolderRoomAsync(g.Key);
            result.Add(             
                new {
                    room,
                    folderId = g.Key,
                    userIds = g.Select(r=> new { owner = r.Owner, count = r.Count}).ToList()
                });
        }
        
        SendNotAwaitableRequest("mark-as-new-folder", result);
    }

    private async Task MakeRequest<T>(string method, FileEntry<T> entry, bool withData = false, IEnumerable<Guid> users = null, Func<Task> action = null)
    {        
        var room = await GetFolderRoomAsync(entry.FolderIdDisplay);
        var whoCanRead = users ?? await GetWhoCanRead(entry);

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
                userIds,
            });
        }
    }

    private async Task<string> GetFileRoomAsync<T>(T fileId)
    {
        var tenantId = await tenantManager.GetCurrentTenantIdAsync();

        return $"{tenantId}-FILE-{fileId}";
    }

    private async Task<string> GetFolderRoomAsync<T>(T folderId)
    {
        var tenantId = await tenantManager.GetCurrentTenantIdAsync();

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

    private async Task<IEnumerable<Guid>> GetWhoCanRead<T>(FileEntry<T> entry)
    {
        var whoCanRead = await fileSecurity.WhoCanReadAsync(entry);
        var userIds = whoCanRead
            .Concat(await GetAdmins())
            .Concat(new []{ entry.CreateBy })
            .Distinct()
            .ToList();

        return userIds;
    }
    
    private List<Guid> _admins;
    private async Task<IEnumerable<Guid>> GetAdmins()
    {
        if (_admins != null)
        {
            return _admins;
        }

        _admins = await userManager.GetUsers(true, EmployeeStatus.Active, null, null, null, null, null, null, null, true, 0, 0)
            .Select(r=> r.Id)
            .ToListAsync();
        
        _admins.Add((await tenantManager.GetCurrentTenantAsync()).OwnerId);

        return _admins;
}
    }

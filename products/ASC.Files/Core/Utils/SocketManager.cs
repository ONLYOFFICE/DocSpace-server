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

using System.Threading.Channels;

namespace ASC.Web.Files.Utils;

public class SocketManager(
    ITariffService tariffService,
    TenantManager tenantManager,
    ChannelWriter<SocketData> channelWriter,
    MachinePseudoKeys machinePseudoKeys,
    IConfiguration configuration,
    ExternalShare externalShare,
    FileSecurity fileSecurity,
    UserManager userManager,
    IDaoFactory daoFactory,
    FileSharing fileSharing,
    GlobalFolderHelper globalFolderHelper)
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
        if (users == null && file.IsForm)
        {
            users = await GetRecipientListForForm(file);
        }
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
                new
                {
                    room,
                    folderId = g.Key,
                    userIds = g.Select(r => new { owner = r.Owner, count = r.Count }).DistinctBy(r => r.owner).ToList()
                });
        }

        await MakeRequest("mark-as-new-folder", result);
    }

    public async Task BackupProgressAsync(int percentage, bool dump)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();
        await MakeRequest("backup-progress", new { tenantId, dump, percentage });
    }

    public async Task EndBackupAsync<T>(T result, bool dump)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();
        await MakeRequest("end-backup", new { tenantId, dump, result });
    }

    public async Task RestoreProgressAsync(int tenantId, bool dump, int percentage)
    {
        await MakeRequest("restore-progress", new { tenantId, dump, percentage });
    }

    public async Task EndRestoreAsync<T>(int tenantId, bool dump, T result)
    {
        await MakeRequest("end-restore", new { tenantId, dump, result });
    }

    public async Task AddToRecentAsync<T>(FileEntry<T> fileEntry, IEnumerable<Guid> users = null)
    {
        await MakeRequest($"add-recent-{fileEntry.FileEntryType.ToStringLowerFast()}", fileEntry, true, users, folderIdDisplay: await globalFolderHelper.GetFolderRecentAsync<T>());
    }

    public async Task RemoveFromRecentAsync<T>(FileEntry<T> fileEntry, IEnumerable<Guid> users = null)
    {
        await MakeRequest($"delete-recent-{fileEntry.FileEntryType.ToStringLowerFast()}", fileEntry, true, users, folderIdDisplay: await globalFolderHelper.GetFolderRecentAsync<T>());
    }

    public async Task AddToFavoritesAsync<T>(FileEntry<T> fileEntry, IEnumerable<Guid> users = null)
    {
        await MakeRequest($"add-favorites-{fileEntry.FileEntryType.ToStringLowerFast()}", fileEntry, true, users, folderIdDisplay: await globalFolderHelper.GetFolderFavoritesAsync<T>());
    }

    public async Task RemoveFromFavoritesAsync<T>(FileEntry<T> fileEntry, IEnumerable<Guid> users = null)
    {
        await MakeRequest($"delete-favorites-{fileEntry.FileEntryType.ToStringLowerFast()}", fileEntry, true, users, folderIdDisplay: await globalFolderHelper.GetFolderFavoritesAsync<T>());
    }

    public async Task AddToSharedAsync<T>(FileEntry<T> fileEntry, IEnumerable<Guid> users = null)
    {
        await MakeRequest($"add-shared-{fileEntry.FileEntryType.ToStringLowerFast()}", fileEntry, true, users, folderIdDisplay: await globalFolderHelper.GetFolderShareAsync<T>());
    }

    public async Task RemoveFromSharedAsync<T>(FileEntry<T> fileEntry, IEnumerable<Guid> users = null)
    {
        await MakeRequest($"delete-shared-{fileEntry.FileEntryType.ToStringLowerFast()}", fileEntry, true, users, folderIdDisplay: await globalFolderHelper.GetFolderShareAsync<T>());
    }

    public async Task SelfRestrictionAsync<T>(FileEntry<T> fileEntry, Guid subject, FileShare access)
    {
        var room = fileEntry.FileEntryType == FileEntryType.File ? FileRoom(fileEntry.Id) : FolderRoom(fileEntry.Id);
        var data = JsonSerializer.Serialize(new Dictionary<Guid, FileShare> { { subject, access } });

        await base.MakeRequest($"self-restriction-{fileEntry.FileEntryType.ToStringLowerFast()}", new
        {
            room,
            fileEntry.Id,
            data
        });
    }

    public async Task UpdateChatAsync<T>(Folder<T> folder, Guid chatId, string chatTitle, Guid userId)
    {
        var room = FolderRoom(folder.Id);
        
        await base.MakeRequest("update-chat", new { room, chatId, chatTitle, userId });
    }
    
    public async Task CommitMessageAsync(Guid chatId, int messageId)
    {
        var room = ChatRoom(chatId);
        await MakeRequest("commit-chat-message", new { room, messageId });
    }
    
    private async Task<IEnumerable<Guid>> GetRecipientListForForm<T>(File<T> form)
    {
        List<Guid> users = null;

        var folderDao = daoFactory.GetFolderDao<T>();
        var room = await folderDao.GetFirstParentTypeFromFileEntryAsync(form);
        if (room is { FolderType: FolderType.VirtualDataRoom })
        {
            var aces = await fileSharing.GetSharedInfoAsync(room);
            users = aces.Where(ace => ace.Access != FileShare.FillForms)
            .Select(ace => ace.Id).ToList();
        }

        return users;
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
    private async Task MakeRequest<T>(string method, FileEntry<T> entry, bool withData = false, IEnumerable<Guid> users = null, Func<Task> action = null, T folderIdDisplay = default)
    {
        if (Equals(folderIdDisplay, default(T)))
        {
            folderIdDisplay = entry.FolderIdDisplay;
        }

        var room = FolderRoom(folderIdDisplay);

        IEnumerable<Guid> sharedUsers = null;

        if (users == null)
        {
            (users, sharedUsers) = await WhoCanRead(entry);
        }

        if (action != null)
        {
            await action();
        }

        var parentId = entry.ParentId;
        switch (method)
        {
            case "add-recent-file":
            case "add-favorites-file":
            case "add-shared-file":
                method = "create-file";
                entry.ParentId = folderIdDisplay;
                break;
            case "delete-recent-file":
            case "delete-favorites-file":
            case "delete-shared-file":
                method = "delete-file";
                entry.ParentId = folderIdDisplay;
                break;
            case "add-favorites-folder":
            case "add-shared-folder":
                method = "create-folder";
                entry.ParentId = folderIdDisplay;
                break;
            case "delete-favorites-folder":
            case "delete-shared-folder":
                method = "delete-folder";
                entry.ParentId = folderIdDisplay;
                break;
        }

        var data = "";

        if (withData)
        {
            data = await Serialize(entry);
        }

        foreach (var userIds in users.Chunk(1000))
        {
            await base.MakeRequest(method, new
            {
                room,
                entry.Id,
                data,
                userIds
            });
        }

        if (sharedUsers != null)
        {
            var sharedFolder = await globalFolderHelper.GetFolderShareAsync<T>();

            if (!EqualityComparer<T>.Default.Equals(folderIdDisplay, sharedFolder))
            {
                room = FolderRoom(sharedFolder);

                if (withData)
                {
                    entry.ParentId = sharedFolder;
                    data = await Serialize(entry);
                }

                foreach (var userIds in sharedUsers.Chunk(1000))
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
        }

        entry.ParentId = parentId;
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
    
    private string ChatRoom(Guid chatId)
    {
        return $"{_tenantManager.GetCurrentTenantId()}-CHAT-{chatId}";
    }

    private async Task<string> Serialize<T>(FileEntry<T> entry)
    {
        var externalMediaAccess = entry.ShareRecord is { SubjectType: SubjectType.PrimaryExternalLink or SubjectType.ExternalLink };
        string requestToken = null;

        if (externalMediaAccess)
        {
            requestToken = await externalShare.CreateShareKeyAsync(entry.ShareRecord.Subject);
        }

        return entry switch
        {
            File<T> file => JsonSerializer.Serialize(new FileDto<T>
            {
                Id = file.Id,
                FolderId = file.ParentId,
                Title = file.Title,
                Version = file.Version,
                VersionGroup = file.VersionGroup,
                RequestToken = requestToken
            }, _jsonSerializerOptions),
            Folder<T> folder => JsonSerializer.Serialize(new FolderDto<T>
            {
                Id = folder.Id,
                ParentId = folder.ParentId,
                Title = folder.Title,
                RoomType = DocSpaceHelper.MapToRoomType(folder.FolderType),
                CreatedBy = new EmployeeDto
                {
                    Id = folder.CreateBy
                },
                RequestToken = requestToken
            }, _jsonSerializerOptions),
            _ => string.Empty
        };
    }

    private async Task<(IEnumerable<Guid> directAccess, IEnumerable<Guid> sharedAccess)> WhoCanRead<T>(FileEntry<T> entry)
    {
        var (direct, shared) = await fileSecurity.WhoCanReadSeparatelyAsync(entry);

        if (entry.RootFolderType is FolderType.VirtualRooms or FolderType.Archive)
        {
            var admins = await Admins();
            direct = direct.Concat(admins);
        }

        direct = direct.Concat([entry.CreateBy]).Distinct();

        shared = shared.Where(x => !direct.Contains(x));

        return (direct, shared);
    }

    private List<Guid> _admins;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
    };

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
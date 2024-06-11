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

namespace ASC.Web.Files.Helpers;

[Scope]
public class FilesMessageService(
    ILogger<FilesMessageService> logger,
    MessageService messageService,
    IHttpContextAccessor httpContextAccessor,
    IDaoFactory daoFactory)
{
    private static readonly JsonSerializerOptions _serializerOptions = new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault };

    public async Task SendAsync(MessageAction action, params string[] description)
    {
        await messageService.SendHeadersMessageAsync(action, description);
    }

    public async Task SendAsync<T>(MessageAction action, FileEntry<T> entry, IDictionary<string, StringValues> headers, params string[] description)
    {
        await SendAsync(action, entry, headers, null, Guid.Empty, FileShare.None, description);
    }

    public async Task SendAsync<T>(MessageAction action, FileEntry<T> entry, params string[] description)
    {
        await SendAsync(action, entry, null, Guid.Empty, FileShare.None, description);
    }

    public async Task SendAsync<T>(MessageAction action, string oldTitle, FileEntry<T> entry, params string[] description)
    {
        await SendAsync(action, entry, oldTitle, description: description);
    }

    public async Task SendAsync<T>(MessageAction action, FileEntry<T> entry, Guid userId, params string[] description)
    {
        await SendAsync(action, entry, null, userId, FileShare.None, description);
    }

    public async Task SendAsync<T>(MessageAction action, FileEntry<T> entry, Guid userId, FileShare currentRole, FileShare? oldRole = null , bool useRoomFormat = false, params string[] description)
    {
        var desc = description.Append(FileShareExtensions.GetAccessString(currentRole, useRoomFormat));

        if (oldRole.HasValue)
        {
            desc = desc.Append(FileShareExtensions.GetAccessString(oldRole.Value, useRoomFormat));
        }
        
        await SendAsync(action, entry, null, userId, currentRole, desc.ToArray());
    }

    private async Task SendAsync<T>(MessageAction action, FileEntry<T> entry, IDictionary<string, StringValues> headers, string oldTitle = null, Guid userId = default, 
        FileShare userRole = FileShare.None, params string[] description)
    {
        if (entry == null)
        {
            return;
        }

        var additionalParam = await GetAdditionalEntryDataAsync(entry, action, oldTitle, userId, userRole);
        description = Append(description, additionalParam.DescriptionPart);

        if (headers == null)//todo check need if
        {
            logger.DebugEmptyRequestHeaders(action);

            return;
        }

        await messageService.SendHeadersMessageAsync(action, MessageTarget.Create(entry.Id), headers, description, additionalParam.References);
    }

    private async Task SendAsync<T>(MessageAction action, FileEntry<T> entry, string oldTitle = null, Guid userId = default, FileShare userRole = FileShare.None, params string[] description)
    {
        if (entry == null)
        {
            return;
        }

        var additionalParam = await GetAdditionalEntryDataAsync(entry, action, oldTitle, userId, userRole);
        description = Append(description, additionalParam.DescriptionPart);

        await messageService.SendHeadersMessageAsync(action, MessageTarget.Create(entry.Id), null, description, additionalParam.References);
    }

    public async Task SendAsync<T1, T2>(MessageAction action, FileEntry<T1> entry1, FileEntry<T2> entry2, IDictionary<string, StringValues> headers, params string[] description)
    {
        if (entry1 == null || entry2 == null)
        {
            return;
        }

        var additionalParams = await GetAdditionalEntryDataAsync(entry1, action);
        description = Append(description, additionalParams.DescriptionPart);

        if (headers == null)//todo check need if
        {
            logger.DebugEmptyRequestHeaders(action);

            return;
        }

        await messageService.SendHeadersMessageAsync(action, MessageTarget.Create([entry1.Id.ToString(), entry2.Id.ToString()]), headers, description, additionalParams.References);
    }

    public async Task SendAsync<T>(MessageAction action, FileEntry<T> entry, string description)
    {
        if (entry == null)
        {
            return;
        }

        if (httpContextAccessor == null)
        {
            logger.DebugEmptyHttpRequest(action);

            return;
        }

        var additionalParam = await GetAdditionalEntryDataAsync(entry, action);
        
        await messageService.SendAsync(action, MessageTarget.Create(entry.Id), description, additionalParam.DescriptionPart, additionalParam.References);
    }

    public async Task SendAsync<T>(MessageAction action, FileEntry<T> entry, string d1, string d2)
    {
        if (entry == null)
        {
            return;
        }
        
        var additionalParam = await GetAdditionalEntryDataAsync(entry, action);

        if (httpContextAccessor == null)
        {
            logger.DebugEmptyHttpRequest(action);
            return;
        }

        await messageService.SendAsync(action, MessageTarget.Create(entry.Id), [d1, d2, additionalParam.DescriptionPart], additionalParam.References);
    }

    public async Task SendAsync<T>(MessageAction action, FileEntry<T> entry, MessageInitiator initiator, params string[] description)
    {
        if (entry == null)
        {
            return;
        }

        var additionalParam = await GetAdditionalEntryDataAsync(entry, action);
        description = Append(description, additionalParam.DescriptionPart);

        await messageService.SendAsync(initiator, action, MessageTarget.Create(entry.Id), additionalParam.References, description);
    }

    private async Task<FileEntryData> GetAdditionalEntryDataAsync<T>(FileEntry<T> entry, MessageAction action, string oldTitle = null, Guid userid = default,
        FileShare userRole = FileShare.None)
    { 
        return entry switch
        {
            FileEntry<int> entryInt => await GetAdditionalEntryDataAsync(entryInt, action, oldTitle, userid, userRole),
            FileEntry<string> entryString => await GetAdditionalEntryDataAsync(entryString, action, oldTitle, userid, userRole),
            _ => throw new NotSupportedException()
        };
    }

    private async Task<FileEntryData> GetAdditionalEntryDataAsync(FileEntry<int> entry, MessageAction action, string oldTitle = null, Guid userid = default, 
        FileShare userRole = FileShare.None)
    {
        var folderDao = daoFactory.GetFolderDao<int>();

        var parents = await folderDao.GetParentFoldersAsync(entry.ParentId).Where(x => !x.IsRoot).ToListAsync();
        
        var room = entry is Folder<int> folder && DocSpaceHelper.IsRoom(folder.FolderType) 
            ? folder 
            : parents.FirstOrDefault(x => DocSpaceHelper.IsRoom(x.FolderType));

        var desc = GetEventDescription(entry, action, oldTitle, userid, userRole, room?.Id ?? -1, room?.Title);

        if (!HistoryService.TrackedActions.Contains(action))
        {
            return new FileEntryData(JsonSerializer.Serialize(desc, _serializerOptions), null);
        }

        var references = parents.Select(x => new FilesAuditReference
        {
            EntryId = x.Id,
            EntryType = (byte)x.FileEntryType
        }).ToList();

        if (action is not (MessageAction.FileDeleted or MessageAction.FolderDeleted or MessageAction.RoomDeleted))
        {
            references.Add(new FilesAuditReference
            {
                EntryId = entry.Id,
                EntryType = (byte)entry.FileEntryType
            });
        }
        
        var parent = parents.LastOrDefault();
        if (parent != null)
        {
            desc.ParentId = parent.Id;
            desc.ParentTitle = parent.Title;
        }
        
        return new FileEntryData(JsonSerializer.Serialize(desc, _serializerOptions), references);
    }
    
    private async Task<FileEntryData> GetAdditionalEntryDataAsync(FileEntry<string> entry, MessageAction action, string oldTitle = null, Guid userid = default, 
        FileShare userRole = FileShare.None)
    {
        var folderDao = daoFactory.GetFolderDao<string>();

        var (roomId, roomTitle) = await folderDao.GetParentRoomInfoFromFileEntryAsync(entry);

        var desc = GetEventDescription(entry, action, oldTitle, userid, userRole, roomId, roomTitle);
        var json = JsonSerializer.Serialize(desc, _serializerOptions);
        
        return new FileEntryData(json, null);
    }

    private static EventDescription<T> GetEventDescription<T>(FileEntry<T> entry, MessageAction action, string oldTitle, Guid userid, FileShare userRole, T roomId, string roomTitle)
    {
        var desc = new EventDescription<T>
        {
            RoomId = roomId,
            RoomTitle = roomTitle
        };

        switch (action)
        {
            case MessageAction.RoomRenamed when !string.IsNullOrEmpty(oldTitle):
                desc.RoomOldTitle = oldTitle;
                break;
            case MessageAction.RoomCreateUser or MessageAction.RoomRemoveUser when userid != Guid.Empty:
                desc.UserIds = [userid];
                break;
            case MessageAction.RoomUpdateAccessForUser when (userRole != FileShare.None) && userid != Guid.Empty:
                desc.UserIds = [userid];
                desc.UserRole = (int)userRole;
                break;
        }

        desc.RootFolderTitle = entry.RootFolderType switch
        {
            FolderType.USER => FilesUCResource.MyFiles,
            FolderType.TRASH => FilesUCResource.Trash,
            _ => null
        };

        return desc;
    }
    
    private static string[] Append(string[] description, string value)
    {
        var newArray = new string[description.Length + 1];
        Array.Copy(description, newArray, description.Length);
        newArray[^1] = value;
        
        return newArray;
    }
    
    private record struct FileEntryData(string DescriptionPart, IEnumerable<FilesAuditReference> References);
}
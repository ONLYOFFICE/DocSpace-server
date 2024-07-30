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
    private static readonly HashSet<MessageAction> _moveCopyActions =
    [
        MessageAction.FolderMoved,
        MessageAction.FolderMovedWithOverwriting,
        MessageAction.FolderCopied,
        MessageAction.FolderCopiedWithOverwriting,
        MessageAction.FileMoved,
        MessageAction.FileMovedWithOverwriting,
        MessageAction.FileCopied,
        MessageAction.FileCopiedWithOverwriting
    ];

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

    public async Task SendAsync(MessageAction action, string d1, IEnumerable<string> d2)
    {
        await SendAsync(action, description: [d1, string.Join(", ", d2)]);
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

    public async Task SendCopyMessageAsync<T1, T2>(FileEntry<T2> target, Folder<T1> from, Folder<T2> to, List<Folder<T2>> toParents, bool overwrite,
        IDictionary<string, StringValues> headers, string[] description)
    {
        var action = target switch
        {
            Folder<int> => overwrite ? MessageAction.FolderCopiedWithOverwriting : MessageAction.FolderCopied,
            File<int> => overwrite ? MessageAction.FileCopiedWithOverwriting : MessageAction.FileCopied,
            _ => MessageAction.None
        };
        
        if (target is FileEntry<int> targetInt && from is Folder<int> fromInt && to is Folder<int> toInt && toParents is List<Folder<int>> toParentsInt)
        {
            await SendMoveOrCopyMessageAsync(action, targetInt, fromInt, toInt, toParentsInt, headers, description);
        }
        else
        {
            await SendAsync(action, target, to, headers, description);
        }
    }
    
    public async Task SendMoveMessageAsync<T1, T2>(FileEntry<T1> target, Folder<T1> from, Folder<T2> to, List<Folder<T2>> toParents, bool overwrite,
        IDictionary<string, StringValues> headers, string[] description)
    {
        var action = target switch
        {
            Folder<int> => overwrite ? MessageAction.FolderMovedWithOverwriting : MessageAction.FolderMoved,
            File<int> => overwrite ? MessageAction.FileMovedWithOverwriting : MessageAction.FileMoved,
            _ => MessageAction.None
        };
        
        if (target is FileEntry<int> targetInt && from is Folder<int> fromInt && to is Folder<int> toInt && toParents is List<Folder<int>> toParentsInt)
        {
            await SendMoveOrCopyMessageAsync(action, targetInt, fromInt, toInt, toParentsInt, headers, description);
        }
        else
        {
            await SendAsync(action, target, to, headers, description);
        }
    }

    private async Task SendMoveOrCopyMessageAsync(MessageAction action, FileEntry<int> target, Folder<int> from, Folder<int> to, List<Folder<int>> toParents,
        IDictionary<string, StringValues> headers, string[] description)
    {
        if (!_moveCopyActions.Contains(action))
        {
            throw new ArgumentException(null, nameof(action));
        }
        
        var folderDao = daoFactory.GetFolderDao<int>();
        var fromParents = await folderDao.GetParentFoldersAsync(from.Id).ToListAsync();
        
        var rootFolderTitle = GetRootFolderTitle(target.RootFolderType);
        
        var eventDescriptionTo = new EventDescription<int>
        {
            ParentId = to.Id,
            ParentTitle = to.Title,
            ParentType = (int)to.FolderType,
            CreateBy = target.CreateBy,
            RootFolderTitle = rootFolderTitle
        };
        
        var eventDescriptionFrom = new EventDescription<int>
        {
            ParentId = to.Id,
            ParentTitle = to.Title,
            ParentType = (int)to.FolderType,
            CreateBy = target.CreateBy,
            RootFolderTitle = rootFolderTitle
        };
        
        var crossEvent = true;

        if (from.RootFolderType == FolderType.VirtualRooms && to.RootFolderType == FolderType.VirtualRooms)
        {
            var toRoom = FindRoom(to, toParents);
            var fromRoom = FindRoom(from, fromParents);
            
            eventDescriptionTo.RoomId = toRoom.Id;
            eventDescriptionTo.RoomTitle = toRoom.Title;
            eventDescriptionFrom.RoomId = fromRoom.Id;
            eventDescriptionFrom.RoomTitle = fromRoom.Title;
            
            if (fromRoom.Id == toRoom.Id)
            {
                eventDescriptionTo.FromParentTitle = from.Title;
                eventDescriptionTo.FromParentType = (int)from.FolderType;
                eventDescriptionTo.FromFolderId = from.Id;
                crossEvent = false;
            }
        }
        else if (from.RootFolderType == FolderType.USER && to.RootFolderType == FolderType.USER && to.CreateBy == from.CreateBy)
        {
            eventDescriptionTo.FromParentTitle = from.Title;
            eventDescriptionTo.FromParentType = (int)from.FolderType;
            eventDescriptionTo.FromFolderId = from.Id;
            crossEvent = false;
        }

        if (!crossEvent)
        {
            var references = GetReferences(fromParents);
            foreach (var toRef in GetReferences(toParents).Where(toRef => !references.Any(r => r.EntryId == toRef.EntryId && r.EntryType == toRef.EntryType)))
            {
                references.Add(toRef);
            }
            
            references.Add(new FilesAuditReference { EntryId = target.Id, EntryType = (byte)target.FileEntryType });
            
            var json = JsonSerializer.Serialize(eventDescriptionTo, _serializerOptions);
            await messageService.SendHeadersMessageAsync(action, MessageTarget.Create([target.Id, to.Id]), headers, Append(description, json), references);
            
            return;
        }

        eventDescriptionFrom.ParentTitle = null;
        eventDescriptionFrom.ParentType = 0;

        var toReferences = GetReferences(toParents);
        var fromReferences = GetReferences(fromParents);
        
        toReferences.Add(new FilesAuditReference { EntryId = target.Id, EntryType = (byte)target.FileEntryType });
        
        var jsonTo = JsonSerializer.Serialize(eventDescriptionTo, _serializerOptions);
        var t1= messageService.SendHeadersMessageAsync(action, MessageTarget.Create([target.Id, to.Id]), headers, Append(description, jsonTo), toReferences);
        
        var jsonFrom = JsonSerializer.Serialize(eventDescriptionFrom, _serializerOptions);
        var t2= messageService.SendHeadersMessageAsync(action, MessageTarget.Create([target.Id, to.Id]), headers, Append(description, jsonFrom), fromReferences);
        
        await Task.WhenAll(t1, t2);
    }

    private static List<FilesAuditReference> GetReferences(List<Folder<int>> parents)
    {
        return parents.Where(x => !x.IsRoot).Select(x => 
            new FilesAuditReference 
            { 
                EntryId = x.Id, 
                EntryType = (byte)x.FileEntryType 
            }).ToList();
    }

    public async Task SendAsync<T1, T2>(MessageAction action, FileEntry<T1> entry1, FileEntry<T2> entry2, IDictionary<string, StringValues> headers, params string[] description)
    {
        if (entry1 == null || entry2 == null)
        {
            return;
        }

        FolderType? parentType = entry2 is Folder<T2> folder 
            ? folder.FolderType 
            : null;

        var additionalParams = await GetAdditionalEntryDataAsync(entry1, action, parentType: parentType);
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
        FileShare userRole = FileShare.None, FolderType? parentType = null)
    { 
        return entry switch
        {
            FileEntry<int> entryInt => await GetAdditionalEntryDataAsync(entryInt, action, oldTitle, userid, userRole, parentType),
            FileEntry<string> entryString => await GetAdditionalEntryDataAsync(entryString, action, oldTitle, userid, userRole),
            _ => throw new NotSupportedException()
        };
    }

    private async Task<FileEntryData> GetAdditionalEntryDataAsync(FileEntry<int> entry, MessageAction action, string oldTitle = null, Guid userid = default, 
        FileShare userRole = FileShare.None, FolderType? parentType = null)
    {
        var folderDao = daoFactory.GetFolderDao<int>();

        var parents = await folderDao.GetParentFoldersAsync(entry.ParentId).ToListAsync();
        
        var room = entry is Folder<int> folder && DocSpaceHelper.IsRoom(folder.FolderType) 
            ? folder 
            : parents.FirstOrDefault(x => DocSpaceHelper.IsRoom(x.FolderType));

        var desc = GetEventDescription(action, oldTitle, userid, userRole, room?.Id ?? -1, room?.Title, entry.CreateBy);

        if (!HistoryService.TrackedActions.Contains(action))
        {
            return new FileEntryData(JsonSerializer.Serialize(desc, _serializerOptions), null);
        }

        var references = parents.Where(x => !x.IsRoot).Select(x => 
            new FilesAuditReference 
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
        if (parent == null)
        {
            return new FileEntryData(JsonSerializer.Serialize(desc, _serializerOptions), references);
        }

        desc.ParentId = parent.Id;

        if (!_moveCopyActions.Contains(action))
        {
            desc.ParentTitle = parent.Title;
        }
        
        desc.RootFolderTitle = entry.RootFolderType switch
        {
            FolderType.USER => FilesUCResource.MyFiles,
            FolderType.TRASH => FilesUCResource.Trash,
            _ => null
        };
        
        desc.ParentType = parentType.HasValue 
            ? (int)parentType.Value 
            : (int)parent.FolderType;

        return new FileEntryData(JsonSerializer.Serialize(desc, _serializerOptions), references);
    }
    
    private async Task<FileEntryData> GetAdditionalEntryDataAsync(FileEntry<string> entry, MessageAction action, string oldTitle = null, Guid userid = default, 
        FileShare userRole = FileShare.None)
    {
        var folderDao = daoFactory.GetFolderDao<string>();

        var (roomId, roomTitle) = await folderDao.GetParentRoomInfoFromFileEntryAsync(entry);

        var desc = GetEventDescription(action, oldTitle, userid, userRole, roomId, roomTitle);
        var json = JsonSerializer.Serialize(desc, _serializerOptions);
        
        return new FileEntryData(json, null);
    }

    private static EventDescription<T> GetEventDescription<T>(MessageAction action, string oldTitle, Guid userid, FileShare userRole, T roomId, string roomTitle, Guid? createBy = null)
    {
        var desc = new EventDescription<T>
        {
            RoomId = roomId,
            RoomTitle = roomTitle,
            CreateBy = createBy
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

        return desc;
    }
    
    private static string[] Append(string[] description, string value)
    {
        var newArray = new string[description.Length + 1];
        Array.Copy(description, newArray, description.Length);
        newArray[^1] = value;
        
        return newArray;
    }

    private static string GetRootFolderTitle(FolderType folderType)
    {
        return folderType switch
        {
            FolderType.USER => FilesUCResource.MyFiles,
            FolderType.TRASH => FilesUCResource.Trash,
            _ => null
        };
    }
    
    private static Folder<T> FindRoom<T>(Folder<T> folder, List<Folder<T>> parents)
    {
        return DocSpaceHelper.IsRoom(folder.FolderType) 
            ? folder 
            : parents.First(x => DocSpaceHelper.IsRoom(x.FolderType));
    }
    
    private record FileEntryData(string DescriptionPart, IEnumerable<FilesAuditReference> References);
}
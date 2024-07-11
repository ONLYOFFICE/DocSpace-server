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

namespace ASC.Files.Core.Core.History;

[Scope]
public class HistoryStore(
    IDbContextFactory<FilesDbContext> filesDbContextFactory,
    TenantManager tenantManager, 
    AuditInterpreter interpreter)
{
    public static HashSet<MessageAction> TrackedActions => [
        MessageAction.FileCreated, 
        MessageAction.FileUploaded,
        MessageAction.FileUploadedWithOverwriting,
        MessageAction.UserFileUpdated, 
        MessageAction.FileRenamed, 
        MessageAction.FileMoved, 
        MessageAction.FileMovedWithOverwriting, 
        MessageAction.FileMovedToTrash, 
        MessageAction.FileCopied, 
        MessageAction.FileCopiedWithOverwriting, 
        MessageAction.FileDeleted, 
        MessageAction.FileConverted, 
        MessageAction.FileRestoreVersion, 
        MessageAction.FolderCreated,
        MessageAction.FolderRenamed,
        MessageAction.FolderMoved,
        MessageAction.FolderMovedWithOverwriting,
        MessageAction.FolderCopied,
        MessageAction.FolderCopiedWithOverwriting,
        MessageAction.FolderMovedToTrash,
        MessageAction.FolderDeleted,
        MessageAction.RoomCreateUser,
        MessageAction.RoomUpdateAccessForUser,
        MessageAction.RoomRemoveUser,
        MessageAction.RoomGroupAdded,
        MessageAction.RoomUpdateAccessForGroup,
        MessageAction.RoomGroupRemove,
        MessageAction.RoomCreated,
        MessageAction.RoomRenamed,
        MessageAction.AddedRoomTags,
        MessageAction.DeletedRoomTags,
        MessageAction.RoomLogoCreated,
        MessageAction.RoomLogoDeleted,
        MessageAction.RoomExternalLinkCreated,
        MessageAction.RoomExternalLinkRenamed,
        MessageAction.RoomExternalLinkDeleted,
        MessageAction.RoomExternalLinkRevoked
    ];
    
    private static readonly HashSet<int?> _stealthSensitiveActions =
    [
        (int)MessageAction.FileCreated,
        (int)MessageAction.FileUploaded,
        (int)MessageAction.FileUploadedWithOverwriting,
        (int)MessageAction.UserFileUpdated, 
        (int)MessageAction.FileRenamed, 
        (int)MessageAction.FileMoved, 
        (int)MessageAction.FileMovedWithOverwriting, 
        (int)MessageAction.FileMovedToTrash, 
        (int)MessageAction.FileCopied, 
        (int)MessageAction.FileCopiedWithOverwriting, 
        (int)MessageAction.FileDeleted, 
        (int)MessageAction.FileConverted, 
        (int)MessageAction.FileRestoreVersion
    ];
    
    public async IAsyncEnumerable<HistoryEntry> GetHistoryAsync(FileEntry<int> entry, bool excludeOtherUsersFilesEvents, Guid userId, int offset, int count)
    {
        var tenantId = await tenantManager.GetCurrentTenantIdAsync();
        var filesDbContext = await filesDbContextFactory.CreateDbContextAsync();

        var events = excludeOtherUsersFilesEvents 
            ? filesDbContext.GetUserHistoryEventsAsync(tenantId, entry.Id, entry.ParentId, _stealthSensitiveActions, userId, offset, count) 
            : filesDbContext.GetHistoryEventsAsync(tenantId, entry.Id, (byte)entry.FileEntryType, offset, count);

        await foreach (var hEntry in events.SelectAwait(interpreter.ToHistoryAsync))
        {
            yield return hEntry;
        }
    }

    public async Task<int> GetHistoryTotalCountAsync(FileEntry<int> entry, bool onlyUserFilesEvents, Guid userId)
    {
        var tenantId = await tenantManager.GetCurrentTenantIdAsync();
        var filesDbContext = await filesDbContextFactory.CreateDbContextAsync();

        if (onlyUserFilesEvents)
        {
            return await filesDbContext.GetUserHistoryEventsTotalCountAsync(tenantId, entry.Id, entry.ParentId, _stealthSensitiveActions, userId);
        }
        
        return await filesDbContext.GetHistoryEventsTotalCountAsync(tenantId, entry.Id, (byte)entry.FileEntryType);
    }
}
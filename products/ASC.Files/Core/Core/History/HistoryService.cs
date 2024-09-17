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

using ASC.MessagingSystem.EF.Context;

namespace ASC.Files.Core.Core.History;

[Scope]
public class HistoryService(
    IDbContextFactory<MessagesContext> dbContextFactory, 
    TenantManager tenantManager, 
    IDaoFactory daoFactory, 
    FileSecurity fileSecurity, 
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
        MessageAction.FileIndexChanged,
        MessageAction.FileLocked,
        MessageAction.FileUnlocked,
        MessageAction.FolderCreated,
        MessageAction.FolderRenamed,
        MessageAction.FolderMoved,
        MessageAction.FolderMovedWithOverwriting,
        MessageAction.FolderCopied,
        MessageAction.FolderCopiedWithOverwriting,
        MessageAction.FolderMovedToTrash,
        MessageAction.FolderDeleted,
        MessageAction.FolderIndexChanged,
        MessageAction.FolderIndexReordered,
        MessageAction.RoomCreateUser,
        MessageAction.RoomUpdateAccessForUser,
        MessageAction.RoomRemoveUser,
        MessageAction.RoomGroupAdded,
        MessageAction.RoomUpdateAccessForGroup,
        MessageAction.RoomGroupRemove,
        MessageAction.RoomCreated,
        MessageAction.RoomCopied,
        MessageAction.RoomRenamed,
        MessageAction.AddedRoomTags,
        MessageAction.DeletedRoomTags,
        MessageAction.RoomLogoCreated,
        MessageAction.RoomLogoDeleted,
        MessageAction.RoomExternalLinkCreated,
        MessageAction.RoomExternalLinkRenamed,
        MessageAction.RoomExternalLinkDeleted,
        MessageAction.RoomExternalLinkRevoked,
        MessageAction.RoomIndexingEnabled,
        MessageAction.RoomIndexingDisabled,
        MessageAction.RoomLifeTimeSet,
        MessageAction.RoomLifeTimeDisabled,
        MessageAction.RoomArchived,
        MessageAction.RoomUnarchived,
        MessageAction.RoomDenyDownloadEnabled,
        MessageAction.RoomDenyDownloadDisabled,
        MessageAction.RoomWatermarkSet,
        MessageAction.RoomWatermarkDisabled,
        MessageAction.PrimaryExternalLinkCopied
    ];
    
    public async IAsyncEnumerable<HistoryEntry> GetHistoryAsync(int entryId, FileEntryType entryType, DateTime? fromDate, DateTime? toDate, int offset, int count)
    {
        FileEntry<int> entry = entryType switch
        {
            FileEntryType.File => await daoFactory.GetFileDao<int>().GetFileAsync(entryId),
            FileEntryType.Folder => await daoFactory.GetFolderDao<int>().GetFolderAsync(entryId),
            _ => throw new ArgumentOutOfRangeException(nameof(entryType), entryType, null)
        };
        
        if (entry == null)
        {
            throw new ItemNotFoundException(entryType == FileEntryType.File 
                ? FilesCommonResource.ErrorMessage_FileNotFound 
                : FilesCommonResource.ErrorMessage_FolderNotFound
                );
        }

        if (!await fileSecurity.CanReadAsync(entry))
        {
            throw new SecurityException(entryType == FileEntryType.File 
                ? FilesCommonResource.ErrorMessage_SecurityException_ReadFile 
                : FilesCommonResource.ErrorMessage_SecurityException_ReadFolder
                );
        }
        
        var messageDbContext = await dbContextFactory.CreateDbContextAsync();
        var tenantId = await tenantManager.GetCurrentTenantIdAsync();

        var query = GetHistoryEventQuery(entryId, entryType, fromDate, toDate, messageDbContext, tenantId);

        query = query.OrderByDescending(x => x.Event.Date);
        
        if (offset > 0)
        {
            query = query.Skip(offset);
        }
        
        if (count > 0)
        {
            query = query.Take(count);
        }

        var history = query
            .Select(x => x.Event)
            .ToAsyncEnumerable()
            .SelectAwait(x => interpreter.ToHistoryAsync(x, entry));

        await foreach (var hEntry in history)
        {
            yield return hEntry;
        }
    }

    public async Task<int> GetHistoryCountAsync(int entryId, FileEntryType entryType, DateTime? fromDate, DateTime? toDate)
    {
        var messageDbContext = await dbContextFactory.CreateDbContextAsync();
        var tenantId = await tenantManager.GetCurrentTenantIdAsync();
        
        var query = GetHistoryEventQuery(entryId, entryType, fromDate, toDate, messageDbContext, tenantId);
        
        return await query.CountAsync();
    }
    
    private static IQueryable<HistoryEvent> GetHistoryEventQuery(
        int entryId,
        FileEntryType entryType,
        DateTime? fromDate,
        DateTime? toDate,
        MessagesContext messageDbContext,
        int tenantId)
    {
        var query = messageDbContext.AuditEvents.Join(
                messageDbContext.FilesAuditReferences,
                e => e.Id,
                r => r.AuditEventId, (@event, reference) => new HistoryEvent { Event = @event, Reference = reference })
            .Where(x => x.Event.TenantId == tenantId && x.Reference.EntryId == entryId && x.Reference.EntryType == (byte)entryType);
        
        if (fromDate.HasValue)
        {
            query = query.Where(x => x.Event.Date >= fromDate.Value);
        }
        
        if (toDate.HasValue)
        {
            query = query.Where(x => x.Event.Date <= toDate.Value);
        }

        return query;
    }

    private record HistoryEvent
    {
        public DbAuditEvent Event { get; init; }
        public DbFilesAuditReference Reference { get; init; }
    }
}
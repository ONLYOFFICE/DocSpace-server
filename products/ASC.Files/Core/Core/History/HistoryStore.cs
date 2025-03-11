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
    IDbContextFactory<FilesDbContext> dbContextFactory, 
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
        MessageAction.FileVersionRemoved, 
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
        MessageAction.FormSubmit,
        MessageAction.FormOpenedForFilling,
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
        MessageAction.RoomColorChanged,
        MessageAction.RoomCoverChanged,
        MessageAction.RoomIndexExportSaved,
        MessageAction.RoomInviteResend,
        MessageAction.RoomStealthEnabled,
        MessageAction.RoomStealthDisabled
    ];

    private static HashSet<int> FilterFolderActions => [
        (int)MessageAction.FolderCreated,
        (int)MessageAction.FolderMovedWithOverwriting,
        (int)MessageAction.FolderMovedToTrash,
        (int)MessageAction.FolderRenamed
    ];

    private static HashSet<int> FilterFileActions => [
        (int)MessageAction.FileCopied,
        (int)MessageAction.FileUploaded,
        (int)MessageAction.FileMoved,
        (int)MessageAction.FileRenamed,
        (int)MessageAction.FormSubmit,
        (int)MessageAction.FormOpenedForFilling
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
        (int)MessageAction.FileRestoreVersion,
        (int)MessageAction.FileVersionRemoved,
        (int)MessageAction.FileLocked,
        (int)MessageAction.FileUnlocked,
        (int)MessageAction.FileIndexChanged
    ];
    
    public async IAsyncEnumerable<HistoryEntry> GetHistoryAsync(
        FileEntry<int> entry,
        int offset,
        int count,
        DateTime? fromDate,
        DateTime? toDate)
    {
        var context = await dbContextFactory.CreateDbContextAsync();
        var tenantId = tenantManager.GetCurrentTenantId();

        var events = context.GetReferenceEventsAsync(tenantId, entry.Id, (byte)entry.FileEntryType, offset, count, fromDate, toDate);

        await foreach (var hEntry in events.SelectAwait(e => interpreter.ToHistoryAsync(e, entry)).Where(x => x != null))
        {
            yield return hEntry;
        }
    }
    
    public async Task<int> GetHistoryCountAsync(
        int entryId,
        FileEntryType entryType,
        DateTime? fromDate,
        DateTime? toDate)
    {
        var messageDbContext = await dbContextFactory.CreateDbContextAsync();
        var tenantId = tenantManager.GetCurrentTenantId();
        
        return await messageDbContext.GetReferenceEventsCountAsync(tenantId, entryId, (byte)entryType, fromDate, toDate);
    }

    public async IAsyncEnumerable<HistoryEntry> GetHistoryByUserIdAsync(FileEntry<int> entry, int offset, int count,
        DateTime? fromDate, DateTime? toDate, Guid userId)
    {
        var context = await dbContextFactory.CreateDbContextAsync();
        var tenantId = tenantManager.GetCurrentTenantId();

        var events = context.GetReferenceEventsByUserIdAsync(tenantId, entry.Id, entry.ParentId, _stealthSensitiveActions, 
            userId, offset, count, fromDate, toDate);
        
        await foreach (var hEntry in events.SelectAwait(e => interpreter.ToHistoryAsync(e, entry)).Where(x => x != null))
        {
            yield return hEntry;
        }
    }
    
    public async Task<int> GetHistoryCountByUserIdAsync(FileEntry<int> entry, DateTime? fromDate, DateTime? toDate, Guid userId)
    {
        var context = await dbContextFactory.CreateDbContextAsync();
        var tenantId = tenantManager.GetCurrentTenantId();

        return await context.GetReferenceEventsCountByUserIdAsync(tenantId, entry.Id, entry.ParentId, _stealthSensitiveActions, userId, fromDate, toDate);
    }
    
    public async IAsyncEnumerable<HistoryEntry> GetHistoryByEntriesAsync(
        FileEntry<int> entry,
        int offset,
        int count,
        List<int> includedFoldersIds,
        List<int> includedFilesIds,
        DateTime? fromDate,
        DateTime? toDate)
    {
        var context = await dbContextFactory.CreateDbContextAsync();
        var tenantId = tenantManager.GetCurrentTenantId();

        var events = context.GetReferenceEventsByEntriesAsync(tenantId, entry.Id, (byte)entry.FileEntryType, offset,
            count, includedFoldersIds, includedFilesIds, FilterFolderActions, FilterFileActions, fromDate, toDate); 

        await foreach (var hEntry in events.SelectAwait(e => interpreter.ToHistoryAsync(e, entry)).Where(x => x != null))
        {
            yield return hEntry;
        }
    }
    
    public async Task<int> GetHistoryCountByEntriesAsync(
        int entryId,
        FileEntryType entryType,
        List<int> filterFolderIds,
        List<int> filterFilesIds,
        DateTime? fromDate,
        DateTime? toDate)
    {
        var messageDbContext = await dbContextFactory.CreateDbContextAsync();
        var tenantId = tenantManager.GetCurrentTenantId();

        return await messageDbContext.GetReferenceEventsCountByEntriesAsync(tenantId, entryId, (byte)entryType, 
            filterFolderIds, filterFilesIds, FilterFolderActions, FilterFileActions, fromDate, toDate);
    }
}
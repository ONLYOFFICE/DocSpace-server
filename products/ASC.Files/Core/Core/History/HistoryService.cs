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
        MessageAction.FormOpenedForFilling
    ];
    
    public async IAsyncEnumerable<HistoryEntry> GetHistoryAsync(int entryId, FileEntryType entryType, int offset, int count)
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

        var events = messageDbContext.GetAuditEventsByReferences(tenantId, entryId, (byte)entryType, offset, count);

        if (await events.CountAsync() > 0 &&
            entryType == FileEntryType.Folder &&
            DocSpaceHelper.IsFormsFillingFolder(entry) &&
            entry.ShareRecord is { Share: FileShare.FillForms })
        {
            var folderActions = new HashSet<int> {
                    (int)MessageAction.FolderCreated,
                    (int)MessageAction.FolderMovedWithOverwriting,
                    (int)MessageAction.FolderMovedToTrash,
                    (int)MessageAction.FolderRenamed
                };
            var fileActions = new HashSet<int> {
                    (int)MessageAction.FileCopied,
                    (int)MessageAction.FileUploaded,
                    (int)MessageAction.FileMoved,
                    (int)MessageAction.FileRenamed,
                    (int)MessageAction.FormSubmit,
                    (int)MessageAction.FormOpenedForFilling
                };

            var folderDao = daoFactory.GetFolderDao<int>();
            var fileDao = daoFactory.GetFileDao<int>();

            var f = entry as Folder<int>;
            var folders = await folderDao.GetFoldersAsync(entryId, new OrderBy(SortedByType.DateAndTime, false), FilterType.None, false, Guid.Empty, null, true, false, 0, -1, default, true, f.FolderType).Select(r => r.Id).ToListAsync();
            var files = await fileDao.GetFilesAsync(entryId, new OrderBy(SortedByType.DateAndTime, false), FilterType.None, false, Guid.Empty, null, null, false, true, false, 0, -1, default, false, true, f.FolderType).Select(r => r.Id).ToListAsync();

            var list = new List<DbAuditEvent>();
            await foreach (var auditEvent in events)
            {
                var target = auditEvent.Target;
                if (target != null && int.TryParse(target.Split(',')[0], out var eventEntryId))
                {
                    if ((folders.Contains(eventEntryId) && folderActions.Contains(auditEvent.Action ?? 0)) ||
                    (files.Contains(eventEntryId) && fileActions.Contains(auditEvent.Action ?? 0)))
                    {
                        list.Add(auditEvent);
                    }
                }
            }
            events = list.ToAsyncEnumerable();
        }

        await foreach (var hEntry in events.SelectAwait(interpreter.ToHistoryAsync).Where(x => x != null))
        {
            yield return hEntry;
        }
    }

    public async Task<int> GetHistoryCountAsync(int entryId, FileEntryType entryType)
    {
        var messageDbContext = await dbContextFactory.CreateDbContextAsync();
        var tenantId = await tenantManager.GetCurrentTenantIdAsync();

        return await messageDbContext.GetAuditEventsByReferencesTotalCount(tenantId, entryId, (byte)entryType);
    }
}
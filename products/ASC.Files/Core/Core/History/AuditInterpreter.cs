// Copyright (C) Ascensio System SIA, 2009-2026
// 
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied
// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For
// details, see the GNU AGPL at: https://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA by email at info@onlyoffice.com
// or by postal mail at 20A-6 Ernesta Birznieka-Upisha Street, Riga,
// LV-1050, Latvia, European Union.
// 
// The interactive user interfaces in modified versions of the Program
// are required to display Appropriate Legal Notices in accordance with
// Section 5 of the GNU AGPL version 3.
// 
// No trademark rights are granted under this License.
// 
// All non-code elements of the Product, including illustrations,
// icon sets, and technical writing content, are licensed under the
// Creative Commons Attribution-ShareAlike 4.0 International License:
// https://creativecommons.org/licenses/by-sa/4.0/legalcode
// 
// This license applies only to such non-code elements and does not
// modify or replace the licensing terms applicable to the Program's
// source code, which remains licensed under the GNU Affero General
// Public License v3.
// 
// SPDX-License-Identifier: AGPL-3.0-only

namespace ASC.Files.Core.Core.History;

[Scope]
public class AuditInterpreter(IServiceProvider serviceProvider)
{
    private static readonly FolderCopiedInterpreter _folderCopiedInterpreter = new();
    private static readonly FolderMovedInterpreter _folderMovedInterpreter = new();
    private static readonly FileMovedInterpreter _fileMovedInterpreter = new();
    private static readonly FileDeletedInterpreter _fileDeletedInterpreter = new();
    private static readonly FileVersionDeletedInterpreter _fileVersionDeletedInterpreter = new();
    private static readonly FolderDeletedInterpreter _folderDeletedInterpreter = new();
    private static readonly FileCopiedInterpreter _fileCopiedInterpreter = new();
    private static readonly RoomLogoChangedInterpreter _roomLogoChangedInterpreter = new();
    private static readonly FileUpdatedInterpreter _fileUpdatedInterpreter = new();
    private static readonly RoomTagsInterpreter _roomTagsInterpreter = new();
    private static readonly RoomIndexingInterpreter _roomIndexingInterpreter = new();
    private static readonly RoomArchivingInterpreter _roomArchivingInterpreter = new();
    private static readonly FileLockInterpreter _fileLockInterpreter = new();
    private static readonly RoomDenyDownloadInterpreter _roomDenyDownloadInterpreter = new();
    private static readonly UserFileUpdatedInterpreter _userFileUpdatedInterpreter = new();
    private static readonly FileCustomFilterInterpreter _fileCustomFilterInterpreter = new();
    private static readonly RoomCreateInterpreter _roomCreateInterpreter = new();
    private static readonly RoomRenamedInterpreter _roomRenamedInterpreter = new();

    private static readonly FrozenDictionary<int, ActionInterpreter> _interpreters = new Dictionary<int, ActionInterpreter>
    {
        { (int)MessageAction.FileCreated, new FileCreateInterpreter() },
        { (int)MessageAction.FileUploaded, new FileUploadedInterpreter() },
        { (int)MessageAction.FileUploadedWithOverwriting, new FileUploadedInterpreter() },
        { (int)MessageAction.UserFileUpdated, _userFileUpdatedInterpreter },
        { (int)MessageAction.FileRenamed, new FileRenamedInterpreter() },
        { (int)MessageAction.FileMoved, _fileMovedInterpreter },
        { (int)MessageAction.FileMovedWithOverwriting, _fileMovedInterpreter },
        { (int)MessageAction.FileMovedToTrash, _fileDeletedInterpreter },
        { (int)MessageAction.FileCopied, _fileCopiedInterpreter },
        { (int)MessageAction.FileCopiedWithOverwriting, _fileCopiedInterpreter },
        { (int)MessageAction.FileDeleted, _fileDeletedInterpreter },
        { (int)MessageAction.FileVersionRemoved, _fileVersionDeletedInterpreter },
        { (int)MessageAction.FileConverted, new FileConvertedInterpreter() },
        { (int)MessageAction.FileRestoreVersion, _fileUpdatedInterpreter },
        { (int)MessageAction.FolderCreated, new FolderCreatedInterpreter() },
        { (int)MessageAction.FolderRenamed, new FolderRenamedInterpreter() },
        { (int)MessageAction.FolderMoved, _folderMovedInterpreter },
        { (int)MessageAction.FolderMovedWithOverwriting, _folderMovedInterpreter },
        { (int)MessageAction.FolderCopied, _folderCopiedInterpreter },
        { (int)MessageAction.FolderCopiedWithOverwriting, _folderCopiedInterpreter },
        { (int)MessageAction.FolderMovedToTrash, _folderDeletedInterpreter },
        { (int)MessageAction.FolderDeleted, _folderDeletedInterpreter },
        { (int)MessageAction.RoomCreateUser, new RoomUserAddedInterpreter() },
        { (int)MessageAction.RoomUpdateAccessForUser, new RoomUserUpdatedAccessInterpreter() },
        { (int)MessageAction.RoomRemoveUser, new RoomUserRemovedInterpreter() },
        { (int)MessageAction.RoomChangeOwner, new ChangeRoomOwnerInterpreter() },
        { (int)MessageAction.RoomGroupAdded, new RoomGroupAddedInterpreter() },
        { (int)MessageAction.RoomUpdateAccessForGroup, new RoomGroupAccessUpdatedInterpreter() },
        { (int)MessageAction.RoomGroupRemove, new RoomRemovedGroupInterpreter() },
        { (int)MessageAction.RoomCreated, _roomCreateInterpreter },
        { (int)MessageAction.RoomCopied, new RoomCopiedInterpreter() },
        { (int)MessageAction.RoomRenamed, _roomRenamedInterpreter },
        { (int)MessageAction.AddedRoomTags, _roomTagsInterpreter },
        { (int)MessageAction.DeletedRoomTags, _roomTagsInterpreter },
        { (int)MessageAction.RoomLogoCreated, _roomLogoChangedInterpreter },
        { (int)MessageAction.RoomLogoDeleted, _roomLogoChangedInterpreter },
        { (int)MessageAction.RoomExternalLinkCreated, new RoomExternalLinkCreatedInterpreter() },
        { (int)MessageAction.RoomExternalLinkRenamed, new RoomExternalLinkRenamedInterpreter() },
        { (int)MessageAction.RoomExternalLinkDeleted, new RoomExternalLinkDeletedInterpreter() },
        { (int)MessageAction.RoomExternalLinkRevoked, new RoomExternalLinkRevokedInterpreter() },
        { (int)MessageAction.FormSubmit, _userFileUpdatedInterpreter },
        { (int)MessageAction.FormStartedToFill, _userFileUpdatedInterpreter },
        { (int)MessageAction.FormOpenedForFilling, _userFileUpdatedInterpreter },
        { (int)MessageAction.FormPartiallyFilled, _userFileUpdatedInterpreter },
        { (int)MessageAction.FormCompletelyFilled, _userFileUpdatedInterpreter },
        { (int)MessageAction.FormStopped, _userFileUpdatedInterpreter },
        { (int)MessageAction.RoomIndexingEnabled, _roomIndexingInterpreter },
        { (int)MessageAction.RoomIndexingDisabled, _roomIndexingInterpreter },
        { (int)MessageAction.RoomLifeTimeSet, new RoomLifeTimeSetInterpreter() },
        { (int)MessageAction.RoomLifeTimeDisabled, new RoomLifeTimeDisabledInterpreter() },
        { (int)MessageAction.FolderIndexChanged, new FolderIndexChangedInterpreter() },
        { (int)MessageAction.FileIndexChanged, new FileIndexChangedInterpreter() },
        { (int)MessageAction.FileCustomFilterEnabled, _fileCustomFilterInterpreter },
        { (int)MessageAction.FileCustomFilterDisabled, _fileCustomFilterInterpreter },
        { (int)MessageAction.FolderIndexReordered, new FolderIndexReorderedInterpreter() },
        { (int)MessageAction.RoomArchived, _roomArchivingInterpreter },
        { (int)MessageAction.RoomUnarchived, _roomArchivingInterpreter },
        { (int)MessageAction.FileLocked, _fileLockInterpreter },
        { (int)MessageAction.FileUnlocked, _fileLockInterpreter },
        { (int)MessageAction.RoomDenyDownloadEnabled, _roomDenyDownloadInterpreter },
        { (int)MessageAction.RoomDenyDownloadDisabled, _roomDenyDownloadInterpreter },
        { (int)MessageAction.RoomWatermarkSet, new RoomWatermarkSetInterpreter() },
        { (int)MessageAction.RoomWatermarkDisabled, new RoomWatermarkDisabledInterpreter() },
        { (int)MessageAction.RoomColorChanged, _roomLogoChangedInterpreter },
        { (int)MessageAction.RoomCoverChanged, _roomLogoChangedInterpreter },
        { (int)MessageAction.RoomIndexExportSaved, new RoomIndexExportSavedInterpreter() },
        { (int)MessageAction.RoomInviteResend, new RoomInviteResendInterpreter() },
        { (int)MessageAction.AgentCreated, _roomCreateInterpreter },
        { (int)MessageAction.AgentRenamed, _roomRenamedInterpreter },
        { (int)MessageAction.FileSavedButUserQuotaExceeded, _userFileUpdatedInterpreter },
        { (int)MessageAction.FileNotSavedDueToUserQuota, _userFileUpdatedInterpreter },
        { (int)MessageAction.FileSavedButRoomQuotaExceeded, _userFileUpdatedInterpreter },
        { (int)MessageAction.FileNotSavedDueToRoomQuota, _userFileUpdatedInterpreter }
    }.ToFrozenDictionary();

    public ValueTask<HistoryEntry> ToHistoryAsync(DbAuditEvent @event, DbFilesAuditReference reference)
    {
        return !_interpreters.TryGetValue(@event.Action ?? -1, out var interpreter)
            ? ValueTask.FromResult<HistoryEntry>(null)
            : interpreter.InterpretAsync(@event, reference, serviceProvider);
    }
}
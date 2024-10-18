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
public class AuditInterpreter(IServiceProvider serviceProvider)
{
    private static readonly FolderCopiedInterpreter _folderCopiedInterpreter = new();
    private static readonly FolderMovedInterpreter _folderMovedInterpreter = new();
    private static readonly FileMovedInterpreter _fileMovedInterpreter = new();
    private static readonly FileDeletedInterpreter _fileDeletedInterpreter = new();
    private static readonly FolderDeletedInterpreter _folderDeletedInterpreter = new();
    private static readonly FileCopiedInterpreter _fileCopiedInterpreter = new();
    private static readonly RoomLogoChangedInterpreter _roomLogoChangedInterpreter = new();
    private static readonly FileUpdatedInterpreter _fileUpdatedInterpreter = new();
    private static readonly RoomTagsInterpreter _roomTagsInterpreter = new();
    private static readonly RoomIndexingInterpreter _roomIndexingInterpreter = new();
    private static readonly IndexChangedInterpreter _indexChangedInterpreter = new();
    private static readonly RoomArchivingInterpreter _roomArchivingInterpreter = new();
    private static readonly FileLockInterpreter _fileLockInterpreter = new();
    private static readonly RoomDenyDownloadInterpreter _roomDenyDownloadInterpreter = new();
    private static readonly UserFileUpdatedInterpreter _userFileUpdatedInterpreter = new();
    
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
        { (int)MessageAction.RoomGroupAdded, new RoomGroupAddedInterpreter() },
        { (int)MessageAction.RoomUpdateAccessForGroup, new RoomGroupAccessUpdatedInterpreter() },
        { (int)MessageAction.RoomGroupRemove, new RoomRemovedGroupInterpreter() },
        { (int)MessageAction.RoomCreated, new RoomCreateInterpreter() },
        { (int)MessageAction.RoomCopied, new RoomCopiedInterpreter() },
        { (int)MessageAction.RoomRenamed, new RoomRenamedInterpreter() },
        { (int)MessageAction.AddedRoomTags, _roomTagsInterpreter },
        { (int)MessageAction.DeletedRoomTags, _roomTagsInterpreter },
        { (int)MessageAction.RoomLogoCreated, _roomLogoChangedInterpreter },
        { (int)MessageAction.RoomLogoDeleted, _roomLogoChangedInterpreter },
        { (int)MessageAction.RoomExternalLinkCreated, new RoomExternalLinkCreatedInterpreter() },
        { (int)MessageAction.RoomExternalLinkRenamed, new RoomExternalLinkRenamedInterpreter() },
        { (int)MessageAction.RoomExternalLinkDeleted, new RoomExternalLinkDeletedInterpreter() },
        { (int)MessageAction.RoomExternalLinkRevoked, new RoomExternalLinkRevokedInterpreter() },
        { (int)MessageAction.FormSubmit, _userFileUpdatedInterpreter },
        { (int)MessageAction.FormOpenedForFilling, _userFileUpdatedInterpreter },
        { (int)MessageAction.RoomIndexingEnabled, _roomIndexingInterpreter },
        { (int)MessageAction.RoomIndexingDisabled, _roomIndexingInterpreter },
        { (int)MessageAction.RoomLifeTimeSet, new RoomLifeTimeSetInterpreter() },
        { (int)MessageAction.RoomLifeTimeDisabled, new RoomLifeTimeDisabledInterpreter() },
        { (int)MessageAction.FolderIndexChanged, _indexChangedInterpreter },
        { (int)MessageAction.FileIndexChanged, _indexChangedInterpreter },
        { (int)MessageAction.FolderIndexReordered, new FolderIndexReorderedInterpreter() },
        { (int)MessageAction.RoomArchived, _roomArchivingInterpreter },
        { (int)MessageAction.RoomUnarchived, _roomArchivingInterpreter },
        { (int)MessageAction.FileLocked, _fileLockInterpreter },
        { (int)MessageAction.FileUnlocked, _fileLockInterpreter },
        { (int)MessageAction.RoomDenyDownloadEnabled, _roomDenyDownloadInterpreter },
        { (int)MessageAction.RoomDenyDownloadDisabled, _roomDenyDownloadInterpreter },
        { (int)MessageAction.PrimaryExternalLinkCopied, new PrimaryLinkCopiedInterpreter() },
        { (int)MessageAction.RoomWatermarkSet, new RoomWatermarkSetInterpreter() },
        { (int)MessageAction.RoomWatermarkDisabled, new RoomWatermarkDisabledInterpreter() },
        { (int)MessageAction.RoomColorChanged, _roomLogoChangedInterpreter },
        { (int)MessageAction.RoomCoverChanged, _roomLogoChangedInterpreter },
        { (int)MessageAction.RoomIndexExportSaved, new RoomIndexExportSavedInterpreter() }
    }.ToFrozenDictionary();
    
    public ValueTask<HistoryEntry> ToHistoryAsync(DbAuditEvent @event, FileEntry<int> entry)
    {
        return !_interpreters.TryGetValue(@event.Action ?? -1, out var interpreter) 
            ? ValueTask.FromResult<HistoryEntry>(null) 
            : interpreter.InterpretAsync(@event, entry, serviceProvider);
    }
}
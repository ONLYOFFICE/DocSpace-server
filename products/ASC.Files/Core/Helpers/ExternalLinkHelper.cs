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

using Status = ASC.Files.Core.Security.Status;

namespace ASC.Files.Core.Helpers;

[Scope]
public class ExternalLinkHelper(
    ExternalShare externalShare,
    SecurityContext securityContext,
    IDaoFactory daoFactory,
    UserManager userManager,
    FileSecurity fileSecurity,
    FileMarker fileMarker,
    SocketManager socketManager,
    GlobalFolderHelper globalFolderHelper)
{
    public async Task<ValidationInfo> ValidateAsync(string key, string password = null, string fileId = null)
    {
        var result = new ValidationInfo
        {
            Status = Status.Invalid, 
            Access = FileShare.Restrict
        };

        var data = await externalShare.ParseShareKeyAsync(key);
        var securityDao = daoFactory.GetSecurityDao<string>();

        var record = await securityDao.GetSharesAsync([data.Id]).FirstOrDefaultAsync();
        if (record == null)
        {
            return result;
        }
        
        var isAuth = securityContext.IsAuthenticated;

        var status = await externalShare.ValidateRecordAsync(record, password, isAuth);
        result.Status = status;

        if (status != Status.Ok && status != Status.RequiredPassword)
        {
            return result;
        }

        var entryId = record.EntryId;

        var entry = int.TryParse(entryId, out var id)
            ? await GetEntryAndProcessAsync(id, record.EntryType, result)
            : await GetEntryAndProcessAsync(entryId, record.EntryType, result);

        if (!string.IsNullOrEmpty(fileId))
        {
            if (int.TryParse(fileId, out var entityId))
            {
                await GetSubEntryAndProcessAsync(entityId, entryId, result);
            }
            else
            {
                await GetSubEntryAndProcessAsync(fileId, entryId, result);
            }
        }

        if (entry == null || entry.RootFolderType is FolderType.TRASH or FolderType.Archive)
        {
            result.Status = Status.Invalid;
            return result;
        }

        if (status == Status.RequiredPassword)
        {
            return result;
        }
        
        result.Access = record.Share;
        result.TenantId = record.TenantId;
        result.LinkId = data.Id;

        if (isAuth)
        {
            var userId = securityContext.CurrentAccount.ID;
            
            if (entry.CreateBy.Equals(userId) || await userManager.IsDocSpaceAdminAsync(userId))
            {
                result.Shared = true;
            }
            else
            {
                result.Shared = (entry switch
                {
                    FileEntry<int> entryInt => await fileSecurity.CanReadAsync(entryInt) && !entryInt.ShareRecord.IsLink,
                    FileEntry<string> entryString => await fileSecurity.CanReadAsync(entryString) && !entryString.ShareRecord.IsLink,
                    _ => false
                });
            }

            if (!result.Shared && result.Status == Status.Ok)
            {
                result.Shared = entry switch
                {
                    Folder<int> folderInt => await MarkAsync(folderInt, linkId, userId),
                    Folder<string> folderString => await MarkAsync(folderString, linkId, userId),
                    _ => false
                };
            }
        }

        if (isAuth || !string.IsNullOrEmpty(externalShare.GetAnonymousSessionKey()))
        {
            return result;
        }

        await externalShare.SetAnonymousSessionKeyAsync();
        
        return result;
    }

    private async Task<FileEntry> GetEntryAndProcessAsync<T>(T id, FileEntryType entryType, ValidationInfo info)
    {
        if (entryType == FileEntryType.Folder)
        {
            var folder = await daoFactory.GetFolderDao<T>().GetFolderAsync(id);
            if (folder == null)
            {
                return null;
            }

            info.Id = folder.Id.ToString();
            info.Title = folder.Title;
        
            return folder;
        }
        
        var file = await daoFactory.GetFileDao<T>().GetFileAsync(id);
        if (file == null)
        {
            return null;
        }
        
        info.Id = file.Id.ToString();
        info.Title = file.Title;

        return file;
    }
    
    private async Task GetSubEntryAndProcessAsync<T>(T id, string rootId, ValidationInfo info)
    {
        var file = await daoFactory.GetFileDao<T>().GetFileAsync(id);
        if (file == null)
        {
            return;
        }
        var (currentRoomId, _) = await daoFactory.GetFolderDao<T>().GetParentRoomInfoFromFileEntryAsync(file);
        
        if (Equals(currentRoomId, default) || !string.Equals(currentRoomId.ToString(), rootId))
        {
            return;
        }
        
        info.EntityId = file.Id.ToString();
        info.EntryTitle = file.Title;
    }

    private async Task<bool> MarkAsync<T>(Folder<T> room, Guid linkId, Guid userId)
    {
        var result = await fileMarker.MarkAsRecentByLink(room, linkId);
        switch (result)
        {
            case MarkResult.NotMarked:
                return false;
            case MarkResult.MarkExists:
                return true;
            case MarkResult.Marked:
                room.FolderIdDisplay = IdConverter.Convert<T>(await globalFolderHelper.FolderVirtualRoomsAsync);
                await socketManager.CreateFolderAsync(room, [userId]);
                return true;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
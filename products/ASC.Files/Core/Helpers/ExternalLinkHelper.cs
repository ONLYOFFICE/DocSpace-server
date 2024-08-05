﻿// (c) Copyright Ascensio System SIA 2009-2024
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
public class ExternalLinkHelper(ExternalShare externalShare, SecurityContext securityContext, IDaoFactory daoFactory, UserManager userManager, FileSecurity fileSecurity)
{
    public async Task<ValidationInfo> ValidateAsync(string key, string password = null)
    {
        var result = new ValidationInfo
        {
            Status = Status.Invalid, 
            Access = FileShare.Restrict
        };

        var linkId = await externalShare.ParseShareKeyAsync(key);
        var securityDao = daoFactory.GetSecurityDao<string>();

        var record = await securityDao.GetSharesAsync(new[] { linkId }).FirstOrDefaultAsync();
        if (record == null)
        {
            return result;
        }

        var status = await externalShare.ValidateRecordAsync(record, password, securityContext.IsAuthenticated);
        result.Status = status;

        if (status != Status.Ok && status != Status.RequiredPassword)
        {
            return result;
        }

        var entryId = record.EntryId;

        var entry = int.TryParse(entryId, out var id)
            ? await GetEntryAndProcessAsync(id, record.EntryType, result)
            : await GetEntryAndProcessAsync(entryId, record.EntryType, result);

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
        result.LinkId = linkId;

        if (securityContext.IsAuthenticated)
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
        }

        if (securityContext.IsAuthenticated || !string.IsNullOrEmpty(externalShare.GetAnonymousSessionKey()))
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
}
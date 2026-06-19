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

namespace ASC.Files.Core.VirtualRooms;

[Scope]
public class CustomTagsService(
    IDaoFactory daoFactory,
    FileSecurity fileSecurity,
    AuthContext authContext,
    FilesMessageService filesMessageService,
    UserManager userManager,
    FileSecurityCommon fileSecurityCommon,
    SocketManager socketManager)
{
    public async Task<TagInfo> CreateTagAsync(string name)
    {
        var userType = await userManager.GetUserTypeAsync(authContext.CurrentAccount.ID);
        if (userType is not EmployeeType.RoomAdmin and not EmployeeType.DocSpaceAdmin)
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException);
        }

        ArgumentException.ThrowIfNullOrEmpty(name);

        var tagDao = daoFactory.GetTagDao<int>();
        var existedTag = await tagDao.GetTagsInfoAsync(name, TagType.Custom, true).FirstOrDefaultAsync();

        if (existedTag != null)
        {
            return existedTag;
        }

        var tagInfo = new TagInfo
        {
            Name = name,
            Owner = Guid.Empty,
            Type = TagType.Custom
        };

        var savedTag = await tagDao.SaveTagInfoAsync(tagInfo);

        filesMessageService.Send(MessageAction.TagCreated, savedTag.Name);

        return savedTag;
    }

    public async Task<TagInfo> UpdateTagAsync(string oldName, string newName)
    {
        var userType = await userManager.GetUserTypeAsync(authContext.CurrentAccount.ID);
        if (userType is not EmployeeType.DocSpaceAdmin)
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException);
        }

        ArgumentException.ThrowIfNullOrEmpty(oldName);
        ArgumentException.ThrowIfNullOrEmpty(newName);

        var tagDao = daoFactory.GetTagDao<int>();
        var existedTag = await tagDao.GetTagsInfoAsync(oldName, TagType.Custom, true).FirstOrDefaultAsync();

        if (existedTag == null)
        {
            throw new ItemNotFoundException();
        }
        var tag = await tagDao.GetTagsInfoAsync(newName, TagType.Custom, true).FirstOrDefaultAsync();
        if (tag != null)
        {
            throw new ArgumentException($"Tag with name '{newName}' already exists");
        }
        existedTag.Name = newName;

        var savedTag = await tagDao.UpdateTagInfoAsync(existedTag);

        return savedTag;
    }

    public async Task DeleteTagsAsync<T>(List<string> names)
    {
        var userType = await userManager.GetUserTypeAsync(authContext.CurrentAccount.ID);
        if (userType is not EmployeeType.DocSpaceAdmin)
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException);
        }

        if (names == null || names.Count == 0)
        {
            return;
        }

        var tagDao = daoFactory.GetTagDao<T>();

        var tagsInfo = await tagDao.GetTagsInfoAsync(names, TagType.Custom).ToListAsync();
        var tags = tagsInfo.Select(tagInfo => new Tag { EntryId = tagInfo.EntryId, Id = tagInfo.Id, Owner = tagInfo.Owner, Type = tagInfo.Type, Name = tagInfo.Name, EntryType = tagInfo.EntryType }).ToList();

        await tagDao.RemoveTagsAsync(tags);

        filesMessageService.Send(MessageAction.TagsDeleted, string.Join(',', tags.Select(t => t.Name).ToArray()));
    }

    public async Task<Folder<T>> AddRoomTagsAsync<T>(T folderId, List<string> names)
    {
        var folder = await daoFactory.GetFolderDao<T>().GetFolderAsync(folderId) ?? throw new ItemNotFoundException();

        var isDocSpaceAdmin = await fileSecurityCommon.IsDocSpaceAdministratorAsync(authContext.CurrentAccount.ID);

        if (folder.RootFolderType == FolderType.Archive || (!isDocSpaceAdmin && !await fileSecurity.CanEditRoomAsync(folder)))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException_EditRoom);
        }

        if (names == null || names.Count == 0)
        {
            return folder;
        }

        var tagDao = daoFactory.GetTagDao<T>();

        var tagsInfos = await tagDao.GetTagsInfoAsync(names, TagType.Custom).ToListAsync();
        var notFoundTags = names.Where(x => tagsInfos.All(r => r.Name != x)).Distinct().ToList();

        foreach (var tagInfo in notFoundTags)
        {
            tagsInfos.Add(await CreateTagAsync(tagInfo));
        }

        if (tagsInfos.Count == 0)
        {
            return folder;
        }

        var tags = tagsInfos.Select(tagInfo => Tag.Custom(Guid.Empty, folder, tagInfo.Name));

        await tagDao.SaveTagsAsync(tags);

        await filesMessageService.SendAsync(MessageAction.AddedRoomTags, folder, folder.Title, string.Join(',', tagsInfos.Select(t => t.Name)));
        await socketManager.UpdateFolderAsync(folder);

        return folder;
    }

    public async Task<Folder<T>> DeleteRoomTagsAsync<T>(T folderId, List<string> names)
    {
        var folder = await daoFactory.GetFolderDao<T>().GetFolderAsync(folderId) ?? throw new ItemNotFoundException();

        if (folder == null)
        {
            throw new ItemNotFoundException(FilesCommonResource.ErrorMessage_FolderNotFound);
        }

        var isDocSpaceAdmin = await fileSecurityCommon.IsDocSpaceAdministratorAsync(authContext.CurrentAccount.ID);

        if (folder.RootFolderType == FolderType.Archive || (!isDocSpaceAdmin && !await fileSecurity.CanEditRoomAsync(folder)))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException_EditRoom);
        }

        if (names.Count == 0)
        {
            return folder;
        }

        var tagDao = daoFactory.GetTagDao<T>();

        var tagsInfos = await tagDao.GetTagsInfoAsync(names, TagType.Custom).ToListAsync();

        await tagDao.RemoveTagsAsync(folder, tagsInfos.Select(t => t.Id).ToList());

        await filesMessageService.SendAsync(MessageAction.DeletedRoomTags, folder, folder.Title, string.Join(',', tagsInfos.Select(t => t.Name)));
        await socketManager.UpdateFolderAsync(folder);

        return folder;
    }

    public async IAsyncEnumerable<object> GetTagsInfoAsync<T>(string searchText, TagType tagType, int from, int count)
    {
        if (!await fileSecurityCommon.IsDocSpaceAdministratorAsync(authContext.CurrentAccount.ID))
        {
            var rooms = await fileSecurity.GetVirtualRoomsAsync(null, Guid.Empty, string.Empty, false, false, SearchArea.Active, false, [], false, ProviderFilter.None, Guid.Empty, QuotaFilter.All, StorageFilter.None);
            var tags = rooms.SelectMany(r => r.Tags)
                .Where(r => r.Type == tagType).Select(r => r.Name).Distinct();

            if (!string.IsNullOrEmpty(searchText))
            {
                var lowerText = searchText.ToLower().Trim().Replace("%", "\\%").Replace("_", "\\_");

                tags = tags.Where(r => r.Contains(lowerText, StringComparison.CurrentCultureIgnoreCase));
            }

            foreach (var tag in tags.Skip(from).Take(count))
            {
                yield return tag;
            }

            yield break;
        }

        await foreach (var tagInfo in daoFactory.GetTagDao<T>().GetTagsInfoAsync(searchText, tagType, false, from, count))
        {
            yield return tagInfo.Name;
        }
    }

    public async Task<bool> HasTagLinks(string name)
    {
        var userType = await userManager.GetUserTypeAsync(authContext.CurrentAccount.ID);
        if (userType is not EmployeeType.DocSpaceAdmin)
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException);
        }
        var tagDao = daoFactory.GetTagDao<int>();
        var existedTag = await tagDao.GetTagsInfoAsync(name, TagType.Custom, true).FirstOrDefaultAsync();

        if (existedTag == null)
        {
            throw new ItemNotFoundException();
        }

        var hasTagLiks = await tagDao.HasTagLinksAsync(existedTag);

        return hasTagLiks;
    }
}

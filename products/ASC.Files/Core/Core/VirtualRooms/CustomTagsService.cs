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

namespace ASC.Files.Core.VirtualRooms;

[Scope]
public class CustomTagsService(
    IDaoFactory daoFactory,
    FileSecurity fileSecurity,
    AuthContext authContext,
    FilesMessageService filesMessageService,
    UserManager userManager,
    FileSecurityCommon fileSecurityCommon)
{
    public async Task<string> CreateTagAsync(string name)
    {
        if (await userManager.IsGuestAsync(authContext.CurrentAccount.ID))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException);
        }

        ArgumentException.ThrowIfNullOrEmpty(name);

        var tagDao = daoFactory.GetTagDao<int>();
        var existedTag = await tagDao.GetTagsInfoAsync(name, TagType.Custom, true).FirstOrDefaultAsync();

        if (existedTag != null)
        {
            return existedTag.Name;
        }

        var tagInfo = new TagInfo
        {
            Name = name,
            Owner = Guid.Empty,
            Type = TagType.Custom
        };

        var savedTag = await tagDao.SaveTagInfoAsync(tagInfo);

        await filesMessageService.SendAsync(MessageAction.TagCreated, savedTag.Name);

        return savedTag.Name;
    }

    public async Task DeleteTagsAsync<T>(IEnumerable<string> names)
    {
        if (await userManager.IsGuestAsync(authContext.CurrentAccount.ID))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException);
        }

        if (!names.Any())
        {
            return;
        }

        var tagDao = daoFactory.GetTagDao<T>();

        var tagsInfo = await tagDao.GetTagsInfoAsync(names, TagType.Custom).ToListAsync();
        var tags = tagsInfo.Select(tagInfo => new Tag { EntryId = tagInfo.EntryId, Id = tagInfo.Id, Owner = tagInfo.Owner, Type = tagInfo.Type, Name = tagInfo.Name, EntryType = tagInfo.EntryType});

        await tagDao.RemoveTagsAsync(tags);

        await filesMessageService.SendAsync(MessageAction.TagsDeleted, string.Join(',', tags.Select(t => t.Name).ToArray()));
    }

    public async Task<Folder<T>> AddRoomTagsAsync<T>(T folderId, IEnumerable<string> names)
    {
        var folder = await daoFactory.GetFolderDao<T>().GetFolderAsync(folderId);

        if (folder.RootFolderType == FolderType.Archive || !await fileSecurity.CanEditRoomAsync(folder))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException_EditRoom);
        }

        if (!names.Any())
        {
            return folder;
        }

        var tagDao = daoFactory.GetTagDao<T>();

        var tagsInfos = await tagDao.GetTagsInfoAsync(names, TagType.Custom).ToListAsync();

        if (tagsInfos.Count == 0)
        {
            return folder;
        }

        var tags = tagsInfos.Select(tagInfo => Tag.Custom(Guid.Empty, folder, tagInfo.Name));

        await tagDao.SaveTagsAsync(tags);

        await filesMessageService.SendAsync(MessageAction.AddedRoomTags, folder, folder.Title, string.Join(',', tagsInfos.Select(t => t.Name)));

        return folder;
    }

    public async Task<Folder<T>> DeleteRoomTagsAsync<T>(T folderId, IEnumerable<string> names)
    {
        var folder = await daoFactory.GetFolderDao<T>().GetFolderAsync(folderId);

        if (folder.RootFolderType == FolderType.Archive || !await fileSecurity.CanEditRoomAsync(folder))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException_EditRoom);
        }

        if (!names.Any())
        {
            return folder;
        }

        var tagDao = daoFactory.GetTagDao<T>();

        var tagsInfos = await tagDao.GetTagsInfoAsync(names, TagType.Custom).ToListAsync();

        await tagDao.RemoveTagsAsync(folder, tagsInfos.Select(t => t.Id).ToList());

        await filesMessageService.SendAsync(MessageAction.DeletedRoomTags, folder, folder.Title, string.Join(',', tagsInfos.Select(t => t.Name)));

        return folder;
    }

    public async IAsyncEnumerable<object> GetTagsInfoAsync<T>(string searchText, TagType tagType, int from, int count)
    {
        if (!await fileSecurityCommon.IsDocSpaceAdministratorAsync(authContext.CurrentAccount.ID))
        {
            var rooms = await fileSecurity.GetVirtualRoomsAsync(FilterType.None, Guid.Empty, string.Empty, false, false, SearchArea.Active, false, [], false, ProviderFilter.None, SubjectFilter.Member, QuotaFilter.All, StorageFilter.None);
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
}
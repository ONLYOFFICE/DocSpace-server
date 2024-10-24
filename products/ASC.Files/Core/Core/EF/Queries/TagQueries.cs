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

namespace ASC.Files.Core.EF;

public partial class FilesDbContext
{    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid, null])]
    public IAsyncEnumerable<TagLinkData> NewTagsForFilesAsync(int tenantId, Guid subject, List<string> where)
    {
        return TagQueries.NewTagsForFilesAsync(this, tenantId, subject, where);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid, null])]
    public IAsyncEnumerable<TagLinkData> NewTagsForFoldersAsync(int tenantId, Guid subject, List<string> monitorFolderIdsStrings)
    {
        return TagQueries.NewTagsForFoldersAsync(this, tenantId, subject, monitorFolderIdsStrings);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid, FolderType.CustomRoom])]
    public IAsyncEnumerable<TagLinkData> TmpShareFileTagsAsync(int tenantId, Guid subject, FolderType folderType)
    {
        return TagQueries.TmpShareFileTagsAsync(this, tenantId, subject, folderType);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid, FolderType.CustomRoom])]
    public IAsyncEnumerable<TagLinkData> TmpShareFolderTagsAsync(int tenantId, Guid subject, FolderType folderType)
    {
        return TagQueries.TmpShareFolderTagsAsync(this, tenantId, subject, folderType);
    }
    
    //[PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid, null])]
    public IAsyncEnumerable<TagLinkData> TmpShareSBoxTagsAsync(int tenantId, Guid subject, IEnumerable<string> selectorsIds)
    {
        return TagQueries.TmpShareSBoxTagsAsync(this, tenantId, subject, selectorsIds);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid])]
    public IAsyncEnumerable<TagLinkData> ProjectsAsync(int tenantId, Guid subject)
    {
        return TagQueries.ProjectsAsync(this, tenantId, subject);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid, null])]
    public IAsyncEnumerable<TagLinkData> NewTagsForSBoxAsync(int tenantId, Guid subject, List<string> thirdpartyFolderIds)
    {
        return TagQueries.NewTagsForSBoxAsync(this, tenantId, subject, thirdpartyFolderIds);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid])]
    public IAsyncEnumerable<TagLinkData> NewTagsThirdpartyRoomsAsync(int tenantId, Guid subject)
    {
        return TagQueries.NewTagsThirdpartyRoomsAsync(this, tenantId, subject);
    }
    
    [PreCompileQuery([null, false])]
    public IAsyncEnumerable<int> FolderAsync(List<int> monitorFolderIdsInt, bool deepSearch)
    {
        return TagQueries.FolderAsync(this, monitorFolderIdsInt, deepSearch);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, FolderType.CustomRoom, PreCompileQuery.DefaultGuid])]
    public IAsyncEnumerable<int> ThirdpartyAccountAsync(int tenantId, FolderType folderType, Guid subject)
    {
        return TagQueries.ThirdpartyAccountAsync(this, tenantId, folderType, subject);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, TagType.Custom, null, null])]
    public IAsyncEnumerable<TagLinkData> TagsAsync(int tenantId, TagType tagType, IEnumerable<string> filesId, IEnumerable<string> foldersId)
    {
        return TagQueries.TagsAsync(this, tenantId, tagType, filesId, foldersId);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, TagType.Custom, null, null, null])]
    public IAsyncEnumerable<TagLinkData> GetTagsByEntryTypeAsync(int tenantId, TagType? tagType, FileEntryType entryType, string mappedId, Guid? owner, string name)
    {
        return TagQueries.GetTagsByEntryTypeAsync(this, tenantId, tagType, entryType, mappedId, owner, name);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, TagType.Custom, PreCompileQuery.DefaultGuid])]
    public IAsyncEnumerable<TagLinkData> TagsByOwnerAsync(int tenantId, TagType tagType, Guid owner)
    {
        return TagQueries.TagsByOwnerAsync(this, tenantId, tagType, owner);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, null, TagType.Custom])]
    public IAsyncEnumerable<TagLinkData> TagsInfoAsync(int tenantId, IEnumerable<string> names, TagType type)
    {
        return TagQueries.TagsInfoAsync(this, tenantId, names, type);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultDateTime])]
    public Task<int> MustBeDeletedFilesAsync(int tenantId, DateTime date)
    {
        return TagQueries.MustBeDeletedFilesAsync(this, tenantId, date);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public Task<bool> AnyTagLinkByIdsAsync(int tenantId, IEnumerable<int> tagsIds)
    {
        return TagQueries.AnyTagLinkByIdsAsync(this, tenantId, tagsIds);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid, null, TagType.Custom])]
    public Task<int> FirstTagIdAsync(int tenantId, Guid owner, string name, TagType type)
    {
        return TagQueries.FirstTagIdAsync(this, tenantId, owner, name, type);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt])]
    public Task<bool> AnyTagLinkByIdAsync(int tenantId, int id)
    {
        return TagQueries.AnyTagLinkByIdAsync(this, tenantId, id);
    }
    
    [PreCompileQuery([])]
    public Task<int> DeleteTagAsync()
    {
        return TagQueries.DeleteTagAsync(this);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultGuid, null, TagType.Custom, PreCompileQuery.DefaultInt])]
    public Task<int> TagIdAsync(Guid owner, string name, TagType type, int tenantId)
    {
        return TagQueries.TagIdAsync(this,  owner, name, type, tenantId);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt, FileEntryType.File, null, PreCompileQuery.DefaultGuid, PreCompileQuery.DefaultDateTime, PreCompileQuery.DefaultInt])]
    public Task<int> UpdateTagLinkAsync(int tenantId, int tagId, FileEntryType tagEntryType, string mappedId, Guid createdBy, DateTime createOn, int count)
    {
        return TagQueries.UpdateTagLinkAsync(this,  tenantId, tagId, tagEntryType, mappedId, createdBy, createOn, count);
    }
    
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, null, null, FileEntryType.File])]
    public Task<int> DeleteTagLinksAsync(int tenantId, IEnumerable<int> tagsIds, string entryId, FileEntryType type)
    {
        return TagQueries.DeleteTagLinksAsync(this,  tenantId, tagsIds, entryId, type);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, null, FileEntryType.File, TagType.Custom])]
    public Task<int> DeleteTagLinksByEntryIdAsync(int tenantId, string mappedId, FileEntryType entryType, TagType tagType)
    {
        return TagQueries.DeleteTagLinksByEntryIdAsync(this,  tenantId, mappedId, entryType, tagType);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt, null, FileEntryType.File])]
    public Task<int> DeleteTagLinksByTagIdAsync(int tenantId, int id, string entryId, FileEntryType entryType)
    {
        return TagQueries.DeleteTagLinksByTagIdAsync(this,  tenantId, id, entryId, entryType);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt])]
    public Task<int> DeleteTagByIdAsync(int tenantId, int id)
    {
        return TagQueries.DeleteTagByIdAsync(this,  tenantId, id);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, null, null, PreCompileQuery.DefaultGuid])]
    public IAsyncEnumerable<TagLinkData> TagLinkDataAsync(int tenantId, IEnumerable<string> entryIds, IEnumerable<int> entryTypes, Guid subject)
    {
        return TagQueries.TagLinkDataAsync(this,  tenantId, entryIds, entryTypes, subject);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public Task<int> DeleteTagsByIdsAsync(int tenantId, IEnumerable<int> tagsIds)
    {
        return TagQueries.DeleteTagsByIdsAsync(this,  tenantId, tagsIds);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, null, FileEntryType.File, null, PreCompileQuery.DefaultGuid, PreCompileQuery.DefaultDateTime])]
    public Task<int> IncrementNewTagsAsync(int tenantId, IEnumerable<int> tagsIds, FileEntryType tagEntryType, string mappedId, Guid createdBy, DateTime createOn)
    {
        return TagQueries.IncrementNewTagsAsync(this,  tenantId, tagsIds, tagEntryType, mappedId, createdBy, createOn);
    }
}

static file class TagQueries
{
    public static readonly Func<FilesDbContext, int, Guid, List<string>, IAsyncEnumerable<TagLinkData>> NewTagsForFilesAsync = 
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, Guid subject, List<string> where) =>
                ctx.Tag
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => subject == Guid.Empty || r.Owner == subject)
                    .Where(r => r.Type == TagType.New)
                    .Join(ctx.TagLink, r => r.Id, l => l.TagId,
                        (tag, link) => new TagLinkData { Tag = tag, Link = link })
                    .Where(r => r.Link.TenantId == r.Tag.TenantId)
                    .Join(ctx.Files,
                        r => Regex.IsMatch(r.Link.EntryId, "^[0-9]+$") ? Convert.ToInt32(r.Link.EntryId) : -1,
                        r => r.Id, (tagLink, file) => new { tagLink, file })
                    .Where(r => r.file.TenantId == r.tagLink.Link.TenantId)
                    .Where(r => where.Contains(r.file.ParentId.ToString()))
                    .Where(r => r.tagLink.Link.EntryType == FileEntryType.File)
                    .Select(r => r.tagLink)
                    .Distinct());

    public static readonly Func<FilesDbContext, int, Guid, List<string>, IAsyncEnumerable<TagLinkData>> NewTagsForFoldersAsync = 
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, Guid subject, List<string> monitorFolderIdsStrings) =>
                ctx.Tag
                    
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => subject == Guid.Empty || r.Owner == subject)
                    .Where(r => r.Type == TagType.New)
                    .Join(ctx.TagLink, r => r.Id, l => l.TagId,
                        (tag, link) => new TagLinkData { Tag = tag, Link = link })
                    .Where(r => r.Link.TenantId == r.Tag.TenantId)
                    .Where(r => monitorFolderIdsStrings.Contains(r.Link.EntryId))
                    .Where(r => r.Link.EntryType == FileEntryType.Folder));

    public static readonly Func<FilesDbContext, int, Guid, FolderType, IAsyncEnumerable<TagLinkData>> TmpShareFileTagsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, Guid subject, FolderType folderType) =>
                ctx.Tag
                    
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => subject == Guid.Empty || r.Owner == subject)
                    .Where(r => r.Type == TagType.New)
                    .Join(ctx.TagLink, r => r.Id, l => l.TagId,
                        (tag, link) => new TagLinkData { Tag = tag, Link = link })
                    .Where(r => r.Link.TenantId == r.Tag.TenantId)
                    .Where(r => ctx.Security.Any(a =>
                        a.TenantId == tenantId && a.EntryId == r.Link.EntryId && a.EntryType == r.Link.EntryType))
                    .Join(ctx.Files,
                        r => Regex.IsMatch(r.Link.EntryId, "^[0-9]+$") ? Convert.ToInt32(r.Link.EntryId) : -1,
                        f => f.Id, (tagLink, file) => new { tagLink, file })
                    .Where(r => r.file.TenantId == tenantId && r.file.CreateBy != subject &&
                                r.tagLink.Link.EntryType == FileEntryType.File)
                    .Select(r => new
                    {
                        r.tagLink,
                        root = ctx.Folders
                            .Join(ctx.Tree, a => a.Id, b => b.ParentId, (folder, tree) => new { folder, tree })
                            .Where(x => x.folder.TenantId == tenantId && x.tree.FolderId == r.file.ParentId)
                            .OrderByDescending(r1 => r1.tree.Level)
                            .Select(r1 => r1.folder)
                            .Take(1)
                            .FirstOrDefault()
                    })
                    .Where(r => r.root.FolderType == folderType)
                    .Select(r => r.tagLink)
                    .Distinct());

    public static readonly Func<FilesDbContext, int, Guid, FolderType, IAsyncEnumerable<TagLinkData>> TmpShareFolderTagsAsync = 
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, Guid subject, FolderType folderType) =>
                ctx.Tag
                    
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => subject == Guid.Empty || r.Owner == subject)
                    .Where(r => r.Type == TagType.New)
                    .Join(ctx.TagLink, r => r.Id, l => l.TagId,
                        (tag, link) => new TagLinkData { Tag = tag, Link = link })
                    .Where(r => r.Link.TenantId == r.Tag.TenantId)
                    .Where(r => ctx.Security.Any(a =>
                        a.TenantId == tenantId && a.EntryId == r.Link.EntryId && a.EntryType == r.Link.EntryType))
                    .Join(ctx.Folders,
                        r => Regex.IsMatch(r.Link.EntryId, "^[0-9]+$") ? Convert.ToInt32(r.Link.EntryId) : -1,
                        f => f.Id, (tagLink, folder) => new { tagLink, folder })
                    .Where(r => r.folder.TenantId == tenantId && r.folder.CreateBy != subject &&
                                r.tagLink.Link.EntryType == FileEntryType.Folder)
                    .Select(r => new
                    {
                        r.tagLink,
                        root = ctx.Folders
                            .Join(ctx.Tree, a => a.Id, b => b.ParentId, (folder, tree) => new { folder, tree })
                            .Where(x => x.folder.TenantId == tenantId)
                            .Where(x => x.tree.FolderId == r.folder.ParentId)
                            .OrderByDescending(r1 => r1.tree.Level)
                            .Select(r1 => r1.folder)
                            .Take(1)
                            .FirstOrDefault()
                    })
                    .Where(r => r.root.FolderType == folderType)
                    .Select(r => r.tagLink)
                    .Distinct());

    public static readonly Func<FilesDbContext, int, Guid, IEnumerable<string>, IAsyncEnumerable<TagLinkData>> TmpShareSBoxTagsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, Guid subject, IEnumerable<string> selectorsIds) =>
                ctx.Tag
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => subject == Guid.Empty || r.Owner == subject)
                    .Where(r => r.Type == TagType.New)
                    .Join(ctx.TagLink, r => r.Id, l => l.TagId, 
                        (tag, link) => new TagLinkData { Tag = tag, Link = link })
                    .Where(r => r.Link.TenantId == r.Tag.TenantId)
                    .Where(r => ctx.Security.Any(a => 
                        a.TenantId == tenantId && a.EntryId == r.Link.EntryId && a.EntryType == r.Link.EntryType))
                    .Join(ctx.ThirdpartyIdMapping, r => r.Link.EntryId, r => r.HashId, 
                        (tagLink, mapping) => new { tagLink, mapping })
                    .Where(r => r.mapping.TenantId == r.tagLink.Link.TenantId)
                    .Join(ctx.ThirdpartyAccount, r => r.mapping.TenantId, r => r.TenantId, 
                        (tagLinkMapping, account) => new { tagLinkMapping.tagLink, tagLinkMapping.mapping, account })
                    .Where(r => r.account.UserId != subject && 
                                r.account.FolderType == FolderType.USER && 
                                selectorsIds.Any(id => r.mapping.Id.StartsWith($"{id}-" + r.account.Id)))
                    .Select(r => r.tagLink)
                    .Distinct());

    public static readonly Func<FilesDbContext, int, Guid, IAsyncEnumerable<TagLinkData>> ProjectsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, Guid subject) =>
                ctx.Tag
                    
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => subject == Guid.Empty || r.Owner == subject)
                    .Where(r => r.Type == TagType.New)
                    .Join(ctx.TagLink, r => r.Id, l => l.TagId,
                        (tag, link) => new TagLinkData { Tag = tag, Link = link })
                    .Where(r => r.Link.TenantId == r.Tag.TenantId)
                    .Join(ctx.BunchObjects, r => r.Link.TenantId, r => r.TenantId,
                        (tagLink, bunch) => new { tagLink, bunch })
                    .Where(r => r.bunch.LeftNode == r.tagLink.Link.EntryId &&
                                r.tagLink.Link.EntryType == FileEntryType.Folder &&
                                r.bunch.RightNode.StartsWith("projects/project/"))
                    .Select(r => r.tagLink)
                    .Distinct());

    public static readonly Func<FilesDbContext, int, Guid, List<string>, IAsyncEnumerable<TagLinkData>> NewTagsForSBoxAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, Guid subject, List<string> thirdpartyFolderIds) =>
                ctx.Tag
                    
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => subject == Guid.Empty || r.Owner == subject)
                    .Where(r => r.Type == TagType.New)
                    .Join(ctx.TagLink, r => r.Id, l => l.TagId,
                        (tag, link) => new TagLinkData { Tag = tag, Link = link })
                    .Where(r => r.Link.TenantId == r.Tag.TenantId)
                    .Join(ctx.ThirdpartyIdMapping, r => r.Link.EntryId, r => r.HashId,
                        (tagLink, mapping) => new { tagLink, mapping })
                    .Where(r => r.mapping.TenantId == tenantId &&
                                thirdpartyFolderIds.Contains(r.mapping.Id) &&
                                r.tagLink.Tag.Owner == subject &&
                                r.tagLink.Link.EntryType == FileEntryType.Folder)
                    .Select(r => r.tagLink)
                    .Distinct());

    public static readonly Func<FilesDbContext, int, Guid, IAsyncEnumerable<TagLinkData>> NewTagsThirdpartyRoomsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, Guid subject) =>
                ctx.Tag
                    
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => subject == Guid.Empty || r.Owner == subject)
                    .Where(r => r.Type == TagType.New)
                    .Join(ctx.TagLink, r => r.Id, l => l.TagId,
                        (tag, link) => new TagLinkData { Tag = tag, Link = link })
                    .Where(r => r.Link.TenantId == r.Tag.TenantId)
                    .Join(ctx.ThirdpartyIdMapping, r => r.Link.EntryId, r => r.HashId,
                        (tagLink, mapping) => new { tagLink, mapping })
                    .Where(r => r.mapping.TenantId == tenantId && r.tagLink.Tag.Owner == subject &&
                                r.tagLink.Link.EntryType == FileEntryType.Folder)
                    .Join(ctx.ThirdpartyAccount, r => r.mapping.Id, r => r.FolderId,
                        (tagLinkData, account) => new { tagLinkData, account })
                    .Where(r => r.tagLinkData.mapping.Id == r.account.FolderId &&
                                r.account.FolderType == FolderType.VirtualRooms)
                    .Select(r => r.tagLinkData.tagLink).Distinct());

    public static readonly Func<FilesDbContext, List<int>, bool, IAsyncEnumerable<int>> FolderAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, List<int> monitorFolderIdsInt, bool deepSearch) =>
                ctx.Tree
                    
                    .Where(r => monitorFolderIdsInt.Contains(r.ParentId))
                    .Where(r => deepSearch || r.Level == 1)
                    .Select(r => r.FolderId));

    public static readonly Func<FilesDbContext, int, FolderType, Guid, IAsyncEnumerable<int>> ThirdpartyAccountAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, FolderType folderType, Guid subject) =>
                ctx.ThirdpartyAccount
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.FolderType == folderType)
                    .Where(r => folderType != FolderType.USER || r.UserId == subject)
                    .Select(r => r.Id));

    public static readonly Func<FilesDbContext, int, TagType, IEnumerable<string>, IEnumerable<string>, IAsyncEnumerable<TagLinkData>> TagsAsync = 
            Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, TagType tagType, IEnumerable<string> filesId, IEnumerable<string> foldersId) =>
                ctx.Tag.Where(r => r.TenantId == tenantId)
                    .Join(ctx.TagLink, r => r.Id, l => l.TagId,
                        (tag, link) => new TagLinkData { Tag = tag, Link = link })
                    .Where(r => r.Link.TenantId == r.Tag.TenantId)
                    .Where(r => r.Tag.Type == tagType)
                    .Where(r => r.Link.EntryType == FileEntryType.File && filesId.Contains(r.Link.EntryId)
                                || r.Link.EntryType == FileEntryType.Folder && foldersId.Contains(r.Link.EntryId)));

    public static readonly Func<FilesDbContext, int, TagType?, FileEntryType, string, Guid?, string, IAsyncEnumerable<TagLinkData>> GetTagsByEntryTypeAsync = 
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
        (FilesDbContext ctx, int tenantId, TagType? tagType, FileEntryType entryType, string mappedId, Guid? owner, string name) =>
            ctx.Tag.Where(r => r.TenantId == tenantId)
            .Join(ctx.TagLink, r => r.Id, l => l.TagId, (tag, link) => new TagLinkData { Tag = tag, Link = link })
            .Where(r => r.Link.TenantId == r.Tag.TenantId)
            .Where(r => tagType == null || r.Tag.Type == tagType.Value)
            .Where(r => owner == null || r.Tag.Owner == owner.Value)
            .Where(r => r.Link.EntryType == entryType)
            .Where(r => r.Link.EntryId == mappedId)
            .Where(r => name == null || r.Tag.Name == name));

    public static readonly Func<FilesDbContext, int, TagType, Guid, IAsyncEnumerable<TagLinkData>> TagsByOwnerAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, TagType tagType, Guid owner) =>
                ctx.Tag.Where(r => r.TenantId == tenantId)
                    .Join(ctx.TagLink, r => r.Id, l => l.TagId,
                        (tag, link) => new TagLinkData { Tag = tag, Link = link })
                    .Where(r => r.Link.TenantId == r.Tag.TenantId)
                    .Where(r => r.Tag.Type == tagType)
                    .Where(r => owner == Guid.Empty || r.Tag.Owner == owner)
                    .OrderByDescending(r => r.Link.CreateOn)
                    .AsQueryable());

    public static readonly Func<FilesDbContext, int, IEnumerable<string>, TagType, IAsyncEnumerable<TagLinkData>> TagsInfoAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, IEnumerable<string> names, TagType type) =>
                ctx.Tag.Where(r => r.TenantId == tenantId && r.Type == type && names.Contains(r.Name))
                .Select(r => new TagLinkData 
                { 
                    Tag = r, 
                    Link = (from f in ctx.TagLink where f.TagId == r.Id select f).FirstOrDefault()
                }));

    public static readonly Func<FilesDbContext, int, DateTime, Task<int>> MustBeDeletedFilesAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, DateTime date) =>
                ctx.Tag.Where(r => r.TenantId == tenantId)
                    .Join(ctx.TagLink, r => r.Id, l => l.TagId, (tag, link) => new TagLinkData { Tag = tag, Link = link })
                    .Where(r => r.Link.TenantId == r.Tag.TenantId)
                    .Where(r => (r.Tag.Type == TagType.New || r.Tag.Type == TagType.Recent) && r.Link.CreateOn <= date)
                    .Select(r=> r.Link)
                    .ExecuteDelete());

    public static readonly Func<FilesDbContext, int, IEnumerable<int>, Task<bool>> AnyTagLinkByIdsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, IEnumerable<int> tagsIds) =>
                ctx.TagLink.Any(r => r.TenantId == tenantId && tagsIds.Contains(r.TagId)));

    public static readonly Func<FilesDbContext, int, Guid, string, TagType, Task<int>> FirstTagIdAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, Guid owner, string name, TagType type) =>
                ctx.Tag
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.Owner == owner)
                    .Where(r => r.Name == name)
                    .Where(r => r.Type == type)
                    .Select(r => r.Id)
                    .FirstOrDefault());

    public static readonly Func<FilesDbContext, int, int, Task<bool>> AnyTagLinkByIdAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int id) =>
                ctx.TagLink.Any(r => r.TenantId == tenantId && r.TagId == id));

    public static readonly Func<FilesDbContext, Task<int>> DeleteTagAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx) =>
                (from ft in ctx.Tag
                    join ftl in ctx.TagLink.DefaultIfEmpty() on new { ft.TenantId, ft.Id } equals new
                    {
                        ftl.TenantId, Id = ftl.TagId
                    }
                    where ftl == null
                    select ft).ExecuteDelete());

    public static readonly Func<FilesDbContext, Guid, string, TagType, int, Task<int>> TagIdAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, Guid owner, string name, TagType type, int tenantId) =>
                ctx.Tag
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.Owner == owner)
                    .Where(r => r.Name == name)
                    .Where(r => r.Type == type)
                    .Select(r => r.Id)
                    .FirstOrDefault());

    public static readonly Func<FilesDbContext, int, int, FileEntryType, string, Guid, DateTime, int, Task<int>> UpdateTagLinkAsync = 
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int tagId, FileEntryType tagEntryType, string mappedId, Guid createdBy, DateTime createOn, int count) =>
                ctx.TagLink
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.TagId == tagId)
                    .Where(r => r.EntryType == tagEntryType)
                    .Where(r => r.EntryId == mappedId)
                    .ExecuteUpdate(f => f
                        .SetProperty(p => p.CreateBy, createdBy)
                        .SetProperty(p => p.CreateOn, createOn)
                        .SetProperty(p => p.Count, count)));

    public static readonly Func<FilesDbContext, int, IEnumerable<int>, string, FileEntryType, Task<int>>
        DeleteTagLinksAsync = Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, IEnumerable<int> tagsIds, string entryId, FileEntryType type) =>
                ctx.TagLink
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => tagsIds.Contains(r.TagId) && r.EntryId == entryId && r.EntryType == type)
                    .ExecuteDelete());

    public static readonly Func<FilesDbContext, int, string, FileEntryType, TagType, Task<int>> DeleteTagLinksByEntryIdAsync = 
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, string mappedId, FileEntryType entryType, TagType tagType) =>
                ctx.TagLink
                    .Where(r => r.TenantId == tenantId)
                    .Where(l => l.EntryId == mappedId && l.EntryType == entryType)
                    .Join(ctx.Tag, l => l.TagId, t => t.Id, (l, t) => new { l, t.Type })
                    .Where(r => r.Type == tagType)
                    .Select(r => r.l)
                    .ExecuteDelete());

    public static readonly Func<FilesDbContext, int, int, string, FileEntryType, Task<int>> DeleteTagLinksByTagIdAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int id, string entryId, FileEntryType entryType) =>
                ctx.TagLink
                    .Where(r => r.TenantId == tenantId &&
                                r.TagId == id &&
                                r.EntryId == entryId &&
                                r.EntryType == entryType)
                    .ExecuteDelete());

    public static readonly Func<FilesDbContext, int, int, Task<int>> DeleteTagByIdAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int id) =>
                ctx.Tag
                    .Where(r => r.TenantId == tenantId && r.Id == id)
                    .ExecuteDelete());

    public static readonly
        Func<FilesDbContext, int, IEnumerable<string>, IEnumerable<int>, Guid, IAsyncEnumerable<TagLinkData>> TagLinkDataAsync = 
            Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, IEnumerable<string> entryIds, IEnumerable<int> entryTypes, Guid subject) =>
                ctx.Tag
                    .Where(r => r.TenantId == tenantId)
                    .Join(ctx.TagLink, r => r.Id, l => l.TagId,
                        (tag, link) => new TagLinkData { Tag = tag, Link = link })
                    .Where(r => r.Link.TenantId == r.Tag.TenantId)
                    .Where(r => r.Tag.Type == TagType.New)
                    .Where(x => x.Link.EntryId != null)
                    //.Where(r => tags.Any(t => t.TenantId == r.Link.TenantId && t.EntryId == r.Link.EntryId && t.EntryType == (int)r.Link.EntryType)); ;
                    .Where(r => entryIds.Contains(r.Link.EntryId) && entryTypes.Contains((int)r.Link.EntryType))
                    .Where(r => subject == Guid.Empty || r.Tag.Owner == subject));

    public static readonly Func<FilesDbContext, int, IEnumerable<int>, Task<int>> DeleteTagsByIdsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, IEnumerable<int> tagsIds) =>
                ctx.Tag
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => tagsIds.Contains(r.Id))
                    .ExecuteDelete());
    
    public static readonly Func<FilesDbContext, int, IEnumerable<int>, FileEntryType, string, Guid, DateTime, Task<int>>
        IncrementNewTagsAsync = Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, IEnumerable<int> tagsIds, FileEntryType tagEntryType, string mappedId, Guid createdBy, DateTime createOn) =>
                ctx.TagLink
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => tagsIds.Contains(r.TagId))
                    .Where(r => r.EntryType == tagEntryType)
                    .Where(r => r.EntryId == mappedId)
                    .ExecuteUpdate(f => f
                        .SetProperty(p => p.CreateBy, createdBy)
                        .SetProperty(p => p.CreateOn, createOn)
                        .SetProperty(p => p.Count, p => p.Count + 1)));
}


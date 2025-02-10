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

using DriveFile = Google.Apis.Drive.v3.Data.File;

namespace ASC.Files.Core.Core.Thirdparty;

[Scope(GenericArguments = [typeof(BoxFile), typeof(BoxFolder), typeof(BoxItem)])]
[Scope(GenericArguments = [typeof(FileMetadata), typeof(FolderMetadata), typeof(Metadata)])]
[Scope(GenericArguments = [typeof(DriveFile), typeof(DriveFile), typeof(DriveFile)])]
[Scope(GenericArguments = [typeof(Item), typeof(Item), typeof(Item)])]
[Scope(GenericArguments = [typeof(WebDavEntry), typeof(WebDavEntry), typeof(WebDavEntry)])]
internal class ThirdPartyTagDao<TFile, TFolder, TItem>(
    IDaoFactory daoFactory,
    IDbContextFactory<FilesDbContext> dbContextFactory,
    IDaoSelector<TFile, TFolder, TItem> daoSelector,
    IDaoBase<TFile, TFolder, TItem> dao,
    TenantManager tenantManager)
    : IThirdPartyTagDao
    where TFile : class, TItem
    where TFolder : class, TItem
    where TItem : class
{
    private string PathPrefix { get; set; }

    public void Init(string pathPrefix)
    {
        PathPrefix = pathPrefix;
    }

    public async IAsyncEnumerable<Tag> GetNewTagsAsync(Guid subject, Folder<string> parentFolder, bool deepSearch)
    {
        var mapping = daoFactory.GetMapping<string>();
        var folderId = daoSelector.ConvertId(parentFolder.Id);
        var tenantId = tenantManager.GetCurrentTenantId();
        
        await using var filesDbContext = await dbContextFactory.CreateDbContextAsync();
        var entryIds = await Queries.HashIdsAsync(filesDbContext, PathPrefix)
            .ToDictionaryAsync(x => x.HashId, x => x.Id);

        if (entryIds.Count == 0)
        {
            yield break;
        }

        var qList = await Queries.TagLinkTagPairAsync(filesDbContext, tenantId, entryIds.Keys, subject).ToListAsync();

        var tags = new List<Tag>();

        foreach (var r in qList)
        {
            tags.Add(new Tag
            {
                Name = r.Tag.Name,
                Type = r.Tag.Type,
                Owner = r.Tag.Owner,
                EntryId = entryIds.TryGetValue(r.TagLink.EntryId, out var entryId) 
                    ? entryId 
                    : await mapping.MappingIdAsync(r.TagLink.EntryId),
                EntryType = r.TagLink.EntryType,
                Count = r.TagLink.Count,
                Id = r.Tag.Id
            });
        }

        if (deepSearch)
        {
            foreach (var e in tags)
            {
                yield return e;
            }
            yield break;
        }
        
        var children = (await dao.GetChildrenAsync(folderId)).Select(dao.MakeId);

        var folderFileIds = new[] { parentFolder.Id }
            .Concat(children);

        foreach (var e in tags.Where(tag => folderFileIds.Contains(tag.EntryId.ToString())))
        {
            yield return e;
        }
    }
}

sealed file class TagLinkTagPair
{
    public DbFilesTag Tag { get; set; }
    public DbFilesTagLink TagLink { get; set; }
}

sealed file class IdMap
{
    public string HashId { get; set; }
    public string Id { get; set; }
}

static file class Queries
{
    public static readonly Func<FilesDbContext, string, IAsyncEnumerable<IdMap>> HashIdsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, string idStart) =>
                ctx.ThirdpartyIdMapping
                    .Where(r => r.Id.StartsWith(idStart))
                    .Select(r => new IdMap
                    {
                        HashId = r.HashId,
                        Id = r.Id
                    }));

    public static readonly Func<FilesDbContext, int, IEnumerable<string>, Guid, IAsyncEnumerable<TagLinkTagPair>>
        TagLinkTagPairAsync =
            Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
                (FilesDbContext ctx, int tenantId, IEnumerable<string> entryIds, Guid owner) =>
                    (from r in ctx.Tag
                     from l in ctx.TagLink.Where(a => a.TenantId == r.TenantId && a.TagId == r.Id).DefaultIfEmpty()
                     where r.TenantId == tenantId && l.TenantId == tenantId && r.Type == TagType.New &&
                           entryIds.Contains(l.EntryId)
                     select new TagLinkTagPair { Tag = r, TagLink = l })
                    .Where(r => owner == Guid.Empty || r.Tag.Owner == owner)
                    .Distinct());
}

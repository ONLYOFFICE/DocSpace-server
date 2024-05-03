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

namespace ASC.Web.Files;

[Scope(Additional = typeof(FilesSpaceUsageStatExtension))]
public class FilesSpaceUsageStatManager(IDbContextFactory<FilesDbContext> dbContextFactory,
        TenantManager tenantManager,
        UserManager userManager,
        UserPhotoManager userPhotoManager,
        DisplayUserSettingsHelper displayUserSettingsHelper,
        CommonLinkUtility commonLinkUtility,
        GlobalFolderHelper globalFolderHelper,
        PathProvider pathProvider,
        IDaoFactory daoFactory,
        GlobalFolder globalFolder)
    : SpaceUsageStatManager, IUserSpaceUsage
{
    public override async ValueTask<List<UsageSpaceStatItem>> GetStatDataAsync()
    {
        var tenant = await tenantManager.GetCurrentTenantAsync();
        await using var filesDbContext = await dbContextFactory.CreateDbContextAsync();
        var myFiles = filesDbContext.Files
            .Join(filesDbContext.Tree, a => a.ParentId, b => b.FolderId, (file, tree) => new { file, tree })
            .Join(filesDbContext.BunchObjects, a => a.tree.ParentId.ToString(), b => b.LeftNode, (fileTree, bunch) => new { fileTree.file, fileTree.tree, bunch })
            .Where(r => r.file.TenantId == r.bunch.TenantId)
            .Where(r => r.file.TenantId == tenant.Id)
            .Where(r => r.bunch.RightNode.StartsWith("files/my/") || r.bunch.RightNode.StartsWith("files/trash/"))
            .GroupBy(r => r.file.CreateBy)
            .Select(r => new { CreateBy = r.Key, Size = r.Sum(a => a.file.ContentLength) });

        var commonFiles = filesDbContext.Files
            .Join(filesDbContext.Tree, a => a.ParentId, b => b.FolderId, (file, tree) => new { file, tree })
            .Join(filesDbContext.BunchObjects, a => a.tree.ParentId.ToString(), b => b.LeftNode, (fileTree, bunch) => new { fileTree.file, fileTree.tree, bunch })
            .Where(r => r.file.TenantId == r.bunch.TenantId)
            .Where(r => r.file.TenantId == tenant.Id)
            .Where(r => r.bunch.RightNode.StartsWith("files/common/"))
            .GroupBy(r => Constants.LostUser.Id)
            .Select(r => new { CreateBy = Constants.LostUser.Id, Size = r.Sum(a => a.file.ContentLength) });

        return await myFiles.Union(commonFiles)
            .AsAsyncEnumerable()
            .GroupByAwait(
            async r => await Task.FromResult(r.CreateBy),
            async r => await Task.FromResult(r.Size),
            async (userId, items) =>
            {
                var user = await userManager.GetUsersAsync(userId);
                var item = new UsageSpaceStatItem { SpaceUsage = await items.SumAsync() };
                if (user.Equals(Constants.LostUser))
                {
                    item.Name = FilesUCResource.CorporateFiles;
                    item.ImgUrl = pathProvider.GetImagePath("corporatefiles_big.png");
                    item.Url = await pathProvider.GetFolderUrlByIdAsync(await globalFolderHelper.FolderCommonAsync);
                }
                else
                {
                    item.Name = user.DisplayUserName(false, displayUserSettingsHelper);
                    item.ImgUrl = await user.GetSmallPhotoURL(userPhotoManager);
                    item.Url =await user.GetUserProfilePageUrl(commonLinkUtility);
                    item.Disabled = user.Status == EmployeeStatus.Terminated;
                }
                return item;
            })
            .OrderByDescending(i => i.SpaceUsage)
            .ToListAsync();

    }


    public async Task<long> GetUserSpaceUsageAsync(Guid userId)
    {
        var tenantId = await tenantManager.GetCurrentTenantIdAsync();
        var my = await globalFolder.GetFolderMyAsync(daoFactory);
        var trash = await globalFolder.GetFolderTrashAsync(daoFactory);

        await using var filesDbContext = await dbContextFactory.CreateDbContextAsync();
        var sum = await Queries.SumContentLengthAsync(filesDbContext, tenantId, userId, my, trash);
        var sumFromRoom = await Queries.SumFromRoomContentLengthAsync(filesDbContext, tenantId, userId);
        return Math.Max(sum - sumFromRoom, 0);
    }

    public async Task RecalculateFoldersUsedSpace(int TenantId)
    {
        await using var filesDbContext = await dbContextFactory.CreateDbContextAsync();

        var queryGroup = filesDbContext.Folders
                    .Join(filesDbContext.Tree, r => r.Id, a => a.ParentId, (folder, tree) => new { folder, tree })
                    .Join(filesDbContext.Files, r => r.tree.FolderId, b => b.ParentId, (temp, file) => new { temp.folder, file })
                    .Where(r => r.file.TenantId == r.folder.TenantId)
                    .Where(r => r.folder.TenantId == TenantId)
                    .GroupBy(temp => temp.folder.Id)
                    .Select(group => new
                    {
                        Id = group.Key,
                        Sum = group.Sum(temp => temp.file.ContentLength)
                    });

        var query = filesDbContext.Folders
                        .Join(queryGroup,
                            folder => folder.Id,
                            result => result.Id,
                            (folder, result) =>
                                new { Folder = folder, Result = result })
                        .ToList();

        foreach (var item in query)
        {
            item.Folder.Counter = item.Result.Sum;
            filesDbContext.Update(item.Folder);
        }

        await filesDbContext.SaveChangesAsync();

    }
    public async Task RecalculateUserQuota(int tenantId, Guid userId)
    {
        await tenantManager.SetCurrentTenantAsync(tenantId);

        var size = await GetUserSpaceUsageAsync(userId);

        await tenantManager.SetTenantQuotaRowAsync(
           new TenantQuotaRow { TenantId = tenantId, Path = $"/{FileConstant.ModuleId}/", Counter = size, Tag = WebItemManager.DocumentsProductID.ToString(), UserId = userId, LastModified = DateTime.UtcNow },
           false);
    }
}

public static class FilesSpaceUsageStatExtension
{
    public static void Register(DIHelper services)
    {
        services.ServiceCollection.AddBaseDbContextPool<FilesDbContext>();
    }
}

static file class Queries
{
    public static readonly Func<FilesDbContext, int, Guid, int, int, Task<long>> SumContentLengthAsync =
        EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, Guid userId, int my, int trash) =>
                ctx.Files
                    .Join(ctx.Tree, a => a.ParentId, b => b.FolderId, (file, tree) => new { file, tree })
                    .Join(ctx.BunchObjects, a => a.tree.ParentId.ToString(), b => b.LeftNode, (fileTree, bunch) => new { fileTree.file, fileTree.tree, bunch })
                    .Where(r => r.file.TenantId == r.bunch.TenantId)
                    .Where(r => r.file.TenantId == tenantId)
                    .Where(r => r.bunch.RightNode.StartsWith("files/my/" + userId.ToString()) || r.bunch.RightNode.StartsWith("files/trash/" + userId.ToString()))
                    .Sum(r => r.file.ContentLength));

    public static readonly Func<FilesDbContext, int, Guid, Task<long>>
        SumFromRoomContentLengthAsync = EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, Guid userId) =>
                ctx.Tag
                    .Where(r => r.TenantId == tenantId)
                    .Join(ctx.TagLink, r => r.Id, l => l.TagId,
                        (tag, link) => new TagLinkData { Tag = tag, Link = link })
                    .Where(r => r.Link.TenantId == r.Tag.TenantId)
                    .Where(r => r.Tag.Type == TagType.FromRoom)
                    .Where(r => r.Tag.Owner == userId)
                    .Join(ctx.Files,
                        r => Regex.IsMatch(r.Link.EntryId, "^[0-9]+$") ? Convert.ToInt32(r.Link.EntryId) : -1,
                        f => f.Id, (tagLink, file) => new { tagLink, file })
                    .Sum(r => r.file.ContentLength));

}
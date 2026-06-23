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

namespace ASC.Web.Files;

[Scope]
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
        var tenant = tenantManager.GetCurrentTenant();
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
            .GroupBy(
            async (r, _) => await Task.FromResult(r.CreateBy),
            async (r, _) => await Task.FromResult(r.Size),
            async (userId, items, _) =>
            {
                var user = await userManager.GetUsersAsync(userId);
                var item = new UsageSpaceStatItem { SpaceUsage = items.Sum() };
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
                    item.Url = await user.GetUserProfilePageUrl(commonLinkUtility);
                    item.Disabled = user.Status == EmployeeStatus.Terminated;
                }
                return item;
            })
            .OrderByDescending(i => i.SpaceUsage)
            .ToListAsync();

    }

    public async Task<long> GetPortalSpaceUsageAsync()
    {
        var tenantId = tenantManager.GetCurrentTenantId();
        await using var filesDbContext = await dbContextFactory.CreateDbContextAsync();
        return await Queries.SumPortalContentLengthAsync(filesDbContext, tenantId);
    }
    public async Task<long> GetUserSpaceUsageAsync(Guid userId)
    {
        var tenantId = tenantManager.GetCurrentTenantId();
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

        await filesDbContext.Folders
            .Where(f => f.TenantId == TenantId)
            .ExecuteUpdateAsync(s => s.SetProperty(f => f.Counter, 0));

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
    public async Task RecalculateQuota(int tenantId)
    {
        await tenantManager.SetCurrentTenantAsync(tenantId);

        var size = await GetPortalSpaceUsageAsync();

        await tenantManager.SetTenantQuotaRowAsync(
           new TenantQuotaRow { TenantId = tenantId, Path = $"/{FileConstant.ModuleId}/", Counter = size, Tag = WebItemManager.DocumentsProductID.ToString(), UserId = Guid.Empty, LastModified = DateTime.UtcNow },
           false);
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

static file class Queries
{
    public static readonly Func<FilesDbContext, int, Task<long>> SumPortalContentLengthAsync =
       EF.CompileAsyncQuery(
           (FilesDbContext ctx, int tenantId) =>
               ctx.Files
                   .Where(r => r.TenantId == tenantId)
                   .Sum(r => r.ContentLength));

    public static readonly Func<FilesDbContext, int, Guid, int, int, Task<long>> SumContentLengthAsync =
        EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, Guid userId, int my, int trash) =>
                ctx.Files
                    .Join(ctx.Tree, a => a.ParentId, b => b.FolderId, (file, tree) => new { file, tree })
                    .Join(ctx.BunchObjects, a => a.tree.ParentId.ToString(), b => b.LeftNode, (fileTree, bunch) => new { fileTree.file, fileTree.tree, bunch })
                    .Where(r => r.file.TenantId == r.bunch.TenantId)
                    .Where(r => r.file.TenantId == tenantId)
                    .Where(r => r.bunch.RightNode.StartsWith("files/my/" + userId) || r.bunch.RightNode.StartsWith("files/trash/" + userId))
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
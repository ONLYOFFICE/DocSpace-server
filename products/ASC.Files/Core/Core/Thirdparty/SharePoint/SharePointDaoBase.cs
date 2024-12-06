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

using File = Microsoft.SharePoint.Client.File;
using Folder = Microsoft.SharePoint.Client.Folder;

namespace ASC.Files.Thirdparty.SharePoint;

internal class SharePointDaoBase(
    IDaoFactory daoFactory,
    IServiceProvider serviceProvider,
    UserManager userManager,
    TenantManager tenantManager,
    TenantUtil tenantUtil,
    IDbContextFactory<FilesDbContext> dbContextFactory,
    FileUtility fileUtility,
    SharePointDaoSelector regexDaoSelectorBase)
    : ThirdPartyProviderDao<File, Folder, ClientObject>(daoFactory, serviceProvider, userManager, tenantManager, tenantUtil, dbContextFactory, fileUtility, regexDaoSelectorBase)
{
    private readonly TenantManager _tenantManager = tenantManager;
    internal SharePointProviderInfo SharePointProviderInfo { get; private set; }

    public void Init(string pathPrefix, IProviderInfo<File, Folder, ClientObject> providerInfo)
    {
        PathPrefix = pathPrefix;
        ProviderInfo = providerInfo;
        SharePointProviderInfo = providerInfo as SharePointProviderInfo;
    }

    protected async ValueTask UpdatePathInDBAsync(string oldValue, string newValue)
    {
        if (oldValue.Equals(newValue))
        {
            return;
        }

        var tenantId = _tenantManager.GetCurrentTenantId();
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        var strategy = filesDbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            var mapping = _daoFactory.GetMapping<string>();
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            await using var tx = await dbContext.Database.BeginTransactionAsync();

            var oldIds = Queries.IdsAsync(dbContext, tenantId, oldValue);

            await foreach (var oldId in oldIds)
            {
                var oldHashId = await mapping.MappingIdAsync(oldId);
                var newId = oldId.Replace(oldValue, newValue);
                var newHashId = await mapping.MappingIdAsync(newId);

                var mappingForDelete = await Queries.ThirdpartyIdMappingsAsync(dbContext, tenantId, oldHashId).ToListAsync();
                var mappingForInsert = mappingForDelete.Select(m => new DbFilesThirdpartyIdMapping
                {
                    TenantId = m.TenantId,
                    Id = newId,
                    HashId = newHashId
                });

                dbContext.RemoveRange(mappingForDelete);
                await dbContext.AddRangeAsync(mappingForInsert);

                var securityForDelete =
                    await Queries.DbFilesSecuritiesAsync(dbContext, tenantId, oldHashId).ToListAsync();

                var securityForInsert = securityForDelete.Select(s => new DbFilesSecurity
                {
                    TenantId = s.TenantId,
                    TimeStamp = DateTime.Now,
                    EntryId = newHashId,
                    Share = s.Share,
                    Subject = s.Subject,
                    EntryType = s.EntryType,
                    Owner = s.Owner
                });

                dbContext.RemoveRange(securityForDelete);
                await dbContext.AddRangeAsync(securityForInsert);

                var linkForDelete =
                    await Queries.DbFilesTagLinksAsync(dbContext, tenantId, oldHashId).ToListAsync();

                var linkForInsert = linkForDelete.Select(l => new DbFilesTagLink
                {
                    EntryId = newHashId,
                    Count = l.Count,
                    CreateBy = l.CreateBy,
                    CreateOn = l.CreateOn,
                    EntryType = l.EntryType,
                    TagId = l.TagId,
                    TenantId = l.TenantId
                });

                dbContext.RemoveRange(linkForDelete);
                await dbContext.AddRangeAsync(linkForInsert);

                await dbContext.SaveChangesAsync();
            }

            await tx.CommitAsync();
        });
    }

    public override string MakeId(string path = null)
    {
        return path;
    }

    public override async Task<IEnumerable<string>> GetChildrenAsync(string folderId)
    {
        var folders = await SharePointProviderInfo.GetFolderFoldersAsync(folderId);
        var subFolders = folders.Select(x => SharePointProviderInfo.MakeId(x.ServerRelativeUrl));

        var folderFiles = await SharePointProviderInfo.GetFolderFilesAsync(folderId);
        var files = folderFiles.Select(x => SharePointProviderInfo.MakeId(x.ServerRelativeUrl));

        return subFolders.Concat(files);
    }
}

static file class Queries
{
    public static readonly Func<FilesDbContext, int, string, IAsyncEnumerable<string>> IdsAsync =
        EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, string idStart) =>
                ctx.ThirdpartyIdMapping
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.Id.StartsWith(idStart))
                    .Select(r => r.Id));

    public static readonly Func<FilesDbContext, int, string, IAsyncEnumerable<DbFilesThirdpartyIdMapping>>
        ThirdpartyIdMappingsAsync = EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, string hashId) =>
                ctx.ThirdpartyIdMapping
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.HashId == hashId));

    public static readonly Func<FilesDbContext, int, string, IAsyncEnumerable<DbFilesSecurity>> DbFilesSecuritiesAsync =
        EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, string entryId) =>
                ctx.Security
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.EntryId == entryId));

    public static readonly Func<FilesDbContext, int, string, IAsyncEnumerable<DbFilesTagLink>> DbFilesTagLinksAsync =
        EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, string entryId) =>
                ctx.TagLink
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.EntryId == entryId));
}

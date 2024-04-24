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

using ASC.Web.Files.Services.WCFService.FileOperations;

namespace ASC.Files.CleanupMarkedEntries;

[Singleton]
public class CleanupMarkedEntriesWorker(ILogger<CleanupMarkedEntriesWorker> logger, IServiceScopeFactory serviceScopeFactory)
{
    public async Task DeleteMarkedEntries(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        List<MarkedEntries> data;

        await using (var scope = serviceScopeFactory.CreateAsyncScope())
        {
            await using var dbContext = await scope.ServiceProvider.GetRequiredService<IDbContextFactory<FilesDbContext>>().CreateDbContextAsync(cancellationToken);
            data = await GetMarkedEntriesAsync(dbContext);
        }

        if (data.Count == 0)
        {
            return;
        }

        logger.InfoFoundUsers(data.Count);

        await Parallel.ForEachAsync(data,
                                    new ParallelOptions { MaxDegreeOfParallelism = 3, CancellationToken = cancellationToken }, //System.Environment.ProcessorCount
                                    DeleteFilesAndFoldersAsync);
    }

    private async ValueTask DeleteFilesAndFoldersAsync(MarkedEntries data, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        if (data.FolderIds.Count == 0 && data.FileIds.Count == 0)
        {
            return;
        }

        try
        {
            await using var scope = serviceScopeFactory.CreateAsyncScope();
            var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();
            var authManager = scope.ServiceProvider.GetRequiredService<AuthManager>();
            var securityContext = scope.ServiceProvider.GetRequiredService<SecurityContext>();
            var fileOperationsManager = scope.ServiceProvider.GetRequiredService<FileOperationsManager>();

            await tenantManager.SetCurrentTenantAsync(data.TenantId);

            var userAccount = await authManager.GetAccountByIDAsync(data.TenantId, data.UserId);
            if (Equals(userAccount, ASC.Core.Configuration.Constants.Guest))
            {
                return;
            }

            await securityContext.AuthenticateMeWithoutCookieAsync(userAccount);

            logger.InfoCleanupMarkedEntries(data.TenantId, data.UserId, string.Join(',', data.FolderIds), string.Join(',', data.FileIds));

            await fileOperationsManager.PublishDelete(data.FolderIds, data.FileIds, true, true);

            logger.InfoCleanupMarkedEntriesWait(data.TenantId, data.UserId);

            while (true)
            {
                var statuses = await fileOperationsManager.GetOperationResults(true);

                if (statuses.TrueForAll(r => r.OperationType != FileOperationType.Delete || r.Finished))
                {
                    break;
                }

                await Task.Delay(100, cancellationToken);
            }

            logger.InfoCleanupMarkedEntriesFinish(data.TenantId, data.UserId);
        }
        catch (Exception ex)
        {
            logger.ErrorWithException(ex);
        }
    }

    private async Task<List<MarkedEntries>> GetMarkedEntriesAsync(FilesDbContext dbContext)
    {
        var dic = new Dictionary<Guid, MarkedEntries>();
        var toDate = DateTime.UtcNow.AddDays(-1);

        var folders = await Queries.GetMarkedFoldersAsync(dbContext, toDate).ToListAsync();
        foreach(var folder in folders)
        {
            if (dic.TryGetValue(folder.UserId, out var value))
            {
                value.FolderIds.Add(folder.EntryId);
            }
            else
            {
                var item = new MarkedEntries(folder.TenantId, folder.UserId);
                item.FolderIds.Add(folder.EntryId);
                dic.Add(folder.UserId, item);
            }
        }

        var files = await Queries.GetMarkedFilesAsync(dbContext, toDate).ToListAsync();
        foreach (var file in files)
        {
            if (dic.TryGetValue(file.UserId, out var value))
            {
                value.FileIds.Add(file.EntryId);
            }
            else
            {
                var item = new MarkedEntries(file.TenantId, file.UserId);
                item.FileIds.Add(file.EntryId);
                dic.Add(file.UserId, item);
            }
        }

        return dic.Values.ToList();
    }
}

static file class Queries
{
    public static readonly Func<FilesDbContext, DateTime, IAsyncEnumerable<MarkedEntry>>
        GetMarkedFoldersAsync = EF.CompileAsyncQuery(
            (FilesDbContext ctx, DateTime toDate) =>
                ctx.Tenants
                    .Join(ctx.Folders, a => a.Id, b => b.TenantId, (tenants, folders) => new { tenants, folders })
                    .Where(r => r.tenants.Status == TenantStatus.Active &&
                                r.folders.ModifiedOn < toDate &&
                                r.folders.Removed)
                    .Select(r => new MarkedEntry(r.folders.TenantId, r.folders.ModifiedBy, r.folders.Id)));

    public static readonly Func<FilesDbContext, DateTime, IAsyncEnumerable<MarkedEntry>>
        GetMarkedFilesAsync = EF.CompileAsyncQuery(
            (FilesDbContext ctx, DateTime toDate) =>
                ctx.Tenants
                    .Join(ctx.Files, a => a.Id, b => b.TenantId, (tenants, files) => new { tenants, files })
                    .Where(x => x.tenants.Status == TenantStatus.Active &&
                                x.files.ModifiedOn < toDate &&
                                x.files.Removed)
                    .Select(r => new MarkedEntry(r.files.TenantId, r.files.ModifiedBy, r.files.Id)));
}
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

        var groupedData = data.GroupBy(r => new MarkedEntriesGroupKey(r.TenantId, r.UserId));

        logger.InfoFoundUsers(groupedData.Count());

        await Parallel.ForEachAsync(groupedData,
                                    new ParallelOptions { MaxDegreeOfParallelism = 3, CancellationToken = cancellationToken }, //System.Environment.ProcessorCount
                                    DeleteFilesAndFoldersAsync);
    }

    private async ValueTask DeleteFilesAndFoldersAsync(IGrouping<MarkedEntriesGroupKey, MarkedEntries> groupedData, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
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

            await tenantManager.SetCurrentTenantAsync(groupedData.Key.TenantId);

            var userAccount = await authManager.GetAccountByIDAsync(groupedData.Key.TenantId, groupedData.Key.UserId);
            if (Equals(userAccount, ASC.Core.Configuration.Constants.Guest))
            {
                return;
            }

            await securityContext.AuthenticateMeWithoutCookieAsync(userAccount);

            var foldersList = new List<int>();
            var filesList = new List<int>();

            foreach (var markedEntries in groupedData)
            {
                if (markedEntries.FileEntryType == FileEntryType.Folder)
                {
                    foldersList.AddRange(markedEntries.EntryIds);
                }
                else
                {
                    filesList.AddRange(markedEntries.EntryIds);
                }
            }

            if (foldersList.Count == 0 && filesList.Count == 0)
            {
                return;
            }

            logger.InfoCleanupMarkedEntries(groupedData.Key.TenantId, groupedData.Key.UserId, string.Join(',', foldersList), string.Join(',', filesList));

            await fileOperationsManager.PublishDelete(foldersList, filesList, true, true);

            logger.InfoCleanupMarkedEntriesWait(groupedData.Key.TenantId, groupedData.Key.UserId);

            while (true)
            {
                var statuses = await fileOperationsManager.GetOperationResults(true);

                if (statuses.TrueForAll(r => r.OperationType != FileOperationType.Delete || r.Finished))
                {
                    break;
                }

                await Task.Delay(100, cancellationToken);
            }

            logger.InfoCleanupMarkedEntriesFinish(groupedData.Key.TenantId, groupedData.Key.UserId);
        }
        catch (Exception ex)
        {
            logger.ErrorWithException(ex);
        }
    }

    private async Task<List<MarkedEntries>> GetMarkedEntriesAsync(FilesDbContext dbContext)
    {
        var markedEntries = new List<MarkedEntries>();
        var fromDate = DateTime.UtcNow.AddDays(-1);
        markedEntries.AddRange(await Queries.GetMarkedFoldersAsync(dbContext, fromDate).ToListAsync());
        markedEntries.AddRange(await Queries.GetMarkedFilesAsync(dbContext, fromDate).ToListAsync());
        return markedEntries;
    }
}

static file class Queries
{
    public static readonly Func<FilesDbContext, DateTime, IAsyncEnumerable<MarkedEntries>>
        GetMarkedFoldersAsync = EF.CompileAsyncQuery(
            (FilesDbContext ctx, DateTime fromDate) =>
                ctx.Tenants
                    .Join(ctx.Folders, a => a.Id, b => b.TenantId, (tenants, folders) => new { tenants, folders })
                    .Where(r => r.tenants.Status == TenantStatus.Active &&
                                r.folders.ModifiedOn > fromDate &&
                                r.folders.Removed)
                    .Select(r => new
                    {
                        TenantId = r.folders.TenantId,
                        UserId = r.folders.ModifiedBy,
                        EntryId = r.folders.Id
                    })
                    .GroupBy(r => new MarkedEntriesGroupKey(r.TenantId, r.UserId), (key, group) => new MarkedEntries
                    {
                        TenantId = key.TenantId,
                        UserId = key.UserId,
                        FileEntryType = FileEntryType.Folder,
                        EntryIds = group.Select(r => r.EntryId)
                    }));

    public static readonly Func<FilesDbContext, DateTime, IAsyncEnumerable<MarkedEntries>>
    GetMarkedFilesAsync = EF.CompileAsyncQuery(
        (FilesDbContext ctx, DateTime fromDate) =>
            ctx.Tenants
                .Join(ctx.Files, a => a.Id, b => b.TenantId, (tenants, files) => new { tenants, files })
                .Where(x => x.tenants.Status == TenantStatus.Active &&
                            x.files.ModifiedOn > fromDate &&
                            x.files.Removed)
                .Select(r => new
                {
                    TenantId = r.files.TenantId,
                    UserId = r.files.ModifiedBy,
                    EntryId = r.files.Id
                })
                .GroupBy(r => new MarkedEntriesGroupKey(r.TenantId, r.UserId), (key, group) => new MarkedEntries
                {
                    TenantId = key.TenantId,
                    UserId = key.UserId,
                    FileEntryType = FileEntryType.File,
                    EntryIds = group.Select(r => r.EntryId)
                }));
}
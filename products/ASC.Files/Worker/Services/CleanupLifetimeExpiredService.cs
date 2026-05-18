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

namespace ASC.Files.Worker.Services;

[Singleton]
public class CleanupLifetimeExpiredService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<CleanupLifetimeExpiredService> logger)
    : ActivePassiveBackgroundService<CleanupLifetimeExpiredService>(logger, scopeFactory)
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

    private TimeSpan DelayInterval { get; set; } = TimeSpan.Parse(configuration.GetValue<string>("files:cleanupLifetimeExpired:delay") ?? "6:0:0");

    protected override TimeSpan ExecuteTaskPeriod { get; set; } = TimeSpan.Parse(configuration.GetValue<string>("files:cleanupLifetimeExpired:period") ?? "0:5:0");

    protected override async Task ExecuteTaskAsync(CancellationToken stoppingToken)
    {
        try
        {
            if (stoppingToken.IsCancellationRequested)
            {
                return;
            }

            List<LifetimeEnabledRoom> lifetimeEnabledRooms;

            await using (var scope = _scopeFactory.CreateAsyncScope())
            {
                await using var dbContext = await scope.ServiceProvider.GetRequiredService<IDbContextFactory<FilesDbContext>>().CreateDbContextAsync(stoppingToken);

                lifetimeEnabledRooms = await GetLifetimeEnabledRoomsAsync(dbContext);

                if (lifetimeEnabledRooms.Count == 0)
                {
                    return;
                }

                foreach (var room in lifetimeEnabledRooms)
                {
                    var lifetime = room.Lifetime.Map();

                    var expiration = lifetime.GetExpirationUtc();

                    room.ExipiredFiles = await GetExpiredFilesAsync(dbContext, room.TenantId, room.RoomId, expiration);

                    logger.InfoCleanupLifetimeExpiredFound(room.TenantId, room.RoomId, room.ExipiredFiles.Count);
                }
            }

            await Parallel.ForEachAsync(lifetimeEnabledRooms.Where(x => x.ExipiredFiles.Count > 0),
                new ParallelOptions { MaxDegreeOfParallelism = 3, CancellationToken = stoppingToken }, //System.Environment.ProcessorCount
                DeleteExpiredFiles);
        }
        catch (Exception e)
        {
            logger.ErrorWithException(e);
        }
    }

    private async ValueTask DeleteExpiredFiles(LifetimeEnabledRoom data, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();
            var authManager = scope.ServiceProvider.GetRequiredService<AuthManager>();
            var securityContext = scope.ServiceProvider.GetRequiredService<SecurityContext>();
            var fileOperationsManager = scope.ServiceProvider.GetRequiredService<FileDeleteOperationsManager>();

            var tenant = await tenantManager.SetCurrentTenantAsync(data.TenantId);

            var userAccount = await authManager.GetAccountByIDAsync(tenant.Id, data.UserId);
            if (Equals(userAccount, ASC.Core.Configuration.Constants.Guest))
            {
                userAccount = await authManager.GetAccountByIDAsync(tenant.Id, tenant.OwnerId);
            }

            await securityContext.AuthenticateMeWithoutCookieAsync(userAccount);

            logger.InfoCleanupLifetimeExpiredStart(data.TenantId, data.RoomId, userAccount.ID, string.Join(',', data.ExipiredFiles));

            await fileOperationsManager.Publish([], data.ExipiredFiles, true, true, data.Lifetime.DeletePermanently);

            logger.InfoCleanupLifetimeExpiredWait(data.TenantId, data.RoomId, userAccount.ID);

            while (true)
            {
                var statuses = await fileOperationsManager.GetOperationResults();

                if (statuses.TrueForAll(r => r.OperationType != FileOperationType.Delete || r.Finished))
                {
                    break;
                }

                await Task.Delay(100, cancellationToken);
            }

            logger.InfoCleanupLifetimeExpiredFinish(data.TenantId, data.RoomId, userAccount.ID);
        }
        catch (Exception ex)
        {
            logger.ErrorWithException(ex);
        }
    }

    private async Task<List<LifetimeEnabledRoom>> GetLifetimeEnabledRoomsAsync(FilesDbContext dbContext)
    {
        var delayedStartDate = DateTime.UtcNow.Subtract(DelayInterval);

        return await Queries.LifetimeEnabledRoomsAsync(dbContext)
            .Where(x => x.Lifetime.StartDate == null || x.Lifetime.StartDate < delayedStartDate)
            .ToListAsync();
    }

    private async Task<List<int>> GetExpiredFilesAsync(FilesDbContext dbContext, int tenantId, int roomId, DateTime expiration)
    {
        return await Queries.ExpiredFilesAsync(dbContext, tenantId, roomId, expiration).ToListAsync();
    }
}


static file class Queries
{
    public static readonly Func<FilesDbContext, IAsyncEnumerable<LifetimeEnabledRoom>>
        LifetimeEnabledRoomsAsync = EF.CompileAsyncQuery(
            (FilesDbContext ctx) =>
                ctx.RoomSettings
                    .Join(ctx.Folders, a => a.RoomId, b => b.Id, (settings, room) => new { settings, room })
                    .Join(ctx.BunchObjects, a => a.room.ParentId.ToString(), b => b.LeftNode, (folder, bunch) => new { folder.settings, folder.room, bunch })
                    .Where(x => x.settings.Lifetime != null)
                    .Where(r => r.bunch.RightNode != "files/archive/")
                    .Select(r => new LifetimeEnabledRoom
                    {
                        TenantId = r.settings.TenantId,
                        RoomId = r.settings.RoomId,
                        UserId = r.room.CreateBy,
                        Lifetime = r.settings.Lifetime
                    }));

    public static readonly Func<FilesDbContext, int, int, DateTime, IAsyncEnumerable<int>>
        ExpiredFilesAsync = EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int roomId, DateTime expiration) =>
                ctx.Tree
                    .Join(ctx.Files, a => a.FolderId, b => b.ParentId, (tree, file) => new { tree, file })
                    .Where(x => x.tree.ParentId == roomId && x.file.TenantId == tenantId && x.file.Version == 1 && x.file.ModifiedOn < expiration)
                    .Select(r => r.file.Id));
}

/// <summary>
/// The file lifetime settings enabled for the specified room.
/// </summary>
public class LifetimeEnabledRoom
{
    /// <summary>
    /// The tenant ID.
    /// </summary>
    public int TenantId { get; init; }

    /// <summary>
    /// The room ID.
    /// </summary>
    public int RoomId { get; init; }

    /// <summary>
    /// The ID of the user who enabled the lifetime settings.
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// The room data lifetime database.
    /// </summary>
    public DbRoomDataLifetime Lifetime { get; init; }

    /// <summary>
    /// The list of the expired files.
    /// </summary>
    public List<int> ExipiredFiles { get; set; }
}
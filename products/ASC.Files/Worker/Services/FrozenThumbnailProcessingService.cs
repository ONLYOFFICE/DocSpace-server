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
public class FrozenThumbnailProcessingService(
    ILogger<FrozenThumbnailProcessingService> logger,
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    IDbContextFactory<FilesDbContext> dbContextFactory)
    : ActivePassiveBackgroundService<FrozenThumbnailProcessingService>(logger, scopeFactory)
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

    protected override async Task ExecuteTaskAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var filesDbContext = await dbContextFactory.CreateDbContextAsync(stoppingToken);

            // MySQL's TIMESTAMPDIFF(MINUTE, modified_on, now) > m, which the query used to ask: changed m + 1 minutes ago or earlier
            var modifiedBefore = DateTime.UtcNow.AddMinutes(-(ExecuteTaskPeriod.Minutes + 1));
            var files = await filesDbContext.FrozenThumbnailFilesAsync(modifiedBefore).ToListAsync(cancellationToken: stoppingToken);
            if (files.Count == 0)
            {
                return;
            }

            logger.Information($"Found {files.Count} files to process");

            foreach (var f in files)
            {
                f.ThumbnailStatus = ASC.Files.Core.Thumbnail.Waiting;
            }

            filesDbContext.UpdateRange(files);
            await filesDbContext.SaveChangesAsync(stoppingToken);

            foreach (var group in files.GroupBy(x => x.TenantId))
            {
                await using var scope = _scopeFactory.CreateAsyncScope();

                var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();
                await tenantManager.SetCurrentTenantAsync(group.Key);

                var thumbnailBuilderService = scope.ServiceProvider.GetRequiredService<Builder<int>>();

                foreach (var file in group)
                {
                    await thumbnailBuilderService.BuildThumbnail(new FileData<int>(file.TenantId, file.CreateBy, file.Id, string.Empty, TariffState.NotPaid));
                }
            }
        }
        catch (Exception e)
        {
            logger.ErrorWithException(e);
        }
    }

    protected override TimeSpan ExecuteTaskPeriod { get; set; } = TimeSpan.Parse(configuration.GetValue<string>("files:frozenThumbProcess:period") ?? "0:10:0");
}

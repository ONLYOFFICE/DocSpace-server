// (c) Copyright Ascensio System SIA 2009-2025
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

using ASC.Files.Service.Services.Thumbnail;

namespace ASC.Files.Service.Services;

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
            
            var files = await Queries.DbFilesAsync(filesDbContext).ToListAsync(cancellationToken: stoppingToken);
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
                
                var commonLinkUtility = scope.ServiceProvider.GetService<CommonLinkUtility>();
                var baseUri = commonLinkUtility.GetFullAbsolutePath(string.Empty);
                commonLinkUtility.ServerUri = baseUri;
                
                var thumbnailBuilderService = scope.ServiceProvider.GetRequiredService<Builder<int>>();

                foreach (var file in group)
                {
                    await thumbnailBuilderService.BuildThumbnail(new FileData<int>(file.TenantId, file.CreateBy, file.Id, baseUri, TariffState.NotPaid));
                }
            }
        }
        catch (Exception e)
        {
            logger.ErrorWithException(e);
        }
    }

    protected override TimeSpan ExecuteTaskPeriod { get; set; } = TimeSpan.Parse(configuration.GetValue<string>("files:thumbWatch:period") ?? "0:15:0");
}

static file class Queries
{
    public static readonly Func<FilesDbContext, IAsyncEnumerable<DbFile>> DbFilesAsync =
        EF.CompileAsyncQuery(
            (FilesDbContext ctx) =>
                ctx.Files
                    .Join(ctx.Tenants, f => f.TenantId, t => t.Id, (file, tenant) => new { file, tenant })
                    .Where(r => r.tenant.Status == TenantStatus.Active)
                    .Where(r => r.file.CurrentVersion && r.file.ThumbnailStatus == ASC.Files.Core.Thumbnail.Creating &&
                                EF.Functions.DateDiffMinute(r.file.ModifiedOn, DateTime.UtcNow) > 5)
                    .Select(r => r.file));
}
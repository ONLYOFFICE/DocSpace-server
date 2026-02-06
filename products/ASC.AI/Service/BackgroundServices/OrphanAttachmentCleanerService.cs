// (c) Copyright Ascensio System SIA 2009-2026
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

namespace ASC.AI.Service.BackgroundServices;

public class OrphanAttachmentCleanerService(
    ILogger<OrphanAttachmentCleanerService> logger, 
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ChatDao chatDao) 
    : ActivePassiveBackgroundService<OrphanAttachmentCleanerService>(logger, scopeFactory)
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

    protected override TimeSpan ExecuteTaskPeriod { get; set; } = TimeSpan.Parse(
        configuration.GetValue<string>("ai:orphanAttachmentCleaner:period") ?? "0:10:0"
        );
    
    protected override async Task ExecuteTaskAsync(CancellationToken stoppingToken)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddMinutes(-ExecuteTaskPeriod.Minutes);

            await foreach (var group in chatDao.GetOrphanedAttachmentsAsync(cutoffDate)
                               .GroupBy(x => x.TenantId)
                               .WithCancellation(stoppingToken))
            {
                await using var scope = _scopeFactory.CreateAsyncScope();

                var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();
                await tenantManager.SetCurrentTenantAsync(group.Key);

                var fileDao = scope.ServiceProvider.GetRequiredService<IFileDao<int>>();

                var count = 0;

                foreach (var (_, fileId) in group)
                {
                    try
                    {
                        await fileDao.DeleteFileAsync(fileId);
                        count++;
                    }
                    catch (Exception ex)
                    {
                        logger.ErrorDeleteOrphanedFile(fileId, ex);
                    }
                }

                logger.InformationDeletedOrphanedFiles(count, group.Key);
            }
        }
        catch (Exception e)
        {
            logger.ErrorCleanUpOrphanedAttachments(e);
        }
    }
}
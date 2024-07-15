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

namespace ASC.Files.Core.Core;

public class CheckPdfStartupTask(IServiceProvider provider) : IStartupTask
{
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = provider.CreateAsyncScope();

        var tenantService = provider.GetService<ITenantService>();
        var dbContextFactory = provider.GetService<IDbContextFactory<FilesDbContext>>();
        var daoFactory = provider.GetService<IDaoFactory>();
        var fileChecker = provider.GetService<FileChecker>();
        var logger = provider.GetService<ILogger<FileDao>>();
        var tenantManager = provider.GetService<TenantManager>();

        var tenants = await tenantService.GetTenantsAsync((DateTime)default);
        var t = Task.Run(async () =>
        {
            await CheckPdf(tenants, dbContextFactory, fileChecker, daoFactory, logger, tenantManager);
        }, cancellationToken);
        _ = t.ConfigureAwait(false);
    }


    private async Task CheckPdf(IEnumerable<Tenant> tenants, IDbContextFactory<FilesDbContext> dbContextFactory, FileChecker fileChecker, IDaoFactory daoFactory, ILogger<FileDao> logger, TenantManager tenantManager)
    {
        foreach (var t in tenants)
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();

            await tenantManager.SetCurrentTenantAsync(t.Id);
            var dbFileIds = context.PdfTenantFileIdsAsync(t.Id);
            var fileDao = daoFactory.GetFileDao<int>();

            await foreach (var dbFileId in dbFileIds)
            {
                try
                {
                    var file = await fileDao.GetFileAsync(dbFileId);
                    var category = (int)FilterType.Pdf;
                    if (await fileChecker.CheckExtendedPDF(file))
                    {
                        category = (int)FilterType.PdfForm;
                    }

                    await using var filesDbContext = await dbContextFactory.CreateDbContextAsync();
                    var toUpdate = await filesDbContext.DbFileAsync(t.Id, file.Id);

                    toUpdate.Category = category;

                    filesDbContext.Update(toUpdate);
                    await filesDbContext.SaveChangesAsync();
                }
                catch(Exception ex) {
                    logger.Error(ex.Message);
                }
            }
        }

    }
}
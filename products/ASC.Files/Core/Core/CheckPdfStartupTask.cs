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

namespace ASC.Files.Core.Core;

public class CheckPdfStartupTask(IServiceProvider provider, ILogger<CheckPdfStartupTask> logger) : IStartupTaskNotAwaitable
{
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        await Task.Yield();

        try
        {
            var tenantService = provider.GetService<ITenantService>();

            var tenants = await tenantService.GetTenantsAsync((DateTime)default);

            await using var scope = provider.CreateAsyncScope();
            var checkPdfExecutor = scope.ServiceProvider.GetService<CheckPdfExecutor>();
            await checkPdfExecutor.CheckPdf(tenants);
        }
        catch (Exception ex)
        {
            logger.ErrorCheckPdfStartupTaskFailed(ex);
        }
    }

}

[Scope]
public class CheckPdfExecutor(IDbContextFactory<FilesDbContext> dbContextFactory, FileChecker fileChecker, IDaoFactory daoFactory, ILogger<CheckPdfExecutor> logger, TenantManager tenantManager)
{
    public async Task CheckPdf(IEnumerable<Tenant> tenants)
    {
        var fileDao = daoFactory.GetFileDao<int>();

        foreach (var t in tenants)
        {
            await tenantManager.SetCurrentTenantAsync(t.Id);
            IEnumerable<int> dbFileIds;
            await using (var context = await dbContextFactory.CreateDbContextAsync())
            {

                dbFileIds = await context.PdfTenantFileIdsAsync(t.Id).ToListAsync();
            }

            foreach (var dbFileId in dbFileIds)
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
                catch (Exception ex)
                {
                    logger.Error(ex.Message);
                }
            }
        }

    }
}
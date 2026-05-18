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

using ASC.Files.Core.Services.DocumentBuilderService;

namespace ASC.Web.Files.Utils;

[Transient]
public class ExportToXLSX(
    ILogger<ExportToXLSX> logger,
    IServiceProvider serviceProvider,
    TenantManager tenantManager,
    IEventBus eventBus,
    DocumentBuilderTaskManager<FormFillingReportTask, int, FormFillingReportTaskData> documentBuilderTaskManager,
    IHttpContextAccessor httpContextAccessor,
    AuthContext authContext)
{

    public async Task<FormFillingReportTask> UpdateXlsxReport(int roomId, int originalFormId, int originalFormVersion, bool isNewFile)
    {
        try
        {
            var tenantId = tenantManager.GetCurrentTenantId();
            var userId = authContext.CurrentAccount.ID;

            var commonLinkUtility = serviceProvider.GetService<CommonLinkUtility>();
            var baseUri = commonLinkUtility.ServerRootPath;

            var statusTask = serviceProvider.GetService<FormFillingReportTask>();
            var taskId = DocumentBuilderTaskManager.GetTaskId(tenantId, userId, originalFormId);
            statusTask.Init(baseUri, tenantId, userId, null, taskId);
            var taskProgress = await documentBuilderTaskManager.StartTask(statusTask, false);

            var headers = MessageSettings.GetHttpHeaders(httpContextAccessor?.HttpContext?.Request);
            var evt = new FormFillingReportIntegrationEvent(userId, tenantId, roomId, originalFormId, originalFormVersion, baseUri, isNewFile: isNewFile, headers: headers != null
                ? headers.ToDictionary(x => x.Key, x => x.Value.ToString())
                : []);

            await eventBus.PublishAsync(evt);

            return taskProgress;
        }
        catch (Exception ex)
        {
            logger.ErrorWhileGeneratingXlsx(ex);
            throw;
        }
    }

    public async Task<FormFillingReportTask> GetXlsxTaskAsync(int formId)
    {
        var tenantId = tenantManager.GetCurrentTenantId();
        var userId = authContext.CurrentAccount.ID;

        return await documentBuilderTaskManager.GetTask(tenantId, userId, formId);
    }
}
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

using ASC.Files.Core.Services.DocumentBuilderService;

namespace ASC.Web.Files.Utils;
[Transient]
public class ExportToXLSX(
    ILogger<AuditReportUploader> logger,
    IServiceProvider serviceProvider,
    TenantManager tenantManager,
    IEventBus eventBus,
    DocumentBuilderTaskManager documentBuilderTaskManager,
    IHttpContextAccessor httpContextAccessor,
    AuthContext authContext)
{

    public async Task UpdateXlsxReport(int roomId, int originalFormId)
    {
        try
        {
            var tenantId = tenantManager.GetCurrentTenantId();
            var userId = authContext.CurrentAccount.ID;

            var task = serviceProvider.GetService<FormFillingReportTask>();

            var commonLinkUtility = serviceProvider.GetService<CommonLinkUtility>();
            var baseUri = commonLinkUtility.ServerRootPath;
            task.Init(baseUri, tenantId, userId, null);

            var taskProgress = await documentBuilderTaskManager.StartTask(task, false);

            var headers = MessageSettings.GetHttpHeaders(httpContextAccessor?.HttpContext?.Request);
            var evt = new FormFillingReportIntegrationEvent(userId, tenantId, roomId, originalFormId, baseUri, headers: headers != null
                ? headers.ToDictionary(x => x.Key, x => x.Value.ToString())
                : []);

            await eventBus.PublishAsync(evt);
        }
        catch (Exception ex)
        {
            logger.ErrorWhileUploading(ex);
            throw;
        }
    }

    
}

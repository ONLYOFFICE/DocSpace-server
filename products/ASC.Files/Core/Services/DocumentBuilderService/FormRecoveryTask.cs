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

namespace ASC.Files.Core.Services.DocumentBuilderService;

/// <summary>
/// Recovers the form responses missing from the search index in a queue of its own, so that the report queue is not
/// held, and then starts the requested report.
/// </summary>
[Transient]
public class FormRecoveryTask : DistributedTaskProgress
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private string _baseUri;
    private int _tenantId;
    private Guid _userId;
    private FormFillingReportTaskData _reportData;

    public FormRecoveryTask() { }

    public FormRecoveryTask(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    public void Init(string baseUri, int tenantId, Guid userId, FormFillingReportTaskData reportData)
    {
        _baseUri = baseUri;
        _tenantId = tenantId;
        _userId = userId;
        _reportData = reportData;
        Id = FormRecoveryTaskManager.GetTaskId(tenantId, reportData.OriginalFormId);
        Status = DistributedTaskStatus.Created;
    }

    protected override async Task DoJob()
    {
        await using var scope = _serviceScopeFactory.CreateAsyncScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<FormRecoveryTask>>();

        try
        {
            if (!string.IsNullOrEmpty(_baseUri))
            {
                scope.ServiceProvider.GetRequiredService<CommonLinkUtility>().ServerUri = _baseUri;
            }

            await scope.ServiceProvider.GetRequiredService<TenantManager>().SetCurrentTenantAsync(_tenantId);
            await scope.ServiceProvider.GetRequiredService<SecurityContext>().AuthenticateMeWithoutCookieAsync(_userId);

            await scope.ServiceProvider.GetRequiredService<FormRecoveryService>()
                .RecoverAsync(_reportData.RoomId, _reportData.OriginalFormId, CancellationToken);

            var reportTask = scope.ServiceProvider.GetRequiredService<FormFillingReportTask>();
            reportTask.Init(_baseUri, _tenantId, _userId, _reportData, DocumentBuilderTaskManager.GetTaskId(_tenantId, _userId, _reportData.OriginalFormId));
            await scope.ServiceProvider.GetRequiredService<DocumentBuilderTaskManager<FormFillingReportTask, int, FormFillingReportTaskData>>().StartTask(reportTask);

            Status = DistributedTaskStatus.Completed;
        }
        catch (OperationCanceledException)
        {
            Status = DistributedTaskStatus.Canceled;
            throw;
        }
        catch (Exception e)
        {
            logger.ErrorWithException(e);
            Exception = e;
            Status = DistributedTaskStatus.Failted;
        }
        finally
        {
            IsCompleted = true;
            Percentage = 100;

            try
            {
                await PublishChanges();
            }
            catch (Exception e)
            {
                logger.ErrorWithException(e);
            }
        }
    }
}

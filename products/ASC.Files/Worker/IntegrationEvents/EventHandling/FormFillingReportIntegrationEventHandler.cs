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

namespace ASC.Files.Worker.IntegrationEvents.EventHandling;

[Scope]
public class FormFillingReportIntegrationEventHandler(
    ILogger<FormFillingReportIntegrationEventHandler> logger,
    CommonLinkUtility commonLinkUtility,
    TenantManager tenantManager,
    DocumentBuilderTaskManager<FormFillingReportTask, int, FormFillingReportTaskData> documentBuilderTaskManager,
    FormRecoveryTaskManager formRecoveryTaskManager,
    FormRecoveryService formRecoveryService,
    IServiceProvider serviceProvider)
    : IIntegrationEventHandler<FormFillingReportIntegrationEvent>
{

    public async Task Handle(FormFillingReportIntegrationEvent @event)
    {
        CustomSynchronizationContext.CreateContext();

        using (logger.BeginScope(new[] { new KeyValuePair<string, object>("integrationEventContext", $"{@event.Id}-{Program.AppName}") }))
        {
            logger.InformationHandlingIntegrationEvent(@event.Id, Program.AppName, @event);

            try
            {
                if (@event.Terminate)
                {
                    await documentBuilderTaskManager.TerminateTask(@event.TenantId, @event.CreateBy);
                    return;
                }

                if (!string.IsNullOrEmpty(@event.BaseUri))
                {
                    commonLinkUtility.ServerUri = @event.BaseUri;
                }

                await tenantManager.SetCurrentTenantAsync(@event.TenantId);

                var reportData = new FormFillingReportTaskData(@event.RoomId, @event.OriginalFormId, @event.OriginalFormVersion, @event.IsNewFile, @event.Headers);

                // The recovery starts the report when it is done, also the one of a repeated request that finds it running.
                if (@event.Recover && await NeedsRecoveryAsync(@event))
                {
                    var recoveryTask = serviceProvider.GetService<FormRecoveryTask>();
                    recoveryTask.Init(@event.BaseUri, @event.TenantId, @event.CreateBy, reportData);
                    await formRecoveryTaskManager.TryEnqueueAsync(recoveryTask);

                    return;
                }

                // A report started meanwhile would take the place of the one the client waits for and show a part of the
                // responses; the recovery's report takes this response in.
                if (!@event.Recover && await IsRecoveringAsync(@event))
                {
                    return;
                }

                var task = serviceProvider.GetService<FormFillingReportTask>();

                var taskId = DocumentBuilderTaskManager.GetTaskId(@event.TenantId, @event.CreateBy, @event.OriginalFormId);
                task.Init(@event.BaseUri, @event.TenantId, @event.CreateBy, reportData, taskId);

                await documentBuilderTaskManager.StartTask(task);
            }
            catch (Exception ex)
            {
                logger.ErrorWithException(ex);
            }
        }
    }

    // On every submission: a failed check starts the report as without the recovery, rather than drop it.
    private async Task<bool> IsRecoveringAsync(FormFillingReportIntegrationEvent @event)
    {
        try
        {
            return await formRecoveryTaskManager.IsRecoveringAsync(@event.TenantId, @event.OriginalFormId);
        }
        catch (Exception ex)
        {
            logger.ErrorWithException(ex);

            return false;
        }
    }

    // Only when there is something to do: forms missing from the index, or the versions of the older form versions that a
    // new report file gets. Otherwise the report starts at once, as without the recovery, and so it does when the check
    // fails.
    private async Task<bool> NeedsRecoveryAsync(FormFillingReportIntegrationEvent @event)
    {
        try
        {
            if (await formRecoveryService.HasMissingResponsesAsync(@event.RoomId, @event.OriginalFormId))
            {
                return true;
            }

            if (!@event.IsNewFile)
            {
                return false;
            }

            var versions = await formRecoveryService.GetVersionsWithResponsesAsync(@event.RoomId, @event.OriginalFormId);

            return versions != null && versions.Any(v => v < @event.OriginalFormVersion);
        }
        catch (Exception ex)
        {
            logger.ErrorWithException(ex);

            return false;
        }
    }
}
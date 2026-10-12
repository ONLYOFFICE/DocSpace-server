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

[Transient]
public class CustomerOperationsReportTask : DocumentBuilderTask<int, CustomerOperationsReportTaskData>
{
    public CustomerOperationsReportTask()
    {
    }

    public CustomerOperationsReportTask(IServiceScopeFactory serviceProvider) : base(serviceProvider)
    {
    }

    protected override Task<DocumentBuilderInputData> GetDocumentBuilderInputDataAsync(IServiceProvider serviceProvider)
    {
        // Builders are resolved from the per-execution scope rather than injected into this task:
        // the tenant and user context the report depends on is only established after DoJob has
        // created that scope.
        ICustomerReportBuilder builder = _data.ReportType switch
        {
            ReportType.Operations => serviceProvider.GetRequiredService<OperationsReportBuilder>(),
            ReportType.ServiceUsage => serviceProvider.GetRequiredService<ServiceUsageReportBuilder>(),
            ReportType.MonthlyUsage => serviceProvider.GetRequiredService<MonthlyUsageReportBuilder>(),
            ReportType.DocsCloudUserQuota => serviceProvider.GetRequiredService<DocsCloudUserQuotaReportBuilder>(),
            _ => throw new ArgumentOutOfRangeException(nameof(_data.ReportType), _data.ReportType, "Unknown report type")
        };

        return builder.BuildAsync(_userId, _data);
    }

    protected override async Task<File<int>> ProcessSourceFileAsync(IServiceProvider serviceProvider, Uri fileUri, DocumentBuilderInputData inputData)
    {
        var fileSaver = serviceProvider.GetRequiredService<ReportResultFileSaver>();
        var messageService = serviceProvider.GetRequiredService<MessageService>();

        var file = await fileSaver.SaveToMyDocumentsAsync(_userId, inputData.OutputFileName, fileUri);

        var headers = _data.Headers != null
            ? _data.Headers.ToDictionary(x => x.Key, x => new StringValues(x.Value))
            : [];

        var messageAction = _data.ReportType == ReportType.DocsCloudUserQuota
            ? MessageAction.DocsCloudQuotaReportDownloaded
            : MessageAction.CustomerOperationsReportDownloaded;

        messageService.SendHeadersMessage(messageAction, target: null, httpHeaders: headers, null);

        return file;
    }
}

public record CustomerOperationsReportTaskData(
    IDictionary<string, string> Headers,
    ReportType ReportType,
    List<string> ServiceName,
    DateTime? StartDate,
    DateTime? EndDate,
    string ParticipantName,
    bool? Credit,
    bool? Debit,
    OperationType? Type,
    OperationStatus? Status,
    Dictionary<string, string> Metadata,
    string OrderBy,
    OperationOrderType? OrderType
);

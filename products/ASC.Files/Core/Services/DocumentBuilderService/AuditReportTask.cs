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
public class AuditReportTask : DocumentBuilderTask<int, AuditReportTaskData>
{
    public AuditReportTask()
    {
    }

    public AuditReportTask(IServiceScopeFactory serviceProvider) : base(serviceProvider)
    {
    }

    public static string GetTaskDiscriminator(AuditReportKind kind, int? folderId = null)
    {
        return folderId.HasValue
            ? $"{kind}_{folderId.Value.ToString(CultureInfo.InvariantCulture)}"
            : $"{kind}";
    }

    protected override Task<DocumentBuilderInputData> GetDocumentBuilderInputDataAsync(IServiceProvider serviceProvider)
    {
        throw new NotSupportedException();
    }

    protected override Task<File<int>> ProcessSourceFileAsync(IServiceProvider serviceProvider, Uri fileUri, DocumentBuilderInputData inputData)
    {
        throw new NotSupportedException();
    }

    // This task drives the whole pipeline itself: the CSV flavour skips the document builder
    // entirely, so it cannot use the two-step flow of the base class.
    protected override async Task DoJob()
    {
        ILogger logger = null;

        try
        {
            CancellationToken.ThrowIfCancellationRequested();

            await using var scope = _serviceProvider.CreateAsyncScope();
            var serviceProvider = scope.ServiceProvider;

            if (!string.IsNullOrEmpty(_baseUri))
            {
                serviceProvider.GetService<CommonLinkUtility>().ServerUri = _baseUri;
            }

            var tenantManager = serviceProvider.GetService<TenantManager>();
            await tenantManager.SetCurrentTenantAsync(_tenantId);

            var securityContext = serviceProvider.GetService<SecurityContext>();
            await securityContext.AuthenticateMeWithoutCookieAsync(_userId);

            logger = serviceProvider.GetService<ILogger<AuditReportTask>>();

            var userManager = serviceProvider.GetService<UserManager>();
            var user = await userManager.GetUsersAsync(_userId);
            var userCulture = user.GetCulture();
            CultureInfo.CurrentCulture = userCulture;
            CultureInfo.CurrentUICulture = userCulture;

            CancellationToken.ThrowIfCancellationRequested();

            switch (_data.Kind)
            {
                case AuditReportKind.LoginHistory:
                    {
                        var events = await serviceProvider.GetService<LoginEventsRepository>()
                            .GetByFilterAsync(fromDate: _data.From, to: _data.To);

                        await ProduceAsync(serviceProvider, events, AuditReportResource.LoginHistoryReportName,
                            _data.From?.ToShortDateString(), _data.To?.ToShortDateString(), userCulture);
                        break;
                    }
                case AuditReportKind.FolderHistory:
                    {
                        var events = await serviceProvider.GetService<HistoryService>()
                            .GetFolderAuditEventsAsync(_data.FolderId.Value, _data.From, _data.To);

                        await ProduceAsync(serviceProvider, events, AuditReportResource.AuditTrailReportName,
                            "room", _data.FolderId.Value.ToString(CultureInfo.InvariantCulture), userCulture);
                        break;
                    }
                default:
                    {
                        var events = await serviceProvider.GetService<AuditEventsRepository>()
                            .GetByFilterAsync(from: _data.From, to: _data.To);

                        await ProduceAsync(serviceProvider, events, AuditReportResource.AuditTrailReportName,
                            _data.From?.ToShortDateString(), _data.To?.ToShortDateString(), userCulture);
                        break;
                    }
            }

            Percentage = 100;
            Status = DistributedTaskStatus.Completed;
        }
        catch (OperationCanceledException)
        {
            Status = DistributedTaskStatus.Canceled;
            throw;
        }
        catch (Exception ex)
        {
            logger?.ErrorWithException(ex);
            Status = DistributedTaskStatus.Failted;
            Exception = ex;
        }
        finally
        {
            IsCompleted = true;
            await PublishChanges();
        }
    }

    private async Task ProduceAsync<T>(IServiceProvider serviceProvider, IEnumerable<T> events, string reportNameFormat, string nameArg0, string nameArg1, CultureInfo culture) where T : BaseEvent
    {
        var descriptor = new AuditReportDescriptor(reportNameFormat, nameArg0, nameArg1, _data.From, _data.To, culture);

        // Writers are resolved from the per-execution scope: the tenant and user context they rely
        // on is only established above, inside this job's own scope.
        var result = _data.Format == AuditReportFormat.Csv
            ? await serviceProvider.GetRequiredService<AuditCsvReportWriter>()
                .WriteAsync(events, descriptor, ReportProgressAsync)
            : await serviceProvider.GetRequiredService<AuditXlsxReportWriter>()
                .WriteAsync(_userId, events, descriptor, ReportProgressAsync, CancellationToken);

        ResultFileId = result.FileId;
        ResultFileName = result.FileName;
        ResultFileUrl = result.FileUrl;

        SendDownloadedMessage(serviceProvider);
    }

    private async Task ReportProgressAsync(int percentage)
    {
        Percentage = percentage;

        await PublishChanges();
    }

    private void SendDownloadedMessage(IServiceProvider serviceProvider)
    {
        if (_data.Kind == AuditReportKind.FolderHistory)
        {
            return;
        }

        var messageService = serviceProvider.GetService<MessageService>();

        var headers = _data.Headers != null
            ? _data.Headers.ToDictionary(x => x.Key, x => new StringValues(x.Value))
            : [];

        var action = _data.Kind == AuditReportKind.LoginHistory
            ? MessageAction.LoginHistoryReportDownloaded
            : MessageAction.AuditTrailReportDownloaded;

        messageService.SendHeadersMessage(action, target: null, httpHeaders: headers, null);
    }
}

public record AuditReportTaskData(
    AuditReportKind Kind,
    AuditReportFormat Format,
    DateTime? From,
    DateTime? To,
    IDictionary<string, string> Headers,
    int? FolderId = null);

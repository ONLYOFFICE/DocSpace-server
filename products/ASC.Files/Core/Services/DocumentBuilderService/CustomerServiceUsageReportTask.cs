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
public class CustomerServiceUsageReportTask : DocumentBuilderTask<int, CustomerServiceUsageReportTaskData>
{
    public CustomerServiceUsageReportTask()
    {
    }

    public CustomerServiceUsageReportTask(IServiceScopeFactory serviceProvider) : base(serviceProvider)
    {
    }

    private const string ScriptName = "CustomerOperationsReport.docbuilder";

    protected override async Task<DocumentBuilderInputData> GetDocumentBuilderInputDataAsync(IServiceProvider serviceProvider)
    {
        var (scriptFilePath, tempFileName, outputFileName) = await GetCustomerServiceUsageReportData(serviceProvider, _userId, _data);

        return new DocumentBuilderInputData(scriptFilePath, tempFileName, outputFileName);
    }

    protected override async Task<File<int>> ProcessSourceFileAsync(IServiceProvider serviceProvider, Uri fileUri, DocumentBuilderInputData inputData)
    {
        var daoFactory = serviceProvider.GetService<IDaoFactory>();
        var clientFactory = serviceProvider.GetService<IHttpClientFactory>();
        var socketManager = serviceProvider.GetService<SocketManager>();
        var globalFolder = serviceProvider.GetService<GlobalFolder>();

        var file = serviceProvider.GetService<File<int>>();

        file.CreateBy = _userId;
        file.ParentId = await globalFolder.GetFolderMyAsync(daoFactory);
        file.Title = inputData.OutputFileName;

        using var request = new HttpRequestMessage();
        request.RequestUri = fileUri;

#pragma warning disable CA2000
        var httpClient = clientFactory.CreateClient();
#pragma warning restore CA2000

        using var response = await httpClient.SendAsync(request);
        await using var stream = await response.Content.ReadAsStreamAsync();

        var fileDao = daoFactory.GetFileDao<int>();

        file.ContentLength = stream.Length;

        file = await fileDao.SaveFileAsync(file, stream);
        await socketManager.CreateFileAsync(file);

        var messageService = serviceProvider.GetService<MessageService>();

        var headers = _data.Headers != null
            ? _data.Headers.ToDictionary(x => x.Key, x => new StringValues(x.Value))
            : [];

        messageService.SendHeadersMessage(MessageAction.CustomerOperationsReportDownloaded, target: null, httpHeaders: headers, null);

        if (System.IO.File.Exists(inputData.Script))
        {
            System.IO.File.Delete(inputData.Script);
        }

        return file;
    }

    private static async Task<(string scriptFilePath, string tempFileName, string outputFileName)> GetCustomerServiceUsageReportData(IServiceProvider serviceProvider, Guid userId, CustomerServiceUsageReportTaskData taskData)
    {
        var tenantManager = serviceProvider.GetService<TenantManager>();
        var tariffService = serviceProvider.GetService<TariffService>();
        var userManager = serviceProvider.GetService<UserManager>();
        var tenantUtil = serviceProvider.GetService<TenantUtil>();
        var quotaService = serviceProvider.GetService<IQuotaService>();
        var tempPath = serviceProvider.GetService<TempPath>();

        var tenant = tenantManager.GetCurrentTenant();

        var user = await userManager.GetUsersAsync(userId);

        var userCulture = user.GetCulture();
        CultureInfo.CurrentCulture = userCulture;
        CultureInfo.CurrentUICulture = userCulture;

        var utcStartDate = tenantUtil.DateTimeToUtc(taskData.StartDate ?? tenant.CreationDateTime);
        var utcEndDate = tenantUtil.DateTimeToUtc(taskData.EndDate ?? DateTime.UtcNow);

        var script = await DocumentBuilderScriptHelper.ReadTemplateFromEmbeddedResource(ScriptName) ?? throw new Exception("Template not found");

        var scriptFilePath = tempPath.GetTempFileName(".docbuilder");
        var tempFileName = DocumentBuilderScriptHelper.GetTempFileName(".xlsx");
        var outputFileName = string.Format(Resource.AccountingServiceUsageReportName + ".xlsx", utcStartDate.ToShortDateString(), utcEndDate.ToShortDateString());

        var keys = new List<string> {
            Resource.AccountingCustomerOperationService,
            Resource.AccountingCustomerOperationQuantity,
            Resource.AccountingCustomerOperationServiceUnit,
            Resource.AccountingCustomerOperationDebit,
            Resource.AccountingCustomerOperationCurrency
        };

        // For ai-tools, usage is displayed in Tokens instead of AI Credits.
        var customUom = new Dictionary<string, string>();
        var aiQuota = await quotaService.GetTenantQuotaAsync((int)TenantWalletService.AITools);
        if (aiQuota != null)
        {
            customUom.Add(aiQuota.ServiceName, "chat");
        }

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        script = script
            .Replace("${sheetName}", Resource.AccountingServiceUsageReportSheetName)
            .Replace("${tempFileName}", tempFileName)
            .Replace("${dataKeys}", JsonSerializer.Serialize(keys));

        var scriptParts = script.Split("${dataValues}");

        await using (var writer = new StreamWriter(scriptFilePath))
        {
            await writer.WriteAsync(scriptParts[0]);

            var filter = new UsageFilter
            {
                ServiceName = taskData.ServiceName,
                ParticipantName = taskData.ParticipantName,
                Status = taskData.Status,
                UtcStartDate = utcStartDate,
                UtcEndDate = utcEndDate,
                Metadata = taskData.Metadata,
                OrderBy = taskData.OrderBy,
                OrderType = taskData.OrderType
            };

            var partialRecords = GetCustomerServiceUsageReportDataAsync(tariffService, tenant.Id, filter);

            if (partialRecords != null)
            {
                await foreach (var records in partialRecords)
                {
                    if (records is not { Count: > 0 })
                    {
                        continue;
                    }

                    var text = Serialize(records, customUom, options);
                    await writer.WriteAsync(text);
                }
            }

            await writer.WriteAsync(scriptParts[1]);
        }

        return (scriptFilePath, tempFileName, outputFileName);
    }

    private static async IAsyncEnumerable<List<CustomerServiceUsage>> GetCustomerServiceUsageReportDataAsync(
        TariffService tariffService,
        int tenantId,
        UsageFilter filter)
    {
        var offset = 0;
        var limit = 1000;

        while (true)
        {
            filter.Offset = offset;
            filter.Limit = limit;

            var report = await tariffService.GetCustomerServiceUsageAsync(tenantId, filter);

            if (report?.Collection == null)
            {
                yield return null;
                break;
            }

            yield return report.Collection;

            if (report.CurrentPage >= report.TotalPage)
            {
                break;
            }

            offset += limit;
        }
    }

    private static string Serialize(List<CustomerServiceUsage> records, Dictionary<string, string> customUom, JsonSerializerOptions jsonSerializerOptions)
    {
        var sb = new StringBuilder();

        foreach (var record in records)
        {
            var serviceName = record.Service;

            // for testing purposes
            if (serviceName != null && serviceName.EndsWith("-1-hour"))
            {
                serviceName = serviceName.Replace("-1-hour", "");
            }

            var unit = customUom.GetValueOrDefault(serviceName, serviceName);

            var title = Resource.ResourceManager.GetString($"AccountingCustomerOperationServiceDesc_{serviceName}");
            var serviceUnit = Resource.ResourceManager.GetString($"AccountingCustomerOperationServiceUOM_{unit}");

            var properties = new List<PropertyValue>
            {
                new(title, "@"),
                new(record.TotalQuantity.ToString(CultureInfo.InvariantCulture), "General", "right"),
                new(serviceUnit, "@"),
                new(record.TotalAmount.ToString(CultureInfo.InvariantCulture), "0.0000000000", "right"),
                new(record.Currency, "@")
            };

            _ = sb.AppendLine(JsonSerializer.Serialize(properties, jsonSerializerOptions) + ",");
        }

        return sb.ToString();
    }

    private record PropertyValue(string Value, string Format, string Halign = null);
}

public record CustomerServiceUsageReportTaskData(
    IDictionary<string, string> Headers,
    string ServiceName,
    DateTime? StartDate,
    DateTime? EndDate,
    string ParticipantName,
    OperationStatus? Status,
    Dictionary<string, string> Metadata,
    string OrderBy,
    OperationOrderType? OrderType
);

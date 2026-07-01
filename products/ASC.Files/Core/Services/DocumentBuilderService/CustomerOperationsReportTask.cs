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

    private const string ScriptName = "CustomerOperationsReport.docbuilder";

    protected override async Task<DocumentBuilderInputData> GetDocumentBuilderInputDataAsync(IServiceProvider serviceProvider)
    {
        return _data.ReportType switch
        {
            ReportType.Operations => await BuildOperationsReportAsync(serviceProvider, _userId, _data),
            ReportType.ServiceUsage => await BuildServiceUsageReportAsync(serviceProvider, _userId, _data),
            ReportType.MonthlyUsage => await BuildMonthlyUsageReportAsync(serviceProvider, _userId, _data),
            _ => throw new ArgumentOutOfRangeException(nameof(serviceProvider), _data.ReportType, "Unknown report type")
        };
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

    private static async Task<TenantWalletService?> GetTenantWalletService(TenantManager tenantManager, string serviceName)
    {
        if (string.IsNullOrEmpty(serviceName))
        {
            return null;
        }

        var quotaList = await tenantManager.GetTenantQuotasAsync(all: false, wallet: true);

        var selectedQuota = quotaList.FirstOrDefault(x =>
            x.ServiceName.Equals(serviceName, StringComparison.InvariantCultureIgnoreCase));

        if (selectedQuota != null && Enum.IsDefined(typeof(TenantWalletService), selectedQuota.TenantId))
        {
            return (TenantWalletService)selectedQuota.TenantId;
        }

        return null;
    }

    // Carries the data shared by every report flavour, computed once in RenderAsync.
    private sealed record RenderContext(
        IServiceProvider ServiceProvider,
        Tenant Tenant,
        CultureInfo Culture,
        DateTime UtcStartDate,
        DateTime UtcEndDate,
        JsonSerializerOptions Options);

    // Common scaffolding for all report types: resolve tenant/user, read the template,
    // substitute the header placeholders, then let the report-specific delegate stream the rows.
    private static async Task<DocumentBuilderInputData> RenderAsync(
        IServiceProvider serviceProvider,
        Guid userId,
        CustomerOperationsReportTaskData taskData,
        string sheetName,
        string outputFileNameFormat,
        List<string> keys,
        Func<StreamWriter, RenderContext, Task> writeValues)
    {
        var tenantManager = serviceProvider.GetService<TenantManager>();
        var userManager = serviceProvider.GetService<UserManager>();
        var tenantUtil = serviceProvider.GetService<TenantUtil>();
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
        var outputFileName = string.Format(outputFileNameFormat + ".xlsx", utcStartDate.ToShortDateString(), utcEndDate.ToShortDateString());

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        script = script
            .Replace("${sheetName}", sheetName)
            .Replace("${tempFileName}", tempFileName)
            .Replace("${dataKeys}", JsonSerializer.Serialize(keys));

        var scriptParts = script.Split("${dataValues}");

        await using (var writer = new StreamWriter(scriptFilePath))
        {
            await writer.WriteAsync(scriptParts[0]);

            var context = new RenderContext(serviceProvider, tenant, userCulture, utcStartDate, utcEndDate, options);

            await writeValues(writer, context);

            await writer.WriteAsync(scriptParts[1]);
        }

        return new DocumentBuilderInputData(scriptFilePath, tempFileName, outputFileName);
    }

    private static async Task<DocumentBuilderInputData> BuildOperationsReportAsync(IServiceProvider serviceProvider, Guid userId, CustomerOperationsReportTaskData taskData)
    {
        var tenantManager = serviceProvider.GetService<TenantManager>();

        var keys = new List<string> {
            Resource.AccountingCustomerOperationDate,
            Resource.AccountingCustomerOperationType,
            Resource.AccountingCustomerOperationDetails,
            Resource.AccountingCustomerOperationContact,
            Resource.AccountingCustomerOperationQuantity,
            Resource.AccountingCustomerOperationServiceUnit,
            Resource.AccountingCustomerOperationCredit,
            Resource.AccountingCustomerOperationDebit,
            Resource.AccountingCustomerOperationCurrency
        };

        var tenantWalletService = await GetTenantWalletService(tenantManager, taskData.ServiceName);
        var addAgentColumn = tenantWalletService is TenantWalletService.AITools;
        if (addAgentColumn)
        {
            keys.Add(Resource.AccountingCustomerOperationAgent);
        }

        return await RenderAsync(serviceProvider, userId, taskData,
            Resource.AccountingCustomerOperationsReportSheetName,
            Resource.AccountingCustomerOperationsReportName,
            keys,
            async (writer, ctx) =>
            {
                var tariffService = ctx.ServiceProvider.GetService<TariffService>();
                var tenantUtil = ctx.ServiceProvider.GetService<TenantUtil>();
                var displayUserSettingsHelper = ctx.ServiceProvider.GetService<DisplayUserSettingsHelper>();

                var dateFormat = $"{ctx.Culture.DateTimeFormat.ShortDatePattern} {ctx.Culture.DateTimeFormat.ShortTimePattern.Replace("tt", "AM/PM")}";

                var filter = new OperationFilter
                {
                    ServiceName = taskData.ServiceName,
                    UtcStartDate = ctx.UtcStartDate,
                    UtcEndDate = ctx.UtcEndDate,
                    ParticipantName = taskData.ParticipantName,
                    Credit = taskData.Credit,
                    Debit = taskData.Debit,
                    Type = taskData.Type,
                    Status = taskData.Status,
                    OrderBy = taskData.OrderBy,
                    OrderType = taskData.OrderType
                };

                await foreach (var records in GetCustomerOperationsReportDataAsync(tariffService, tenantUtil, displayUserSettingsHelper, ctx.Tenant.Id, filter))
                {
                    if (records is not { Count: > 0 })
                    {
                        continue;
                    }

                    await writer.WriteAsync(SerializeOperations(records, dateFormat, ctx.Options, addAgentColumn));
                }
            });
    }

    private static async Task<DocumentBuilderInputData> BuildServiceUsageReportAsync(IServiceProvider serviceProvider, Guid userId, CustomerOperationsReportTaskData taskData)
    {
        var keys = new List<string> {
            Resource.AccountingCustomerOperationService,
            Resource.AccountingCustomerOperationQuantity,
            Resource.AccountingCustomerOperationServiceUnit,
            Resource.AccountingCustomerOperationDebit,
            Resource.AccountingCustomerOperationCurrency
        };

        return await RenderAsync(serviceProvider, userId, taskData,
            Resource.AccountingServiceUsageReportSheetName,
            Resource.AccountingServiceUsageReportName,
            keys,
            async (writer, ctx) =>
            {
                var tariffService = ctx.ServiceProvider.GetService<TariffService>();
                var quotaService = ctx.ServiceProvider.GetService<IQuotaService>();

                // For ai-tools, usage is displayed in Tokens instead of AI Credits.
                var customUom = new Dictionary<string, string>();
                var aiQuota = await quotaService.GetTenantQuotaAsync((int)TenantWalletService.AITools);
                if (aiQuota != null)
                {
                    customUom.Add(aiQuota.ServiceName, "chat");
                }

                var filter = new UsageFilter
                {
                    ServiceName = taskData.ServiceName,
                    ParticipantName = taskData.ParticipantName,
                    Status = taskData.Status,
                    UtcStartDate = ctx.UtcStartDate,
                    UtcEndDate = ctx.UtcEndDate,
                    Metadata = taskData.Metadata,
                    OrderBy = taskData.OrderBy,
                    OrderType = taskData.OrderType
                };

                await foreach (var records in GetCustomerServiceUsageReportDataAsync(tariffService, ctx.Tenant.Id, filter))
                {
                    if (records is not { Count: > 0 })
                    {
                        continue;
                    }

                    await writer.WriteAsync(SerializeServiceUsage(records, customUom, ctx.Options));
                }
            });
    }

    private static async Task<DocumentBuilderInputData> BuildMonthlyUsageReportAsync(IServiceProvider serviceProvider, Guid userId, CustomerOperationsReportTaskData taskData)
    {
        var keys = new List<string> {
            Resource.AccountingCustomerOperationMonth,
            Resource.AccountingCustomerOperationDebit,
            Resource.AccountingCustomerOperationCurrency
        };

        return await RenderAsync(serviceProvider, userId, taskData,
            Resource.AccountingMonthlyUsageReportSheetName,
            Resource.AccountingMonthlyUsageReportName,
            keys,
            async (writer, ctx) =>
            {
                var tariffService = ctx.ServiceProvider.GetService<TariffService>();

                var filter = new MonthlyUsageFilter
                {
                    UtcStartDate = ctx.UtcStartDate,
                    UtcEndDate = ctx.UtcEndDate
                };

                var records = await tariffService.GetCustomerMonthlyUsageAsync(ctx.Tenant.Id, filter);

                if (records is { Count: > 0 })
                {
                    await writer.WriteAsync(SerializeMonthly(records, ctx.Culture, ctx.Options));
                }
            });
    }

    private static async IAsyncEnumerable<List<Operation>> GetCustomerOperationsReportDataAsync(
        TariffService tariffService,
        TenantUtil tenantUtil,
        DisplayUserSettingsHelper displayUserSettingsHelper,
        int tenantId,
        OperationFilter filter)
    {
        var offset = 0;
        var limit = 1000;

        while (true)
        {
            filter.Offset = offset;
            filter.Limit = limit;

            var report = await tariffService.GetCustomerOperationsAsync(tenantId, filter);

            if (report?.Collection == null)
            {
                yield return null;
                break;
            }

            var participantDisplayNames = await report.GetParticipantDisplayNamesAsync(displayUserSettingsHelper, false);

            foreach (var operation in report.Collection)
            {
                var (description, unitOfMeasurement, quantity) = WalletServiceDescriptionManager.GetServiceDescriptionAndUom(operation, filter.ServiceName, operation.Metadata);
                var (agentId, agentTitle) = WalletServiceDescriptionManager.GetAgentInfo(operation.Metadata);

                operation.Description = description;
                operation.Details = WalletServiceDescriptionManager.GetServiceDetails(operation.Metadata);
                operation.ServiceUnit = unitOfMeasurement;
                operation.Quantity = quantity;
                operation.Date = tenantUtil.DateTimeFromUtc(operation.Date);
                operation.ParticipantDisplayName = operation.ParticipantName != null && participantDisplayNames.TryGetValue(operation.ParticipantName, out var value)
                    ? value
                    : operation.ParticipantName;
                operation.AgentId = agentId;
                operation.AgentTitle = agentTitle;
            }

            yield return report.Collection;

            if (report.CurrentPage >= report.TotalPage)
            {
                break;
            }

            offset += limit;
        }
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

    private static string SerializeOperations(List<Operation> records, string dateFormat, JsonSerializerOptions jsonSerializerOptions, bool addAgentColumn)
    {
        var sb = new StringBuilder();

        foreach (var record in records)
        {
            var properties = new List<PropertyValue>
            {
                new(record.Date.ToString("G", CultureInfo.InvariantCulture), dateFormat),
                new(record.Description, "@"),
                new(record.Details, "@"),
                new(record.ParticipantDisplayName, "@"),
                new(record.Quantity.ToString(CultureInfo.InvariantCulture), "General", "right"),
                new(record.ServiceUnit, "@"),
                new(record.Credit.ToString(CultureInfo.InvariantCulture), "0.0000000000", "right"),
                new(record.Debit.ToString(CultureInfo.InvariantCulture), "0.0000000000", "right"),
                new(record.Currency, "@")
            };

            if (addAgentColumn)
            {
                properties.Add(new PropertyValue(record.AgentTitle, "@"));
            }

            _ = sb.AppendLine(JsonSerializer.Serialize(properties, jsonSerializerOptions) + ",");
        }

        return sb.ToString();
    }

    private static string SerializeServiceUsage(List<CustomerServiceUsage> records, Dictionary<string, string> customUom, JsonSerializerOptions jsonSerializerOptions)
    {
        var sb = new StringBuilder();

        foreach (var record in records)
        {
            var (_, title, serviceUnit) = WalletServiceDescriptionManager.GetServiceTitleAndUom(record.Service, customUom);

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

    private static string SerializeMonthly(List<CustomerMonthlyUsage> records, CultureInfo culture, JsonSerializerOptions jsonSerializerOptions)
    {
        var sb = new StringBuilder();

        foreach (var record in records)
        {
            var month = new DateTime(record.Year, record.Month, 1).ToString("MMMM yyyy", culture);

            var properties = new List<PropertyValue>
            {
                new(month, "@"),
                new(record.TotalAmount.ToString(CultureInfo.InvariantCulture), "0.0000000000", "right"),
                new(record.Currency, "@")
            };

            _ = sb.AppendLine(JsonSerializer.Serialize(properties, jsonSerializerOptions) + ",");
        }

        return sb.ToString();
    }

    private record PropertyValue(string Value, string Format, string Halign = null);
}

public record CustomerOperationsReportTaskData(
    IDictionary<string, string> Headers,
    ReportType ReportType,
    string ServiceName,
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

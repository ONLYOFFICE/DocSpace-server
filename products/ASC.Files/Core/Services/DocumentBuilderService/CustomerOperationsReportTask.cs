// (c) Copyright Ascensio System SIA 2009-2026
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
        var (scriptFilePath, tempFileName, outputFileName) = await GetCustomerOperationsReportData(serviceProvider, _userId, _data);

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

    private static async Task<TenantWalletService?> GetTenantWalletService(TenantManager tenantManager, string serviceName)
    {
        if (string.IsNullOrEmpty(serviceName))
        {
            return null;
        }

        var quotaList = await tenantManager.GetTenantQuotasAsync(true, true);

        var selectedQuota = quotaList.FirstOrDefault(x =>
            x.ServiceName.Equals(serviceName, StringComparison.InvariantCultureIgnoreCase));

        if (selectedQuota != null && Enum.IsDefined(typeof(TenantWalletService), selectedQuota.TenantId))
        {
            return (TenantWalletService)selectedQuota.TenantId;
        }

        return null;
    }

    private static async Task<(string scriptFilePath, string tempFileName, string outputFileName)> GetCustomerOperationsReportData(IServiceProvider serviceProvider, Guid userId, CustomerOperationsReportTaskData taskData)
    {
        var tenantManager = serviceProvider.GetService<TenantManager>();
        var tariffService = serviceProvider.GetService<TariffService>();
        var userManager = serviceProvider.GetService<UserManager>();
        var tenantUtil = serviceProvider.GetService<TenantUtil>();
        var displayUserSettingsHelper = serviceProvider.GetService<DisplayUserSettingsHelper>();
        var tenantLogoManager = serviceProvider.GetService<TenantLogoManager>();
        var tempPath = serviceProvider.GetService<TempPath>();

        var tenant = tenantManager.GetCurrentTenant();

        var user = await userManager.GetUsersAsync(userId);

        var usertCulture = user.GetCulture();
        CultureInfo.CurrentCulture = usertCulture;
        CultureInfo.CurrentUICulture = usertCulture;

        var utcStartDate = tenantUtil.DateTimeToUtc(taskData.StartDate ?? tenant.CreationDateTime);
        var utcEndDate = tenantUtil.DateTimeToUtc(taskData.EndDate ?? DateTime.UtcNow);

        var script = await DocumentBuilderScriptHelper.ReadTemplateFromEmbeddedResource(ScriptName) ?? throw new Exception("Template not found");

        var scriptFilePath = tempPath.GetTempFileName(".docbuilder");
        var tempFileName = DocumentBuilderScriptHelper.GetTempFileName(".xlsx");
        var outputFileName = string.Format(Resource.AccountingCustomerOperationsReportName + ".xlsx", utcStartDate.ToShortDateString(), utcEndDate.ToShortDateString());

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

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        script = script
            .Replace("${sheetName}", Resource.AccountingCustomerOperationsReportSheetName)
            .Replace("${tempFileName}", tempFileName)
            .Replace("${dataKeys}", JsonSerializer.Serialize(keys));

        var scriptParts = script.Split("${dataValues}");

        var dateFormat = $"{usertCulture.DateTimeFormat.ShortDatePattern} {usertCulture.DateTimeFormat.ShortTimePattern.Replace("tt", "AM/PM")}";

        await using (var writer = new StreamWriter(scriptFilePath))
        {
            await writer.WriteAsync(scriptParts[0]);

            var filter = new OperationFilter
            {
                ServiceName = taskData.ServiceName,
                WriteOffServiceQuota = taskData.WriteOffServiceQuota,
                UtcStartDate = utcStartDate,
                UtcEndDate = utcEndDate,
                ParticipantName = taskData.ParticipantName,
                Credit = taskData.Credit,
                Debit = taskData.Debit,
                Types = taskData.Types,
                Status = taskData.Status,
                OrderBy = taskData.OrderBy,
                OrderType = taskData.OrderType
            };

            var partialRecords = GetCustomerOperationsReportDataAsync(
                tariffService,
                tenantUtil,
                displayUserSettingsHelper,
                tenantLogoManager,
                tenant.Id,
                filter);

            if (partialRecords != null)
            {
                await foreach (var records in partialRecords)
                {
                    if (records is not { Count: > 0 })
                    {
                        continue;
                    }

                    var text = Serialize(records, dateFormat, options, addAgentColumn);
                    await writer.WriteAsync(text);
                }
            }

            await writer.WriteAsync(scriptParts[1]);
        }

        return (scriptFilePath, tempFileName, outputFileName);
    }

    private static async IAsyncEnumerable<List<Operation>> GetCustomerOperationsReportDataAsync(
        TariffService tariffService,
        TenantUtil tenantUtil,
        DisplayUserSettingsHelper displayUserSettingsHelper,
        TenantLogoManager tenantLogoManager,
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
            var logoText = await tenantLogoManager.GetLogoTextAsync();

            foreach (var operation in report.Collection)
            {
                var (description, unitOfMeasurement, quantity) = WalletServiceDescriptionManager.GetServiceDescriptionAndUom(operation, filter.ServiceName, operation.Metadata, logoText);
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

    private static string Serialize(List<Operation> records, string dateFormat, JsonSerializerOptions jsonSerializerOptions, bool addAgentColumn)
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
                new(record.Quantity.ToString(), "General", "right"),
                new(record.ServiceUnit, "@"),
                new(record.Credit.ToString(), "0.0000000000", "right"),
                new(record.Debit.ToString(), "0.0000000000", "right"),
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

    private record PropertyValue(string Value, string Format, string Halign = null);
}

public record CustomerOperationsReportTaskData(
    IDictionary<string, string> Headers,
    string ServiceName,
    bool WriteOffServiceQuota,
    DateTime? StartDate,
    DateTime? EndDate,
    string ParticipantName,
    bool? Credit,
    bool? Debit,
    OperationType? Types,
    OperationStatus? Status,
    string OrderBy,
    OperationOrderType? OrderType
);

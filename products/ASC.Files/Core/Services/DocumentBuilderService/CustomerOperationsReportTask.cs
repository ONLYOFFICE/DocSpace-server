// (c) Copyright Ascensio System SIA 2009-2025
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

        using var httpClient = clientFactory.CreateClient();
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

    private static async Task<(string scriptFilePath, string tempFileName, string outputFileName)> GetCustomerOperationsReportData(IServiceProvider serviceProvider, Guid userId, CustomerOperationsReportTaskData taskData)
    {
        var tenantManager = serviceProvider.GetService<TenantManager>();
        var tariffService = serviceProvider.GetService<TariffService>();
        var userManager = serviceProvider.GetService<UserManager>();
        var tenantUtil = serviceProvider.GetService<TenantUtil>();
        var tempPath = serviceProvider.GetService<TempPath>();

        var tenant = tenantManager.GetCurrentTenant();

        var user = await userManager.GetUsersAsync(userId);

        var usertCulture = user.GetCulture();
        CultureInfo.CurrentCulture = usertCulture;
        CultureInfo.CurrentUICulture = usertCulture;

        var utcStartDate = taskData.StartDate != null ? tenantUtil.DateTimeToUtc(taskData.StartDate.Value) : tenant.CreationDateTime;
        var utcEndDate = taskData.EndDate != null ? tenantUtil.DateTimeToUtc(taskData.EndDate.Value) : DateTime.UtcNow;

        var script = await DocumentBuilderScriptHelper.ReadTemplateFromEmbeddedResource(ScriptName) ?? throw new Exception("Template not found");

        var scriptFilePath = tempPath.GetTempFileName(".docbuilder");
        var tempFileName = DocumentBuilderScriptHelper.GetTempFileName(".xlsx");
        var outputFileName = string.Format(Resource.AccountingCustomerOperationsReportName + ".xlsx",
            utcStartDate.ToString(usertCulture.DateTimeFormat.ShortDatePattern, CultureInfo.InvariantCulture),
            utcEndDate.ToString(usertCulture.DateTimeFormat.ShortDatePattern, CultureInfo.InvariantCulture));

        var keys = new[] {
            Resource.AccountingCustomerOperationDate,
            Resource.AccountingCustomerOperationDescription,
            Resource.AccountingCustomerOperationService,
            Resource.AccountingCustomerOperationServiceUnit,
            Resource.AccountingCustomerOperationQuantity,
            Resource.AccountingCustomerOperationCurrency,
            Resource.AccountingCustomerOperationCredit,
            Resource.AccountingCustomerOperationWithdrawal
        };

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        script = script
            .Replace("${sheetName}", Resource.AccountingCustomerOperationsReportSheetName)
            .Replace("${tempFileName}", tempFileName)
            .Replace("${dataKeys}", JsonSerializer.Serialize(keys));

        var scriptParts = script.Split("${dataValues}");

        var dateFormat = $"{usertCulture.DateTimeFormat.ShortDatePattern} {usertCulture.DateTimeFormat.ShortTimePattern.Replace("tt", "AM/PM")}";

        await using (var writer = new StreamWriter(scriptFilePath))
        {
            await writer.WriteAsync(scriptParts[0]);

            var partialRecords = GetCustomerOperationsReportDataAsync(tariffService, tenantUtil, tenant.Id, utcStartDate, utcEndDate, taskData.Credit, taskData.Withdrawal);

            if (partialRecords != null)
            {
                await foreach (var records in partialRecords)
                {
                    var text = Serialize(records, usertCulture, dateFormat, options);
                    await writer.WriteAsync(text);
                }
            }

            await writer.WriteAsync(scriptParts[1]);
        }

        return (scriptFilePath, tempFileName, outputFileName);
    }

    private static async IAsyncEnumerable<List<Operation>> GetCustomerOperationsReportDataAsync(TariffService tariffService, TenantUtil tenantUtil, int tenantId, DateTime utcStartDate, DateTime utcEndDate, bool? credit, bool? withdrawal)
    {
        var offset = 0;
        var limit = 1000;

        while (true)
        {
            var report = await tariffService.GetCustomerOperationsAsync(tenantId, utcStartDate, utcEndDate, credit, withdrawal, offset, limit);

            if (report?.Collection == null)
            {
                yield return null;
                break;
            }

            foreach (var operation in report.Collection)
            {
                operation.Description = GetServiceDesc(operation.Service);
                operation.Date = tenantUtil.DateTimeFromUtc(operation.Date);

                if (string.IsNullOrEmpty(operation.Service))
                {
                    operation.Quantity = 0;
                }
            }

            yield return report.Collection;

            if (report.CurrentPage == report.TotalPage)
            {
                break;
            }

            offset += limit;
        }
    }

    private static string Serialize(List<Operation> records, CultureInfo culture, string dateFormat, JsonSerializerOptions jsonSerializerOptions)
    {
        var sb = new StringBuilder();

        foreach (var record in records)
        {
            var properties = new List<PropertyValue>
            {
                new(record.Date.ToString("G", culture), dateFormat),
                new(record.Description, "@"),
                new(record.Service, "@"),
                new(record.ServiceUnit, "@"),
                new(record.Quantity.ToString(), "General"),
                new(record.Currency, "@"),
                new(record.Credit.ToString(), "0.0000000000"),
                new(record.Withdrawal.ToString(), "0.0000000000")
            };

            _ = sb.AppendLine(JsonSerializer.Serialize(properties, jsonSerializerOptions) + ",");
        }

        return sb.ToString();
    }

    private static string GetServiceDesc(string serviceName)
    {
        if (serviceName != null && serviceName.StartsWith("disk-storage"))
        {
            serviceName = "disk-storage";
        }

        return Resource.ResourceManager.GetString("AccountingCustomerOperationServiceDesc_" + (serviceName ?? "top-up"));
    }

    record PropertyValue(string Value, string Format);
}

public record CustomerOperationsReportTaskData(IDictionary<string, string> Headers, DateTime? StartDate, DateTime? EndDate, bool? Credit, bool? Withdrawal);

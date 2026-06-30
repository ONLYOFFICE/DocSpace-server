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
public class CustomerMonthlyUsageReportTask : DocumentBuilderTask<int, CustomerMonthlyUsageReportTaskData>
{
    public CustomerMonthlyUsageReportTask()
    {
    }

    public CustomerMonthlyUsageReportTask(IServiceScopeFactory serviceProvider) : base(serviceProvider)
    {
    }

    private const string ScriptName = "MonthlyUsageReport.docbuilder";

    protected override async Task<DocumentBuilderInputData> GetDocumentBuilderInputDataAsync(IServiceProvider serviceProvider)
    {
        var (scriptFilePath, tempFileName, outputFileName) = await GetCustomerMonthlyUsageReportData(serviceProvider, _userId, _data);

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

    private static async Task<(string scriptFilePath, string tempFileName, string outputFileName)> GetCustomerMonthlyUsageReportData(IServiceProvider serviceProvider, Guid userId, CustomerMonthlyUsageReportTaskData taskData)
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

        var utcStartDate = tenantUtil.DateTimeToUtc(taskData.StartDate ?? tenant.CreationDateTime);
        var utcEndDate = tenantUtil.DateTimeToUtc(taskData.EndDate ?? DateTime.UtcNow);

        var script = await DocumentBuilderScriptHelper.ReadTemplateFromEmbeddedResource(ScriptName) ?? throw new Exception("Template not found");

        var scriptFilePath = tempPath.GetTempFileName(".docbuilder");
        var tempFileName = DocumentBuilderScriptHelper.GetTempFileName(".xlsx");
        var outputFileName = string.Format(Resource.AccountingMonthlyUsageReportName + ".xlsx", utcStartDate.ToShortDateString(), utcEndDate.ToShortDateString());

        var keys = new List<string> {
            Resource.AccountingCustomerOperationMonth,
            Resource.AccountingCustomerOperationDebit,
            Resource.AccountingCustomerOperationCurrency
        };

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        script = script
            .Replace("${sheetName}", Resource.AccountingMonthlyUsageReportSheetName)
            .Replace("${tempFileName}", tempFileName)
            .Replace("${dataKeys}", JsonSerializer.Serialize(keys));

        var scriptParts = script.Split("${dataValues}");

        await using (var writer = new StreamWriter(scriptFilePath))
        {
            await writer.WriteAsync(scriptParts[0]);

            var records = await tariffService.GetCustomerMonthlyUsageAsync(tenant.Id, utcStartDate, utcEndDate);

            if (records is { Count: > 0 })
            {
                var text = Serialize(records, usertCulture, options);
                await writer.WriteAsync(text);
            }

            await writer.WriteAsync(scriptParts[1]);
        }

        return (scriptFilePath, tempFileName, outputFileName);
    }

    private static string Serialize(List<CustomerMonthlyUsage> records, CultureInfo culture, JsonSerializerOptions jsonSerializerOptions)
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

public record CustomerMonthlyUsageReportTaskData(
    IDictionary<string, string> Headers,
    DateTime? StartDate,
    DateTime? EndDate
);

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
/// Aggregated usage grouped by service.
/// </summary>
[Scope]
public class ServiceUsageReportBuilder(
    TenantManager tenantManager,
    UserManager userManager,
    TenantUtil tenantUtil,
    TempPath tempPath,
    ReportHeaderService reportHeaderService,
    TariffService tariffService,
    IQuotaService quotaService)
    : CustomerReportBuilderBase(tenantManager, userManager, tenantUtil, tempPath, reportHeaderService)
{
    protected override Task<DocumentBuilderInputData> BuildCoreAsync(RenderContext context, CustomerOperationsReportTaskData taskData)
    {
        var columns = new List<ReportColumn>
        {
            new(Resource.AccountingCustomerOperationService),
            new(Resource.AccountingCustomerOperationQuantity, "right"),
            new(Resource.AccountingCustomerOperationServiceUnit),
            new(Resource.AccountingCustomerOperationDebit, "right", Sum: true),
            new(Resource.AccountingCustomerOperationCurrency, Currency: true)
        };

        var definition = new ReportDefinition(
            Resource.AccountingServiceUsageReportSheetName,
            Resource.AccountingServiceUsageReportSheetName,
            Resource.AccountingServiceUsageReportName,
            columns,
            async writer =>
            {
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
                    UtcStartDate = context.UtcStartDate,
                    UtcEndDate = context.UtcEndDate,
                    Metadata = taskData.Metadata,
                    OrderBy = taskData.OrderBy,
                    OrderType = taskData.OrderType
                };

                await foreach (var records in GetReportDataAsync(context.Tenant.Id, filter))
                {
                    if (records is not { Count: > 0 })
                    {
                        continue;
                    }

                    await writer.WriteAsync(SerializeServiceUsage(records, customUom, context.Options));
                }
            });

        return RenderAsync(context, definition);
    }

    private async IAsyncEnumerable<List<CustomerServiceUsage>> GetReportDataAsync(int tenantId, UsageFilter filter)
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

    private static string SerializeServiceUsage(List<CustomerServiceUsage> records, Dictionary<string, string> customUom, JsonSerializerOptions jsonSerializerOptions)
    {
        var sb = new StringBuilder();

        foreach (var record in records)
        {
            var (_, title, serviceUnit) = WalletServiceDescriptionManager.GetServiceTitleAndUom(record.Service, customUom);

            var properties = new List<PropertyValue>
            {
                new(title, "@"),
                new(record.TotalQuantity.ToString(CultureInfo.InvariantCulture), CountFormat, "right"),
                new(serviceUnit, "@"),
                new(record.TotalAmount.ToString(CultureInfo.InvariantCulture), MoneyFormat, "right"),
                new(record.Currency, "@")
            };

            _ = sb.AppendLine(JsonSerializer.Serialize(properties, jsonSerializerOptions) + ",");
        }

        return sb.ToString();
    }
}

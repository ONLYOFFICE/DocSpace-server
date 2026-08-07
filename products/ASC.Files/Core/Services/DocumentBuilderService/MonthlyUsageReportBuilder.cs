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
/// Aggregated usage grouped by month.
/// </summary>
[Scope]
public class MonthlyUsageReportBuilder(
    TenantManager tenantManager,
    UserManager userManager,
    TenantUtil tenantUtil,
    TempPath tempPath,
    ReportHeaderService reportHeaderService,
    TariffService tariffService)
    : CustomerReportBuilderBase(tenantManager, userManager, tenantUtil, tempPath, reportHeaderService)
{
    protected override Task<DocumentBuilderInputData> BuildCoreAsync(RenderContext context, CustomerOperationsReportTaskData taskData)
    {
        var columns = new List<ReportColumn>
        {
            new(Resource.AccountingCustomerOperationMonth),
            new(Resource.AccountingCustomerOperationDebit, "right", Sum: true),
            new(Resource.AccountingCustomerOperationCurrency, Currency: true)
        };

        var definition = new ReportDefinition(
            Resource.AccountingMonthlyUsageReportSheetName,
            Resource.AccountingMonthlyUsageReportSheetName,
            Resource.AccountingMonthlyUsageReportName,
            columns,
            async writer =>
            {
                var filter = new MonthlyUsageFilter
                {
                    UtcStartDate = context.UtcStartDate,
                    UtcEndDate = context.UtcEndDate
                };

                var records = await tariffService.GetCustomerMonthlyUsageAsync(context.Tenant.Id, filter);

                if (records is { Count: > 0 })
                {
                    await writer.WriteAsync(SerializeMonthly(records, context.Culture, context.Options));
                }
            });

        return RenderAsync(context, definition);
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
                new(record.TotalAmount.ToString(CultureInfo.InvariantCulture), MoneyFormat, "right"),
                new(record.Currency, "@")
            };

            _ = sb.AppendLine(JsonSerializer.Serialize(properties, jsonSerializerOptions) + ",");
        }

        return sb.ToString();
    }
}

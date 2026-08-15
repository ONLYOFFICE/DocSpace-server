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
/// Detailed operations (transactions) report - one row per wallet operation.
/// </summary>
[Scope]
public class OperationsReportBuilder(
    TenantManager tenantManager,
    UserManager userManager,
    TenantUtil tenantUtil,
    TempPath tempPath,
    ReportHeaderService reportHeaderService,
    TariffService tariffService,
    DisplayUserSettingsHelper displayUserSettingsHelper)
    : CustomerReportBuilderBase(tenantManager, userManager, tenantUtil, tempPath, reportHeaderService)
{
    protected override async Task<DocumentBuilderInputData> BuildCoreAsync(RenderContext context, CustomerOperationsReportTaskData taskData)
    {
        var columns = new List<ReportColumn>
        {
            new(Resource.AccountingCustomerOperationDate),
            new(Resource.AccountingCustomerOperationType),
            new(Resource.AccountingCustomerOperationDetails),
            new(Resource.AccountingCustomerOperationContact),
            new(Resource.AccountingCustomerOperationQuantity, "right"),
            new(Resource.AccountingCustomerOperationServiceUnit),
            new(Resource.AccountingCustomerOperationCredit, "right", Sum: true),
            new(Resource.AccountingCustomerOperationDebit, "right", Sum: true),
            new(Resource.AccountingCustomerOperationCurrency, Currency: true)
        };

        var tenantWalletService = taskData.ServiceName is { Count: 1 }
            ? await GetTenantWalletServiceAsync(taskData.ServiceName.First())
            : null;

        var addAgentColumn = tenantWalletService is TenantWalletService.AITools;
        if (addAgentColumn)
        {
            columns.Add(new ReportColumn(Resource.AccountingCustomerOperationAgent));
        }

        var dateFormat = context.Header.LongDateFormat;

        var definition = new ReportDefinition(
            Resource.AccountingCustomerOperationsReportSheetName,
            Resource.AccountingCustomerOperationsReportSheetName,
            Resource.AccountingCustomerOperationsReportName,
            columns,
            async writer =>
            {
                var filter = new OperationFilter
                {
                    ServiceName = taskData.ServiceName,
                    UtcStartDate = context.UtcStartDate,
                    UtcEndDate = context.UtcEndDate,
                    ParticipantName = taskData.ParticipantName,
                    Credit = taskData.Credit,
                    Debit = taskData.Debit,
                    Type = taskData.Type,
                    Status = taskData.Status,
                    OrderBy = taskData.OrderBy,
                    OrderType = taskData.OrderType
                };

                await foreach (var records in GetReportDataAsync(context.Tenant.Id, filter))
                {
                    if (records is not { Count: > 0 })
                    {
                        continue;
                    }

                    await writer.WriteAsync(SerializeOperations(records, dateFormat, context.Options, addAgentColumn));
                }
            });

        return await RenderAsync(context, definition);
    }

    private async Task<TenantWalletService?> GetTenantWalletServiceAsync(string serviceName)
    {
        if (string.IsNullOrEmpty(serviceName))
        {
            return null;
        }

        var quotaList = await TenantManager.GetTenantQuotasAsync(all: false, wallet: true);

        var selectedQuota = quotaList.FirstOrDefault(x =>
            x.ServiceName.Equals(serviceName, StringComparison.InvariantCultureIgnoreCase));

        if (selectedQuota != null && Enum.IsDefined(typeof(TenantWalletService), selectedQuota.TenantId))
        {
            return (TenantWalletService)selectedQuota.TenantId;
        }

        return null;
    }

    private async IAsyncEnumerable<List<Operation>> GetReportDataAsync(int tenantId, OperationFilter filter)
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
                var (description, unitOfMeasurement, quantity) = WalletServiceDescriptionManager.GetServiceDescriptionAndUom(operation, operation.Metadata);
                var (agentId, agentTitle) = WalletServiceDescriptionManager.GetAgentInfo(operation.Metadata);

                operation.Description = description;
                operation.Details = WalletServiceDescriptionManager.GetServiceDetails(operation.Metadata);
                operation.ServiceUnit = unitOfMeasurement;
                operation.Quantity = quantity;
                operation.Date = TenantUtil.DateTimeFromUtc(operation.Date);
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

    private static string SerializeOperations(List<Operation> records, string dateFormat, JsonSerializerOptions jsonSerializerOptions, bool addAgentColumn)
    {
        var sb = new StringBuilder();

        foreach (var record in records)
        {
            var properties = new List<PropertyValue>
            {
                new(record.Date.ConvertNumerals("G"), dateFormat),
                new(record.Description, "@"),
                new(record.Details, "@"),
                new(record.ParticipantDisplayName, "@"),
                new(record.Quantity.ToString(CultureInfo.InvariantCulture), CountFormat, "right"),
                new(record.ServiceUnit, "@"),
                new(record.Credit.ToString(CultureInfo.InvariantCulture), MoneyFormat, "right"),
                new(record.Debit.ToString(CultureInfo.InvariantCulture), MoneyFormat, "right"),
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
}

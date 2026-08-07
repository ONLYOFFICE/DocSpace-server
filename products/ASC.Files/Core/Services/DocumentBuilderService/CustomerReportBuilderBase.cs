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
/// Builds the document-builder script for one flavour of the customer report.
/// One implementation per <see cref="ReportType"/>.
/// </summary>
public interface ICustomerReportBuilder
{
    Task<DocumentBuilderInputData> BuildAsync(Guid userId, CustomerOperationsReportTaskData data);
}

/// Carries the data shared by every report flavour, computed once before the report is built.
public sealed record RenderContext(
    Tenant Tenant,
    CultureInfo Culture,
    DateTime UtcStartDate,
    DateTime UtcEndDate,
    JsonSerializerOptions Options,
    ReportHeader Header);

/// A single report column: its localized header, horizontal alignment, and whether it participates
/// in the total row (summed, or the currency echoed next to the sums).
public sealed record ReportColumn(string Header, string Align = "left", bool Sum = false, bool Currency = false);

/// The report-specific pieces, resolved after the user's culture is applied so that
/// the sheet name, report title, file name and column headers are localized correctly.
public sealed record ReportDefinition(
    string SheetName,
    string ReportTitle,
    string OutputFileNameFormat,
    List<ReportColumn> Columns,
    Func<StreamWriter, Task> WriteValues);

/// <summary>
/// Common scaffolding for all customer report types: resolves tenant/user, applies the user's
/// culture, assembles the shared header block and writes the ready-to-run builder script.
/// </summary>
public abstract class CustomerReportBuilderBase(
    TenantManager tenantManager,
    UserManager userManager,
    TenantUtil tenantUtil,
    TempPath tempPath,
    ReportHeaderService reportHeaderService) : ICustomerReportBuilder
{
    private const string ScriptName = "CustomerOperationsReport.docbuilder";

    // Exposed as properties rather than captured primary-constructor parameters so that derived
    // builders can reuse them without storing a second copy of the same dependency.
    protected TenantManager TenantManager { get; } = tenantManager;
    protected UserManager UserManager { get; } = userManager;
    protected TenantUtil TenantUtil { get; } = tenantUtil;

    // Money cells (credit, debit, amounts) and the total row share one high-precision format:
    // debits can be fractions of a cent, so all monetary values are shown with full precision.
    protected const string MoneyFormat = "#,##0.0000000000";

    // Whole-number cells (quantities, user counts).
    protected const string CountFormat = "#,##0";

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<DocumentBuilderInputData> BuildAsync(Guid userId, CustomerOperationsReportTaskData data)
    {
        var tenant = TenantManager.GetCurrentTenant();

        var user = await UserManager.GetUsersAsync(userId);

        // The culture is applied here, in the outermost frame, and never inside an awaited helper:
        // CurrentCulture is backed by an AsyncLocal, so a value assigned inside a helper is restored
        // when that helper returns and would never reach the header, the localized resources or the
        // row serialization below. Everything that depends on it runs further down this call chain.
        var userCulture = user.GetCulture();
        CultureInfo.CurrentCulture = userCulture;
        CultureInfo.CurrentUICulture = userCulture;

        var context = await CreateRenderContextAsync(tenant, userCulture, data);

        return await BuildCoreAsync(context, data);
    }

    /// Runs once the user's culture has been applied, so localized resources resolve correctly.
    protected abstract Task<DocumentBuilderInputData> BuildCoreAsync(RenderContext context, CustomerOperationsReportTaskData data);

    // Builds the shared date range and the header block. The header is built here, once, so that
    // every flavour shares one instance.
    private async Task<RenderContext> CreateRenderContextAsync(Tenant tenant, CultureInfo userCulture, CustomerOperationsReportTaskData taskData)
    {
        var utcStartDate = TenantUtil.DateTimeToUtc(taskData.StartDate ?? tenant.CreationDateTime);
        var utcEndDate = TenantUtil.DateTimeToUtc(taskData.EndDate ?? DateTime.UtcNow);

        var header = await reportHeaderService.BuildAsync(userCulture);

        return new RenderContext(tenant, userCulture, utcStartDate, utcEndDate, _jsonOptions, header);
    }

    // Scaffolding for the single-table report flavours: assembles the shared header block (logo,
    // company, report title, period, generation date), the theme colors used to style the sheet
    // and the column/total metadata, then streams the report rows.
    protected async Task<DocumentBuilderInputData> RenderAsync(RenderContext context, ReportDefinition definition)
    {
        var header = context.Header;

        var totalColumns = definition.Columns
            .Select((column, index) => (column, index))
            .Where(x => x.column.Sum)
            .Select(x => x.index)
            .ToList();

        var totalCurrencyColumn = definition.Columns.FindIndex(x => x.Currency);

        var localStartDate = TenantUtil.DateTimeFromUtc(context.UtcStartDate);
        var localEndDate = TenantUtil.DateTimeFromUtc(context.UtcEndDate);

        var inputData = new
        {
            resources = new
            {
                company = Resource.AccountingReportCompany + ":",
                report = Resource.AccountingReportTitle + ":",
                period = Resource.AccountingReportPeriod + ":",
                dateGenerated = Resource.AccountingReportDateGenerated + ":",
                total = Resource.AccountingReportTotal,
                sheetName = definition.SheetName,
                dateGeneratedFormat = header.LongDateFormat,
                totalFormat = MoneyFormat
            },
            info = new
            {
                company = header.Company,
                report = definition.ReportTitle,
                period = $"{localStartDate.ConvertNumerals("d")} – {localEndDate.ConvertNumerals("d")}",
                dateGenerated = header.DateGenerated
            },
            logoSrc = header.LogoSrc,
            themeColors = new
            {
                mainBgColor = header.MainBgColor,
                lightBgColor = header.LightBgColor,
                mainFontColor = header.MainFontColor
            },
            keys = definition.Columns.Select(x => x.Header).ToList(),
            aligns = definition.Columns.Select(x => x.Align).ToList(),
            totalColumns,
            totalCurrencyColumn
        };

        var outputFileName = string.Format(definition.OutputFileNameFormat + ".xlsx", context.UtcStartDate.ToShortDateString(), context.UtcEndDate.ToShortDateString());

        return await WriteReportScriptAsync(context, ScriptName, inputData, outputFileName, definition.WriteValues);
    }

    // Reads the embedded script template, injects the serialized input data and output name, optionally
    // streams the (potentially large) data rows in place of the ${dataValues} placeholder, and writes the
    // ready-to-run script to a temp file.
    protected async Task<DocumentBuilderInputData> WriteReportScriptAsync(
        RenderContext context,
        string scriptName,
        object inputData,
        string outputFileName,
        Func<StreamWriter, Task> writeValues = null)
    {
        var script = await DocumentBuilderScriptHelper.ReadTemplateFromEmbeddedResource(scriptName) ?? throw new Exception("Template not found");

        var scriptFilePath = tempPath.GetTempFileName(".docbuilder");
        var tempFileName = DocumentBuilderScriptHelper.GetTempFileName(".xlsx");

        script = script
            .Replace("${inputData}", JsonSerializer.Serialize(inputData, context.Options))
            .Replace("${tempFileName}", tempFileName);

        await using (var writer = new StreamWriter(scriptFilePath))
        {
            if (writeValues != null)
            {
                var scriptParts = script.Split("${dataValues}");
                await writer.WriteAsync(scriptParts[0]);
                await writeValues(writer);
                await writer.WriteAsync(scriptParts[1]);
            }
            else
            {
                await writer.WriteAsync(script);
            }
        }

        return new DocumentBuilderInputData(scriptFilePath, tempFileName, outputFileName);
    }

    protected sealed record PropertyValue(string Value, string Format, string Halign = null);
}

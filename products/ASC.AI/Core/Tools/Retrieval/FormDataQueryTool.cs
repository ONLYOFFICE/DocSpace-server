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

namespace ASC.AI.Core.Tools.Retrieval;

[Scope]
public class FormDataQueryTool(
    ExternalDatabaseClient externalDatabaseClient,
    IDaoFactory daoFactory,
    FileSecurity fileSecurity,
    ILogger<FormDataQueryTool> logger)
{
    public const string Name = "query_form_data";

    public async Task<AIFunction?> InitAsync(int fileId, string tableName, long rowCount, IEnumerable<DbColumnDefinition> columns)
    {
        var fileDao = daoFactory.GetFileDao<int>();
        var file = await fileDao.GetFileAsync(fileId);
        if (file == null || !await fileSecurity.CanEditAsync(file))
        {
            return null;
        }

        var columnList = columns.ToList();
        var allowedColumns = columnList.Select(c => c.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var description = BuildDescription(tableName, rowCount, columnList);

        return AIFunctionFactory.Create(Function, new AIFunctionFactoryOptions
        {
            Name = Name,
            Description = description,
            SerializerOptions = FormDataToolHelpers.FlexibleJsonOptions
        });

        async Task<ToolResponse<string>> Function(
            [Description("Column names to include in the result. Always specify only the columns relevant to the question — do not request all columns unless explicitly needed. Pass as a JSON array: [\"col_a\",\"col_b\"], never as a JSON-encoded string.")] IEnumerable<string>? selectColumns = null,
            [Description("Filter conditions applied with AND. Each filter is a string: \"column_name OPERATOR value\" — e.g. \"col_status = approved\", \"col_age > 25\", \"col_name IS NULL\", \"col_status IN approved,pending\". Column-to-column comparison is also supported: \"col_start < col_end\". Operators: =, !=, <, >, <=, >=, LIKE, NOT LIKE, IS NULL, IS NOT NULL, IN, NOT IN. For IN/NOT IN the value is a comma-separated list: \"col_status IN approved,pending,rejected\". Omit the value for IS NULL / IS NOT NULL.")] IEnumerable<string>? filters = null,
            [Description("Primary column to sort results by.")] string? orderByColumn = null,
            [Description("Set to true to sort the primary column in descending order. Default: false.")] bool orderByDescending = false,
            [Description("Secondary column to sort by when rows have the same value in orderByColumn.")] string? thenByColumn = null,
            [Description("Set to true to sort the secondary column in descending order. Default: false.")] bool thenByDescending = false,
            [Description("Maximum number of rows to return (1–500). Default: 50.")] int limit = 50,
            [Description("Number of rows to skip for pagination. Default: 0.")] int offset = 0,
            [Description("Date-part filter conditions applied with AND. Each filter is a string: \"column_name DATE_PART OPERATOR value[,v2,...]\". DATE_PART: YEAR, MONTH, WEEK, DAYOFYEAR, QUARTER. Operators: =, !=, <, >, <=, >=, IN. Examples: \"col_date MONTH IN 6,7,8\", \"col_date YEAR = 2024\".")] IEnumerable<string>? datePartFilters = null,
            [Description("Filter on the difference between two date/datetime columns. Format: \"col_a col_b OPERATOR value [UNIT]\" — UNIT is optional and defaults to DAYS. Allowed units: DAYS, HOURS, MINUTES. Examples: \"col_start col_submitted < 7\" (fewer than 7 days apart), \"col_start col_submitted < 48 HOURS\" (fewer than 48 hours apart).")] string? dateDiffFilter = null)
        {
            try
            {
                var (normalFilters, autoDatePartFilters, autoDiff) = FormDataToolHelpers.ExtractDatePartFilters(filters);
                dateDiffFilter ??= autoDiff;
                var allDatePartFilters = (datePartFilters ?? []).Concat(autoDatePartFilters);

                var parsedFilters = normalFilters.Select(QueryFilter.Parse);
                var parsedDatePartFilters = allDatePartFilters.Select(DatePartFilter.Parse);
                var parsedDateDiffFilter = dateDiffFilter != null ? DateDiffFilter.Parse(dateDiffFilter) : null;
                var result = await externalDatabaseClient.QueryAsync(
                    tableName, allowedColumns, selectColumns, parsedFilters,
                    orderByColumn, orderByDescending, thenByColumn, thenByDescending, limit, offset,
                    parsedDatePartFilters, parsedDateDiffFilter);
                return new ToolResponse<string> { Data = result };
            }
            catch (Exception e)
            {
                logger.ErrorQueryFailed(e, tableName);
                return new ToolResponse<string> { Error = $"Query execution failed: {e.Message}" };
            }
        }
    }
    private static string BuildDescription(string tableName, long rowCount, IEnumerable<DbColumnDefinition> columns)
    {
        var sb = new StringBuilder();
        sb.Append("Retrieve specific rows from form submission data. ");
        sb.Append("Use ONLY to retrieve and display specific individual records — when the question asks 'which records', 'show me', 'list', 'find the rows'. ");
        sb.Append($"PROHIBITED for any counting, summing, averaging, grouping, or statistics — use '{AggregateFormDataTool.Name}' for those. ");
        sb.Append($"Using this tool for statistics gives WRONG ANSWERS because it sees at most {ExternalDatabaseClient.MaxRowsPerRequest} rows out of {rowCount:N0} total. ");
        sb.Append("Always specify selectColumns (only the columns you need) and filters to narrow the result set. ");
        sb.Append($"Returns at most {ExternalDatabaseClient.MaxRowsPerRequest} rows per call. ");
        sb.Append("NULL awareness: columns may contain NULL values. Use IS NULL / IS NOT NULL filters to explicitly include or exclude nulls. ");
        sb.Append("Error recovery: if the tool returns a column error, check that all column names match the schema exactly (plain names, no quotes, no aliases). ");
        sb.Append("Available columns: ");
        sb.Append(string.Join(", ", columns.Select(c =>
        {
            var label = c.Label is not null && c.Label != c.Name ? $" \"{c.Label}\"" : string.Empty;
            var desc = $"{c.Name}{label} ({c.Type})";
            if (c.EnumValues?.Count > 0)
            {
                desc += $" [{string.Join("/", c.EnumValues)}]";
            }
            return desc;
        })));
        return sb.ToString();
    }
}

internal static partial class FormDataQueryToolLogger
{
    [LoggerMessage(LogLevel.Error, "Form data query failed for table {TableName}")]
    public static partial void ErrorQueryFailed(this ILogger logger, Exception exception, string tableName);
}

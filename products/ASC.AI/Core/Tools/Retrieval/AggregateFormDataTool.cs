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

namespace ASC.AI.Core.Tools.Retrieval;

[Scope]
public class AggregateFormDataTool(
    ExternalDatabaseClient externalDatabaseClient,
    IDaoFactory daoFactory,
    FileSecurity fileSecurity,
    ILogger<AggregateFormDataTool> logger)
{
    public const string Name = "aggregated_form_data";

    public async Task<AIFunction?> InitAsync(int fileId, string tableName, IEnumerable<DbColumnDefinition> columns)
    {
        var fileDao = daoFactory.GetFileDao<int>();
        var file = await fileDao.GetFileAsync(fileId);
        if (file == null || !await fileSecurity.CanEditAsync(file))
        {
            return null;
        }

        var columnList = columns.ToList();
        var allowedColumns = columnList.Select(c => c.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var description = BuildDescription(columnList);

        return AIFunctionFactory.Create(Function, new AIFunctionFactoryOptions
        {
            Name = Name,
            Description = description,
            SerializerOptions = FormDataToolHelpers.FlexibleJsonOptions
        });

        async Task<ToolResponse<string>> Function(
            [Description("Exactly one aggregate function keyword. Allowed values: COUNT, COUNT_DISTINCT, SUM, AVG, MIN, MAX. COUNT — counts rows (or non-null values when valueColumn is set). COUNT_DISTINCT — counts unique non-null values (requires valueColumn). SUM, AVG, MIN, MAX — numeric aggregates (require either valueColumn for Integer columns, or dateDiffValueExpr for DateTime columns). Do NOT pass multiple values or comma-separated keywords — exactly one keyword per call.")] string aggregateFunction,
            [Description("Column to apply the aggregate function to. Omit only for plain COUNT(*) or when using dateDiffValueExpr. Required for COUNT_DISTINCT, SUM, AVG, MIN, MAX on Integer columns.")] string? valueColumn = null,
            [Description("Column to group results by. Must be a plain column name from the schema — e.g. \"col_status\". Do NOT put SQL expressions like DATE_FORMAT() here. For date grouping by YEAR/MONTH/WEEK/DAYOFYEAR/QUARTER/DAYOFWEEK use groupByDatePart instead.")] string? groupByColumn = null,
            [Description("Filter conditions applied with AND. Each filter is a string: \"column_name OPERATOR value\" — e.g. \"col_status = approved\", \"col_age > 25\", \"col_status IN approved,pending\". Column-to-column comparison is also supported: \"col_start < col_end\". Operators: =, !=, <, >, <=, >=, LIKE, NOT LIKE, IS NULL, IS NOT NULL, IN, NOT IN. For IN/NOT IN the value is a comma-separated list: \"col_status IN approved,pending,rejected\".")] IEnumerable<string>? filters = null,
            [Description("Date part to extract from groupByColumn for grouping. Allowed values: YEAR, MONTH, WEEK, DAYOFYEAR, QUARTER, DAYOFWEEK. DAYOFWEEK: 1=Sunday, 2=Monday, 3=Tuesday, 4=Wednesday, 5=Thursday, 6=Friday, 7=Saturday. Example: groupByColumn='col_start_date', groupByDatePart='MONTH' groups by calendar month.")] string? groupByDatePart = null,
            [Description("Second column to include in GROUP BY for two-dimensional breakdowns (e.g. group by category AND date year). Combine with secondGroupByDatePart when the column is a date.")] string? secondGroupByColumn = null,
            [Description("Date part to extract from secondGroupByColumn. Same allowed values as groupByDatePart: YEAR, MONTH, WEEK, DAYOFYEAR, QUARTER, DAYOFWEEK.")] string? secondGroupByDatePart = null,
            [Description("Third column to include in GROUP BY for three-dimensional breakdowns (e.g. group by entity AND date year AND date month). Combine with thirdGroupByDatePart when the column is a date.")] string? thirdGroupByColumn = null,
            [Description("Date part to extract from thirdGroupByColumn. Same allowed values as groupByDatePart: YEAR, MONTH, WEEK, DAYOFYEAR, QUARTER, DAYOFWEEK.")] string? thirdGroupByDatePart = null,
            [Description("Date-part filter conditions applied with AND. Each filter is a string: \"column_name DATE_PART OPERATOR value[,v2,...]\". DATE_PART: YEAR, MONTH, WEEK, DAYOFYEAR, QUARTER, DAYOFWEEK. Operators: =, !=, <, >, <=, >=, IN. Examples: \"col_start_date MONTH IN 6,7,8\" (summer months), \"col_start_date YEAR >= 2022\", \"col_start_date DAYOFWEEK = 2\" (Mondays).")] IEnumerable<string>? datePartFilters = null,
            [Description("Filter on the difference between two date/datetime columns. Format: \"col_a col_b OPERATOR value [UNIT]\" — UNIT is optional and defaults to DAYS. Allowed units: DAYS, HOURS, MINUTES. Examples: \"col_start col_submitted < 7\" (fewer than 7 days apart), \"col_start col_submitted < 48 HOURS\" (fewer than 48 hours apart).")] string? dateDiffFilter = null,
            [Description("Use the difference between two DateTime columns as the value for SUM/AVG/MIN/MAX. Format: \"col_a col_b [UNIT]\" — UNIT is optional and defaults to DAYS. Allowed units: DAYS, HOURS, MINUTES. Example: \"col_created col_submitted HOURS\" computes AVG(hours elapsed between creation and submission). Use this instead of valueColumn when aggregating over a time difference.")] string? dateDiffValueExpr = null,
            [Description("Post-aggregation filter on the aggregate result (HAVING clause). Format: 'OPERATOR value' — e.g. '> 5' keeps only groups where the count/sum exceeds 5, '= 0' keeps only groups with zero. Only applies when groupByColumn is set. Operators: =, !=, <, >, <=, >=. Use this instead of fetching all groups and filtering manually.")] string? having = null,
            [Description("Exclude from results any groupByColumn values that appear in rows matching these filter conditions (generates NOT IN subquery). Use to answer 'which entities had NO records matching X?' — e.g. 'which employees had no records in 2025?'. Format: same as filters — \"column OPERATOR value\". Combine with excludeDatePartFilters for date-based exclusion.")] IEnumerable<string>? excludeFilters = null,
            [Description("Exclude from results any groupByColumn values that appear in rows matching these date-part conditions (generates NOT IN subquery). Format: same as datePartFilters — \"column DATE_PART OPERATOR value\". Example: excludeDatePartFilters=[\"col_date YEAR = 2025\"] excludes entities that have ANY record in 2025. Combine with excludeFilters for additional conditions.")] IEnumerable<string>? excludeDatePartFilters = null,
            [Description("When true, returns a single number — the count of distinct groups that satisfy the query conditions. Use when the question asks 'how many X+Y pairs/groups had more than N records?' and you need only the total count, not the list of groups. Requires groupByColumn to be set.")] bool countGroupsOnly = false)
        {
            try
            {
                // Detect if model passed a date-part keyword as groupByColumn instead of groupByDatePart
                if (groupByColumn != null && FormDataToolHelpers.ValidDateParts.Contains(groupByColumn))
                {
                    return new ToolResponse<string>
                    {
                        Error = $"'{groupByColumn}' is a date-part keyword, not a column name. " +
                                $"Set groupByDatePart='{groupByColumn.ToUpperInvariant()}' and groupByColumn to the actual date column name from the schema."
                    };
                }

                // Auto-split "col_start_date MONTH" → groupByColumn="col_start_date" + groupByDatePart="MONTH"
                if (groupByColumn != null && groupByColumn.Contains(' ') && !groupByColumn.Contains('('))
                {
                    var gParts = groupByColumn.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                    if (gParts.Length == 2 && FormDataToolHelpers.ValidDateParts.Contains(gParts[1].Trim()))
                    {
                        groupByDatePart ??= gParts[1].Trim().ToUpperInvariant();
                        groupByColumn = gParts[0].Trim();
                    }
                    else
                    {
                        return new ToolResponse<string>
                        {
                            Error = $"groupByColumn must be a plain column name, not a SQL expression (got: \"{groupByColumn}\"). " +
                                    "For date grouping use groupByDatePart='YEAR'/'MONTH'/'WEEK'/'DAYOFYEAR'/'QUARTER'/'DAYOFWEEK' together with the column name."
                        };
                    }
                }
                else if (groupByColumn != null && groupByColumn.Contains('('))
                {
                    return new ToolResponse<string>
                    {
                        Error = $"groupByColumn must be a plain column name, not a SQL expression (got: \"{groupByColumn}\"). " +
                                "For date grouping use groupByDatePart='YEAR'/'MONTH'/'WEEK'/'DAYOFYEAR'/'QUARTER'/'DAYOFWEEK' together with the column name."
                    };
                }

                var (normalFilters, autoDatePartFilters, autoDiff) = FormDataToolHelpers.ExtractDatePartFilters(filters);
                dateDiffFilter ??= autoDiff;
                var allDatePartFilters = (datePartFilters ?? []).Concat(autoDatePartFilters);

                var parsedFilters = normalFilters.Select(QueryFilter.Parse);
                var parsedDatePartFilters = allDatePartFilters.Select(DatePartFilter.Parse);
                var parsedDateDiffFilter = dateDiffFilter != null ? DateDiffFilter.Parse(dateDiffFilter) : null;
                var parsedDateDiffAggregate = dateDiffValueExpr != null ? DateDiffAggregate.Parse(dateDiffValueExpr) : null;
                var (normalExcludeFilters, autoExcludeDatePartFilters, _) = FormDataToolHelpers.ExtractDatePartFilters(excludeFilters);
                var allExcludeDatePartFilters = (excludeDatePartFilters ?? []).Concat(autoExcludeDatePartFilters);
                var parsedExcludeFilters = normalExcludeFilters.Select(QueryFilter.Parse);
                var parsedExcludeDatePartFilters = allExcludeDatePartFilters.Select(DatePartFilter.Parse);
                var result = await externalDatabaseClient.AggregateAsync(
                    tableName, allowedColumns, aggregateFunction, valueColumn, groupByColumn, parsedFilters,
                    groupByDatePart, secondGroupByColumn, secondGroupByDatePart,
                    parsedDatePartFilters, parsedDateDiffFilter, parsedDateDiffAggregate,
                    havingFilter: having, thirdGroupByColumn: thirdGroupByColumn, thirdGroupByDatePart: thirdGroupByDatePart,
                    excludeFilters: parsedExcludeFilters, excludeDatePartFilters: parsedExcludeDatePartFilters,
                    countGroupsOnly: countGroupsOnly);
                return new ToolResponse<string> { Data = result };
            }
            catch (Exception e)
            {
                logger.ErrorAggregateFailed(e, tableName);
                return new ToolResponse<string> { Error = $"Aggregate query failed: {e.Message}" };
            }
        }
    }
    private static string BuildDescription(IEnumerable<DbColumnDefinition> columns)
    {
        var sb = new StringBuilder();
        sb.Append("Compute statistics and distributions over form submissions directly in the database. ");
        sb.Append("Analyzes ALL rows in the table regardless of table size — no row limit applies to the input. ");
        sb.Append("NEVER use 'query_form_data' to paginate rows for in-memory analysis — always prefer this tool for any counting, grouping, or statistics question. ");
        sb.Append("CRITICAL — IN operator: to filter by multiple values of the same date part, use IN not two separate = filters. ");
        sb.Append("Wrong: datePartFilters=[\"col_date YEAR = 2025\", \"col_date YEAR = 2026\"]. ");
        sb.Append("Correct: datePartFilters=[\"col_date YEAR IN 2025,2026\"]. Two = filters for the same column are ANDed and always return zero rows. ");
        sb.Append("High-cardinality grouping: if groupByColumn has many distinct values (user IDs, emails, free text), first call without groupByColumn to get the total count, then ask the user to narrow focus before using groupByColumn. ");
        sb.Append("NULL awareness: COUNT(*) counts all rows including nulls; COUNT(valueColumn) counts only non-null values; COUNT_DISTINCT ignores nulls. Use IS NULL / IS NOT NULL filters to control null handling. ");
        sb.Append("Error recovery: if the tool returns 'groupByColumn must be a plain column name', remove any SQL expression and use groupByDatePart for date parts. ");
        sb.Append("Examples: distribution by status (groupByColumn='col_status', aggregateFunction='COUNT'), ");
        sb.Append("monthly breakdown (groupByColumn='col_date', groupByDatePart='MONTH', aggregateFunction='COUNT'), ");
        sb.Append("peak week (groupByColumn='col_date', groupByDatePart='WEEK', aggregateFunction='COUNT'), ");
        sb.Append("trend by year for summer only (groupByColumn='col_date', groupByDatePart='YEAR', datePartFilters=['col_date MONTH IN 6,7,8'], aggregateFunction='COUNT'), ");
        sb.Append("per-entity per-year-month breakdown — ALWAYS include YEAR alongside MONTH to avoid merging the same month across years: (groupByColumn='col_entity', secondGroupByColumn='col_date', secondGroupByDatePart='YEAR', thirdGroupByColumn='col_date', thirdGroupByDatePart='MONTH', aggregateFunction='COUNT'), ");
        sb.Append("late submissions total (dateDiffFilter='col_start col_submitted < 7', aggregateFunction='COUNT'), ");
        sb.Append("who submitted late — group by person with date-diff filter (groupByColumn='col_employee', dateDiffFilter='col_start col_submitted < 7', aggregateFunction='COUNT'), ");
        sb.Append("average processing time in hours (dateDiffValueExpr='col_created col_submitted HOURS', aggregateFunction='AVG'), ");
        sb.Append("average processing time per employee (groupByColumn='col_employee', dateDiffValueExpr='col_created col_submitted HOURS', aggregateFunction='AVG'). ");
        sb.Append("IMPORTANT: dateDiffFilter is a WHERE condition — it can be freely combined with groupByColumn, secondGroupByColumn, filters, and datePartFilters in a single call. ");
        sb.Append("Set-difference ('entities NOT in subset'): use excludeDatePartFilters/excludeFilters to exclude groupByColumn values that appear in rows matching given conditions — e.g. excludeDatePartFilters=['col_date YEAR = 2025'] returns only entities with NO records in 2025. This avoids manual list comparison. Example: groupByColumn='col_entity', excludeDatePartFilters=['col_date YEAR = 2025'], aggregateFunction='COUNT'. ");
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

internal static partial class AggregateFormDataToolLogger
{
    [LoggerMessage(LogLevel.Error, "Form data aggregate failed for table {TableName}")]
    public static partial void ErrorAggregateFailed(this ILogger logger, Exception exception, string tableName);
}

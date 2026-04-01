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
public class SelfJoinFormDataTool(
    ExternalDatabaseClient externalDatabaseClient,
    IDaoFactory daoFactory,
    FileSecurity fileSecurity,
    ILogger<SelfJoinFormDataTool> logger)
{
    public const string Name = "self_join_form_data";

    public async Task<AIFunction?> InitAsync(int fileId, string tableName, IEnumerable<DbColumnDefinition> columns)
    {
        var fileDao = daoFactory.GetFileDao<int>();
        var file = await fileDao.GetFileAsync(fileId);
        if (file == null || !await fileSecurity.CanEditAsync(file))
        {
            return null;
        }

        var columnList = columns.ToList();
        if (columnList.Count < 2)
        {
            return null;
        }

        // Resolve the best deduplication key: prefer explicit PK, fall back to first integer column, then first column
        var pkColumn = columnList.FirstOrDefault(c => c.IsPrimaryKey)
            ?? columnList.FirstOrDefault(c => c.Type == DbColumnType.Integer)
            ?? columnList[0];

        var allowedColumns = columnList.Select(c => c.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var pkName = pkColumn.Name;
        var description = BuildDescription(tableName, columnList);

        return AIFunctionFactory.Create(Function, new AIFunctionFactoryOptions
        {
            Name = Name,
            Description = description,
            SerializerOptions = FormDataToolHelpers.FlexibleJsonOptions
        });

        async Task<ToolResponse<string>> Function(
            [Description(
                "Cross-row comparison conditions — each condition compares a column from record A against a column from record B. " +
                "Format: \"left_col OPERATOR right_col\". Operators: =, !=, <, >, <=, >=. " +
                "Use PLAIN column names from the schema — do NOT add 'a_', 'b_', 'a.', 'b.' prefixes (those appear only in the output). " +
                "Do NOT use DATE_PART syntax here — for filtering by year/month use the datePartFilters parameter instead. " +
                "To find overlapping periods provide two conditions: " +
                "[\"col_start <= col_end\", \"col_end >= col_start\"] — " +
                "this means a.col_start <= b.col_end AND a.col_end >= b.col_start. " +
                "To find rows sharing the same value: [\"col_name = col_name\"]. " +
                "All column names must be from the schema."
            )] IEnumerable<string> joinConditions,
            [Description(
                "Columns to include from both records in the result. Use PLAIN column names — do NOT add 'a_'/'b_' prefixes. " +
                "Each column appears twice: prefixed with 'a_' for record A and 'b_' for record B. " +
                "Example: [\"col_employee\", \"col_start_date\"] produces a_col_employee, a_col_start_date, b_col_employee, b_col_start_date."
            )] IEnumerable<string>? displayColumns = null,
            [Description("Maximum number of matching pairs to return (1–500). Default: 100.")] int limit = 100,
            [Description("Row-level filter conditions applied to both records with AND. Same format as query_form_data filters: \"column_name OPERATOR value\". Example: \"col_year = 2025\". Column-to-column comparison is also supported: \"col_start < col_end\".")] IEnumerable<string>? filters = null,
            [Description("Date-part filter conditions applied to both records. Format: \"column_name DATE_PART OPERATOR value[,v2,...]\". DATE_PART: YEAR, MONTH, WEEK, QUARTER, DAYOFYEAR. Example: \"col_start_date YEAR = 2025\" to limit to records in 2025.")] IEnumerable<string>? datePartFilters = null)
        {
            try
            {
                var (normalFilters, autoDatePartFilters, _) = FormDataToolHelpers.ExtractDatePartFilters(filters);

                // Detect date-part syntax in joinConditions and redirect to datePartFilters
                var joinList = joinConditions.ToList();
                var (parsedJoins, extraDatePart) = SeparateDatePartJoins(joinList);

                // Also scan datePartFilters for cross-row date-part comparisons
                // (e.g. "col_a YEAR = col_b YEAR") that the model put in the wrong parameter
                var allDatePartFilters = (datePartFilters ?? []).Concat(autoDatePartFilters).Concat(extraDatePart);
                var (extraJoins, cleanDatePartFilters) = SeparateDatePartJoins(allDatePartFilters);

                var parsedJoinList = parsedJoins.Concat(extraJoins).ToList();
                if (parsedJoinList.Count == 0)
                {
                    return new ToolResponse<string>
                    {
                        Error = "joinConditions must contain at least one column-to-column comparison (e.g. \"col_start_date <= col_end_date\"). " +
                                "Date-part comparisons like \"col YEAR = 2025\" belong in datePartFilters, not joinConditions."
                    };
                }

                var parsedFilters = normalFilters.Select(QueryFilter.Parse);
                var parsedDatePartFilters = cleanDatePartFilters.Select(DatePartFilter.Parse);
                var result = await externalDatabaseClient.SelfJoinAsync(
                    tableName, allowedColumns, pkName, parsedJoinList, displayColumns, limit,
                    parsedFilters, parsedDatePartFilters);
                return new ToolResponse<string> { Data = result };
            }
            catch (Exception e)
            {
                logger.ErrorSelfJoinFailed(e, tableName);
                return new ToolResponse<string> { Error = $"Self-join query failed: {e.Message}" };
            }
        }
    }

    /// <summary>
    /// Parses join condition strings, separating date-part syntax variants from plain conditions.
    /// <list type="bullet">
    /// <item>"col_a YEAR = col_b YEAR" → <see cref="SelfJoinCondition"/> with DatePart="YEAR" (same-year cross-row comparison)</item>
    /// <item>"col_a YEAR = 2025" → datePartFilter constant "col_a YEAR = 2025" (applied to both rows)</item>
    /// <item>"col_a &lt;= col_b" → plain <see cref="SelfJoinCondition"/></item>
    /// </list>
    /// </summary>
    private static (IEnumerable<SelfJoinCondition> parsedJoins, IEnumerable<string> datePart) SeparateDatePartJoins(IEnumerable<string> joinConditions)
    {
        var parsed = new List<SelfJoinCondition>();
        var datePart = new List<string>();

        foreach (var rawCond in joinConditions)
        {
            var cond = rawCond.Trim();
            var parts = cond.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length >= 3 && FormDataToolHelpers.ValidDateParts.Contains(parts[1]))
            {
                if (parts.Length >= 5 && FormDataToolHelpers.ValidDateParts.Contains(parts[4]))
                {
                    // "col_a YEAR = col_b YEAR" → date-part cross-row join: YEAR(a.col_a) = YEAR(b.col_b)
                    parsed.Add(new SelfJoinCondition(parts[0], parts[2].ToUpperInvariant(), parts[3], parts[1].ToUpperInvariant()));
                }
                else if (parts.Length >= 4)
                {
                    // "col_a YEAR = 2025" → datePartFilter constant, applies to both rows
                    datePart.Add($"{parts[0]} {parts[1]} {parts[2]} {parts[3].TrimEnd(',')}");
                }
            }
            else if (parts.Length == 3)
            {
                // Detect "a_col_X_YEAR = b_col_Y_YEAR" — model appended _DATPART suffix to column names
                var dp = FormDataToolHelpers.ValidDateParts.FirstOrDefault(d =>
                    parts[0].EndsWith("_" + d, StringComparison.OrdinalIgnoreCase) &&
                    parts[2].EndsWith("_" + d, StringComparison.OrdinalIgnoreCase));
                if (dp != null)
                {
                    var leftCol = parts[0][..^(dp.Length + 1)];
                    var rightCol = parts[2][..^(dp.Length + 1)];
                    parsed.Add(new SelfJoinCondition(leftCol, parts[1].ToUpperInvariant(), rightCol, dp.ToUpperInvariant()));
                }
                else
                {
                    try
                    {
                        parsed.Add(SelfJoinCondition.Parse(cond));
                    }
                    catch (ArgumentException)
                    {
                        // Invalid condition format — will surface as missing join conditions
                    }
                }
            }
            else
            {
                try
                {
                    parsed.Add(SelfJoinCondition.Parse(cond));
                }
                catch (ArgumentException)
                {
                    // Invalid condition format — will surface as missing join conditions
                }
            }
        }

        return (parsed, datePart);
    }

    private static string BuildDescription(string tableName, IReadOnlyList<DbColumnDefinition> columns)
    {
        var sb = new StringBuilder();
        sb.Append("Compares every record against every other record to find matching or related pairs (self-join). ");
        sb.Append("USE THIS TOOL when the question asks about: ");
        sb.Append("(1) overlapping time periods — \"which records overlap\", \"scheduling conflicts\", \"concurrent periods\"; ");
        sb.Append("(2) concurrent events — \"simultaneous occurrences\", \"parallel records\"; ");
        sb.Append("(3) same value across multiple records — \"submitted on the same date\", \"same employee appears twice\"; ");
        sb.Append("(4) any comparison between two different rows of the form \"find pairs where...\". ");
        sb.Append("DO NOT use query_form_data to fetch all rows for manual comparison — always use this tool instead. ");
        sb.Append("joinConditions format: \"left_col OPERATOR right_col\" — plain column names only, NO 'a_'/'b_' prefixes. ");
        sb.Append("Operators: =, !=, <, >, <=, >=. ");
        sb.Append("Overlap example: joinConditions=[\"col_start_date <= col_end_date\", \"col_end_date >= col_start_date\"]. ");
        sb.Append("To filter results by year/month add datePartFilters: e.g. datePartFilters=[\"col_start_date YEAR = 2025\"]. ");
        sb.Append("Same-date example: joinConditions=[\"col_submission_date = col_submission_date\"]. ");
        sb.Append("Different employees same start: joinConditions=[\"col_start_date = col_start_date\", \"col_employee != col_employee\"]. ");
        sb.Append("Each result row contains a_pk and b_pk (the pair) plus displayColumns prefixed with a_ and b_. ");
        sb.Append($"Table: '{tableName}'. ");
        sb.Append("Available columns: ");
        sb.Append(string.Join(", ", columns.Select(c =>
        {
            var desc = $"{c.Name} ({c.Type})";
            return c.EnumValues?.Count > 0 ? desc + $" [{string.Join("/", c.EnumValues)}]" : desc;
        })));
        sb.Append('.');
        return sb.ToString();
    }
}

internal static partial class SelfJoinFormDataToolLogger
{
    [LoggerMessage(LogLevel.Error, "Self-join failed for table {TableName}")]
    public static partial void ErrorSelfJoinFailed(this ILogger logger, Exception exception, string tableName);
}

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
            Description = description
        });

        async Task<ToolResponse<string>> Function(
            [Description("Column names to include in the result. Always specify only the columns relevant to the question — do not request all columns unless explicitly needed.")] IEnumerable<string>? selectColumns,
            [Description("Filter conditions applied with AND. Always apply filters to narrow down the data to what is relevant. Each filter specifies a column, an operator (=, !=, <, >, <=, >=, LIKE, NOT LIKE, IS NULL, IS NOT NULL), and an optional value.")] IEnumerable<QueryFilter>? filters,
            [Description("Column name to sort results by.")] string? orderByColumn,
            [Description("Set to true to sort in descending order.")] bool orderByDescending,
            [Description("Maximum number of rows to return per request (1–500). Use a small value (10–50) for exploration. Use 500 and increase offset for paginated full-table analysis.")] int limit,
            [Description("Number of rows to skip (for pagination). Set to 0 for the first page, then increment by limit to retrieve subsequent pages.")] int offset)
        {
            try
            {
                var result = await externalDatabaseClient.QueryAsync(
                    tableName, allowedColumns, selectColumns, filters,
                    orderByColumn, orderByDescending, limit, offset);
                return new ToolResponse<string> { Data = result };
            }
            catch (Exception e)
            {
                logger.LogError(e, "Form data query failed for table {TableName}", tableName);
                return new ToolResponse<string> { Error = "Query execution failed." };
            }
        }
    }

    private static string BuildDescription(string tableName, long rowCount, IEnumerable<DbColumnDefinition> columns)
    {
        var sb = new StringBuilder();
        sb.Append("Query form submission data from an external database. ");
        sb.Append("You may call this tool multiple times with different filters and column selections to answer a question — prefer targeted queries over fetching all rows. ");
        sb.Append("Always use selectColumns to request only the columns you need, and use filters to narrow down the result set. ");
        sb.Append($"A single request returns at most {ExternalDatabaseClient.MaxRowsPerRequest} rows. ");
        sb.Append("When a complete analysis requires more rows than that, paginate: call the tool repeatedly with limit=500 and offset=0, 500, 1000, … until all rows are retrieved. ");
        sb.Append($"Table: '{tableName}', total rows: {rowCount}. ");
        sb.Append("Available columns: ");
        sb.Append(string.Join(", ", columns.Select(c =>
        {
            var desc = $"{c.Name} ({c.Type})";
            if (c.EnumValues?.Count > 0)
            {
                desc += $" [{string.Join("/", c.EnumValues)}]";
            }
            return desc;
        })));
        return sb.ToString();
    }
}

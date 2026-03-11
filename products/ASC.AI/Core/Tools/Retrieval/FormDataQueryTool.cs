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

using System.Text.RegularExpressions;

namespace ASC.AI.Core.Tools.Retrieval;

[Scope]
public class FormDataQueryTool(
    ExternalDatabaseClient externalDatabaseClient,
    IDaoFactory daoFactory,
    FileSecurity fileSecurity)
{
    public const string Name = "query_form_data";

    private static readonly Regex[] _dangerousKeywordRegexes = Array.ConvertAll(
        (string[])["DROP", "DELETE", "INSERT", "UPDATE", "ALTER", "CREATE", "EXEC", "TRUNCATE",
                   "GRANT", "REVOKE", "UNION", "WITH", "INTO", "ATTACH", "PRAGMA", "LOAD_EXTENSION"],
        k => new Regex($@"\b{k}\b", RegexOptions.Compiled | RegexOptions.IgnoreCase));

    private static readonly Regex _tableRefPattern = new(
        @"(?:FROM|JOIN)\s+[`""']?(\w+)[`""']?",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex _implicitJoinPattern = new(
        @"\bFROM\s+[`""']?\w+[`""']?\s*,",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public async Task<AIFunction?> InitAsync(int fileId, string tableName, long rowCount, IEnumerable<DbColumnDefinition> columns)
    {
        var fileDao = daoFactory.GetFileDao<int>();
        var file = await fileDao.GetFileAsync(fileId);
        if (file == null || !await fileSecurity.CanEditAsync(file))
        {
            return null;
        }

        var description = BuildDescription(tableName, rowCount, columns);

        return AIFunctionFactory.Create(Function, new AIFunctionFactoryOptions
        {
            Name = Name,
            Description = description
        });

        async Task<ToolResponse<string>> Function([Description("SQL SELECT query to execute against the form data table")] string sql)
        {
            if (!IsSafeSelect(sql, tableName))
            {
                return new ToolResponse<string> { Error = "Only SELECT queries against the form data table are allowed." };
            }

            try
            {
                var result = await externalDatabaseClient.QueryAsync(sql, tableName);
                return new ToolResponse<string> { Data = result };
            }
            catch (Exception e)
            {
                return new ToolResponse<string> { Error = e.Message };
            }
        }
    }

    private static bool IsSafeSelect(string sql, string allowedTableName)
    {
        var trimmed = sql.Trim();

        if (!trimmed.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var withoutTrailingSemicolon = trimmed.TrimEnd().TrimEnd(';');
        if (withoutTrailingSemicolon.Contains(';'))
        {
            return false;
        }

        if (trimmed.Contains("--") || trimmed.Contains("/*") || trimmed.Contains("*/"))
        {
            return false;
        }

        if (_implicitJoinPattern.IsMatch(trimmed))
        {
            return false;
        }

        if (_dangerousKeywordRegexes.Any(r => r.IsMatch(trimmed)))
        {
            return false;
        }

        if (!ReferencesOnlyAllowedTable(trimmed, allowedTableName))
        {
            return false;
        }

        return true;
    }

    private static bool ReferencesOnlyAllowedTable(string sql, string allowedTableName)
    {
        var matches = _tableRefPattern.Matches(sql);
        if (matches.Count == 0)
        {
            return false;
        }

        return matches.All(m => m.Groups[1].Value.Equals(allowedTableName, StringComparison.OrdinalIgnoreCase));
    }

    private static string BuildDescription(string tableName, long rowCount, IEnumerable<DbColumnDefinition> columns)
    {
        var sb = new StringBuilder();
        sb.Append($"Execute a SQL SELECT query against form submission data stored in an external database. ");
        sb.Append($"Table: '{tableName}', total rows: {rowCount}. ");
        sb.Append("Columns: ");
        sb.Append(string.Join(", ", columns.Select(c =>
        {
            var desc = $"{c.Name} ({c.Type})";
            if (c.EnumValues?.Count > 0)
            {
                desc += $" [{string.Join("/", c.EnumValues)}]";
            }
            return desc;
        })));
        sb.Append(". Only SELECT queries are allowed.");
        return sb.ToString();
    }
}

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

#nullable enable
namespace ASC.Files.Core.ExternalDatabase;

public enum DbColumnType { Text, Integer, Boolean, Date, DateTime, Enum }

public record DbColumnDefinition(string Name, DbColumnType Type, IReadOnlyList<string>? EnumValues = null, bool IsPrimaryKey = false);

/// <summary>Column filter for structured queries against an external database table.</summary>
/// <param name="Column">Name of the column to filter on.</param>
/// <param name="Operator">Comparison operator: =, !=, &lt;, &gt;, &lt;=, &gt;=, LIKE, NOT LIKE, IS NULL, IS NOT NULL.</param>
/// <param name="Value">Value to compare against. Not required for IS NULL / IS NOT NULL.</param>
public record QueryFilter(string Column, string Operator, string? Value = null)
{
    private static readonly string[] _multiWordOperators = ["IS NOT NULL", "NOT LIKE", "IS NULL"];

    /// <summary>
    /// Parses a filter string of the form <c>column_name OPERATOR value</c> into a <see cref="QueryFilter"/>.
    /// Multi-word operators (<c>IS NULL</c>, <c>IS NOT NULL</c>, <c>NOT LIKE</c>) are supported.
    /// The value may be omitted for <c>IS NULL</c> and <c>IS NOT NULL</c>.
    /// </summary>
    public static QueryFilter Parse(string filter)
    {
        var s = filter.AsSpan().Trim();
        var firstSpace = s.IndexOf(' ');
        if (firstSpace < 0)
        {
            throw new ArgumentException($"Invalid filter expression: '{filter}'");
        }

        var column = s[..firstSpace].ToString();
        var rest = s[(firstSpace + 1)..].TrimStart().ToString();

        foreach (var op in _multiWordOperators)
        {
            if (!rest.StartsWith(op, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            var val = rest.Length > op.Length ? rest[op.Length..].TrimStart() : null;
            return new QueryFilter(column, op, string.IsNullOrEmpty(val) ? null : val);
        }

        var opEnd = rest.IndexOf(' ');
        return opEnd < 0
            ? new QueryFilter(column, rest)
            : new QueryFilter(column, rest[..opEnd], StripQuotes(rest[(opEnd + 1)..].Trim()));
    }

    private static string? StripQuotes(string? value)
    {
        if (value is null || value.Length < 2)
        {
            return value;
        }
        if ((value.StartsWith('\'') && value.EndsWith('\'')) ||
            (value.StartsWith('"') && value.EndsWith('"')))
        {
            return value[1..^1];
        }
        return value;
    }
}

/// <summary>Filter that applies a date-part function to a date/datetime column.</summary>
/// <param name="Column">The date/datetime column name.</param>
/// <param name="DatePart">YEAR, MONTH, WEEK, DAYOFYEAR, or QUARTER.</param>
/// <param name="Operator">Comparison operator: =, !=, &lt;, &gt;, &lt;=, &gt;=, IN.</param>
/// <param name="Values">One or more integer values. For IN, multiple values are allowed.</param>
public record DatePartFilter(string Column, string DatePart, string Operator, IReadOnlyList<int> Values)
{
    /// <summary>
    /// Parses a filter string of the form <c>column_name DATE_PART OPERATOR value[,v2,...]</c>.
    /// Examples: "col_start_date MONTH IN 6,7,8" or "col_start_date YEAR >= 2022".
    /// </summary>
    public static DatePartFilter Parse(string filter)
    {
        var parts = filter.Split(' ', 4, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 4)
        {
            throw new ArgumentException($"Invalid DatePartFilter expression (expected 'column DATE_PART OPERATOR value[,v2,...]'): '{filter}'");
        }

        // Strip surrounding parentheses that models sometimes add: "(2020, 2021)" → "2020, 2021"
        var rawValues = parts[3].Trim().Trim('(', ')');

        List<int> values;
        try
        {
            values = rawValues.Split(',').Select(v => int.Parse(v.Trim())).ToList();
        }
        catch (FormatException)
        {
            string[] dateParts = ["YEAR", "MONTH", "WEEK", "DAYOFYEAR", "QUARTER"];
            var hint = dateParts.Any(k => string.Equals(rawValues.Trim(), k, StringComparison.OrdinalIgnoreCase))
                ? $"'{rawValues}' is a date-part keyword, not a value. Write the integer: \"col_start_date YEAR = 2025\"."
                : $"value must be an integer (got: '{rawValues}'). Example: \"col_start_date YEAR = 2025\" or \"col_start_date MONTH IN 6,7,8\".";
            throw new ArgumentException($"Invalid DatePartFilter: {hint}");
        }

        return new DatePartFilter(parts[0], parts[1].ToUpperInvariant(), parts[2].ToUpperInvariant(), values);
    }
}

/// <summary>Filter that computes the difference in days between two date columns.</summary>
/// <param name="StartColumn">The earlier date column (minuend in DATEDIFF).</param>
/// <param name="EndColumn">The later date column (subtrahend in DATEDIFF).</param>
/// <param name="Operator">Comparison operator: =, !=, &lt;, &gt;, &lt;=, &gt;=.</param>
/// <param name="Days">Integer number of days to compare against.</param>
public record DateDiffFilter(string StartColumn, string EndColumn, string Operator, int Days)
{
    /// <summary>
    /// Parses a filter string of the form <c>start_col end_col OPERATOR days</c>.
    /// Example: "col_start_date col_submission_date &lt; 7".
    /// </summary>
    public static DateDiffFilter Parse(string filter)
    {
        var parts = filter.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Handle "col_a DATEDIFF col_b OP days" — model included the "DATEDIFF" keyword
        if (parts.Length == 5 && parts[1].Equals("DATEDIFF", StringComparison.OrdinalIgnoreCase))
        {
            if (!int.TryParse(parts[4], out var days5))
            {
                throw new ArgumentException($"Invalid DateDiffFilter: days value must be an integer (got: '{parts[4]}'). Example: \"col_start col_end < 7\".");
            }
            return new DateDiffFilter(parts[0], parts[2], parts[3].ToUpperInvariant(), days5);
        }

        if (parts.Length != 4)
        {
            throw new ArgumentException($"Invalid DateDiffFilter expression (expected 'start_col end_col OPERATOR days'): '{filter}'");
        }

        if (!int.TryParse(parts[3], out var days))
        {
            throw new ArgumentException($"Invalid DateDiffFilter: days value must be an integer (got: '{parts[3]}'). Example: \"col_start col_end < 7\".");
        }
        return new DateDiffFilter(parts[0], parts[1], parts[2].ToUpperInvariant(), days);
    }
}

/// <summary>A cross-row comparison condition for a self-join query.</summary>
/// <param name="LeftColumn">Column from row a (left side of the comparison).</param>
/// <param name="Operator">Comparison operator: =, !=, &lt;, &gt;, &lt;=, &gt;=.</param>
/// <param name="RightColumn">Column from row b (right side of the comparison).</param>
/// <param name="DatePart">Optional date-part (YEAR/MONTH/WEEK/QUARTER/DAYOFYEAR) to apply to both sides.
/// When set, generates SQL like <c>YEAR(a.left_col) = YEAR(b.right_col)</c>.</param>
public record SelfJoinCondition(string LeftColumn, string Operator, string RightColumn, string? DatePart = null)
{
    /// <summary>
    /// Parses a condition string of the form <c>left_col OPERATOR right_col</c>.
    /// The left column belongs to row a, the right column to row b.
    /// Example: "col_start_date &lt;= col_end_date" → a.col_start_date &lt;= b.col_end_date.
    /// </summary>
    public static SelfJoinCondition Parse(string condition)
    {
        var parts = condition.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3)
        {
            throw new ArgumentException($"Invalid self-join condition (expected 'left_col OPERATOR right_col'): '{condition}'");
        }

        return new SelfJoinCondition(parts[0], parts[1].ToUpperInvariant(), parts[2]);
    }
}

[Scope]
public class ExternalDatabaseClient(ConsumerFactory consumerFactory, ILogger<ExternalDatabaseClient> logger)
{
    private static readonly Regex _tableNameRegex = new(@"^[a-zA-Z0-9_]+$", RegexOptions.Compiled);

    private static void ValidateTableName(string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName) || !_tableNameRegex.IsMatch(tableName))
        {
            throw new ArgumentException($"Invalid table name: '{tableName}'.");
        }
    }

    private ExternalDatabaseProvider Provider => consumerFactory.Get<ExternalDatabaseProvider>();

    public bool IsEnabled() => Provider.IsEnabled();

    public async Task CreateTableIfNotExistsAsync(string tableName, IEnumerable<DbColumnDefinition> columns)
    {
        ValidateTableName(tableName);
        var provider = Provider;
        var dbType = provider.DatabaseTypeEnum;
        await using var connection = await provider.CreateConnectionAsync(dbType);
        await connection.OpenAsync();
        await SetupSqliteAsync(connection, dbType);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = dbType switch
        {
            ExternalDatabaseType.MySql => BuildMySqlCreateTable(tableName, columns),
            ExternalDatabaseType.Sqlite => BuildSqliteCreateTable(tableName, columns),
            _ => throw new NotSupportedException($"Database type '{provider.DatabaseType}' is not supported yet.")
        };
        await cmd.ExecuteNonQueryAsync();
    }

    private static string BuildMySqlCreateTable(string tableName, IEnumerable<DbColumnDefinition> columns)
    {
        var colDefs = columns.Select(c =>
        {
            var type = MapMySqlType(c);
            return c.IsPrimaryKey ? $"`{c.Name}` {type} PRIMARY KEY" : $"`{c.Name}` {type}";
        });
        return $"CREATE TABLE IF NOT EXISTS `{tableName}` ({string.Join(", ", colDefs)});";
    }

    private static string BuildSqliteCreateTable(string tableName, IEnumerable<DbColumnDefinition> columns)
    {
        var colDefs = columns.Select(c =>
        {
            var type = MapSqliteType(c);
            return c.IsPrimaryKey ? $"\"{c.Name}\" {type} PRIMARY KEY" : $"\"{c.Name}\" {type}";
        });
        return $"CREATE TABLE IF NOT EXISTS \"{tableName}\" ({string.Join(", ", colDefs)});";
    }

    private static string MapSqliteType(DbColumnDefinition col) => col.Type switch
    {
        DbColumnType.Integer => "INTEGER",
        DbColumnType.Boolean => "INTEGER",
        DbColumnType.Date => "DATE",
        DbColumnType.DateTime => "DATETIME",
        DbColumnType.Enum when col.EnumValues?.Count > 0 =>
            $"TEXT CHECK(\"{col.Name}\" IN ({string.Join(", ", col.EnumValues.Select(v => $"'{v.Replace("'", "''")}'"))}))",
        DbColumnType.Enum => "TEXT",
        _ => "TEXT"
    };

    private static string MapMySqlType(DbColumnDefinition col) => col.Type switch
    {
        DbColumnType.Integer => "INT",
        DbColumnType.Boolean => "BOOLEAN",
        DbColumnType.Date => "DATE",
        DbColumnType.DateTime => "DATETIME",
        DbColumnType.Enum when col.EnumValues?.Count > 0 =>
            $"ENUM({string.Join(", ", col.EnumValues.Select(v => $"'{v.Replace("'", "\\'")}'"))})",
        DbColumnType.Enum => "VARCHAR(255)",
        _ => "TEXT"
    };

    public async Task<bool> TableExistsAsync(string tableName)
    {
        ValidateTableName(tableName);
        var provider = Provider;
        var dbType = provider.DatabaseTypeEnum;
        await using var connection = await provider.CreateConnectionAsync(dbType);
        await connection.OpenAsync();
        await SetupSqliteAsync(connection, dbType);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = dbType switch
        {
            ExternalDatabaseType.MySql => "SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME=@tableName",
            ExternalDatabaseType.Sqlite => "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=@tableName",
            _ => throw new NotSupportedException($"Database type '{provider.DatabaseType}' is not supported yet.")
        };
        var param = cmd.CreateParameter();
        param.ParameterName = "@tableName";
        param.Value = tableName;
        cmd.Parameters.Add(param);

        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt64(result) > 0;
    }

    public async Task<long> CountAsync(string tableName)
    {
        ValidateTableName(tableName);
        var provider = Provider;
        var dbType = provider.DatabaseTypeEnum;
        await using var connection = await provider.CreateConnectionAsync(dbType);
        await connection.OpenAsync();
        await SetupSqliteAsync(connection, dbType);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = dbType switch
        {
            ExternalDatabaseType.MySql => $"SELECT COUNT(*) FROM `{tableName}`",
            ExternalDatabaseType.Sqlite => $"SELECT COUNT(*) FROM \"{tableName}\"",
            _ => throw new NotSupportedException($"Database type '{provider.DatabaseType}' is not supported yet.")
        };

        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt64(result);
    }

    private static readonly HashSet<string> _allowedOperators = new(StringComparer.OrdinalIgnoreCase)
    {
        "=", "!=", "<>", "<", ">", "<=", ">=", "LIKE", "NOT LIKE", "IS NULL", "IS NOT NULL"
    };

    private static readonly HashSet<string> _nullaryOperators = new(StringComparer.OrdinalIgnoreCase)
    {
        "IS NULL", "IS NOT NULL"
    };

    private static readonly HashSet<string> _allowedAggregateFunctions = new(StringComparer.OrdinalIgnoreCase)
    {
        "COUNT", "COUNT_DISTINCT", "SUM", "AVG", "MIN", "MAX"
    };

    private static readonly HashSet<string> _allowedDateParts = new(StringComparer.OrdinalIgnoreCase)
    {
        "YEAR", "MONTH", "WEEK", "DAYOFYEAR", "QUARTER"
    };

    private static readonly HashSet<string> _datePartFilterOperators = new(StringComparer.OrdinalIgnoreCase)
    {
        "=", "!=", "<", ">", "<=", ">=", "IN"
    };

    private static readonly HashSet<string> _allowedJoinOperators = new(StringComparer.OrdinalIgnoreCase)
    {
        "=", "!=", "<>", "<", ">", "<=", ">="
    };

    public const int MaxRowsPerRequest = 500;

    /// <summary>
    /// Strips table-alias prefixes that models sometimes attach to column names
    /// (e.g. "a.col_name" → "col_name", "b_col_name" → "col_name").
    /// Only strips when the result is a recognized column; returns original otherwise.
    /// </summary>
    private static string NormalizeColumnRef(string col, IReadOnlyCollection<string> allowedColumns)
    {
        col = col.Trim();

        // Strip surrounding quotes that models sometimes add: "col_name" → col_name
        if (col.Length >= 2 && ((col[0] == '"' && col[^1] == '"') || (col[0] == '\'' && col[^1] == '\'')))
        {
            col = col[1..^1].Trim();
        }

        // Strip SQL aliases: "col_employee as a_employee" → "col_employee"
        var asIdx = col.IndexOf(" as ", StringComparison.OrdinalIgnoreCase);
        if (asIdx >= 0)
        {
            col = col[..asIdx].Trim();
        }

        if (allowedColumns.Contains(col, StringComparer.OrdinalIgnoreCase))
        {
            return col;
        }

        // Strip dot-prefix alias: "a.col_name" or "b.col_name"
        var dotIdx = col.IndexOf('.');
        if (dotIdx > 0)
        {
            var stripped = col[(dotIdx + 1)..];
            if (allowedColumns.Contains(stripped, StringComparer.OrdinalIgnoreCase))
            {
                return stripped;
            }
        }

        // Strip single-char underscore prefix: "a_col_name" → "col_name", "b_col_name" → "col_name"
        if (col.Length > 2 && col[1] == '_')
        {
            var stripped = col[2..];
            if (allowedColumns.Contains(stripped, StringComparer.OrdinalIgnoreCase))
            {
                return stripped;
            }
        }

        return col;
    }

    private static string BuildDatePartExpr(string column, string datePart, ExternalDatabaseType dbType, char q)
    {
        if (dbType == ExternalDatabaseType.MySql)
        {
            return $"{datePart.ToUpperInvariant()}({q}{column}{q})";
        }

        return datePart.ToUpperInvariant() switch
        {
            "YEAR"      => $"CAST(strftime('%Y', {q}{column}{q}) AS INTEGER)",
            "MONTH"     => $"CAST(strftime('%m', {q}{column}{q}) AS INTEGER)",
            "WEEK"      => $"CAST(strftime('%W', {q}{column}{q}) AS INTEGER)",
            "DAYOFYEAR" => $"CAST(strftime('%j', {q}{column}{q}) AS INTEGER)",
            "QUARTER"   => $"((CAST(strftime('%m', {q}{column}{q}) AS INTEGER) - 1) / 3 + 1)",
            _           => throw new ArgumentException($"Unsupported date part for SQLite: {datePart}")
        };
    }

    private static string BuildDateDiffExpr(string startCol, string endCol, ExternalDatabaseType dbType, char q)
    {
        return dbType == ExternalDatabaseType.MySql
            ? $"ABS(DATEDIFF({q}{startCol}{q}, {q}{endCol}{q}))"
            : $"ABS(CAST(julianday({q}{startCol}{q}) - julianday({q}{endCol}{q}) AS INTEGER))";
    }

    private static void ValidateFilters(IReadOnlyList<QueryFilter> filters, IReadOnlyCollection<string> allowedColumns)
    {
        foreach (var f in filters)
        {
            if (!allowedColumns.Contains(f.Column, StringComparer.OrdinalIgnoreCase))
            {
                if (f.Column.Contains('('))
                {
                    throw new ArgumentException(
                        $"Filter column '{f.Column}' is a SQL expression. " +
                        "Do not use DATEDIFF() or other functions in filter columns. " +
                        "For date difference comparisons use the dateDiffFilter parameter: \"col_start col_end OPERATOR days\".");
                }

                throw new UnauthorizedAccessException($"Unknown column in filter: '{f.Column}'. Available: {string.Join(", ", allowedColumns)}");
            }

            if (!_allowedOperators.Contains(f.Operator))
            {
                throw new UnauthorizedAccessException($"Operator not allowed: {f.Operator}");
            }

            if (f.Value != null && !_nullaryOperators.Contains(f.Operator.ToUpperInvariant()))
            {
                if (f.Value.Contains('(') || f.Value.Contains(')')
                    || f.Value.Contains(" - ") || f.Value.Contains(" + "))
                {
                    throw new ArgumentException(
                        $"Filter value '{f.Value}' contains arithmetic or SQL expressions. " +
                        "For date arithmetic use the dateDiffFilter parameter: \"col_start col_end OPERATOR days\". " +
                        "Example: \"col_start_date col_submission_date > 7\" finds records where start is 7+ days after submission.");
                }
            }
        }
    }

    private static void ValidateDatePartFilters(IReadOnlyList<DatePartFilter> filters, IReadOnlyCollection<string> allowedColumns)
    {
        foreach (var dpf in filters)
        {
            if (!allowedColumns.Contains(dpf.Column, StringComparer.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException($"Unknown column in date-part filter: {dpf.Column}. Available: {string.Join(", ", allowedColumns)}");
            }

            if (!_allowedDateParts.Contains(dpf.DatePart))
            {
                throw new UnauthorizedAccessException($"Date part not allowed: {dpf.DatePart}");
            }

            if (!_datePartFilterOperators.Contains(dpf.Operator))
            {
                throw new UnauthorizedAccessException($"Operator not allowed in date-part filter: {dpf.Operator}");
            }
        }
    }

    /// <summary>
    /// Merges multiple <c>column DATEPART = X</c> conditions targeting the same column and date part
    /// into a single <c>column DATEPART IN X,Y,...</c> condition.
    /// Prevents the common model mistake of "YEAR = 2025 AND YEAR = 2026" (always false).
    /// </summary>
    private static List<DatePartFilter> MergeDatePartFilters(IReadOnlyList<DatePartFilter> filters)
    {
        var equalGroups = new Dictionary<(string Column, string DatePart), List<int>>();
        var result = new List<DatePartFilter>();

        foreach (var f in filters)
        {
            if (f.Operator == "=")
            {
                var key = (f.Column, f.DatePart);
                if (!equalGroups.TryGetValue(key, out var vals))
                {
                    vals = [];
                    equalGroups[key] = vals;
                }
                vals.AddRange(f.Values);
            }
            else
            {
                result.Add(f);
            }
        }

        foreach (var (key, vals) in equalGroups)
        {
            result.Add(vals.Count == 1
                ? new DatePartFilter(key.Column, key.DatePart, "=", vals)
                : new DatePartFilter(key.Column, key.DatePart, "IN", vals));
        }

        return result;
    }

    private static void ValidateDateDiffFilter(DateDiffFilter filter, IReadOnlyCollection<string> allowedColumns)
    {
        if (!allowedColumns.Contains(filter.StartColumn, StringComparer.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException($"Unknown column in date-diff filter: {filter.StartColumn}. Available: {string.Join(", ", allowedColumns)}");
        }

        if (!allowedColumns.Contains(filter.EndColumn, StringComparer.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException($"Unknown column in date-diff filter: {filter.EndColumn}. Available: {string.Join(", ", allowedColumns)}");
        }

        if (!_allowedOperators.Contains(filter.Operator))
        {
            throw new UnauthorizedAccessException($"Operator not allowed in date-diff filter: {filter.Operator}");
        }
    }

    /// <summary>
    /// Executes an aggregate query (COUNT, SUM, AVG, MIN, MAX, COUNT_DISTINCT) against an external
    /// database table, optionally grouped by a column. Returns JSON-serialized results.
    /// Use this instead of <see cref="QueryAsync"/> for any statistical or distribution question —
    /// the database computes the answer directly, so no row-level pagination is needed.
    /// </summary>
    public async Task<string> AggregateAsync(
        string tableName,
        IReadOnlyCollection<string> allowedColumns,
        string aggregateFunction,
        string? valueColumn,
        string? groupByColumn,
        IEnumerable<QueryFilter>? filters = null,
        string? groupByDatePart = null,
        string? secondGroupByColumn = null,
        string? secondGroupByDatePart = null,
        IEnumerable<DatePartFilter>? datePartFilters = null,
        DateDiffFilter? dateDiffFilter = null)
    {
        ValidateTableName(tableName);

        var upperFn = aggregateFunction.ToUpperInvariant();
        if (!_allowedAggregateFunctions.Contains(upperFn))
        {
            throw new UnauthorizedAccessException($"Aggregate function not allowed: {aggregateFunction}");
        }

        if (upperFn is "COUNT_DISTINCT" or "SUM" or "AVG" or "MIN" or "MAX" && valueColumn is null)
        {
            throw new ArgumentException($"{aggregateFunction} requires valueColumn. To count all rows per group use COUNT (without valueColumn).");
        }

        if (valueColumn != null && !allowedColumns.Contains(valueColumn, StringComparer.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException($"Unknown column '{valueColumn}'. Available: {string.Join(", ", allowedColumns)}");
        }

        if (groupByColumn != null && !allowedColumns.Contains(groupByColumn, StringComparer.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException($"Unknown column in GROUP BY: '{groupByColumn}'. Available: {string.Join(", ", allowedColumns)}");
        }

        if (groupByDatePart != null && !_allowedDateParts.Contains(groupByDatePart))
        {
            throw new UnauthorizedAccessException($"Date part not allowed: {groupByDatePart}");
        }

        if (secondGroupByColumn != null && !allowedColumns.Contains(secondGroupByColumn, StringComparer.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException($"Unknown column in second GROUP BY: '{secondGroupByColumn}'. Available: {string.Join(", ", allowedColumns)}");
        }

        if (secondGroupByDatePart != null && !_allowedDateParts.Contains(secondGroupByDatePart))
        {
            throw new UnauthorizedAccessException($"Date part not allowed for second GROUP BY: {secondGroupByDatePart}");
        }

        var datePartFilterList = MergeDatePartFilters(datePartFilters?.ToList() ?? []);
        ValidateDatePartFilters(datePartFilterList, allowedColumns);

        if (dateDiffFilter != null)
        {
            ValidateDateDiffFilter(dateDiffFilter, allowedColumns);
        }

        var filterList = filters?.ToList() ?? [];
        ValidateFilters(filterList, allowedColumns);

        var provider = Provider;
        var dbType = provider.DatabaseTypeEnum;
        var q = dbType == ExternalDatabaseType.MySql ? '`' : '"';

        var aggExpr = upperFn switch
        {
            "COUNT" when valueColumn is null => "COUNT(*)",
            "COUNT" => $"COUNT({q}{valueColumn}{q})",
            "COUNT_DISTINCT" => $"COUNT(DISTINCT {q}{valueColumn}{q})",
            _ => $"{upperFn}({q}{valueColumn}{q})"
        };

        var selectParts = new List<string>();
        var groupByParts = new List<string>();

        if (groupByColumn != null)
        {
            var gbExpr = groupByDatePart != null
                ? BuildDatePartExpr(groupByColumn, groupByDatePart, dbType, q)
                : $"{q}{groupByColumn}{q}";
            var gbAlias = groupByDatePart != null
                ? $"{q}{groupByColumn}_{groupByDatePart.ToLowerInvariant()}{q}"
                : $"{q}{groupByColumn}{q}";
            selectParts.Add($"{gbExpr} AS {gbAlias}");
            groupByParts.Add(gbExpr);
        }

        if (secondGroupByColumn != null)
        {
            var sgbExpr = secondGroupByDatePart != null
                ? BuildDatePartExpr(secondGroupByColumn, secondGroupByDatePart, dbType, q)
                : $"{q}{secondGroupByColumn}{q}";
            var sgbAlias = secondGroupByDatePart != null
                ? $"{q}{secondGroupByColumn}_{secondGroupByDatePart.ToLowerInvariant()}{q}"
                : $"{q}{secondGroupByColumn}{q}";
            selectParts.Add($"{sgbExpr} AS {sgbAlias}");
            groupByParts.Add(sgbExpr);
        }

        selectParts.Add($"{aggExpr} AS result");

        var sql = new StringBuilder($"SELECT {string.Join(", ", selectParts)} FROM {q}{tableName}{q}");

        var whereParts = new List<string>();
        var parameters = new Dictionary<string, object?>();
        var paramIndex = 0;

        foreach (var f in filterList)
        {
            var colQuoted = $"{q}{f.Column}{q}";
            var op = f.Operator.ToUpperInvariant();
            if (_nullaryOperators.Contains(op))
            {
                whereParts.Add($"{colQuoted} {op}");
            }
            else if (f.Value != null && allowedColumns.Contains(f.Value, StringComparer.OrdinalIgnoreCase))
            {
                // column-to-column comparison — inline both sides, no parameters
                whereParts.Add($"{colQuoted} {op} {q}{f.Value}{q}");
            }
            else
            {
                var paramName = $"@w{paramIndex++}";
                whereParts.Add($"{colQuoted} {op} {paramName}");
                parameters[paramName] = f.Value;
            }
        }

        foreach (var dpf in datePartFilterList)
        {
            var dpExpr = BuildDatePartExpr(dpf.Column, dpf.DatePart, dbType, q);
            whereParts.Add(dpf.Operator == "IN"
                ? $"{dpExpr} IN ({string.Join(", ", dpf.Values)})"
                : $"{dpExpr} {dpf.Operator} {dpf.Values[0]}");
        }

        if (dateDiffFilter != null)
        {
            var ddExpr = BuildDateDiffExpr(dateDiffFilter.StartColumn, dateDiffFilter.EndColumn, dbType, q);
            whereParts.Add($"{ddExpr} {dateDiffFilter.Operator} {dateDiffFilter.Days}");
        }

        if (whereParts.Count > 0)
        {
            sql.Append($" WHERE {string.Join(" AND ", whereParts)}");
        }

        if (groupByParts.Count > 0)
        {
            sql.Append($" GROUP BY {string.Join(", ", groupByParts)} ORDER BY result DESC LIMIT 1000");
        }
        // No GROUP BY → aggregate returns exactly one row; LIMIT is intentionally omitted.

        await using var connection = await provider.CreateConnectionAsync(dbType);
        await connection.OpenAsync();
        await SetupSqliteAsync(connection, dbType);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql.ToString();
        cmd.CommandTimeout = 30;

        foreach (var (name, value) in parameters)
        {
            var param = cmd.CreateParameter();
            param.ParameterName = name;
            param.Value = value ?? DBNull.Value;
            cmd.Parameters.Add(param);
        }

        await using var reader = await cmd.ExecuteReaderAsync();
        var results = new List<Dictionary<string, object?>>();

        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object?>();
            for (var i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }
            results.Add(row);
        }

        return JsonSerializer.Serialize(results);
    }

    public async Task<string> QueryAsync(
        string tableName,
        IReadOnlyCollection<string> allowedColumns,
        IEnumerable<string>? selectColumns = null,
        IEnumerable<QueryFilter>? filters = null,
        string? orderByColumn = null,
        bool orderByDescending = false,
        string? thenByColumn = null,
        bool thenByDescending = false,
        int maxRows = 50,
        int offset = 0,
        IEnumerable<DatePartFilter>? datePartFilters = null,
        DateDiffFilter? dateDiffFilter = null)
    {
        ValidateTableName(tableName);
        var provider = Provider;
        var dbType = provider.DatabaseTypeEnum;
        var q = dbType == ExternalDatabaseType.MySql ? '`' : '"';

        var selectList = selectColumns?.ToList();
        if (selectList is { Count: > 0 })
        {
            var invalid = selectList.Except(allowedColumns, StringComparer.OrdinalIgnoreCase).ToList();
            if (invalid.Count > 0)
            {
                throw new UnauthorizedAccessException($"Unknown columns: {string.Join(", ", invalid)}");
            }
        }

        var filterList = filters?.ToList() ?? [];
        ValidateFilters(filterList, allowedColumns);

        if (orderByColumn != null && !allowedColumns.Contains(orderByColumn, StringComparer.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException($"Unknown column in ORDER BY: {orderByColumn}. Available: {string.Join(", ", allowedColumns)}");
        }

        if (thenByColumn != null && !allowedColumns.Contains(thenByColumn, StringComparer.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException($"Unknown column in ORDER BY: {thenByColumn}. Available: {string.Join(", ", allowedColumns)}");
        }

        var datePartFilterList = MergeDatePartFilters(datePartFilters?.ToList() ?? []);
        ValidateDatePartFilters(datePartFilterList, allowedColumns);

        if (dateDiffFilter != null)
        {
            ValidateDateDiffFilter(dateDiffFilter, allowedColumns);
        }

        var selectPart = selectList is { Count: > 0 }
            ? string.Join(", ", selectList.Select(c => $"{q}{c}{q}"))
            : "*";

        var whereParts = new List<string>();
        var parameters = new Dictionary<string, object?>();
        var paramIndex = 0;

        foreach (var f in filterList)
        {
            var colQuoted = $"{q}{f.Column}{q}";
            var op = f.Operator.ToUpperInvariant();
            if (_nullaryOperators.Contains(op))
            {
                whereParts.Add($"{colQuoted} {op}");
            }
            else if (f.Value != null && allowedColumns.Contains(f.Value, StringComparer.OrdinalIgnoreCase))
            {
                // column-to-column comparison — inline both sides, no parameters
                whereParts.Add($"{colQuoted} {op} {q}{f.Value}{q}");
            }
            else
            {
                var paramName = $"@w{paramIndex++}";
                whereParts.Add($"{colQuoted} {op} {paramName}");
                parameters[paramName] = f.Value;
            }
        }

        foreach (var dpf in datePartFilterList)
        {
            var dpExpr = BuildDatePartExpr(dpf.Column, dpf.DatePart, dbType, q);
            whereParts.Add(dpf.Operator == "IN"
                ? $"{dpExpr} IN ({string.Join(", ", dpf.Values)})"
                : $"{dpExpr} {dpf.Operator} {dpf.Values[0]}");
        }

        if (dateDiffFilter != null)
        {
            var ddExpr = BuildDateDiffExpr(dateDiffFilter.StartColumn, dateDiffFilter.EndColumn, dbType, q);
            whereParts.Add($"{ddExpr} {dateDiffFilter.Operator} {dateDiffFilter.Days}");
        }

        var sql = new StringBuilder($"SELECT {selectPart} FROM {q}{tableName}{q}");

        if (whereParts.Count > 0)
        {
            sql.Append($" WHERE {string.Join(" AND ", whereParts)}");
        }

        if (orderByColumn != null)
        {
            var dir = orderByDescending ? "DESC" : "ASC";
            sql.Append($" ORDER BY {q}{orderByColumn}{q} {dir}");
            if (thenByColumn != null)
            {
                var thenDir = thenByDescending ? "DESC" : "ASC";
                sql.Append($", {q}{thenByColumn}{q} {thenDir}");
            }
        }

        var pageSize = Math.Clamp(maxRows, 1, MaxRowsPerRequest);
        var pageOffset = Math.Max(0, offset);
        sql.Append($" LIMIT {pageSize} OFFSET {pageOffset}");

        await using var connection = await provider.CreateConnectionAsync(dbType);
        await connection.OpenAsync();
        await SetupSqliteAsync(connection, dbType);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql.ToString();
        cmd.CommandTimeout = 30;

        foreach (var (name, value) in parameters)
        {
            var param = cmd.CreateParameter();
            param.ParameterName = name;
            param.Value = value ?? DBNull.Value;
            cmd.Parameters.Add(param);
        }

        await using var reader = await cmd.ExecuteReaderAsync();
        var results = new List<Dictionary<string, object?>>();

        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object?>();
            for (var i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }
            results.Add(row);
        }

        return JsonSerializer.Serialize(results);
    }

    /// <summary>
    /// Compares every row against every other row using column-to-column conditions (self-join).
    /// Each pair appears exactly once thanks to the <c>a.pk &lt; b.pk</c> deduplication constraint.
    /// </summary>
    public async Task<string> SelfJoinAsync(
        string tableName,
        IReadOnlyCollection<string> allowedColumns,
        string pkColumn,
        IEnumerable<SelfJoinCondition> joinConditions,
        IEnumerable<string>? displayColumns = null,
        int limit = 100,
        IEnumerable<QueryFilter>? filters = null,
        IEnumerable<DatePartFilter>? datePartFilters = null)
    {
        ValidateTableName(tableName);

        if (!allowedColumns.Contains(pkColumn, StringComparer.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException($"Unknown PK column: {pkColumn}");
        }

        var conditionList = joinConditions
            .Select(c => new SelfJoinCondition(
                NormalizeColumnRef(c.LeftColumn, allowedColumns),
                c.Operator,
                NormalizeColumnRef(c.RightColumn, allowedColumns),
                c.DatePart))
            .ToList();

        if (conditionList.Count == 0)
        {
            throw new ArgumentException("At least one join condition is required.");
        }

        foreach (var cond in conditionList)
        {
            if (!allowedColumns.Contains(cond.LeftColumn, StringComparer.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException($"Unknown column in join condition: '{cond.LeftColumn}'. Available: {string.Join(", ", allowedColumns)}");
            }

            if (!allowedColumns.Contains(cond.RightColumn, StringComparer.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException($"Unknown column in join condition: '{cond.RightColumn}'. Available: {string.Join(", ", allowedColumns)}");
            }

            if (!_allowedJoinOperators.Contains(cond.Operator))
            {
                throw new UnauthorizedAccessException($"Operator not allowed in join condition: {cond.Operator}");
            }
        }

        // Normalize and validate display columns — strip a_/b_ prefixes the model may add
        var displayList = (displayColumns ?? [])
            .Select(col => NormalizeColumnRef(col, allowedColumns))
            .ToList();
        foreach (var col in displayList)
        {
            if (!allowedColumns.Contains(col, StringComparer.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException($"Unknown display column: {col}");
            }
        }

        var filterList = filters?.ToList() ?? [];
        ValidateFilters(filterList, allowedColumns);

        var datePartFilterList = MergeDatePartFilters(datePartFilters?.ToList() ?? []);
        ValidateDatePartFilters(datePartFilterList, allowedColumns);

        var pageSize = Math.Clamp(limit, 1, MaxRowsPerRequest);
        var provider = Provider;
        var dbType = provider.DatabaseTypeEnum;
        var q = dbType == ExternalDatabaseType.MySql ? '`' : '"';

        var selectParts = new List<string> { $"a.{q}{pkColumn}{q} AS {q}a_pk{q}" };
        selectParts.AddRange(displayList.Select(col => $"a.{q}{col}{q} AS {q}a_{col}{q}"));
        selectParts.Add($"b.{q}{pkColumn}{q} AS {q}b_pk{q}");
        selectParts.AddRange(displayList.Select(col => $"b.{q}{col}{q} AS {q}b_{col}{q}"));

        // Join conditions use a./b. prefixes; date-part conditions wrap both sides in the date function
        var whereParts = conditionList.Select(c =>
        {
            if (c.DatePart != null)
            {
                var leftExpr = BuildDatePartExpr(c.LeftColumn, c.DatePart, dbType, q)
                    .Replace($"{q}{c.LeftColumn}{q}", $"a.{q}{c.LeftColumn}{q}");
                var rightExpr = BuildDatePartExpr(c.RightColumn, c.DatePart, dbType, q)
                    .Replace($"{q}{c.RightColumn}{q}", $"b.{q}{c.RightColumn}{q}");
                return $"{leftExpr} {c.Operator} {rightExpr}";
            }
            return $"a.{q}{c.LeftColumn}{q} {c.Operator} b.{q}{c.RightColumn}{q}";
        }).ToList();

        // Extra row-level filters apply to BOTH records (a. and b.) so neither record can bypass the constraint
        var parameters = new Dictionary<string, object?>();
        var paramIndex = 0;
        foreach (var f in filterList)
        {
            var aCol = $"a.{q}{f.Column}{q}";
            var bCol = $"b.{q}{f.Column}{q}";
            var op = f.Operator.ToUpperInvariant();
            if (_nullaryOperators.Contains(op))
            {
                whereParts.Add($"{aCol} {op}");
                whereParts.Add($"{bCol} {op}");
            }
            else if (f.Value != null && allowedColumns.Contains(f.Value, StringComparer.OrdinalIgnoreCase))
            {
                whereParts.Add($"{aCol} {op} a.{q}{f.Value}{q}");
                whereParts.Add($"{bCol} {op} b.{q}{f.Value}{q}");
            }
            else
            {
                var paramName = $"@w{paramIndex++}";
                whereParts.Add($"{aCol} {op} {paramName}");
                whereParts.Add($"{bCol} {op} {paramName}");
                parameters[paramName] = f.Value;
            }
        }

        foreach (var dpf in datePartFilterList)
        {
            var dpExpr = BuildDatePartExpr(dpf.Column, dpf.DatePart, dbType, q);
            var aExpr = dpExpr.Replace($"{q}{dpf.Column}{q}", $"a.{q}{dpf.Column}{q}");
            var bExpr = dpExpr.Replace($"{q}{dpf.Column}{q}", $"b.{q}{dpf.Column}{q}");
            if (dpf.Operator == "IN")
            {
                var inList = string.Join(", ", dpf.Values);
                whereParts.Add($"{aExpr} IN ({inList})");
                whereParts.Add($"{bExpr} IN ({inList})");
            }
            else
            {
                whereParts.Add($"{aExpr} {dpf.Operator} {dpf.Values[0]}");
                whereParts.Add($"{bExpr} {dpf.Operator} {dpf.Values[0]}");
            }
        }

        var sql =
            $"SELECT {string.Join(", ", selectParts)} " +
            $"FROM {q}{tableName}{q} a " +
            $"JOIN {q}{tableName}{q} b ON a.{q}{pkColumn}{q} < b.{q}{pkColumn}{q} " +
            $"WHERE {string.Join(" AND ", whereParts)} " +
            $"LIMIT {pageSize}";

        await using var connection = await provider.CreateConnectionAsync(dbType);
        await connection.OpenAsync();
        await SetupSqliteAsync(connection, dbType);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.CommandTimeout = 30;

        foreach (var (name, value) in parameters)
        {
            var param = cmd.CreateParameter();
            param.ParameterName = name;
            param.Value = value ?? DBNull.Value;
            cmd.Parameters.Add(param);
        }

        await using var reader = await cmd.ExecuteReaderAsync();
        var results = new List<Dictionary<string, object?>>();

        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object?>();
            for (var i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }
            results.Add(row);
        }

        return JsonSerializer.Serialize(results);
    }

    public Task InsertDataAsync(string tableName, Dictionary<string, object> data)
        => ExecuteInsertAsync(tableName, data, keyColumn: null);

    public Task UpsertDataAsync(string tableName, Dictionary<string, object> data, string keyColumn)
        => ExecuteInsertAsync(tableName, data, keyColumn);

    public async Task CreateTableAndUpsertAsync(string tableName, IEnumerable<DbColumnDefinition> columns, Dictionary<string, object> data, string keyColumn)
    {
        ValidateTableName(tableName);
        if (data == null || data.Count == 0)
        {
            throw new ArgumentException("Data dictionary is empty.", nameof(data));
        }

        var provider = Provider;
        var dbType = provider.DatabaseTypeEnum;
        await using var connection = await provider.CreateConnectionAsync(dbType);
        await connection.OpenAsync();
        await SetupSqliteAsync(connection, dbType);

        // SQLite only: MySQL DDL (CREATE TABLE) causes an implicit commit,
        // making it impossible to wrap CREATE TABLE + INSERT in one atomic transaction.
        DbTransaction? tx = dbType == ExternalDatabaseType.Sqlite ? await connection.BeginTransactionAsync() : null;

        try
        {
            await using var createCmd = connection.CreateCommand();
            createCmd.Transaction = tx;
            createCmd.CommandText = dbType switch
            {
                ExternalDatabaseType.MySql => BuildMySqlCreateTable(tableName, columns),
                ExternalDatabaseType.Sqlite => BuildSqliteCreateTable(tableName, columns),
                _ => throw new NotSupportedException($"Database type '{provider.DatabaseType}' is not supported yet.")
            };
            await createCmd.ExecuteNonQueryAsync();

            await using var insertCmd = connection.CreateCommand();
            insertCmd.Transaction = tx;
            insertCmd.CommandText = BuildInsertSql(tableName, data.Keys, keyColumn, dbType);
            foreach (var kvp in data)
            {
                var param = insertCmd.CreateParameter();
                param.ParameterName = "@" + kvp.Key;
                param.Value = kvp.Value ?? DBNull.Value;
                insertCmd.Parameters.Add(param);
            }
            await insertCmd.ExecuteNonQueryAsync();

            if (tx != null)
            {
                await tx.CommitAsync();
            }
        }
        catch
        {
            if (tx != null)
            {
                await tx.RollbackAsync();
            }
            throw;
        }
        finally
        {
            if (tx != null)
            {
                await tx.DisposeAsync();
            }
        }
    }

    private async Task ExecuteInsertAsync(string tableName, Dictionary<string, object> data, string? keyColumn)
    {
        ValidateTableName(tableName);
        try
        {
            if (data == null || data.Count == 0)
            {
                throw new ArgumentException("Data dictionary is empty.", nameof(data));
            }
            var dbType = Provider.DatabaseTypeEnum;
            await using var connection = await Provider.CreateConnectionAsync(dbType);
            await connection.OpenAsync();
            await SetupSqliteAsync(connection, dbType);

            await using var cmd = connection.CreateCommand();
            cmd.CommandText = BuildInsertSql(tableName, data.Keys, keyColumn, dbType);

            foreach (var kvp in data)
            {
                var param = cmd.CreateParameter();
                param.ParameterName = "@" + kvp.Key;
                param.Value = kvp.Value ?? DBNull.Value;
                cmd.Parameters.Add(param);
            }

            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            logger.ErrorInsertFailed(ex, tableName);
            throw;
        }
    }

    private static string BuildInsertSql(string tableName, IEnumerable<string> keys, string? keyColumn, ExternalDatabaseType dbType)
    {
        var keyList = keys.ToList();
        var parameters = string.Join(", ", keyList.Select(k => $"@{k}"));

        return dbType switch
        {
            ExternalDatabaseType.MySql => BuildMySqlInsert(tableName, keyList, parameters, keyColumn),
            ExternalDatabaseType.Sqlite => BuildSqliteInsert(tableName, keyList, parameters, keyColumn),
            _ => throw new NotSupportedException($"Database type '{dbType.ToStringFast()}' is not supported yet.")
        };
    }

    private static string BuildMySqlInsert(string tableName, List<string> keys, string parameters, string? keyColumn)
    {
        var columns = string.Join(", ", keys.Select(k => $"`{k}`"));
        var sql = new StringBuilder($"INSERT INTO `{tableName}` ({columns}) VALUES ({parameters})");

        if (keyColumn != null)
        {
            var updateParts = string.Join(", ", keys
                .Where(k => k != keyColumn)
                .Select(k => $"`{k}` = VALUES(`{k}`)"));
            sql.Append($" ON DUPLICATE KEY UPDATE {updateParts}");
        }

        sql.Append(';');
        return sql.ToString();
    }

    private static async Task SetupSqliteAsync(DbConnection connection, ExternalDatabaseType dbType)
    {
        if (dbType != ExternalDatabaseType.Sqlite)
        {
            return;
        }

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "PRAGMA busy_timeout = 5000;";
        await cmd.ExecuteNonQueryAsync();
    }

    private static string BuildSqliteInsert(string tableName, List<string> keys, string parameters, string? keyColumn)
    {
        var columns = string.Join(", ", keys.Select(k => $"\"{k}\""));

        if (keyColumn == null)
        {
            return $"INSERT INTO \"{tableName}\" ({columns}) VALUES ({parameters});";
        }

        var updateParts = string.Join(", ", keys
            .Where(k => k != keyColumn)
            .Select(k => $"\"{k}\" = excluded.\"{k}\""));

        return $"INSERT INTO \"{tableName}\" ({columns}) VALUES ({parameters}) ON CONFLICT(\"{keyColumn}\") DO UPDATE SET {updateParts};";
    }
}

internal static partial class ExternalDatabaseClientLogger
{
    [LoggerMessage(LogLevel.Error, "Insert into table {TableName} failed")]
    public static partial void ErrorInsertFailed(this ILogger<ExternalDatabaseClient> logger, Exception exception, string tableName);
}

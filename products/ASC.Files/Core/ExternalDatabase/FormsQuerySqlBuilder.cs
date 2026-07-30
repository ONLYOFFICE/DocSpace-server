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

#nullable enable
namespace ASC.Files.Core.ExternalDatabase;

/// <summary>
/// Database-agnostic SQL fragment building and validation shared by <see cref="ExternalDatabaseClient"/>
/// (tenant-configured MySQL/SQLite/PostgreSQL) and <see cref="BuiltinFormsDatabaseClient"/> (auto-provisioned
/// PostgreSQL). Every method here is stateless and connection-agnostic — callers own the connection, the
/// table-name qualification (plain vs schema-qualified), and the actual SQL execution.
/// </summary>
internal static class FormsQuerySqlBuilder
{
    private static readonly Regex _tableNameRegex = new(@"^[a-zA-Z0-9_]+$", RegexOptions.Compiled);

    public const int MaxRowsPerRequest = 500;

    public static readonly HashSet<string> AllowedOperators = new(StringComparer.OrdinalIgnoreCase)
    {
        "=", "!=", "<>", "<", ">", "<=", ">=", "LIKE", "NOT LIKE", "IS NULL", "IS NOT NULL", "IN", "NOT IN"
    };

    public static readonly HashSet<string> NullaryOperators = new(StringComparer.OrdinalIgnoreCase)
    {
        "IS NULL", "IS NOT NULL"
    };

    public static readonly HashSet<string> AllowedAggregateFunctions = new(StringComparer.OrdinalIgnoreCase)
    {
        "COUNT", "COUNT_DISTINCT", "SUM", "AVG", "MIN", "MAX"
    };

    public static readonly HashSet<string> AllowedDateParts = new(StringComparer.OrdinalIgnoreCase)
    {
        "YEAR", "MONTH", "WEEK", "DAYOFYEAR", "QUARTER", "DAYOFWEEK"
    };

    public static readonly HashSet<string> DatePartFilterOperators = new(StringComparer.OrdinalIgnoreCase)
    {
        "=", "!=", "<", ">", "<=", ">=", "IN"
    };

    public static readonly HashSet<string> AllowedJoinOperators = new(StringComparer.OrdinalIgnoreCase)
    {
        "=", "!=", "<>", "<", ">", "<=", ">="
    };

    public static void ValidateTableName(string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName) || !_tableNameRegex.IsMatch(tableName))
        {
            throw new ArgumentException($"Invalid table name: '{tableName}'.");
        }
    }

    /// <summary>
    /// Strips table-alias prefixes that models sometimes attach to column names
    /// (e.g. "a.col_name" → "col_name", "b_col_name" → "col_name").
    /// Only strips when the result is a recognized column; returns original otherwise.
    /// </summary>
    public static string NormalizeColumnRef(string col, IReadOnlyCollection<string> allowedColumns)
    {
        col = col.Trim();

        // Strip surrounding quotes that models sometimes add: "col_name" → col_name
        if (col.Length >= 2 && ((col[0] == '"' && col[^1] == '"') || (col[0] == '\'' && col[^1] == '\'')))
        {
            col = col[1..^1].Trim();
        }

        // Strip JSON-array brackets that models sometimes add: ["col_name"] → col_name, ["col_name" → col_name
        if (col.StartsWith("[\"", StringComparison.Ordinal))
        {
            col = col[2..].TrimEnd('"', ']').Trim();
        }
        else if (col.StartsWith('[') || col.EndsWith(']'))
        {
            col = col.Trim('[', ']').Trim();
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

    public static void BuildFilterClauses(
        IReadOnlyList<QueryFilter> filters,
        IReadOnlyCollection<string> allowedColumns,
        char q,
        ref int paramIndex,
        Dictionary<string, object?> parameters,
        List<string> whereParts)
    {
        foreach (var f in filters)
        {
            var colQuoted = $"{q}{f.Column}{q}";
            var op = f.Operator.ToUpperInvariant();
            if (NullaryOperators.Contains(op))
            {
                whereParts.Add($"{colQuoted} {op}");
            }
            else if (op is "IN" or "NOT IN")
            {
                var values = (f.Value ?? "").Split(',');
                var inParamNames = new List<string>(values.Length);
                foreach (var v in values)
                {
                    var pn = $"@p{paramIndex++}";
                    parameters[pn] = v.Trim();
                    inParamNames.Add(pn);
                }

                whereParts.Add($"{colQuoted} {op} ({string.Join(", ", inParamNames)})");
            }
            else if (f.Value != null && allowedColumns.Contains(f.Value, StringComparer.OrdinalIgnoreCase))
            {
                whereParts.Add($"{colQuoted} {op} {q}{f.Value}{q}");
            }
            else
            {
                var pn = $"@p{paramIndex++}";
                whereParts.Add($"{colQuoted} {op} {pn}");
                parameters[pn] = f.Value;
            }
        }
    }

    public static void AppendDatePartClauses(
        IReadOnlyList<DatePartFilter> datePartFilters,
        ExternalDatabaseType dbType,
        char q,
        List<string> whereParts)
    {
        foreach (var dpf in datePartFilters)
        {
            var dpExpr = BuildDatePartExpr(dpf.Column, dpf.DatePart, dbType, q);
            whereParts.Add(dpf.Operator == "IN"
                ? $"{dpExpr} IN ({string.Join(", ", dpf.Values)})"
                : $"{dpExpr} {dpf.Operator} {dpf.Values[0]}");
        }
    }

    public static string BuildDatePartExpr(string column, string datePart, ExternalDatabaseType dbType, char q)
    {
        // Defense-in-depth: the MySQL branch injects datePart as a raw SQL token, so allowlist it here
        // regardless of the dialect (the PostgreSQL/SQLite switches already reject unknown values).
        if (!AllowedDateParts.Contains(datePart))
        {
            throw new UnauthorizedAccessException($"Date part not allowed: {datePart}");
        }

        if (dbType == ExternalDatabaseType.MySql)
        {
            return $"{datePart.ToUpperInvariant()}({q}{column}{q})";
        }

        if (dbType == ExternalDatabaseType.PostgreSql)
        {
            return datePart.ToUpperInvariant() switch
            {
                "YEAR"      => $"EXTRACT(YEAR FROM {q}{column}{q})::INTEGER",
                "MONTH"     => $"EXTRACT(MONTH FROM {q}{column}{q})::INTEGER",
                "WEEK"      => $"EXTRACT(WEEK FROM {q}{column}{q})::INTEGER",
                "DAYOFYEAR" => $"EXTRACT(DOY FROM {q}{column}{q})::INTEGER",
                "QUARTER"   => $"EXTRACT(QUARTER FROM {q}{column}{q})::INTEGER",
                "DAYOFWEEK" => $"EXTRACT(DOW FROM {q}{column}{q})::INTEGER + 1",
                _ => throw new ArgumentException($"Unsupported date part for PostgreSQL: {datePart}")
            };
        }

        return datePart.ToUpperInvariant() switch
        {
            "YEAR"      => $"CAST(strftime('%Y', {q}{column}{q}) AS INTEGER)",
            "MONTH"     => $"CAST(strftime('%m', {q}{column}{q}) AS INTEGER)",
            "WEEK"      => $"CAST(strftime('%W', {q}{column}{q}) AS INTEGER)",
            "DAYOFYEAR" => $"CAST(strftime('%j', {q}{column}{q}) AS INTEGER)",
            "QUARTER"   => $"((CAST(strftime('%m', {q}{column}{q}) AS INTEGER) - 1) / 3 + 1)",
            "DAYOFWEEK" => $"(CAST(strftime('%w', {q}{column}{q}) AS INTEGER) + 1)",
            _           => throw new ArgumentException($"Unsupported date part for SQLite: {datePart}")
        };
    }

    public static string BuildDateDiffExpr(string startCol, string endCol, ExternalDatabaseType dbType, char q, string unit = "DAYS", bool inclusive = false)
    {
        var plus1 = inclusive ? " + 1" : "";

        if (dbType == ExternalDatabaseType.MySql)
        {
            return unit switch
            {
                "HOURS"   => $"ABS(TIMESTAMPDIFF(HOUR, {q}{startCol}{q}, {q}{endCol}{q})){plus1}",
                "MINUTES" => $"ABS(TIMESTAMPDIFF(MINUTE, {q}{startCol}{q}, {q}{endCol}{q})){plus1}",
                _         => $"ABS(DATEDIFF({q}{startCol}{q}, {q}{endCol}{q})){plus1}"
            };
        }

        if (dbType == ExternalDatabaseType.PostgreSql)
        {
            return unit switch
            {
                "HOURS"   => $"ABS(EXTRACT(EPOCH FROM ({q}{endCol}{q}::TIMESTAMP - {q}{startCol}{q}::TIMESTAMP)) / 3600)::INTEGER{plus1}",
                "MINUTES" => $"ABS(EXTRACT(EPOCH FROM ({q}{endCol}{q}::TIMESTAMP - {q}{startCol}{q}::TIMESTAMP)) / 60)::INTEGER{plus1}",
                _         => $"ABS(({q}{endCol}{q}::DATE - {q}{startCol}{q}::DATE)){plus1}"
            };
        }

        return unit switch
        {
            "HOURS"   => $"ABS(CAST((julianday({q}{endCol}{q}) - julianday({q}{startCol}{q})) * 24 AS INTEGER)){plus1}",
            "MINUTES" => $"ABS(CAST((julianday({q}{endCol}{q}) - julianday({q}{startCol}{q})) * 1440 AS INTEGER)){plus1}",
            _         => $"ABS(CAST(julianday({q}{endCol}{q}) - julianday({q}{startCol}{q}) AS INTEGER)){plus1}"
        };
    }

    public static void ValidateGroupBy(string? column, string? datePart, string label, IReadOnlyCollection<string> allowedColumns)
    {
        if (column != null && !allowedColumns.Contains(column, StringComparer.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException($"Unknown column in {label}: '{column}'. Available: {string.Join(", ", allowedColumns)}");
        }

        if (datePart != null && !AllowedDateParts.Contains(datePart))
        {
            throw new UnauthorizedAccessException($"Date part not allowed for {label}: {datePart}");
        }
    }

    public static void ValidateFilters(IReadOnlyList<QueryFilter> filters, IReadOnlyCollection<string> allowedColumns)
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

            if (!AllowedOperators.Contains(f.Operator))
            {
                throw new UnauthorizedAccessException($"Operator not allowed: {f.Operator}");
            }

            var upperOp = f.Operator.ToUpperInvariant();
            if ((upperOp is "IN" or "NOT IN") && string.IsNullOrWhiteSpace(f.Value))
            {
                throw new ArgumentException($"Operator {f.Operator} requires a comma-separated value list. Example: \"col_status IN approved,pending\".");
            }

            if (f.Value != null && !NullaryOperators.Contains(f.Operator.ToUpperInvariant()))
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

    public static void ValidateDatePartFilters(IReadOnlyList<DatePartFilter> filters, IReadOnlyCollection<string> allowedColumns)
    {
        foreach (var dpf in filters)
        {
            if (!allowedColumns.Contains(dpf.Column, StringComparer.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException($"Unknown column in date-part filter: {dpf.Column}. Available: {string.Join(", ", allowedColumns)}");
            }

            if (!AllowedDateParts.Contains(dpf.DatePart))
            {
                throw new UnauthorizedAccessException($"Date part not allowed: {dpf.DatePart}");
            }

            if (!DatePartFilterOperators.Contains(dpf.Operator))
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
    public static List<DatePartFilter> MergeDatePartFilters(IReadOnlyList<DatePartFilter> filters)
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

    public static void ValidateDateDiffFilter(DateDiffFilter filter, IReadOnlyCollection<string> allowedColumns)
    {
        if (!allowedColumns.Contains(filter.StartColumn, StringComparer.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException($"Unknown column in date-diff filter: {filter.StartColumn}. Available: {string.Join(", ", allowedColumns)}");
        }

        if (!allowedColumns.Contains(filter.EndColumn, StringComparer.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException($"Unknown column in date-diff filter: {filter.EndColumn}. Available: {string.Join(", ", allowedColumns)}");
        }

        if (!AllowedOperators.Contains(filter.Operator))
        {
            throw new UnauthorizedAccessException($"Operator not allowed in date-diff filter: {filter.Operator}");
        }
    }
}

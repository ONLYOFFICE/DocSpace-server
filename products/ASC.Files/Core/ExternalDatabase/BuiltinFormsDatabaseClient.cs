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

using Npgsql;

#nullable enable
namespace ASC.Files.Core.ExternalDatabase;

[Scope]
public class BuiltinFormsDatabaseClient(
    FormsDbProvisioningService provisioner,
    TenantManager tenantManager,
    ILogger<BuiltinFormsDatabaseClient> logger)
    : IFormsDatabaseClient
{
    private const ExternalDatabaseType DbType = ExternalDatabaseType.PostgreSql;
    private const char Q = '"';

    public bool IsEnabled() => provisioner.IsEnabled();

    private static void ValidateTableName(string tableName) => FormsQuerySqlBuilder.ValidateTableName(tableName);

    private async Task<(NpgsqlConnection connection, string schemaName)> OpenConnectionAsync()
    {
        var tenantId = tenantManager.GetCurrentTenantId();
        var credentials = await provisioner.GetOrProvisionAsync(tenantId);
        var connection = new NpgsqlConnection(credentials.RwConnectionString);
        await connection.OpenAsync();
        return (connection, credentials.SchemaName);
    }

    public async Task CreateTableAndUpsertAsync(string tableName, IEnumerable<DbColumnDefinition> columns,
        Dictionary<string, object> data, string keyColumn)
    {
        ValidateTableName(tableName);
        if (data == null || data.Count == 0)
        {
            throw new ArgumentException("Data dictionary is empty.", nameof(data));
        }

        try
        {
            var (connection, schemaName) = await OpenConnectionAsync();
            await using (connection)
            {
                await using var createCmd = connection.CreateCommand();
                createCmd.CommandText = BuildPgCreateTable(schemaName, tableName, columns);
                await createCmd.ExecuteNonQueryAsync();

                await using var insertCmd = connection.CreateCommand();
                insertCmd.CommandText = BuildPgUpsert(schemaName, tableName, data.Keys.ToList(), keyColumn);
                foreach (var kvp in data)
                {
                    var param = insertCmd.CreateParameter();
                    param.ParameterName = "@" + kvp.Key;
                    param.Value = kvp.Value ?? DBNull.Value;
                    insertCmd.Parameters.Add(param);
                }
                await insertCmd.ExecuteNonQueryAsync();
            }
        }
        catch (Exception ex)
        {
            logger.ErrorBuiltinInsertFailed(ex, tableName);
            throw;
        }
    }

    public async Task<long> GetTableCountAsync(string tableName) =>
        await TableExistsAsync(tableName) ? await CountAsync(tableName) : 0;

    public async Task<IReadOnlySet<int>> GetExistingFormIdsAsync(string tableName)
    {
        if (!await TableExistsAsync(tableName))
        {
            return new HashSet<int>();
        }

        ValidateTableName(tableName);
        var (connection, schemaName) = await OpenConnectionAsync();
        await using (connection)
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT \"form_id\" FROM \"{schemaName}\".\"{tableName}\"";

            await using var reader = await cmd.ExecuteReaderAsync();
            var ids = new HashSet<int>();
            while (await reader.ReadAsync())
            {
                if (!reader.IsDBNull(0))
                {
                    ids.Add(reader.GetInt32(0));
                }
            }

            return ids;
        }
    }

    public async Task<bool> TableExistsAsync(string tableName)
    {
        ValidateTableName(tableName);
        var (connection, schemaName) = await OpenConnectionAsync();
        await using (connection)
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM information_schema.tables " +
                              "WHERE table_schema = @schema AND table_name = @tableName";
            var schemaParam = cmd.CreateParameter();
            schemaParam.ParameterName = "@schema";
            schemaParam.Value = schemaName;
            cmd.Parameters.Add(schemaParam);

            var tableParam = cmd.CreateParameter();
            tableParam.ParameterName = "@tableName";
            tableParam.Value = tableName;
            cmd.Parameters.Add(tableParam);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt64(result) > 0;
        }
    }

    public async Task<long> CountAsync(string tableName)
    {
        ValidateTableName(tableName);
        var (connection, schemaName) = await OpenConnectionAsync();
        await using (connection)
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM \"{schemaName}\".\"{tableName}\"";
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt64(result);
        }
    }

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
        DateDiffFilter? dateDiffFilter = null,
        DateDiffAggregate? dateDiffAggregate = null,
        string? havingFilter = null,
        string? thirdGroupByColumn = null,
        string? thirdGroupByDatePart = null,
        IEnumerable<QueryFilter>? excludeFilters = null,
        IEnumerable<DatePartFilter>? excludeDatePartFilters = null,
        bool countGroupsOnly = false)
    {
        ValidateTableName(tableName);

        var upperFn = aggregateFunction
            .Split(',')
            .Select(t => t.Trim().Trim('\'', '"').ToUpperInvariant())
            .FirstOrDefault(t => FormsQuerySqlBuilder.AllowedAggregateFunctions.Contains(t));

        if (upperFn is null)
        {
            throw new ArgumentException($"Aggregate function not allowed: '{aggregateFunction}'. Pass exactly one keyword: COUNT, COUNT_DISTINCT, SUM, AVG, MIN, or MAX.");
        }

        if (upperFn is "COUNT_DISTINCT" or "SUM" or "AVG" or "MIN" or "MAX" && valueColumn is null && dateDiffAggregate is null)
        {
            throw new ArgumentException($"{aggregateFunction} requires either valueColumn or dateDiffValueExpr. To count all rows per group use COUNT (without valueColumn).");
        }

        if (valueColumn != null && !allowedColumns.Contains(valueColumn, StringComparer.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException($"Unknown column '{valueColumn}'. Available: {string.Join(", ", allowedColumns)}");
        }

        if (dateDiffAggregate != null)
        {
            if (!allowedColumns.Contains(dateDiffAggregate.StartColumn, StringComparer.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException($"Unknown column in dateDiffValueExpr: '{dateDiffAggregate.StartColumn}'. Available: {string.Join(", ", allowedColumns)}");
            }
            if (!allowedColumns.Contains(dateDiffAggregate.EndColumn, StringComparer.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException($"Unknown column in dateDiffValueExpr: '{dateDiffAggregate.EndColumn}'. Available: {string.Join(", ", allowedColumns)}");
            }
        }

        FormsQuerySqlBuilder.ValidateGroupBy(groupByColumn, groupByDatePart, "GROUP BY", allowedColumns);
        FormsQuerySqlBuilder.ValidateGroupBy(secondGroupByColumn, secondGroupByDatePart, "second GROUP BY", allowedColumns);
        FormsQuerySqlBuilder.ValidateGroupBy(thirdGroupByColumn, thirdGroupByDatePart, "third GROUP BY", allowedColumns);

        var datePartFilterList = FormsQuerySqlBuilder.MergeDatePartFilters(datePartFilters?.ToList() ?? []);
        FormsQuerySqlBuilder.ValidateDatePartFilters(datePartFilterList, allowedColumns);

        if (dateDiffFilter != null)
        {
            FormsQuerySqlBuilder.ValidateDateDiffFilter(dateDiffFilter, allowedColumns);
        }

        var filterList = filters?.ToList() ?? [];
        FormsQuerySqlBuilder.ValidateFilters(filterList, allowedColumns);

        var excludeFilterList = excludeFilters?.ToList() ?? [];
        FormsQuerySqlBuilder.ValidateFilters(excludeFilterList, allowedColumns);

        var excludeDatePartFilterList = FormsQuerySqlBuilder.MergeDatePartFilters(excludeDatePartFilters?.ToList() ?? []);
        FormsQuerySqlBuilder.ValidateDatePartFilters(excludeDatePartFilterList, allowedColumns);

        string? innerExpr = dateDiffAggregate != null
            ? FormsQuerySqlBuilder.BuildDateDiffExpr(dateDiffAggregate.StartColumn, dateDiffAggregate.EndColumn, DbType, Q, dateDiffAggregate.Unit, inclusive: true)
            : valueColumn != null ? $"{Q}{valueColumn}{Q}" : null;

        var aggExpr = upperFn switch
        {
            "COUNT" when innerExpr is null => "COUNT(*)",
            "COUNT" => $"COUNT({innerExpr})",
            "COUNT_DISTINCT" => $"COUNT(DISTINCT {innerExpr})",
            _ => $"{upperFn}({innerExpr})"
        };

        var selectParts = new List<string>();
        var groupByParts = new List<string>();

        AddGroupByParts(groupByColumn, groupByDatePart);
        AddGroupByParts(secondGroupByColumn, secondGroupByDatePart);
        AddGroupByParts(thirdGroupByColumn, thirdGroupByDatePart);

        selectParts.Add($"{aggExpr} AS result");

        void AddGroupByParts(string? column, string? datePart)
        {
            if (column is null)
            {
                return;
            }

            var expr = datePart != null
                ? FormsQuerySqlBuilder.BuildDatePartExpr(column, datePart, DbType, Q)
                : $"{Q}{column}{Q}";
            var alias = datePart != null
                ? $"{Q}{column}_{datePart.ToLowerInvariant()}{Q}"
                : $"{Q}{column}{Q}";
            selectParts.Add($"{expr} AS {alias}");
            groupByParts.Add(expr);
        }

        var (connection, schemaName) = await OpenConnectionAsync();
        await using (connection)
        {
            var qualifiedTable = $"{Q}{schemaName}{Q}.{Q}{tableName}{Q}";
            var sql = new StringBuilder($"SELECT {string.Join(", ", selectParts)} FROM {qualifiedTable}");

            var whereParts = new List<string>();
            var parameters = new Dictionary<string, object?>();
            var paramIndex = 0;

            FormsQuerySqlBuilder.BuildFilterClauses(filterList, allowedColumns, Q, ref paramIndex, parameters, whereParts);
            FormsQuerySqlBuilder.AppendDatePartClauses(datePartFilterList, DbType, Q, whereParts);

            if (dateDiffFilter != null)
            {
                var ddExpr = FormsQuerySqlBuilder.BuildDateDiffExpr(dateDiffFilter.StartColumn, dateDiffFilter.EndColumn, DbType, Q, dateDiffFilter.Unit);
                whereParts.Add($"{ddExpr} {dateDiffFilter.Operator} {dateDiffFilter.Value}");
            }

            if (groupByColumn != null && (excludeFilterList.Count > 0 || excludeDatePartFilterList.Count > 0))
            {
                var subWhereParts = new List<string>();
                FormsQuerySqlBuilder.BuildFilterClauses(excludeFilterList, allowedColumns, Q, ref paramIndex, parameters, subWhereParts);
                FormsQuerySqlBuilder.AppendDatePartClauses(excludeDatePartFilterList, DbType, Q, subWhereParts);
                var subWhere = subWhereParts.Count > 0 ? $" WHERE {string.Join(" AND ", subWhereParts)}" : string.Empty;
                whereParts.Add($"{Q}{groupByColumn}{Q} NOT IN (SELECT DISTINCT {Q}{groupByColumn}{Q} FROM {qualifiedTable}{subWhere})");
            }

            if (whereParts.Count > 0)
            {
                sql.Append($" WHERE {string.Join(" AND ", whereParts)}");
            }

            if (groupByParts.Count > 0)
            {
                sql.Append($" GROUP BY {string.Join(", ", groupByParts)}");

                if (!string.IsNullOrWhiteSpace(havingFilter))
                {
                    var havingParts = havingFilter.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                    if (havingParts.Length != 2 ||
                        !FormsQuerySqlBuilder.DatePartFilterOperators.Contains(havingParts[0]) ||
                        !double.TryParse(havingParts[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out _))
                    {
                        throw new ArgumentException(
                            $"Invalid having filter '{havingFilter}'. Expected format: 'OPERATOR value' (e.g. '> 5'). " +
                            $"Operators: =, !=, <, >, <=, >=.");
                    }

                    sql.Append($" HAVING {aggExpr} {havingParts[0]} {havingParts[1]}");
                }

                if (countGroupsOnly)
                {
                    var innerSql = sql.ToString();
                    sql.Clear();
                    sql.Append($"SELECT COUNT(*) AS result FROM ({innerSql}) AS sub");
                }
                else
                {
                    sql.Append(" ORDER BY result DESC LIMIT 1000");
                }
            }

            await using var cmd = connection.CreateCommand();
            cmd.CommandText = sql.ToString();
            cmd.CommandTimeout = 60;

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

        var selectList = selectColumns?.ToList();
        if (selectList is { Count: > 0 })
        {
            var invalid = selectList.Except(allowedColumns, StringComparer.OrdinalIgnoreCase).ToList();
            if (invalid.Count > 0)
            {
                throw new UnauthorizedAccessException($"Unknown columns: {string.Join(", ", invalid)}. Available: {string.Join(", ", allowedColumns)}");
            }
        }

        var filterList = filters?.ToList() ?? [];
        FormsQuerySqlBuilder.ValidateFilters(filterList, allowedColumns);

        if (orderByColumn != null && !allowedColumns.Contains(orderByColumn, StringComparer.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException($"Unknown column in ORDER BY: {orderByColumn}. Available: {string.Join(", ", allowedColumns)}");
        }

        if (thenByColumn != null && !allowedColumns.Contains(thenByColumn, StringComparer.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException($"Unknown column in ORDER BY: {thenByColumn}. Available: {string.Join(", ", allowedColumns)}");
        }

        var datePartFilterList = FormsQuerySqlBuilder.MergeDatePartFilters(datePartFilters?.ToList() ?? []);
        FormsQuerySqlBuilder.ValidateDatePartFilters(datePartFilterList, allowedColumns);

        if (dateDiffFilter != null)
        {
            FormsQuerySqlBuilder.ValidateDateDiffFilter(dateDiffFilter, allowedColumns);
        }

        var selectPart = selectList is { Count: > 0 }
            ? string.Join(", ", selectList.Select(c => $"{Q}{c}{Q}"))
            : "*";

        var whereParts = new List<string>();
        var parameters = new Dictionary<string, object?>();
        var paramIndex = 0;

        FormsQuerySqlBuilder.BuildFilterClauses(filterList, allowedColumns, Q, ref paramIndex, parameters, whereParts);
        FormsQuerySqlBuilder.AppendDatePartClauses(datePartFilterList, DbType, Q, whereParts);

        if (dateDiffFilter != null)
        {
            var ddExpr = FormsQuerySqlBuilder.BuildDateDiffExpr(dateDiffFilter.StartColumn, dateDiffFilter.EndColumn, DbType, Q, dateDiffFilter.Unit);
            whereParts.Add($"{ddExpr} {dateDiffFilter.Operator} {dateDiffFilter.Value}");
        }

        var (connection, schemaName) = await OpenConnectionAsync();
        await using (connection)
        {
            var qualifiedTable = $"{Q}{schemaName}{Q}.{Q}{tableName}{Q}";
            var sql = new StringBuilder($"SELECT {selectPart} FROM {qualifiedTable}");

            if (whereParts.Count > 0)
            {
                sql.Append($" WHERE {string.Join(" AND ", whereParts)}");
            }

            if (orderByColumn != null)
            {
                var dir = orderByDescending ? "DESC" : "ASC";
                sql.Append($" ORDER BY {Q}{orderByColumn}{Q} {dir}");
                if (thenByColumn != null)
                {
                    var thenDir = thenByDescending ? "DESC" : "ASC";
                    sql.Append($", {Q}{thenByColumn}{Q} {thenDir}");
                }
            }

            var pageSize = Math.Clamp(maxRows, 1, FormsQuerySqlBuilder.MaxRowsPerRequest);
            var pageOffset = Math.Max(0, offset);
            sql.Append($" LIMIT {pageSize} OFFSET {pageOffset}");

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
    }

    public async Task<string> SelfJoinAsync(
        string tableName,
        IReadOnlyCollection<string> allowedColumns,
        string pkColumn,
        IEnumerable<SelfJoinCondition> joinConditions,
        IEnumerable<string>? displayColumns = null,
        int limit = 100,
        IEnumerable<QueryFilter>? filters = null,
        IEnumerable<DatePartFilter>? datePartFilters = null,
        string? countDistinctColumn = null)
    {
        ValidateTableName(tableName);

        if (!allowedColumns.Contains(pkColumn, StringComparer.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException($"Unknown PK column: {pkColumn}");
        }

        var conditionList = joinConditions
            .Select(c => new SelfJoinCondition(
                FormsQuerySqlBuilder.NormalizeColumnRef(c.LeftColumn, allowedColumns),
                c.Operator,
                FormsQuerySqlBuilder.NormalizeColumnRef(c.RightColumn, allowedColumns),
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

            if (!FormsQuerySqlBuilder.AllowedJoinOperators.Contains(cond.Operator))
            {
                throw new UnauthorizedAccessException($"Operator not allowed in join condition: {cond.Operator}");
            }
        }

        var displayList = (displayColumns ?? [])
            .Select(col => FormsQuerySqlBuilder.NormalizeColumnRef(col, allowedColumns))
            .ToList();
        foreach (var col in displayList)
        {
            if (!allowedColumns.Contains(col, StringComparer.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException($"Unknown display column: {col}");
            }
        }

        if (countDistinctColumn != null)
        {
            var normalized = FormsQuerySqlBuilder.NormalizeColumnRef(countDistinctColumn, allowedColumns);
            if (!allowedColumns.Contains(normalized, StringComparer.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"Unknown countDistinctColumn: '{countDistinctColumn}'. Available: {string.Join(", ", allowedColumns)}");
            }

            countDistinctColumn = normalized;
        }

        var filterList = filters?.ToList() ?? [];
        FormsQuerySqlBuilder.ValidateFilters(filterList, allowedColumns);

        var datePartFilterList = FormsQuerySqlBuilder.MergeDatePartFilters(datePartFilters?.ToList() ?? []);
        FormsQuerySqlBuilder.ValidateDatePartFilters(datePartFilterList, allowedColumns);

        var pageSize = Math.Clamp(limit, 1, FormsQuerySqlBuilder.MaxRowsPerRequest);

        List<string> selectParts;
        if (countDistinctColumn != null)
        {
            selectParts = [$"COUNT(DISTINCT a.{Q}{countDistinctColumn}{Q}) AS {Q}count{Q}"];
        }
        else
        {
            selectParts = [$"a.{Q}{pkColumn}{Q} AS {Q}a_pk{Q}"];
            selectParts.AddRange(displayList.Select(col => $"a.{Q}{col}{Q} AS {Q}a_{col}{Q}"));
            selectParts.Add($"b.{Q}{pkColumn}{Q} AS {Q}b_pk{Q}");
            selectParts.AddRange(displayList.Select(col => $"b.{Q}{col}{Q} AS {Q}b_{col}{Q}"));
        }

        var whereParts = conditionList.Select(c =>
        {
            if (c.DatePart != null)
            {
                var leftExpr = FormsQuerySqlBuilder.BuildDatePartExpr(c.LeftColumn, c.DatePart, DbType, Q)
                    .Replace($"{Q}{c.LeftColumn}{Q}", $"a.{Q}{c.LeftColumn}{Q}");
                var rightExpr = FormsQuerySqlBuilder.BuildDatePartExpr(c.RightColumn, c.DatePart, DbType, Q)
                    .Replace($"{Q}{c.RightColumn}{Q}", $"b.{Q}{c.RightColumn}{Q}");
                return $"{leftExpr} {c.Operator} {rightExpr}";
            }
            return $"a.{Q}{c.LeftColumn}{Q} {c.Operator} b.{Q}{c.RightColumn}{Q}";
        }).ToList();

        var parameters = new Dictionary<string, object?>();
        var paramIndex = 0;
        foreach (var f in filterList)
        {
            var aCol = $"a.{Q}{f.Column}{Q}";
            var bCol = $"b.{Q}{f.Column}{Q}";
            var op = f.Operator.ToUpperInvariant();
            if (FormsQuerySqlBuilder.NullaryOperators.Contains(op))
            {
                whereParts.Add($"{aCol} {op}");
                whereParts.Add($"{bCol} {op}");
            }
            else if (op is "IN" or "NOT IN")
            {
                var inParamNames = (f.Value ?? "").Split(',').Select((v, i) =>
                {
                    var pn = $"@w{paramIndex++}";
                    parameters[pn] = v.Trim();
                    return pn;
                }).ToList();
                var inList = string.Join(", ", inParamNames);
                whereParts.Add($"{aCol} {op} ({inList})");
                whereParts.Add($"{bCol} {op} ({inList})");
            }
            else if (f.Value != null && allowedColumns.Contains(f.Value, StringComparer.OrdinalIgnoreCase))
            {
                whereParts.Add($"{aCol} {op} a.{Q}{f.Value}{Q}");
                whereParts.Add($"{bCol} {op} b.{Q}{f.Value}{Q}");
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
            var dpExpr = FormsQuerySqlBuilder.BuildDatePartExpr(dpf.Column, dpf.DatePart, DbType, Q);
            var aExpr = dpExpr.Replace($"{Q}{dpf.Column}{Q}", $"a.{Q}{dpf.Column}{Q}");
            var bExpr = dpExpr.Replace($"{Q}{dpf.Column}{Q}", $"b.{Q}{dpf.Column}{Q}");
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

        var (connection, schemaName) = await OpenConnectionAsync();
        await using (connection)
        {
            var qualifiedTable = $"{Q}{schemaName}{Q}.{Q}{tableName}{Q}";

            var sql = countDistinctColumn != null
                ? $"SELECT COUNT(DISTINCT a.{Q}{countDistinctColumn}{Q}) AS {Q}count{Q} " +
                  $"FROM {qualifiedTable} a " +
                  $"WHERE EXISTS (" +
                  $"SELECT 1 FROM {qualifiedTable} b " +
                  $"WHERE a.{Q}{pkColumn}{Q} != b.{Q}{pkColumn}{Q} " +
                  $"AND {string.Join(" AND ", whereParts)})"
                : $"SELECT {string.Join(", ", selectParts)} " +
                  $"FROM {qualifiedTable} a " +
                  $"JOIN {qualifiedTable} b ON a.{Q}{pkColumn}{Q} < b.{Q}{pkColumn}{Q} " +
                  $"WHERE {string.Join(" AND ", whereParts)} " +
                  $"LIMIT {pageSize}";

            await using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandTimeout = 120;

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
    }

    private static string BuildPgCreateTable(string schemaName, string tableName, IEnumerable<DbColumnDefinition> columns)
    {
        var colDefs = columns.Select(c =>
        {
            var type = MapPgType(c);
            return c.IsPrimaryKey
                ? $"\"{c.Name}\" {type} PRIMARY KEY"
                : $"\"{c.Name}\" {type}";
        });
        return $"CREATE TABLE IF NOT EXISTS \"{schemaName}\".\"{tableName}\" ({string.Join(", ", colDefs)});";
    }

    private static string MapPgType(DbColumnDefinition col) => col.Type switch
    {
        DbColumnType.Integer => "INTEGER",
        DbColumnType.Boolean => "BOOLEAN",
        DbColumnType.Date => "DATE",
        DbColumnType.DateTime => "TIMESTAMP",
        DbColumnType.Enum when col.EnumValues?.Count > 0 =>
            $"TEXT CHECK (\"{col.Name}\" IN ({string.Join(", ", col.EnumValues.Select(v => $"'{v.Replace("'", "''")}'"))}))",
        DbColumnType.Enum => "TEXT",
        _ => "TEXT"
    };

    private static string BuildPgUpsert(string schemaName, string tableName, List<string> keys, string keyColumn)
    {
        var columns = string.Join(", ", keys.Select(k => $"\"{k}\""));
        var parameters = string.Join(", ", keys.Select(k => $"@{k}"));
        var updateParts = string.Join(", ", keys
            .Where(k => k != keyColumn)
            .Select(k => $"\"{k}\" = EXCLUDED.\"{k}\""));

        return $"INSERT INTO \"{schemaName}\".\"{tableName}\" ({columns}) VALUES ({parameters}) " +
               $"ON CONFLICT (\"{keyColumn}\") DO UPDATE SET {updateParts};";
    }
}

internal static partial class BuiltinFormsDatabaseClientLogger
{
    [LoggerMessage(LogLevel.Error, "Builtin forms DB insert into table {TableName} failed")]
    public static partial void ErrorBuiltinInsertFailed(this ILogger<BuiltinFormsDatabaseClient> logger, Exception exception, string tableName);
}

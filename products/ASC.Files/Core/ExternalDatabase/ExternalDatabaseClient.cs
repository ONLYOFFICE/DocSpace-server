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

using ASC.FederatedLogin.DatabaseProviders;

namespace ASC.Files.Core.ExternalDatabase;

public enum DbColumnType { Text, Integer, Boolean, Date, DateTime, Enum }

public record DbColumnDefinition(string Name, DbColumnType Type, IReadOnlyList<string>? EnumValues = null, bool IsPrimaryKey = false);

/// <summary>Column filter for structured queries against an external database table.</summary>
/// <param name="Column">Name of the column to filter on.</param>
/// <param name="Operator">Comparison operator: =, !=, &lt;, &gt;, &lt;=, &gt;=, LIKE, NOT LIKE, IS NULL, IS NOT NULL.</param>
/// <param name="Value">Value to compare against. Not required for IS NULL / IS NOT NULL.</param>
public record QueryFilter(string Column, string Operator, string? Value = null);

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
        await using var connection = Provider.CreateConnection();
        await connection.OpenAsync();
        await SetupSqliteAsync(connection);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = Provider.DatabaseType.ToLowerInvariant() switch
        {
            "mysql" => BuildMySqlCreateTable(tableName, columns),
            "sqlite" => BuildSqliteCreateTable(tableName, columns),
            _ => throw new NotSupportedException($"Database type '{Provider.DatabaseType}' is not supported yet.")
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
        await using var connection = Provider.CreateConnection();
        await connection.OpenAsync();
        await SetupSqliteAsync(connection);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = Provider.DatabaseType.ToLowerInvariant() switch
        {
            "mysql" => "SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME=@tableName",
            "sqlite" => "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=@tableName",
            _ => throw new NotSupportedException($"Database type '{Provider.DatabaseType}' is not supported yet.")
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
        await using var connection = Provider.CreateConnection();
        await connection.OpenAsync();
        await SetupSqliteAsync(connection);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = Provider.DatabaseType.ToLowerInvariant() switch
        {
            "mysql" => $"SELECT COUNT(*) FROM `{tableName}`",
            "sqlite" => $"SELECT COUNT(*) FROM \"{tableName}\"",
            _ => throw new NotSupportedException($"Database type '{Provider.DatabaseType}' is not supported yet.")
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

    public const int MaxRowsPerRequest = 500;

    public async Task<string> QueryAsync(
        string tableName,
        IReadOnlyCollection<string> allowedColumns,
        IEnumerable<string>? selectColumns = null,
        IEnumerable<QueryFilter>? filters = null,
        string? orderByColumn = null,
        bool orderByDescending = false,
        int maxRows = 50,
        int offset = 0)
    {
        ValidateTableName(tableName);
        var dbType = Provider.DatabaseType.ToLowerInvariant();
        var q = dbType == "mysql" ? '`' : '"';

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
        foreach (var f in filterList)
        {
            if (!allowedColumns.Contains(f.Column, StringComparer.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException($"Unknown column in filter: {f.Column}");
            }
            if (!_allowedOperators.Contains(f.Operator))
            {
                throw new UnauthorizedAccessException($"Operator not allowed: {f.Operator}");
            }
        }

        if (orderByColumn != null && !allowedColumns.Contains(orderByColumn, StringComparer.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException($"Unknown column in ORDER BY: {orderByColumn}");
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
            else
            {
                var paramName = $"@w{paramIndex++}";
                whereParts.Add($"{colQuoted} {op} {paramName}");
                parameters[paramName] = f.Value;
            }
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
        }

        var pageSize = Math.Clamp(maxRows, 1, MaxRowsPerRequest);
        var pageOffset = Math.Max(0, offset);
        sql.Append($" LIMIT {pageSize} OFFSET {pageOffset}");

        await using var connection = Provider.CreateConnection();
        await connection.OpenAsync();
        await SetupSqliteAsync(connection);

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

        return System.Text.Json.JsonSerializer.Serialize(results);
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

        await using var connection = Provider.CreateConnection();
        await connection.OpenAsync();
        await SetupSqliteAsync(connection);

        var dbType = Provider.DatabaseType.ToLowerInvariant();
        // SQLite only: MySQL DDL (CREATE TABLE) causes an implicit commit,
        // making it impossible to wrap CREATE TABLE + INSERT in one atomic transaction.
        DbTransaction? tx = dbType == "sqlite" ? await connection.BeginTransactionAsync() : null;

        try
        {
            await using var createCmd = connection.CreateCommand();
            createCmd.Transaction = tx;
            createCmd.CommandText = dbType switch
            {
                "mysql" => BuildMySqlCreateTable(tableName, columns),
                "sqlite" => BuildSqliteCreateTable(tableName, columns),
                _ => throw new NotSupportedException($"Database type '{Provider.DatabaseType}' is not supported yet.")
            };
            await createCmd.ExecuteNonQueryAsync();

            await using var insertCmd = connection.CreateCommand();
            insertCmd.Transaction = tx;
            insertCmd.CommandText = BuildInsertSql(tableName, data.Keys, keyColumn);
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
            await using var connection = Provider.CreateConnection();
            await connection.OpenAsync();
            await SetupSqliteAsync(connection);

            await using var cmd = connection.CreateCommand();
            cmd.CommandText = BuildInsertSql(tableName, data.Keys, keyColumn);

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
            logger.LogError(ex, "Insert into table {TableName} failed", tableName);
            throw;
        }
    }

    private string BuildInsertSql(string tableName, IEnumerable<string> keys, string? keyColumn)
    {
        var keyList = keys.ToList();
        var parameters = string.Join(", ", keyList.Select(k => $"@{k}"));

        return Provider.DatabaseType.ToLowerInvariant() switch
        {
            "mysql" => BuildMySqlInsert(tableName, keyList, parameters, keyColumn),
            "sqlite" => BuildSqliteInsert(tableName, keyList, parameters, keyColumn),
            _ => throw new NotSupportedException($"Database type '{Provider.DatabaseType}' is not supported yet.")
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

    private async Task SetupSqliteAsync(DbConnection connection)
    {
        if (Provider.DatabaseType?.ToLowerInvariant() != "sqlite")
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

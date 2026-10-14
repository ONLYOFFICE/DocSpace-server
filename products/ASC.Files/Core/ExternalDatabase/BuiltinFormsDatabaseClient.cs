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
/// Runs the external-database SQL against the portal's own schema in the built-in PostgreSQL forms database.
/// </summary>
[Scope]
public class BuiltinFormsDatabaseClient(
    FormsDbProvisioningService provisioner,
    TenantManager tenantManager,
    IFusionCacheProvider cacheProvider,
    ConsumerFactory consumerFactory,
    ILogger<ExternalDatabaseClient> logger)
    : ExternalDatabaseClient(consumerFactory, logger)
{
    // Last use of each form table, for dropping the idle ones; form tables are named form_{id}_v{version}.
    private const string ActivityTable = "table_activity";
    private const string FormTablePattern = "^form_[0-9]+_v[0-9]+$";

    private static readonly TimeSpan _touchInterval = TimeSpan.FromHours(1);
    private static readonly Regex _formTableRegex = new(FormTablePattern, RegexOptions.Compiled);
    private static readonly Regex _columnNameRegex = new("^[a-z0-9_]+$", RegexOptions.Compiled);

    private readonly IFusionCache _cache = cacheProvider.GetMemoryCache();

    public override bool IsEnabled() => provisioner.IsEnabled();

    protected override ExternalDatabaseType DatabaseType => ExternalDatabaseType.PostgreSql;

    // The read-only account is left to portal administrators, so its connection limit never blocks the server.
    protected override async Task<DbConnection> CreateConnectionAsync(ExternalDatabaseType dbType)
    {
        var credentials = await provisioner.GetOrProvisionAsync(tenantManager.GetCurrentTenantId());
        return new NpgsqlConnection(credentials.RwConnectionString);
    }

    public static string GetActivityCacheKey(int tenantId, string tableName) => tenantId + "formsdbactivity" + tableName;

    /// <summary>Records that the table is in use; written at most once an hour per table.</summary>
    public async Task TouchTableAsync(string tableName)
    {
        await _cache.GetOrSetAsync<bool>(GetActivityCacheKey(tenantManager.GetCurrentTenantId(), tableName), async (_, _) =>
        {
            await ExecuteAsync(
                $"CREATE TABLE IF NOT EXISTS \"{ActivityTable}\" (\"table_name\" TEXT PRIMARY KEY, \"last_access\" TIMESTAMP NOT NULL); " +
                $"INSERT INTO \"{ActivityTable}\" (\"table_name\", \"last_access\") VALUES (@tableName, LOCALTIMESTAMP) " +
                "ON CONFLICT (\"table_name\") DO UPDATE SET \"last_access\" = EXCLUDED.\"last_access\"",
                ("@tableName", tableName));
            return true;
        }, _touchInterval);
    }

    /// <summary>
    /// The form tables not used since <paramref name="cutoff"/> (UTC). A table with no recorded use gets one now, so it
    /// becomes idle only after a full idle period.
    /// </summary>
    public async Task<List<string>> GetIdleTablesAsync(DateTime cutoff)
    {
        await using var connection = await CreateConnectionAsync(DatabaseType);
        await connection.OpenAsync();

        await using var cmd = connection.CreateCommand();
        cmd.CommandText =
            $"CREATE TABLE IF NOT EXISTS \"{ActivityTable}\" (\"table_name\" TEXT PRIMARY KEY, \"last_access\" TIMESTAMP NOT NULL); " +
            $"INSERT INTO \"{ActivityTable}\" (\"table_name\", \"last_access\") " +
            "SELECT table_name, LOCALTIMESTAMP FROM information_schema.tables " +
            "WHERE table_schema = current_schema() AND table_name ~ @pattern ON CONFLICT (\"table_name\") DO NOTHING; " +
            $"SELECT \"table_name\" FROM \"{ActivityTable}\" WHERE \"last_access\" < @cutoff";
        cmd.Parameters.Add(new NpgsqlParameter("@pattern", FormTablePattern));
        cmd.Parameters.Add(new NpgsqlParameter("@cutoff", cutoff));

        var tables = new List<string>();
        await using var reader = await cmd.ExecuteReaderAsync();
        do
        {
            while (await reader.ReadAsync())
            {
                tables.Add(reader.GetString(0));
            }
        }
        while (await reader.NextResultAsync());

        return tables;
    }

    /// <summary>
    /// Drops the table if it is still unused since <paramref name="cutoff"/> (UTC); the check and the drop are one
    /// transaction. Returns whether it was dropped.
    /// </summary>
    public async Task<bool> DropIdleTableAsync(string tableName, DateTime cutoff)
    {
        if (!_formTableRegex.IsMatch(tableName))
        {
            return false;
        }

        await using var connection = await CreateConnectionAsync(DatabaseType);
        await connection.OpenAsync();
        await using var tx = await connection.BeginTransactionAsync();

        await using var deleteCmd = connection.CreateCommand();
        deleteCmd.Transaction = tx;
        deleteCmd.CommandText = $"DELETE FROM \"{ActivityTable}\" WHERE \"table_name\" = @tableName AND \"last_access\" < @cutoff";
        deleteCmd.Parameters.Add(new NpgsqlParameter("@tableName", tableName));
        deleteCmd.Parameters.Add(new NpgsqlParameter("@cutoff", cutoff));

        if (await deleteCmd.ExecuteNonQueryAsync() == 0)
        {
            return false;
        }

        await using var dropCmd = connection.CreateCommand();
        dropCmd.Transaction = tx;
        dropCmd.CommandText = $"DROP TABLE IF EXISTS \"{tableName}\"";
        await dropCmd.ExecuteNonQueryAsync();

        await tx.CommitAsync();
        await _cache.RemoveAndNotifyAsync(GetActivityCacheKey(tenantManager.GetCurrentTenantId(), tableName));

        return true;
    }

    /// <summary>
    /// Renames the columns of a form table and adds the missing ones in one transaction. Changes nothing when a column
    /// would take the name of another one or keep its name under another type: the data of two fields would mix.
    /// </summary>
    public async Task<FormTableChange> AlterTableAsync(string tableName, IReadOnlyCollection<(string OldName, string NewName)> renames, IEnumerable<DbColumnDefinition> columns)
    {
        ValidateFormTableName(tableName);

        await using var connection = await CreateConnectionAsync(DatabaseType);
        await connection.OpenAsync();
        await using var tx = await connection.BeginTransactionAsync();

        var existing = await GetColumnTypesAsync(connection, tx, tableName);
        if (existing.Count == 0)
        {
            return FormTableChange.Missing;
        }

        var statements = new List<string>();
        if (!TryRename(tableName, existing, renames, statements))
        {
            return FormTableChange.Conflict;
        }

        foreach (var column in columns)
        {
            var type = GetDataType(column.Type);
            if (existing.TryGetValue(ValidateColumnName(column.Name), out var existingType))
            {
                if (existingType != type)
                {
                    return FormTableChange.Conflict;
                }

                continue;
            }

            existing[column.Name] = type;
            statements.Add($"ALTER TABLE \"{tableName}\" ADD COLUMN \"{column.Name}\" {MapPgType(column)}");
        }

        await ExecuteAsync(connection, tx, statements);
        await tx.CommitAsync();

        return FormTableChange.Changed;
    }

    /// <summary>
    /// Renames the columns of the target table, adds the columns of the source one and copies its rows, in one
    /// transaction. A row whose form_id the target already holds is kept as it is there. Changes nothing on the same
    /// conflicts as <see cref="AlterTableAsync"/>.
    /// </summary>
    public async Task<FormTableChange> MergeTableAsync(string targetTable, string sourceTable, IReadOnlyCollection<(string OldName, string NewName)> renames)
    {
        ValidateFormTableName(targetTable);
        ValidateFormTableName(sourceTable);

        await using var connection = await CreateConnectionAsync(DatabaseType);
        await connection.OpenAsync();
        await using var tx = await connection.BeginTransactionAsync();

        var target = await GetColumnTypesAsync(connection, tx, targetTable);
        var source = await GetColumnTypesAsync(connection, tx, sourceTable);
        if (target.Count == 0 || source.Count == 0)
        {
            return FormTableChange.Missing;
        }

        var statements = new List<string>();
        if (!TryRename(targetTable, target, renames, statements))
        {
            return FormTableChange.Conflict;
        }

        foreach (var (name, type) in source)
        {
            if (target.TryGetValue(ValidateColumnName(name), out var targetType))
            {
                if (targetType != type)
                {
                    return FormTableChange.Conflict;
                }

                continue;
            }

            statements.Add($"ALTER TABLE \"{targetTable}\" ADD COLUMN \"{name}\" {type}");
        }

        var columnList = string.Join(", ", source.Keys.Select(c => $"\"{c}\""));
        statements.Add($"INSERT INTO \"{targetTable}\" ({columnList}) SELECT {columnList} FROM \"{sourceTable}\" ON CONFLICT (\"form_id\") DO NOTHING");

        await ExecuteAsync(connection, tx, statements);
        await tx.CommitAsync();

        return FormTableChange.Changed;
    }

    /// <summary>
    /// Drops a table merged into another one, only when every row of it is in the target. Returns whether it was dropped.
    /// </summary>
    public async Task<bool> DropMergedTableAsync(string sourceTable, string targetTable)
    {
        ValidateFormTableName(targetTable);
        ValidateFormTableName(sourceTable);

        await using var connection = await CreateConnectionAsync(DatabaseType);
        await connection.OpenAsync();
        await using var tx = await connection.BeginTransactionAsync();

        await using (var checkCmd = connection.CreateCommand())
        {
            checkCmd.Transaction = tx;
            checkCmd.CommandText =
                $"SELECT EXISTS (SELECT 1 FROM \"{sourceTable}\" s WHERE NOT EXISTS " +
                $"(SELECT 1 FROM \"{targetTable}\" t WHERE t.\"form_id\" = s.\"form_id\"))";
            if (await checkCmd.ExecuteScalarAsync() is true)
            {
                return false;
            }
        }

        await using (var dropCmd = connection.CreateCommand())
        {
            dropCmd.Transaction = tx;
            dropCmd.CommandText =
                $"DROP TABLE \"{sourceTable}\"; " +
                $"CREATE TABLE IF NOT EXISTS \"{ActivityTable}\" (\"table_name\" TEXT PRIMARY KEY, \"last_access\" TIMESTAMP NOT NULL); " +
                $"DELETE FROM \"{ActivityTable}\" WHERE \"table_name\" = @tableName";
            dropCmd.Parameters.Add(new NpgsqlParameter("@tableName", sourceTable));
            await dropCmd.ExecuteNonQueryAsync();
        }

        await tx.CommitAsync();
        await _cache.RemoveAndNotifyAsync(GetActivityCacheKey(tenantManager.GetCurrentTenantId(), sourceTable));

        return true;
    }

    private static void ValidateFormTableName(string tableName)
    {
        if (!_formTableRegex.IsMatch(tableName))
        {
            throw new ArgumentException($"Invalid table name: {tableName}", nameof(tableName));
        }
    }

    // Column name to its information_schema data type; empty when the table does not exist.
    private static async Task<Dictionary<string, string>> GetColumnTypesAsync(DbConnection connection, DbTransaction tx, string tableName)
    {
        await using var cmd = connection.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText =
            "SELECT column_name, data_type FROM information_schema.columns " +
            "WHERE table_schema = current_schema() AND table_name = @tableName ORDER BY ordinal_position";
        cmd.Parameters.Add(new NpgsqlParameter("@tableName", tableName));

        var columns = new Dictionary<string, string>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            columns[reader.GetString(0)] = reader.GetString(1);
        }

        return columns;
    }

    // Renames in order; false when a new name is taken by another column.
    private static bool TryRename(string tableName, Dictionary<string, string> columns, IEnumerable<(string OldName, string NewName)> renames, List<string> statements)
    {
        foreach (var rename in renames)
        {
            var oldName = ValidateColumnName(rename.OldName);
            var newName = ValidateColumnName(rename.NewName);
            if (oldName == newName || !columns.Remove(oldName, out var type))
            {
                continue;
            }

            if (!columns.TryAdd(newName, type))
            {
                return false;
            }

            statements.Add($"ALTER TABLE \"{tableName}\" RENAME COLUMN \"{oldName}\" TO \"{newName}\"");
        }

        return true;
    }

    private static string GetDataType(DbColumnType type) => type switch
    {
        DbColumnType.Integer => "integer",
        DbColumnType.Boolean => "boolean",
        DbColumnType.Date => "date",
        DbColumnType.DateTime => "timestamp without time zone",
        _ => "text"
    };

    private static async Task ExecuteAsync(DbConnection connection, DbTransaction tx, IEnumerable<string> statements)
    {
        foreach (var statement in statements)
        {
            await using var cmd = connection.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = statement;
            await cmd.ExecuteNonQueryAsync();
        }
    }

    private static string ValidateColumnName(string name)
    {
        return _columnNameRegex.IsMatch(name) ? name : throw new ArgumentException($"Invalid column name: {name}", nameof(name));
    }

    private async Task ExecuteAsync(string sql, params (string Name, object Value)[] parameters)
    {
        await using var connection = await CreateConnectionAsync(DatabaseType);
        await connection.OpenAsync();

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (name, value) in parameters)
        {
            cmd.Parameters.Add(new NpgsqlParameter(name, value));
        }

        await cmd.ExecuteNonQueryAsync();
    }
}

public enum FormTableChange
{
    Changed,
    Missing,
    Conflict
}

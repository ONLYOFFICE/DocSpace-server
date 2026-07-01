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
    private static readonly Regex _tableNameRegex = new(@"^[a-zA-Z0-9_]+$", RegexOptions.Compiled);

    public bool IsEnabled() => provisioner.IsEnabled();

    private static void ValidateTableName(string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName) || !_tableNameRegex.IsMatch(tableName))
        {
            throw new ArgumentException($"Invalid table name: '{tableName}'.");
        }
    }

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

    private async Task<bool> TableExistsAsync(string tableName)
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

    private async Task<long> CountAsync(string tableName)
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

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

using MySqlConnector;

#nullable enable
namespace ASC.Files.Core.ExternalDatabase;

[Scope]
public class BuiltinFormsDatabaseClient(
    IConfiguration configuration,
    ILogger<BuiltinFormsDatabaseClient> logger)
    : IFormsDatabaseClient
{
    private static readonly Regex _tableNameRegex = new(@"^[a-zA-Z0-9_]+$", RegexOptions.Compiled);

    private string? ConnectionString => configuration["ConnectionStrings:forms:connectionString"];

    public bool IsEnabled() => !string.IsNullOrWhiteSpace(ConnectionString);

    private static void ValidateTableName(string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName) || !_tableNameRegex.IsMatch(tableName))
        {
            throw new ArgumentException($"Invalid table name: '{tableName}'.");
        }
    }

    private DbConnection CreateConnection() => new MySqlConnection(ConnectionString);

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
            await using var connection = CreateConnection();
            await connection.OpenAsync();

            await using var createCmd = connection.CreateCommand();
            createCmd.CommandText = BuildMySqlCreateTable(tableName, columns);
            await createCmd.ExecuteNonQueryAsync();

            await using var insertCmd = connection.CreateCommand();
            insertCmd.CommandText = BuildMySqlInsert(tableName, data.Keys.ToList(), keyColumn);
            foreach (var kvp in data)
            {
                var param = insertCmd.CreateParameter();
                param.ParameterName = "@" + kvp.Key;
                param.Value = kvp.Value ?? DBNull.Value;
                insertCmd.Parameters.Add(param);
            }
            await insertCmd.ExecuteNonQueryAsync();
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
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT `form_id` FROM `{tableName}`;";

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

    private async Task<bool> TableExistsAsync(string tableName)
    {
        ValidateTableName(tableName);
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME=@tableName";
        var param = cmd.CreateParameter();
        param.ParameterName = "@tableName";
        param.Value = tableName;
        cmd.Parameters.Add(param);

        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt64(result) > 0;
    }

    private async Task<long> CountAsync(string tableName)
    {
        ValidateTableName(tableName);
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM `{tableName}`";
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt64(result);
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

    private static string BuildMySqlInsert(string tableName, List<string> keys, string keyColumn)
    {
        var parameters = string.Join(", ", keys.Select(k => $"@{k}"));
        var columns = string.Join(", ", keys.Select(k => $"`{k}`"));
        var sql = new StringBuilder($"INSERT INTO `{tableName}` ({columns}) VALUES ({parameters})");
        var updateParts = string.Join(", ", keys
            .Where(k => k != keyColumn)
            .Select(k => $"`{k}` = VALUES(`{k}`)"));
        sql.Append($" ON DUPLICATE KEY UPDATE {updateParts}");
        sql.Append(';');
        return sql.ToString();
    }
}

internal static partial class BuiltinFormsDatabaseClientLogger
{
    [LoggerMessage(LogLevel.Error, "Builtin forms DB insert into table {TableName} failed")]
    public static partial void ErrorBuiltinInsertFailed(this ILogger<BuiltinFormsDatabaseClient> logger, Exception exception, string tableName);
}

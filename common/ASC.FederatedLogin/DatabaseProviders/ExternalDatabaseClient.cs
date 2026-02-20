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

using System.Data;

namespace ASC.FederatedLogin.DatabaseProviders;

public enum DbColumnType { Text, Integer, Boolean, Date, DateTime, Enum }

public record DbColumnDefinition(string Name, DbColumnType Type, IReadOnlyList<string>? EnumValues = null, bool IsPrimaryKey = false);

[Scope]
public class ExternalDatabaseClient(ConsumerFactory consumerFactory)
{
    private ExternalDatabaseProvider Provider => consumerFactory.Get<ExternalDatabaseProvider>();

    public bool IsEnabled() => Provider.IsEnabled();

    public async Task CreateTableIfNotExistsAsync(string tableName, IEnumerable<DbColumnDefinition> columns)
    {
        await using var connection = Provider.CreateConnection();
        await connection.OpenAsync();

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
        => throw new NotImplementedException("SQLite support will be added in the future.");

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

    public Task InsertDataAsync(string tableName, Dictionary<string, object> data)
        => ExecuteInsertAsync(tableName, data, keyColumn: null);

    public Task UpsertDataAsync(string tableName, Dictionary<string, object> data, string keyColumn)
        => ExecuteInsertAsync(tableName, data, keyColumn);

    private async Task ExecuteInsertAsync(string tableName, Dictionary<string, object> data, string? keyColumn)
    {
        if (data == null || data.Count == 0)
        {
            throw new ArgumentException("Data dictionary is empty.", nameof(data));
        }
        await using var connection = Provider.CreateConnection();
        await connection.OpenAsync();

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

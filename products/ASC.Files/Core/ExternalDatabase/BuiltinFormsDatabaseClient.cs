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

[Scope]
public class BuiltinFormsDatabaseClient(
    FormsDbProvisioningService provisioner,
    TenantManager tenantManager,
    ILogger<BuiltinFormsDatabaseClient> logger)
    : IFormsDatabaseClient
{
    public bool IsEnabled() => provisioner.IsEnabled();

    // The read-only account is left to portal administrators, so its connection limit never blocks the server.
    private async Task<NpgsqlConnection> OpenConnectionAsync()
    {
        var credentials = await provisioner.GetOrProvisionAsync(tenantManager.GetCurrentTenantId());
        var connection = new NpgsqlConnection(credentials.RwConnectionString);
        await connection.OpenAsync();
        return connection;
    }

    public async Task CreateTableAndUpsertAsync(string tableName, IEnumerable<DbColumnDefinition> columns,
        Dictionary<string, object> data, string keyColumn)
    {
        ExternalDatabaseClient.ValidateTableName(tableName);
        if (data == null || data.Count == 0)
        {
            throw new ArgumentException("Data dictionary is empty.", nameof(data));
        }

        try
        {
            await using var connection = await OpenConnectionAsync();

            await using var createCmd = connection.CreateCommand();
            createCmd.CommandText = ExternalDatabaseClient.BuildPgCreateTable(tableName, columns);
            await createCmd.ExecuteNonQueryAsync();

            await using var insertCmd = connection.CreateCommand();
            insertCmd.CommandText = ExternalDatabaseClient.BuildInsertSql(tableName, data.Keys, keyColumn, ExternalDatabaseType.PostgreSql);
            foreach (var kvp in data)
            {
                insertCmd.Parameters.Add(new NpgsqlParameter("@" + kvp.Key, kvp.Value ?? DBNull.Value));
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

        await using var connection = await OpenConnectionAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT \"form_id\" FROM \"{tableName}\"";

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

    public async Task<bool> TableExistsAsync(string tableName)
    {
        ExternalDatabaseClient.ValidateTableName(tableName);

        await using var connection = await OpenConnectionAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = ExternalDatabaseClient.PgTableExistsSql;
        cmd.Parameters.Add(new NpgsqlParameter("@tableName", tableName));

        return Convert.ToInt64(await cmd.ExecuteScalarAsync()) > 0;
    }

    public async Task<long> CountAsync(string tableName)
    {
        ExternalDatabaseClient.ValidateTableName(tableName);

        await using var connection = await OpenConnectionAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM \"{tableName}\"";

        return Convert.ToInt64(await cmd.ExecuteScalarAsync());
    }
}

internal static partial class BuiltinFormsDatabaseClientLogger
{
    [LoggerMessage(LogLevel.Error, "Builtin forms DB insert into table {TableName} failed")]
    public static partial void ErrorBuiltinInsertFailed(this ILogger<BuiltinFormsDatabaseClient> logger, Exception exception, string tableName);
}

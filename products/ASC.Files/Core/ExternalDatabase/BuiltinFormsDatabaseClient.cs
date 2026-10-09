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

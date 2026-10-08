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

public record FormsDbCredentials(
    string SchemaName,
    string RwConnectionString,
    string RoConnectionString,
    string Host,
    int Port,
    string Database,
    string RoUser,
    string RoPassword);

[Scope]
public class FormsDbProvisioningService(
    IConfiguration configuration,
    SettingsManager settingsManager,
    IDistributedLockProvider distributedLockProvider,
    InstanceCrypto instanceCrypto,
    IFusionCacheProvider cacheProvider,
    ILogger<FormsDbProvisioningService> logger)
{
    private static readonly TimeSpan _credentialsExpiration = TimeSpan.FromMinutes(10);

    // Every portal gets its own pools on the shared server, so keep them small and short-lived.
    private const int MaxPoolSize = 5;
    private const int ConnectionIdleLifetime = 60;

    private const int RoConnectionLimit = 5;
    private const string RoStatementTimeout = "30s";

    private readonly IFusionCache _cache = cacheProvider.GetMemoryCache();

    private string? AdminConnectionString => configuration["ConnectionStrings:formsAdmin:connectionString"];

    public bool IsEnabled() => !string.IsNullOrWhiteSpace(AdminConnectionString);

    public static string GetCacheKey(int tenantId) => tenantId + "formsdbcredentials";

    private static string GetSchemaName(int tenantId) => $"tenant_{tenantId}";

    private static string GetRwUser(int tenantId) => $"forms_t{tenantId}_rw";

    private static string GetRoUser(int tenantId) => $"forms_t{tenantId}_ro";

    public async Task<FormsDbCredentials> GetOrProvisionAsync(int tenantId)
    {
        return await _cache.GetOrSetAsync<FormsDbCredentials>(GetCacheKey(tenantId), async (_, _) =>
        {
            var credentials = await TryGetProvisionedAsync(tenantId);
            if (credentials != null)
            {
                return credentials;
            }

            await using (await distributedLockProvider.TryAcquireFairLockAsync($"forms_db_provision_{tenantId}"))
            {
                return await TryGetProvisionedAsync(tenantId) ?? await ProvisionAsync(tenantId);
            }
        }, _credentialsExpiration);
    }

    public async Task DeprovisionAsync(int tenantId)
    {
        var schemaName = GetSchemaName(tenantId);

        await using var connection = new NpgsqlConnection(AdminConnectionString);
        await connection.OpenAsync();

        await ExecuteAsync(connection, $"DROP SCHEMA IF EXISTS \"{schemaName}\" CASCADE");
        await ExecuteAsync(connection, $"DROP ROLE IF EXISTS \"{GetRoUser(tenantId)}\"");
        await ExecuteAsync(connection, $"DROP ROLE IF EXISTS \"{GetRwUser(tenantId)}\"");

        await settingsManager.SaveAsync(new BuiltinFormsDbSettings(), tenantId);
        await _cache.RemoveAndNotifyAsync(GetCacheKey(tenantId));

        logger.InfoFormsDbDeprovisioned(tenantId, schemaName);
    }

    // Settings restored from another portal's backup name that portal's schema, so they are accepted only for this tenant.
    private async Task<FormsDbCredentials?> TryGetProvisionedAsync(int tenantId)
    {
        var settings = await settingsManager.LoadAsync<BuiltinFormsDbSettings>(tenantId);
        if (!settings.IsProvisioned || settings.SchemaName != GetSchemaName(tenantId))
        {
            return null;
        }

        var rwPassword = TryDecrypt(settings.RwPassword!);
        var roPassword = TryDecrypt(settings.RoPassword!);
        if (rwPassword == null || roPassword == null)
        {
            return null;
        }

        var credentials = BuildCredentials(settings.SchemaName, settings.RwUser!, rwPassword, settings.RoUser!, roPassword);
        if (!await CanUseAsync(credentials))
        {
            logger.WarnFormsDbUnusable(tenantId, settings.SchemaName);
            return null;
        }

        return credentials;
    }

    // A restored backup or a repointed server can leave stored passwords that no longer sign in. Network failures
    // are not caught here: they must not trigger re-provisioning.
    private static async Task<bool> CanUseAsync(FormsDbCredentials credentials)
    {
        try
        {
            await using var connection = new NpgsqlConnection(credentials.RwConnectionString);
            await connection.OpenAsync();

            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT 1 FROM information_schema.schemata WHERE schema_name = @schemaName";
            cmd.Parameters.Add(new NpgsqlParameter("@schemaName", credentials.SchemaName));

            return await cmd.ExecuteScalarAsync() != null;
        }
        catch (PostgresException e) when (e.SqlState is PostgresErrorCodes.InvalidPassword or PostgresErrorCodes.InvalidAuthorizationSpecification)
        {
            return false;
        }
    }

    private async Task<FormsDbCredentials> ProvisionAsync(int tenantId)
    {
        var schemaName = GetSchemaName(tenantId);
        var rwUser = GetRwUser(tenantId);
        var roUser = GetRoUser(tenantId);
        var rwPassword = GeneratePassword();
        var roPassword = GeneratePassword();

        await using var connection = new NpgsqlConnection(AdminConnectionString);
        await connection.OpenAsync();

        // Before PostgreSQL 15 every role may create objects in "public" and temporary tables; the portal accounts must not.
        await ExecuteAsync(connection, "REVOKE CREATE ON SCHEMA public FROM PUBLIC");
        await ExecuteAsync(connection, $"REVOKE TEMPORARY ON DATABASE \"{connection.Database}\" FROM PUBLIC");

        await ExecuteAsync(connection, $"CREATE SCHEMA IF NOT EXISTS \"{schemaName}\"");

        await CreateOrResetRoleAsync(connection, rwUser, rwPassword);
        await CreateOrResetRoleAsync(connection, roUser, roPassword);
        await ExecuteAsync(connection, $"ALTER ROLE \"{roUser}\" CONNECTION LIMIT {RoConnectionLimit}");
        await ExecuteAsync(connection, $"ALTER ROLE \"{roUser}\" SET statement_timeout = '{RoStatementTimeout}'");

        await ExecuteAsync(connection, $"GRANT ALL PRIVILEGES ON SCHEMA \"{schemaName}\" TO \"{rwUser}\"");
        await ExecuteAsync(connection, $"GRANT USAGE ON SCHEMA \"{schemaName}\" TO \"{roUser}\"");

        // Only the role itself can set its own default privileges.
        var rwConnectionString = new NpgsqlConnectionStringBuilder(AdminConnectionString)
        {
            Username = rwUser,
            Password = rwPassword
        }.ConnectionString;

        await using (var rwConnection = new NpgsqlConnection(rwConnectionString))
        {
            await rwConnection.OpenAsync();
            await ExecuteAsync(rwConnection, $"ALTER DEFAULT PRIVILEGES IN SCHEMA \"{schemaName}\" GRANT SELECT ON TABLES TO \"{roUser}\"");
        }

        await ExecuteAsync(connection, $"ALTER ROLE \"{rwUser}\" SET search_path TO \"{schemaName}\"");
        await ExecuteAsync(connection, $"ALTER ROLE \"{roUser}\" SET search_path TO \"{schemaName}\"");

        await settingsManager.SaveAsync(new BuiltinFormsDbSettings
        {
            SchemaName = schemaName,
            RwUser = rwUser,
            RwPassword = instanceCrypto.Encrypt(rwPassword),
            RoUser = roUser,
            RoPassword = instanceCrypto.Encrypt(roPassword)
        }, tenantId);

        // Other nodes may still cache the passwords that were just reset.
        await _cache.RemoveAndNotifyAsync(GetCacheKey(tenantId));

        logger.InfoFormsDbProvisioned(tenantId, schemaName);
        return BuildCredentials(schemaName, rwUser, rwPassword, roUser, roPassword);
    }

    private FormsDbCredentials BuildCredentials(string schemaName, string rwUser, string rwPassword, string roUser, string roPassword)
    {
        var rwBuilder = CreateConnectionStringBuilder(schemaName, rwUser, rwPassword);
        var roBuilder = CreateConnectionStringBuilder(schemaName, roUser, roPassword);

        return new FormsDbCredentials(
            SchemaName: schemaName,
            RwConnectionString: rwBuilder.ConnectionString,
            RoConnectionString: roBuilder.ConnectionString,
            Host: roBuilder.Host ?? string.Empty,
            Port: roBuilder.Port,
            Database: roBuilder.Database ?? string.Empty,
            RoUser: roUser,
            RoPassword: roPassword);
    }

    private NpgsqlConnectionStringBuilder CreateConnectionStringBuilder(string schemaName, string user, string password)
    {
        return new NpgsqlConnectionStringBuilder(AdminConnectionString)
        {
            Username = user,
            Password = password,
            SearchPath = schemaName,
            Timezone = "UTC",
            MaxPoolSize = MaxPoolSize,
            ConnectionIdleLifetime = ConnectionIdleLifetime
        };
    }

    // Settings encrypted with another machine key (restored elsewhere) cannot be read: re-provision instead.
    private string? TryDecrypt(string value)
    {
        try
        {
            return instanceCrypto.Decrypt(value);
        }
        catch (Exception e) when (e is FormatException or CryptographicException)
        {
            return null;
        }
    }

    private static async Task CreateOrResetRoleAsync(NpgsqlConnection connection, string role, string password)
    {
        var escapedPassword = password.Replace("'", "''");

        await ExecuteAsync(connection, $"DO $$ BEGIN IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = '{role}') THEN " +
            $"CREATE ROLE \"{role}\" LOGIN PASSWORD '{escapedPassword}'; END IF; END $$");
        await ExecuteAsync(connection, $"ALTER ROLE \"{role}\" WITH PASSWORD '{escapedPassword}'");
    }

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();
    }

    private static string GeneratePassword() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(24))
               .Replace("+", "A").Replace("/", "B").Replace("=", "C");
}

internal static partial class FormsDbProvisioningServiceLogger
{
    [LoggerMessage(LogLevel.Information, "Forms DB schema '{SchemaName}' provisioned for tenant {TenantId}")]
    public static partial void InfoFormsDbProvisioned(this ILogger<FormsDbProvisioningService> logger, int tenantId, string schemaName);

    [LoggerMessage(LogLevel.Information, "Forms DB schema '{SchemaName}' deprovisioned for tenant {TenantId}")]
    public static partial void InfoFormsDbDeprovisioned(this ILogger<FormsDbProvisioningService> logger, int tenantId, string schemaName);

    [LoggerMessage(LogLevel.Warning, "Forms DB schema '{SchemaName}' for tenant {TenantId} is missing or its account cannot sign in; re-provisioning")]
    public static partial void WarnFormsDbUnusable(this ILogger<FormsDbProvisioningService> logger, int tenantId, string schemaName);
}

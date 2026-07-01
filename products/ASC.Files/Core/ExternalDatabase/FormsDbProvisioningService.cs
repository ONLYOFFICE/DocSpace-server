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
    ILogger<FormsDbProvisioningService> logger)
{
    private string? AdminConnectionString => configuration["ConnectionStrings:formsAdmin:connectionString"];

    public bool IsEnabled() => !string.IsNullOrWhiteSpace(AdminConnectionString);

    public async Task<FormsDbCredentials> GetOrProvisionAsync(int tenantId)
    {
        var settings = await settingsManager.LoadAsync<BuiltinFormsDbSettings>(tenantId);
        if (settings.IsProvisioned)
        {
            return BuildCredentials(settings);
        }

        return await ProvisionAsync(tenantId);
    }

    private async Task<FormsDbCredentials> ProvisionAsync(int tenantId)
    {
        var schemaName = $"tenant_{tenantId}";
        var rwUser = $"forms_t{tenantId}_rw";
        var roUser = $"forms_t{tenantId}_ro";
        var rwPassword = GeneratePassword();
        var roPassword = GeneratePassword();

        await using var connection = new NpgsqlConnection(AdminConnectionString);
        await connection.OpenAsync();

        await ExecuteAsync(connection, $"CREATE SCHEMA IF NOT EXISTS \"{schemaName}\"");

        await ExecuteAsync(connection, $"DO $$ BEGIN " +
            $"IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = '{rwUser}') THEN " +
            $"CREATE ROLE \"{rwUser}\" LOGIN PASSWORD '{EscapePgString(rwPassword)}'; " +
            $"END IF; END $$");
        await ExecuteAsync(connection, $"ALTER ROLE \"{rwUser}\" WITH PASSWORD '{EscapePgString(rwPassword)}'");

        await ExecuteAsync(connection, $"DO $$ BEGIN " +
            $"IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = '{roUser}') THEN " +
            $"CREATE ROLE \"{roUser}\" LOGIN PASSWORD '{EscapePgString(roPassword)}'; " +
            $"END IF; END $$");
        await ExecuteAsync(connection, $"ALTER ROLE \"{roUser}\" WITH PASSWORD '{EscapePgString(roPassword)}'");

        await ExecuteAsync(connection, $"GRANT ALL PRIVILEGES ON SCHEMA \"{schemaName}\" TO \"{rwUser}\"");
        await ExecuteAsync(connection, $"GRANT USAGE ON SCHEMA \"{schemaName}\" TO \"{roUser}\"");

        // Only the role itself can set its own default privileges; connect as rw user to do so
        var rwConnStr = new NpgsqlConnectionStringBuilder(AdminConnectionString!)
        {
            Username = rwUser,
            Password = rwPassword
        }.ConnectionString;

        await using var rwConnection = new NpgsqlConnection(rwConnStr);
        await rwConnection.OpenAsync();
        await ExecuteAsync(rwConnection,
            $"ALTER DEFAULT PRIVILEGES IN SCHEMA \"{schemaName}\" " +
            $"GRANT SELECT ON TABLES TO \"{roUser}\"");

        await ExecuteAsync(connection, $"ALTER ROLE \"{rwUser}\" SET search_path TO \"{schemaName}\"");
        await ExecuteAsync(connection, $"ALTER ROLE \"{roUser}\" SET search_path TO \"{schemaName}\"");

        var settings = new BuiltinFormsDbSettings
        {
            SchemaName = schemaName,
            RwUser = rwUser,
            RwPassword = rwPassword,
            RoUser = roUser,
            RoPassword = roPassword
        };
        await settingsManager.SaveAsync(settings, tenantId);

        logger.InfoFormsDbProvisioned(tenantId, schemaName);
        return BuildCredentials(settings);
    }

    public async Task DeprovisionAsync(int tenantId)
    {
        var settings = await settingsManager.LoadAsync<BuiltinFormsDbSettings>(tenantId);
        if (!settings.IsProvisioned)
        {
            return;
        }

        try
        {
            await using var connection = new NpgsqlConnection(AdminConnectionString);
            await connection.OpenAsync();

            await ExecuteAsync(connection, $"DROP SCHEMA IF EXISTS \"{settings.SchemaName}\" CASCADE");
            await ExecuteAsync(connection, $"DROP ROLE IF EXISTS \"{settings.RoUser}\"");
            await ExecuteAsync(connection, $"DROP ROLE IF EXISTS \"{settings.RwUser}\"");

            await settingsManager.SaveAsync(new BuiltinFormsDbSettings(), tenantId);

            logger.InfoFormsDbDeprovisioned(tenantId, settings.SchemaName!);
        }
        catch (Exception ex)
        {
            logger.ErrorFormsDbDeprovisionFailed(ex, tenantId);
            throw;
        }
    }

    private FormsDbCredentials BuildCredentials(BuiltinFormsDbSettings settings)
    {
        var builder = new NpgsqlConnectionStringBuilder(AdminConnectionString)
        {
            Username = settings.RwUser,
            Password = settings.RwPassword,
            SearchPath = settings.SchemaName
        };

        var roBuilder = new NpgsqlConnectionStringBuilder(AdminConnectionString)
        {
            Username = settings.RoUser,
            Password = settings.RoPassword,
            SearchPath = settings.SchemaName
        };

        return new FormsDbCredentials(
            SchemaName: settings.SchemaName!,
            RwConnectionString: builder.ConnectionString,
            RoConnectionString: roBuilder.ConnectionString,
            Host: builder.Host ?? string.Empty,
            Port: builder.Port,
            Database: builder.Database ?? string.Empty,
            RoUser: settings.RoUser!,
            RoPassword: settings.RoPassword!);
    }

    private static Task ExecuteAsync(NpgsqlConnection connection, string sql)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        return cmd.ExecuteNonQueryAsync();
    }

    private static string GeneratePassword() =>
        Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(24))
               .Replace("+", "A").Replace("/", "B").Replace("=", "C");

    private static string EscapePgString(string value) => value.Replace("'", "''");
}

internal static partial class FormsDbProvisioningServiceLogger
{
    [LoggerMessage(LogLevel.Information, "Forms DB schema '{SchemaName}' provisioned for tenant {TenantId}")]
    public static partial void InfoFormsDbProvisioned(this ILogger<FormsDbProvisioningService> logger, int tenantId, string schemaName);

    [LoggerMessage(LogLevel.Information, "Forms DB schema '{SchemaName}' deprovisioned for tenant {TenantId}")]
    public static partial void InfoFormsDbDeprovisioned(this ILogger<FormsDbProvisioningService> logger, int tenantId, string schemaName);

    [LoggerMessage(LogLevel.Error, "Failed to deprovision forms DB for tenant {TenantId}")]
    public static partial void ErrorFormsDbDeprovisionFailed(this ILogger<FormsDbProvisioningService> logger, Exception exception, int tenantId);
}

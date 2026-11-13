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

/// <summary>
/// The portal's own database: its credentials once the portal lives there; otherwise, when the last move failed on this
/// node, how long until it is tried again.
/// </summary>
public record FormsDbIsolation(FormsDbCredentials? Credentials, TimeSpan? RetryAfter);

[Scope]
public class FormsDbProvisioningService(
    ConfigurationExtension configurationExtension,
    SettingsManager settingsManager,
    IDistributedLockProvider distributedLockProvider,
    InstanceCrypto instanceCrypto,
    IFusionCacheProvider cacheProvider,
    TenantManager tenantManager,
    IServiceScopeFactory serviceScopeFactory,
    Microsoft.Extensions.Hosting.IHostApplicationLifetime applicationLifetime,
    BuiltinFormsDbOptions options,
    ILogger<FormsDbProvisioningService> logger)
{
    private static readonly TimeSpan _credentialsExpiration = TimeSpan.FromMinutes(10);

    // Portals whose move into their own database runs on this node.
    private static readonly ConcurrentDictionary<int, byte> _isolating = new();

    // When the last move of a portal failed on this node; one that keeps failing is not restarted by every poll.
    private static readonly ConcurrentDictionary<int, DateTime> _isolationFailures = new();
    internal static readonly TimeSpan IsolationRetryDelay = TimeSpan.FromMinutes(5);

    private const string LiveDatabaseComment = "docspace forms: live";

    // Every portal gets its own pools on the shared server, so keep them short-lived; their size is configured.
    private const int ConnectionIdleLifetime = 60;

    private const int RoConnectionLimit = 5;
    internal const string RoStatementTimeout = "30s";

    private readonly IFusionCache _cache = cacheProvider.GetMemoryCache();

    private const string AdminConnectionStringName = "formsAdmin";

    private string? AdminConnectionString => configurationExtension.GetConnectionStrings(AdminConnectionStringName)?.ConnectionString;

    public bool IsEnabled() => !string.IsNullOrWhiteSpace(AdminConnectionString);

    public static string GetCacheKey(int tenantId) => tenantId + "formsdbcredentials";

    public static string GetTableLockKey(int tenantId, string tableName) => $"forms_db_table_{tenantId}_{tableName}";

    private static string GetProvisionLockKey(int tenantId) => $"forms_db_provision_{tenantId}";

    private static string GetSchemaName(int tenantId) => $"tenant_{tenantId}";

    private static string GetDatabaseName(int tenantId) => $"forms_t{tenantId}";

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

            await using (await distributedLockProvider.TryAcquireFairLockAsync(GetProvisionLockKey(tenantId)))
            {
                return await TryGetProvisionedAsync(tenantId) ?? await ProvisionAsync(tenantId);
            }
        }, _credentialsExpiration);
    }

    /// <summary>
    /// The credentials of the portal's own database, or no credentials while it still lives in the shared one; then its
    /// move is started in the background and the caller asks again later. The catalog of a shared database shows the
    /// tables and columns of every portal, so direct access is given only to a database of its own. A move that failed
    /// on this node a short while ago is reported instead of being started again at once.
    /// </summary>
    public async Task<FormsDbIsolation> GetIsolatedAsync(int tenantId)
    {
        var credentials = await GetOrProvisionAsync(tenantId);
        if (credentials.Database == GetDatabaseName(tenantId))
        {
            return new FormsDbIsolation(credentials, null);
        }

        if (_isolationFailures.TryGetValue(tenantId, out var failedAt) && DateTime.UtcNow - failedAt < IsolationRetryDelay)
        {
            return new FormsDbIsolation(null, failedAt + IsolationRetryDelay - DateTime.UtcNow);
        }

        StartIsolation(tenantId);
        return new FormsDbIsolation(null, null);
    }

    private void StartIsolation(int tenantId)
    {
        if (!_isolating.TryAdd(tenantId, 0))
        {
            return;
        }

        var stopping = applicationLifetime.ApplicationStopping;
        _ = Task.Run(async () =>
        {
            try
            {
                await using var scope = serviceScopeFactory.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<TenantManager>().SetCurrentTenantAsync(tenantId);
                await scope.ServiceProvider.GetRequiredService<FormsDbProvisioningService>().IsolateAsync(tenantId, stopping);
                _isolationFailures.TryRemove(tenantId, out _);
            }
            catch (OperationCanceledException) when (stopping.IsCancellationRequested)
            {
            }
            catch (Exception e)
            {
                _isolationFailures[tenantId] = DateTime.UtcNow;
                logger.ErrorFormsDbIsolation(e, tenantId);
            }
            finally
            {
                _isolating.TryRemove(tenantId, out _);
            }
        }, stopping);
    }

    /// <summary>
    /// Moves the portal from its schema in the shared database into a database of its own. The portal's accounts lose
    /// the shared schema first, so nothing is written there any more, even over a connection a pool still holds; then
    /// every table is copied whole under the lock its writers take, the new database is marked live, and only then are
    /// the settings switched and the shared schema dropped. A failure before the mark gives the shared schema back, and
    /// a move interrupted after it is completed by the next call or by the reconciliation. A submission refused while
    /// the tables are copied is added from the search index on the next use of its form.
    /// </summary>
    public async Task<FormsDbCredentials> IsolateAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        await using (await distributedLockProvider.TryAcquireFairLockAsync(GetProvisionLockKey(tenantId)))
        {
            var current = await TryGetProvisionedAsync(tenantId) ?? await ProvisionAsync(tenantId);
            var databaseName = GetDatabaseName(tenantId);
            if (current.Database == databaseName)
            {
                return current;
            }

            // The roles belong to the whole server, so the new database is reached with the same passwords.
            var rwPassword = new NpgsqlConnectionStringBuilder(current.RwConnectionString).Password!;
            var target = BuildCredentials(current.SchemaName, databaseName, GetRwUser(tenantId), rwPassword, current.RoUser, current.RoPassword);

            if (!await IsLiveAsync(databaseName))
            {
                await using (var admin = await OpenAdminAsync(null))
                {
                    await CreateDatabaseAsync(admin, databaseName);
                }

                await SetUpSchemaAsync(tenantId, databaseName, rwPassword, current.RoPassword);

                await FenceSharedSchemaAsync(tenantId);
                try
                {
                    await CopyTablesAsync(tenantId, target.RwConnectionString, cancellationToken);
                    await MarkLiveAsync(databaseName);
                }
                catch
                {
                    // Not live yet: the portal goes on in the shared schema, and the half-built database is rebuilt
                    // on the next call or dropped by the reconciliation.
                    await UnfenceSharedSchemaAsync(tenantId);
                    throw;
                }
            }

            await CompleteSwitchAsync(tenantId);
            logger.InfoFormsDbIsolated(tenantId, databaseName);

            return target;
        }
    }

    // Under the provision lock, once the portal's database is live: the settings name it, and the shared schema goes.
    private async Task CompleteSwitchAsync(int tenantId)
    {
        var databaseName = GetDatabaseName(tenantId);
        var settings = await settingsManager.LoadAsync<BuiltinFormsDbSettings>(tenantId);
        if (settings.DatabaseName != databaseName)
        {
            settings.DatabaseName = databaseName;
            await settingsManager.SaveAsync(settings, tenantId);
        }

        await _cache.RemoveAndNotifyAsync(GetCacheKey(tenantId));

        await using (var admin = await OpenAdminAsync(null))
        {
            await ExecuteAsync(admin, $"DROP SCHEMA IF EXISTS {Quote(GetSchemaName(tenantId))} CASCADE");
        }

        // Connections to the shared database that this node's pool still holds; a pool is keyed by its whole string.
        var rwPassword = settings.RwPassword == null ? null : TryDecrypt(settings.RwPassword);
        if (rwPassword != null)
        {
            var shared = CreateConnectionStringBuilder(GetSchemaName(tenantId), null, GetRwUser(tenantId), rwPassword);
            await using var stale = new NpgsqlConnection(shared.ConnectionString);
            NpgsqlConnection.ClearPool(stale);
        }
    }

    // Privileges are checked on every statement, so this stops the writes of open pooled connections on every node too,
    // which a revoked CONNECT does not. The owner reads the tables through the portal's role from now on.
    private async Task FenceSharedSchemaAsync(int tenantId)
    {
        var schema = Quote(GetSchemaName(tenantId));
        var rw = Quote(GetRwUser(tenantId));
        var ro = Quote(GetRoUser(tenantId));

        await using var admin = await OpenAdminAsync(null);
        await ExecuteAsync(admin, $"GRANT {rw} TO CURRENT_USER");
        await ExecuteAsync(admin, $"REVOKE ALL ON SCHEMA {schema} FROM {rw}, {ro}");
        await ExecuteAsync(admin, $"REVOKE CONNECT ON DATABASE {Quote(admin.Database)} FROM {rw}, {ro}");
    }

    private async Task UnfenceSharedSchemaAsync(int tenantId)
    {
        var schema = Quote(GetSchemaName(tenantId));
        var rw = Quote(GetRwUser(tenantId));
        var ro = Quote(GetRoUser(tenantId));

        await using var admin = await OpenAdminAsync(null);
        await ExecuteAsync(admin, $"GRANT CONNECT ON DATABASE {Quote(admin.Database)} TO {rw}, {ro}");
        await ExecuteAsync(admin, $"GRANT ALL PRIVILEGES ON SCHEMA {schema} TO {rw}");
        await ExecuteAsync(admin, $"GRANT USAGE ON SCHEMA {schema} TO {ro}");
    }

    // The mark lives on the database itself rather than in the settings, which other processes may read from a cache:
    // a database without it is a leftover, one with it holds every row of the portal.
    private async Task<bool> IsLiveAsync(string databaseName)
    {
        await using var admin = await OpenAdminAsync(null);
        await using var cmd = admin.CreateCommand();
        cmd.CommandText = "SELECT shobj_description(oid, 'pg_database') FROM pg_database WHERE datname = @name";
        cmd.Parameters.Add(new NpgsqlParameter("@name", databaseName));

        return await cmd.ExecuteScalarAsync() as string == LiveDatabaseComment;
    }

    private async Task MarkLiveAsync(string databaseName)
    {
        await using var admin = await OpenAdminAsync(null);
        await ExecuteAsync(admin, $"COMMENT ON DATABASE {Quote(databaseName)} IS '{LiveDatabaseComment}'");
    }

    public async Task DeprovisionAsync(int tenantId)
    {
        var schemaName = GetSchemaName(tenantId);

        await using var connection = await OpenAdminAsync(null);

        await ExecuteAsync(connection, $"DROP SCHEMA IF EXISTS \"{schemaName}\" CASCADE");
        await ExecuteAsync(connection, $"DROP DATABASE IF EXISTS \"{GetDatabaseName(tenantId)}\" WITH (FORCE)");
        await DropRoleAsync(connection, tenantId, GetRoUser(tenantId));
        await DropRoleAsync(connection, tenantId, GetRwUser(tenantId));

        await settingsManager.SaveAsync(new BuiltinFormsDbSettings(), tenantId);
        await _cache.RemoveAndNotifyAsync(GetCacheKey(tenantId));

        logger.InfoFormsDbDeprovisioned(tenantId, schemaName);
    }

    /// <summary>
    /// Removes what portal removal or a broken move left behind: the databases, schemas and roles of portals that no
    /// longer exist, a database the portal's settings do not name, and the shared schema of a portal that moved out.
    /// </summary>
    public async Task ReconcileAsync(CancellationToken cancellationToken)
    {
        HashSet<int> databases, schemas, roles;
        await using (var admin = await OpenAdminAsync(null))
        {
            databases = await ReadTenantIdsAsync(admin, "SELECT substring(datname from 8) FROM pg_database WHERE datname ~ '^forms_t[0-9]+$'");
            schemas = await ReadTenantIdsAsync(admin, "SELECT substring(nspname from 8) FROM pg_namespace WHERE nspname ~ '^tenant_[0-9]+$'");
            roles = await ReadTenantIdsAsync(admin, "SELECT substring(rolname from '^forms_t([0-9]+)_') FROM pg_roles WHERE rolname ~ '^forms_t[0-9]+_r[wo]$'");
        }

        foreach (var tenantId in databases.Union(schemas).Union(roles))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            try
            {
                await ReconcileTenantAsync(tenantId, databases.Contains(tenantId), schemas.Contains(tenantId));
            }
            catch (Exception e)
            {
                logger.WarnFormsDbNotReconciled(e, tenantId);
            }
        }
    }

    private async Task ReconcileTenantAsync(int tenantId, bool hasDatabase, bool hasSchema)
    {
        var tenant = await tenantManager.GetTenantAsync(tenantId);
        if (tenant == null || tenant.Status == TenantStatus.RemovePending)
        {
            await DeprovisionAsync(tenantId);
            logger.InfoFormsDbOrphanRemoved(tenantId, GetDatabaseName(tenantId));
            return;
        }

        // A portal still in the shared schema has nothing to reconcile.
        if (!hasDatabase)
        {
            return;
        }

        // A move in progress holds this lock, so its database is not taken for a leftover. What to drop is decided by the
        // mark on the database, never by the settings, which this process may read from a stale cache.
        await using (await distributedLockProvider.TryAcquireFairLockAsync(GetProvisionLockKey(tenantId)))
        {
            var databaseName = GetDatabaseName(tenantId);
            if (await IsLiveAsync(databaseName))
            {
                // A move interrupted after the copy: the settings and the shared schema are brought in line.
                var settings = await settingsManager.LoadAsync<BuiltinFormsDbSettings>(tenantId);
                if (hasSchema || settings.DatabaseName != databaseName)
                {
                    await CompleteSwitchAsync(tenantId);
                    logger.InfoFormsDbOrphanRemoved(tenantId, GetSchemaName(tenantId));
                }

                return;
            }

            await using (var admin = await OpenAdminAsync(null))
            {
                await ExecuteAsync(admin, $"DROP DATABASE IF EXISTS {Quote(databaseName)} WITH (FORCE)");
            }

            logger.InfoFormsDbOrphanRemoved(tenantId, databaseName);

            // A move interrupted during the copy left the shared schema closed to the portal.
            if (hasSchema)
            {
                await UnfenceSharedSchemaAsync(tenantId);
            }
        }
    }

    private static async Task<HashSet<int>> ReadTenantIdsAsync(NpgsqlConnection connection, string sql)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;

        var ids = new HashSet<int>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (int.TryParse(reader.GetString(0), CultureInfo.InvariantCulture, out var id))
            {
                ids.Add(id);
            }
        }

        return ids;
    }

    /// <summary>
    /// Gives the accounts of the portals provisioned before the connection limit was changed the configured one: the
    /// limit is set when an account is created, so a new value would otherwise reach only new portals. An account
    /// without a limit is one being provisioned right now, which sets the limit itself; an account that cannot be changed
    /// (dropped with its portal meanwhile, or changed at the same moment) is left to the next pass. Returns how many
    /// accounts were changed.
    /// </summary>
    public async Task<int> ApplyConnectionLimitAsync(CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(AdminConnectionString);
        await connection.OpenAsync(cancellationToken);

        var roles = new List<string>();
        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT rolname FROM pg_roles WHERE rolname ~ '^forms_t[0-9]+_rw$' AND rolconnlimit <> -1 AND rolconnlimit <> @limit";
            cmd.Parameters.Add(new NpgsqlParameter("@limit", options.RwConnectionLimit));
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                roles.Add(reader.GetString(0));
            }
        }

        var changed = 0;
        foreach (var role in roles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await ExecuteAsync(connection, $"ALTER ROLE \"{role}\" CONNECTION LIMIT {options.RwConnectionLimit}");
                changed++;
            }
            catch (PostgresException e)
            {
                logger.WarnFormsDbConnectionLimitNotSet(e, role);
            }
        }

        return changed;
    }

    /// <summary>
    /// The stored credentials of the portal when they still sign in, or null; never provisions, so background work
    /// cannot recreate a dropped schema or reset its passwords.
    /// </summary>
    public Task<FormsDbCredentials?> GetProvisionedAsync(int tenantId) => TryGetProvisionedAsync(tenantId);

    // Settings restored from another portal's backup name that portal's schema or database, so they are accepted only
    // for this tenant.
    private async Task<FormsDbCredentials?> TryGetProvisionedAsync(int tenantId)
    {
        var settings = await settingsManager.LoadAsync<BuiltinFormsDbSettings>(tenantId);
        if (!settings.IsProvisioned || settings.SchemaName != GetSchemaName(tenantId) ||
            (settings.DatabaseName != null && settings.DatabaseName != GetDatabaseName(tenantId)))
        {
            return null;
        }

        var rwPassword = TryDecrypt(settings.RwPassword!);
        var roPassword = TryDecrypt(settings.RoPassword!);
        if (rwPassword == null || roPassword == null)
        {
            return null;
        }

        var credentials = BuildCredentials(settings.SchemaName, settings.DatabaseName, settings.RwUser!, rwPassword, settings.RoUser!, roPassword);
        if (!await CanUseAsync(credentials))
        {
            logger.WarnFormsDbUnusable(tenantId, settings.SchemaName);
            return null;
        }

        return credentials;
    }

    // A restored backup, a repointed server or a lost database can leave stored passwords that no longer sign in.
    // Network failures are not caught here: they must not trigger re-provisioning.
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
        catch (PostgresException e) when (e.SqlState is PostgresErrorCodes.InvalidPassword
                                              or PostgresErrorCodes.InvalidAuthorizationSpecification
                                              or PostgresErrorCodes.InvalidCatalogName
                                              or PostgresErrorCodes.InsufficientPrivilege)
        {
            return false;
        }
    }

    // Before PostgreSQL 15 every role may create objects in "public" and temporary tables, and every role may connect to
    // every database; the portal accounts must not. The roles of the portals already living in the database keep their
    // way in. Database-wide, so revoked once under a lock of its own: portals provisioned at once would otherwise update
    // the same catalog rows together and fail with "tuple concurrently updated".
    private async Task RevokePublicDefaultsAsync(NpgsqlConnection connection)
    {
        if (!await PublicKeepsDefaultsAsync(connection))
        {
            return;
        }

        await using (await distributedLockProvider.TryAcquireFairLockAsync($"forms_db_public_defaults_{connection.Database}"))
        {
            if (await PublicKeepsDefaultsAsync(connection))
            {
                var database = connection.Database;
                await ExecuteAsync(connection, "REVOKE CREATE ON SCHEMA public FROM PUBLIC");
                await ExecuteAsync(connection, $"REVOKE TEMPORARY ON DATABASE \"{database}\" FROM PUBLIC");
                await ExecuteAsync(connection,
                    "DO $$ DECLARE r record; BEGIN " +
                    "FOR r IN SELECT rolname FROM pg_roles WHERE rolname ~ '^forms_t[0-9]+_r[wo]$' " +
                    "AND 'tenant_' || substring(rolname from '^forms_t([0-9]+)_') IN (SELECT nspname FROM pg_namespace) LOOP " +
                    "EXECUTE format('GRANT CONNECT ON DATABASE %I TO %I', current_database(), r.rolname); END LOOP; END $$");
                await ExecuteAsync(connection, $"GRANT CONNECT ON DATABASE \"{database}\" TO CURRENT_USER");
                await ExecuteAsync(connection, $"REVOKE CONNECT ON DATABASE \"{database}\" FROM PUBLIC");
            }
        }
    }

    // Whether PUBLIC (grantee 0) still may create in "public", make temporary tables or connect; a NULL ACL is the
    // default one.
    private static async Task<bool> PublicKeepsDefaultsAsync(NpgsqlConnection connection)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText =
            "SELECT EXISTS (SELECT 1 FROM pg_namespace n, aclexplode(COALESCE(n.nspacl, acldefault('n', n.nspowner))) a " +
            "WHERE n.nspname = 'public' AND a.grantee = 0 AND a.privilege_type = 'CREATE') " +
            "OR EXISTS (SELECT 1 FROM pg_database d, aclexplode(COALESCE(d.datacl, acldefault('d', d.datdba))) a " +
            "WHERE d.datname = current_database() AND a.grantee = 0 AND a.privilege_type IN ('TEMPORARY', 'CONNECT'))";

        return await cmd.ExecuteScalarAsync() is true;
    }

    // A role that cannot be dropped (it still owns something elsewhere) must at least no longer sign in.
    private async Task DropRoleAsync(NpgsqlConnection connection, int tenantId, string role)
    {
        try
        {
            if (await RoleExistsAsync(connection, role))
            {
                await ExecuteAsync(connection, $"DROP OWNED BY \"{role}\"");
            }

            await ExecuteAsync(connection, $"DROP ROLE IF EXISTS \"{role}\"");
        }
        catch (PostgresException e)
        {
            logger.WarnFormsDbRoleNotDropped(e, tenantId, role);
            await ExecuteAsync(connection, $"ALTER ROLE \"{role}\" NOLOGIN");
        }
    }

    private static async Task<bool> RoleExistsAsync(NpgsqlConnection connection, string role)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT 1 FROM pg_roles WHERE rolname = @role";
        cmd.Parameters.Add(new NpgsqlParameter("@role", role));

        return await cmd.ExecuteScalarAsync() != null;
    }

    private static async Task<bool> DatabaseExistsAsync(NpgsqlConnection connection, string databaseName)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT 1 FROM pg_database WHERE datname = @name";
        cmd.Parameters.Add(new NpgsqlParameter("@name", databaseName));

        return await cmd.ExecuteScalarAsync() != null;
    }

    // CREATE DATABASE cannot run in a transaction; another node may have created it a moment earlier.
    private static async Task CreateDatabaseAsync(NpgsqlConnection connection, string databaseName)
    {
        if (await DatabaseExistsAsync(connection, databaseName))
        {
            return;
        }

        try
        {
            await ExecuteAsync(connection, $"CREATE DATABASE \"{databaseName}\"");
        }
        catch (PostgresException e) when (e.SqlState == PostgresErrorCodes.DuplicateDatabase)
        {
        }
    }

    // A portal that moved into its own database is provisioned there again while that database is still there.
    private async Task<FormsDbCredentials> ProvisionAsync(int tenantId)
    {
        var schemaName = GetSchemaName(tenantId);
        var rwUser = GetRwUser(tenantId);
        var roUser = GetRoUser(tenantId);
        var rwPassword = UserManagerWrapper.GeneratePassword();
        var roPassword = UserManagerWrapper.GeneratePassword();

        string? databaseName = null;
        var stored = await settingsManager.LoadAsync<BuiltinFormsDbSettings>(tenantId);
        if (stored.DatabaseName == GetDatabaseName(tenantId))
        {
            await using var admin = await OpenAdminAsync(null);
            if (await DatabaseExistsAsync(admin, stored.DatabaseName))
            {
                databaseName = stored.DatabaseName;
            }
        }

        await SetUpSchemaAsync(tenantId, databaseName, rwPassword, roPassword);

        await settingsManager.SaveAsync(new BuiltinFormsDbSettings
        {
            SchemaName = schemaName,
            DatabaseName = databaseName,
            RwUser = rwUser,
            RwPassword = instanceCrypto.Encrypt(rwPassword),
            RoUser = roUser,
            RoPassword = instanceCrypto.Encrypt(roPassword)
        }, tenantId);

        // Other nodes may still cache the passwords that were just reset.
        await _cache.RemoveAndNotifyAsync(GetCacheKey(tenantId));

        logger.InfoFormsDbProvisioned(tenantId, schemaName);
        return BuildCredentials(schemaName, databaseName, rwUser, rwPassword, roUser, roPassword);
    }

    // The schema, the roles and their rights in the given database (null: the shared one).
    private async Task SetUpSchemaAsync(int tenantId, string? databaseName, string rwPassword, string roPassword)
    {
        var schemaName = GetSchemaName(tenantId);
        var rwUser = GetRwUser(tenantId);
        var roUser = GetRoUser(tenantId);

        await using var connection = await OpenAdminAsync(databaseName);

        await RevokePublicDefaultsAsync(connection);

        await ExecuteAsync(connection, $"CREATE SCHEMA IF NOT EXISTS \"{schemaName}\"");

        await CreateOrResetRoleAsync(connection, rwUser, rwPassword);
        await CreateOrResetRoleAsync(connection, roUser, roPassword);
        await ExecuteAsync(connection, $"ALTER ROLE \"{rwUser}\" CONNECTION LIMIT {options.RwConnectionLimit}");
        await ExecuteAsync(connection, $"ALTER ROLE \"{roUser}\" CONNECTION LIMIT {RoConnectionLimit}");
        await ExecuteAsync(connection, $"ALTER ROLE \"{roUser}\" SET statement_timeout = '{RoStatementTimeout}'");

        await ExecuteAsync(connection, $"GRANT CONNECT ON DATABASE \"{connection.Database}\" TO \"{rwUser}\", \"{roUser}\"");
        await ExecuteAsync(connection, $"GRANT ALL PRIVILEGES ON SCHEMA \"{schemaName}\" TO \"{rwUser}\"");
        await ExecuteAsync(connection, $"GRANT USAGE ON SCHEMA \"{schemaName}\" TO \"{roUser}\"");

        // Only the role itself can set its own default privileges.
        var rwConnectionString = new NpgsqlConnectionStringBuilder(connection.ConnectionString)
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
    }

    // Every table of the portal's fenced shared schema into its own database, whole and as it is now: each under the
    // lock its writers take, so a write that passed the privilege check before the fence ends first. The schema is read
    // as its owner, since the portal's accounts lost it. Tables of an interrupted earlier attempt are replaced, and those
    // the shared schema no longer has (dropped as idle or merged meanwhile) are dropped.
    private async Task CopyTablesAsync(int tenantId, string targetConnectionString, CancellationToken cancellationToken)
    {
        var sourceBuilder = new NpgsqlConnectionStringBuilder(AdminConnectionString) { SearchPath = GetSchemaName(tenantId), Pooling = false };
        await using var source = new NpgsqlConnection(sourceBuilder.ConnectionString);
        await source.OpenAsync(cancellationToken);
        await using var target = new NpgsqlConnection(targetConnectionString);
        await target.OpenAsync(cancellationToken);

        var tables = await ReadListAsync(source, "SELECT tablename FROM pg_tables WHERE schemaname = current_schema()");
        foreach (var table in tables)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using (await distributedLockProvider.TryAcquireFairLockAsync(GetTableLockKey(tenantId, table), cancellationToken: cancellationToken))
            {
                var columns = await ReadColumnsAsync(source, table);
                await ExecuteAsync(target, $"DROP TABLE IF EXISTS {Quote(table)}");
                await ExecuteAsync(target, await BuildCreateTableAsync(source, table, columns));
                await CopyRowsAsync(source, target, table, columns, cancellationToken);
            }
        }

        foreach (var leftover in (await ReadListAsync(target, "SELECT tablename FROM pg_tables WHERE schemaname = current_schema()")).Except(tables))
        {
            await ExecuteAsync(target, $"DROP TABLE IF EXISTS {Quote(leftover)}");
        }
    }

    private static async Task CopyRowsAsync(NpgsqlConnection source, NpgsqlConnection target, string table, List<(string Name, string Type, bool NotNull)> columns, CancellationToken cancellationToken)
    {
        var list = string.Join(", ", columns.Select(c => Quote(c.Name)));

        await using var reader = await source.BeginRawBinaryCopyAsync($"COPY {Quote(table)} ({list}) TO STDOUT (FORMAT BINARY)", cancellationToken);
        await using var writer = await target.BeginRawBinaryCopyAsync($"COPY {Quote(table)} ({list}) FROM STDIN (FORMAT BINARY)", cancellationToken);
        await reader.CopyToAsync(writer, cancellationToken);
    }

    private static async Task<List<(string Name, string Type, bool NotNull)>> ReadColumnsAsync(NpgsqlConnection connection, string table)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText =
            "SELECT a.attname, format_type(a.atttypid, a.atttypmod), a.attnotnull FROM pg_attribute a " +
            "WHERE a.attrelid = to_regclass(quote_ident(@table)) AND a.attnum > 0 AND NOT a.attisdropped ORDER BY a.attnum";
        cmd.Parameters.Add(new NpgsqlParameter("@table", table));

        var columns = new List<(string, string, bool)>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            columns.Add((reader.GetString(0), reader.GetString(1), reader.GetBoolean(2)));
        }

        return columns;
    }

    // The same columns and constraints (the key, the lists of the choice columns) as in the shared schema.
    private static async Task<string> BuildCreateTableAsync(NpgsqlConnection connection, string table, List<(string Name, string Type, bool NotNull)> columns)
    {
        var definitions = columns.Select(c => $"{Quote(c.Name)} {c.Type}{(c.NotNull ? " NOT NULL" : "")}").ToList();

        await using var cmd = connection.CreateCommand();
        cmd.CommandText =
            "SELECT conname, pg_get_constraintdef(oid) FROM pg_constraint " +
            "WHERE conrelid = to_regclass(quote_ident(@table)) AND contype IN ('p', 'u', 'c')";
        cmd.Parameters.Add(new NpgsqlParameter("@table", table));

        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                definitions.Add($"CONSTRAINT {Quote(reader.GetString(0))} {reader.GetString(1)}");
            }
        }

        return $"CREATE TABLE IF NOT EXISTS {Quote(table)} ({string.Join(", ", definitions)})";
    }

    private static async Task<List<string>> ReadListAsync(NpgsqlConnection connection, string sql)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;

        var values = new List<string>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            values.Add(reader.GetString(0));
        }

        return values;
    }

    private static string Quote(string identifier) => "\"" + identifier.Replace("\"", "\"\"") + "\"";

    private async Task<NpgsqlConnection> OpenAdminAsync(string? databaseName)
    {
        var builder = new NpgsqlConnectionStringBuilder(AdminConnectionString);
        if (databaseName != null)
        {
            builder.Database = databaseName;
        }

        var connection = new NpgsqlConnection(builder.ConnectionString);
        try
        {
            await connection.OpenAsync();
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    private FormsDbCredentials BuildCredentials(string schemaName, string? databaseName, string rwUser, string rwPassword, string roUser, string roPassword)
    {
        var rwBuilder = CreateConnectionStringBuilder(schemaName, databaseName, rwUser, rwPassword);
        var roBuilder = CreateConnectionStringBuilder(schemaName, databaseName, roUser, roPassword);

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

    private NpgsqlConnectionStringBuilder CreateConnectionStringBuilder(string schemaName, string? databaseName, string user, string password)
    {
        var builder = new NpgsqlConnectionStringBuilder(AdminConnectionString)
        {
            Username = user,
            Password = password,
            SearchPath = schemaName,
            Timezone = "UTC",
            MaxPoolSize = options.MaxPoolSize,
            ConnectionIdleLifetime = ConnectionIdleLifetime
        };

        if (databaseName != null)
        {
            builder.Database = databaseName;
        }

        return builder;
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
        // LOGIN too: a role that could not be dropped was left NOLOGIN and would never sign in again.
        await ExecuteAsync(connection, $"ALTER ROLE \"{role}\" WITH LOGIN PASSWORD '{escapedPassword}'");
    }

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();
    }
}

internal static partial class FormsDbProvisioningServiceLogger
{
    [LoggerMessage(LogLevel.Information, "Forms DB schema '{SchemaName}' provisioned for tenant {TenantId}")]
    public static partial void InfoFormsDbProvisioned(this ILogger<FormsDbProvisioningService> logger, int tenantId, string schemaName);

    [LoggerMessage(LogLevel.Information, "Forms DB schema '{SchemaName}' deprovisioned for tenant {TenantId}")]
    public static partial void InfoFormsDbDeprovisioned(this ILogger<FormsDbProvisioningService> logger, int tenantId, string schemaName);

    [LoggerMessage(LogLevel.Warning, "Forms DB schema '{SchemaName}' for tenant {TenantId} is missing or its account cannot sign in; re-provisioning")]
    public static partial void WarnFormsDbUnusable(this ILogger<FormsDbProvisioningService> logger, int tenantId, string schemaName);

    [LoggerMessage(LogLevel.Warning, "Built-in forms DB: role {role} of tenant {tenantId} could not be dropped and can no longer sign in")]
    public static partial void WarnFormsDbRoleNotDropped(this ILogger<FormsDbProvisioningService> logger, Exception exception, int tenantId, string role);

    [LoggerMessage(LogLevel.Information, "Built-in forms DB: tenant {tenantId} moved into its own database {databaseName}")]
    public static partial void InfoFormsDbIsolated(this ILogger<FormsDbProvisioningService> logger, int tenantId, string databaseName);

    [LoggerMessage(LogLevel.Error, "Built-in forms DB: moving tenant {tenantId} into its own database failed")]
    public static partial void ErrorFormsDbIsolation(this ILogger<FormsDbProvisioningService> logger, Exception exception, int tenantId);

    [LoggerMessage(LogLevel.Information, "Built-in forms DB: leftover {objectName} of tenant {tenantId} removed")]
    public static partial void InfoFormsDbOrphanRemoved(this ILogger<FormsDbProvisioningService> logger, int tenantId, string objectName);

    [LoggerMessage(LogLevel.Warning, "Built-in forms DB: leftovers of tenant {tenantId} could not be checked")]
    public static partial void WarnFormsDbNotReconciled(this ILogger<FormsDbProvisioningService> logger, Exception exception, int tenantId);

    [LoggerMessage(LogLevel.Warning, "Built-in forms DB: the connection limit of role {role} was not changed, the next cleanup pass tries again")]
    public static partial void WarnFormsDbConnectionLimitNotSet(this ILogger<FormsDbProvisioningService> logger, Exception exception, string role);
}

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
/// The portal's own database: its credentials once the portal lives there; otherwise, when the last move failed, how
/// long until it is tried again.
/// </summary>
public record FormsDbIsolation(FormsDbCredentials? Credentials, TimeSpan? RetryAfter);

/// <summary>The failed moves of a portal in a row, and when the next one may start.</summary>
public record FormsDbIsolationFailure(DateTime RetryAt, int Attempts);

[Scope]
public class FormsDbProvisioningService(
    ConfigurationExtension configurationExtension,
    SettingsManager settingsManager,
    IDistributedLockProvider distributedLockProvider,
    InstanceCrypto instanceCrypto,
    IFusionCacheProvider cacheProvider,
    IFusionCache distributedCache,
    TenantManager tenantManager,
    IServiceScopeFactory serviceScopeFactory,
    IHostApplicationLifetime applicationLifetime,
    BuiltinFormsDbOptions options,
    ILogger<FormsDbProvisioningService> logger)
{
    private static readonly TimeSpan _credentialsExpiration = TimeSpan.FromMinutes(10);

    // Portals whose move into their own database runs on this node.
    private static readonly ConcurrentDictionary<int, byte> _isolating = new();

    // A move that failed is not restarted by every poll; the mark is shared by all nodes, so none of them starts one
    // before the Retry-After the caller was given (without Redis it is this node's only). Every move fences the portal's
    // writes while it copies, so one that keeps failing is tried a few times and then once a day.
    private static readonly TimeSpan _isolationRetryDelay = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan _isolationGiveUpDelay = TimeSpan.FromDays(1);
    internal const int IsolationAttempts = 3;

    // The first key of the advisory lock a move holds on the shared database for as long as it runs ("form"); the second
    // is the tenant. Unlike the Redis lease of the provision lock it cannot lapse while the move goes on: it ends only with
    // the move's session.
    internal const int MoveLockClass = 0x666F726D;

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

    private static string GetIsolationFailureKey(int tenantId) => tenantId + "formsdbisolationfailure";

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

            // A move fences the account and holds the provision lock for the whole copy: the write is refused at once
            // instead of waiting for that lock, and is added from the search index once the move completes.
            if (await IsMoveRunningAsync(tenantId))
            {
                throw new FormsDbMovingException(tenantId);
            }

            await using (await distributedLockProvider.TryAcquireFairLockAsync(GetProvisionLockKey(tenantId)))
            {
                return await TryGetProvisionedAsync(tenantId, logUnusable: true) ?? await ProvisionAsync(tenantId, null);
            }
        }, _credentialsExpiration);
    }

    /// <summary>
    /// The credentials of the portal's own database, or no credentials while it still lives in the shared one; then its
    /// move is started in the background and the caller asks again later. The catalog of a shared database shows the
    /// tables and columns of every portal, so direct access is given only to a database of its own. A move that failed
    /// a short while ago is reported instead of being started again at once.
    /// </summary>
    public async Task<FormsDbIsolation> GetIsolatedAsync(int tenantId)
    {
        var databaseName = GetDatabaseName(tenantId);

        // While a move copies the tables, the portal's account has lost the shared schema and new credentials wait for
        // the lock the move holds for the whole copy: the caller is answered at once instead.
        var settings = await settingsManager.LoadAsync<BuiltinFormsDbSettings>(tenantId);
        if (settings.DatabaseName != databaseName && await IsSharedSchemaFencedAsync(tenantId))
        {
            return await StartIsolationUnlessFailedAsync(tenantId);
        }

        FormsDbCredentials credentials;
        try
        {
            credentials = await GetOrProvisionAsync(tenantId);
        }
        catch (FormsDbMovingException)
        {
            // Another node is moving the portal: the caller asks again, as while this node moves it.
            return new FormsDbIsolation(null, null);
        }

        if (credentials.Database == databaseName)
        {
            return new FormsDbIsolation(credentials, null);
        }

        return await StartIsolationUnlessFailedAsync(tenantId);
    }

    /// <summary>
    /// Sets a new password for the read-only account of the portal's own database and ends the account's open sessions,
    /// so whoever got the old one can no longer read. While the portal does not live there yet nothing was handed out:
    /// the move is started or reported as by <see cref="GetIsolatedAsync"/>, and no password changes.
    /// </summary>
    public async Task<FormsDbIsolation> ResetReadOnlyPasswordAsync(int tenantId)
    {
        var isolation = await GetIsolatedAsync(tenantId);
        if (isolation.Credentials == null)
        {
            return isolation;
        }

        var databaseName = GetDatabaseName(tenantId);
        await using (await distributedLockProvider.TryAcquireFairLockAsync(GetProvisionLockKey(tenantId)))
        {
            var current = await TryGetProvisionedAsync(tenantId);
            if (current?.Database != databaseName)
            {
                // Provisioned again meanwhile; the next call sees the result.
                return new FormsDbIsolation(null, null);
            }

            var settings = await settingsManager.LoadAsync<BuiltinFormsDbSettings>(tenantId);
            var roPassword = UserManagerWrapper.GeneratePassword();

            await using var admin = await OpenAdminAsync(null);
            await SetPasswordAsync(admin, current.RoUser, roPassword);

            // The stored password must be the one that signs in: if it cannot be saved, the old one is set back. A failure
            // of that is logged and the save's failure reported, since the account then signs in with neither.
            settings.RoPassword = instanceCrypto.Encrypt(roPassword);
            if (!await settingsManager.SaveAsync(settings, tenantId))
            {
                try
                {
                    await SetPasswordAsync(admin, current.RoUser, current.RoPassword);
                }
                catch (Exception e)
                {
                    logger.ErrorFormsDbPasswordNotRestored(e, tenantId);
                }

                throw new FormsDbProvisioningException($"The built-in forms database settings of tenant {tenantId} could not be saved.");
            }

            await _cache.RemoveAndNotifyAsync(GetCacheKey(tenantId));

            // The new password is in effect and stored: sessions signed in with the old one that cannot be ended now are
            // logged, and the reset is reported as done.
            try
            {
                await EndSessionsAsync(admin, current.RoUser);
            }
            catch (Exception e)
            {
                logger.WarnFormsDbSessionsNotEnded(e, tenantId, current.RoUser);
            }

            var rwPassword = new NpgsqlConnectionStringBuilder(current.RwConnectionString).Password!;
            return new FormsDbIsolation(
                BuildCredentials(current.SchemaName, databaseName, GetRwUser(tenantId), rwPassword, current.RoUser, roPassword), null);
        }
    }

    private async Task<FormsDbIsolation> StartIsolationUnlessFailedAsync(int tenantId)
    {
        var failure = (await distributedCache.TryGetAsync<FormsDbIsolationFailure>(GetIsolationFailureKey(tenantId))).GetValueOrDefault();
        if (failure != null)
        {
            var retryAfter = failure.RetryAt - DateTime.UtcNow;
            if (retryAfter > TimeSpan.Zero)
            {
                return new FormsDbIsolation(null, retryAfter);
            }
        }

        StartIsolation(tenantId, failure);
        return new FormsDbIsolation(null, null);
    }

    /// <summary>
    /// The mark after one more failed move: a short wait for the first <see cref="IsolationAttempts"/> - 1 failures in a
    /// row, a day after each one from then on, so a move that keeps failing does not fence the portal's writes every few
    /// minutes.
    /// </summary>
    internal static FormsDbIsolationFailure NextIsolationFailure(FormsDbIsolationFailure? previous, DateTime now)
    {
        var attempts = (previous?.Attempts ?? 0) + 1;
        return new FormsDbIsolationFailure(now + (attempts < IsolationAttempts ? _isolationRetryDelay : _isolationGiveUpDelay), attempts);
    }

    // The portal's account has no use of its shared schema: a move is copying it, or one was broken off, which the next
    // attempt or the reconciliation completes.
    private async Task<bool> IsSharedSchemaFencedAsync(int tenantId)
    {
        await using var admin = await OpenAdminAsync(null);
        return await IsSharedSchemaFencedAsync(admin, tenantId);
    }

    private static async Task<bool> IsSharedSchemaFencedAsync(NpgsqlConnection admin, int tenantId)
    {
        await using var cmd = admin.CreateCommand();
        cmd.CommandText =
            "SELECT NOT has_schema_privilege(r.oid, n.oid, 'USAGE') FROM pg_roles r, pg_namespace n " +
            "WHERE r.rolname = @role AND n.nspname = @schema";
        cmd.Parameters.Add(new NpgsqlParameter("@role", GetRwUser(tenantId)));
        cmd.Parameters.Add(new NpgsqlParameter("@schema", GetSchemaName(tenantId)));

        return await cmd.ExecuteScalarAsync() is true;
    }

    // Some session holds the move's advisory lock of the portal; read from the lock table, so this never takes it. Only
    // a move holds it exclusively: provisioning and the reconciliation share it and are not a move.
    private async Task<bool> IsMoveRunningAsync(int tenantId)
    {
        await using var admin = await OpenAdminAsync(null);
        await using var cmd = admin.CreateCommand();
        cmd.CommandText =
            "SELECT EXISTS (SELECT 1 FROM pg_locks WHERE locktype = 'advisory' AND granted AND objsubid = 2 " +
            "AND mode = 'ExclusiveLock' AND database = (SELECT oid FROM pg_database WHERE datname = current_database()) " +
            "AND classid = @class AND objid = @tenant)";
        cmd.Parameters.Add(new NpgsqlParameter("@class", NpgsqlDbType.Oid) { Value = (uint)MoveLockClass });
        cmd.Parameters.Add(new NpgsqlParameter("@tenant", NpgsqlDbType.Oid) { Value = (uint)tenantId });

        return await cmd.ExecuteScalarAsync() is true;
    }

    /// <summary>
    /// A session of its own on the shared database that holds the move's advisory lock of the portal, or null when
    /// another session holds it in a way that excludes this one. A move holds it exclusively; provisioning and the
    /// reconciliation share it (<paramref name="shared"/>), so they shut a move out and a move shuts them out, but they
    /// never pass for a move to a writer. The lock ends with the session, also when the node dies, so it is never left
    /// behind.
    /// </summary>
    private async Task<NpgsqlConnection?> TryLockMoveAsync(int tenantId, bool shared = false)
    {
        var builder = new NpgsqlConnectionStringBuilder(AdminConnectionString) { Pooling = false, KeepAlive = 30 };
        var connection = new NpgsqlConnection(builder.ConnectionString);
        try
        {
            await connection.OpenAsync();

            await using var cmd = connection.CreateCommand();
            cmd.CommandText = shared ? "SELECT pg_try_advisory_lock_shared(@class, @tenant)" : "SELECT pg_try_advisory_lock(@class, @tenant)";
            cmd.Parameters.Add(new NpgsqlParameter("@class", MoveLockClass));
            cmd.Parameters.Add(new NpgsqlParameter("@tenant", tenantId));
            if (await cmd.ExecuteScalarAsync() is true)
            {
                return connection;
            }
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }

        await connection.DisposeAsync();
        return null;
    }

    private void StartIsolation(int tenantId, FormsDbIsolationFailure? previousFailure)
    {
        if (!_isolating.TryAdd(tenantId, 0))
        {
            return;
        }

        var stopping = applicationLifetime.ApplicationStopping;
        _ = Task.Run(async () =>
        {
            FormsDbCredentials? moved = null;
            try
            {
                await using var scope = serviceScopeFactory.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<TenantManager>().SetCurrentTenantAsync(tenantId);
                moved = await scope.ServiceProvider.GetRequiredService<FormsDbProvisioningService>().IsolateAsync(tenantId, stopping);
            }
            catch (OperationCanceledException) when (stopping.IsCancellationRequested)
            {
                // The host stops: the shared schema was given back (or is, by the next provisioning or the
                // reconciliation), and the next call starts the move again.
            }
            catch (Exception e)
            {
                logger.ErrorFormsDbIsolation(e, tenantId);
                await RecordIsolationFailureAsync(tenantId, previousFailure);
            }
            finally
            {
                _isolating.TryRemove(tenantId, out _);
            }

            // Null: another node holds the portal's lock and finishes what it does; its own mark stays as it is.
            if (moved != null)
            {
                await ForgetIsolationFailureAsync(tenantId);
            }
        }, stopping);
    }

    // Runs in a task nobody awaits: a cache that cannot be reached is logged here and must not leave the task faulted.
    private async Task RecordIsolationFailureAsync(int tenantId, FormsDbIsolationFailure? previous)
    {
        try
        {
            var failure = NextIsolationFailure(previous, DateTime.UtcNow);

            // Kept a day past the retry, so the next failure in a row still counts this one.
            await distributedCache.SetAsync(GetIsolationFailureKey(tenantId), failure, failure.RetryAt - DateTime.UtcNow + _isolationGiveUpDelay);
        }
        catch (Exception e)
        {
            logger.WarnFormsDbIsolationFailureNotRecorded(e, tenantId);
        }
    }

    private async Task ForgetIsolationFailureAsync(int tenantId)
    {
        try
        {
            await distributedCache.RemoveAsync(GetIsolationFailureKey(tenantId));
        }
        catch (Exception e)
        {
            logger.WarnFormsDbIsolationFailureNotCleared(e, tenantId);
        }
    }

    /// <summary>
    /// Moves the portal from its schema in the shared database into a database of its own. The portal's accounts lose
    /// the shared schema first, so nothing is written there any more, even over a connection a pool still holds; then
    /// every table is copied whole under the lock its writers take, the new database is marked live, and only then are
    /// the settings switched and the shared schema dropped. A failure before the mark gives the shared schema back (or
    /// leaves that to the next provisioning or the reconciliation), and a move interrupted after it is completed by the
    /// next call or by the reconciliation. A submission refused while the tables are copied is added from the search index once the move
    /// completes when its table was copied, otherwise when the table is created on the next use of the form. Null when
    /// another node holds the portal's lock past the wait, moving or provisioning it. The move also holds the portal's
    /// advisory lock on the shared database for as long as it runs: nothing gives the shared schema back meanwhile, even
    /// when the Redis lease of the provision lock lapses.
    /// </summary>
    public async Task<FormsDbCredentials?> IsolateAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        IDistributedLockHandle handle;
        try
        {
            handle = await distributedLockProvider.TryAcquireFairLockAsync(GetProvisionLockKey(tenantId));
        }
        catch (DistributedLockException)
        {
            // Not a failure of the move: the node holding the lock finishes it, and the caller asks again.
            return null;
        }

        FormsDbCredentials target;
        await using (handle)
        {
            await using var moveLock = await TryLockMoveAsync(tenantId);
            if (moveLock == null)
            {
                // A move of another node still runs past the lease of its provision lock.
                return null;
            }

            var databaseName = GetDatabaseName(tenantId);
            var current = await TryGetProvisionedAsync(tenantId, logUnusable: true);
            if (current?.Database == databaseName)
            {
                return current;
            }

            // Provisioned again in a live database of its own: a move broken off after the mark, completed below.
            current ??= await ProvisionAsync(tenantId, moveLock);

            // The roles belong to the whole server, so the new database is reached with the same passwords.
            var rwPassword = new NpgsqlConnectionStringBuilder(current.RwConnectionString).Password!;
            target = BuildCredentials(current.SchemaName, databaseName, GetRwUser(tenantId), rwPassword, current.RoUser, current.RoPassword);

            if (current.Database != databaseName && !await IsLiveAsync(databaseName))
            {
                await using (var admin = await OpenAdminAsync(null))
                {
                    await CreateDatabaseAsync(admin, databaseName);
                }

                await SetUpSchemaAsync(tenantId, databaseName, rwPassword, current.RoPassword);

                await FenceSharedSchemaAsync(tenantId);
                try
                {
                    // Nodes holding the credentials would write into the fenced schema and fail on its privileges;
                    // without them a write is refused as one that waits for the move.
                    await _cache.RemoveAndNotifyAsync(GetCacheKey(tenantId));
                    await CopyTablesAsync(tenantId, target.RwConnectionString, cancellationToken);
                    await MarkLiveAsync(moveLock, tenantId, databaseName);
                }
                catch
                {
                    // Not live yet: the portal goes on in the shared schema, and the half-built database is rebuilt
                    // on the next call or dropped by the reconciliation. If giving the schema back fails, the next
                    // provisioning or the reconciliation gives it back.
                    try
                    {
                        await UnfenceSharedSchemaAsync(tenantId);
                    }
                    catch (Exception e)
                    {
                        logger.WarnFormsDbNotUnfenced(e, tenantId);
                    }

                    throw;
                }
            }

            await CompleteSwitchAsync(tenantId);
            logger.InfoFormsDbIsolated(tenantId, databaseName);
        }

        await SyncMovedTablesAsync(tenantId);

        return target;
    }

    // Under the provision lock, once the portal's database is live: the settings name it, and the shared schema goes.
    private async Task CompleteSwitchAsync(int tenantId)
    {
        var databaseName = GetDatabaseName(tenantId);
        var settings = await settingsManager.LoadAsync<BuiltinFormsDbSettings>(tenantId);
        if (settings.DatabaseName != databaseName)
        {
            settings.DatabaseName = databaseName;

            // The save logs and swallows its own failure; the shared schema is dropped only once the settings name the new
            // database, otherwise a node reading them would provision the shared schema again and lose what it takes.
            if (!await settingsManager.SaveAsync(settings, tenantId))
            {
                throw new InvalidOperationException($"The built-in forms database settings of tenant {tenantId} could not be saved.");
            }
        }

        await _cache.RemoveAndNotifyAsync(GetCacheKey(tenantId));

        await using (var admin = await OpenAdminAsync(null))
        {
            await ExecuteAsync(admin, $"DROP SCHEMA IF EXISTS {Quote(GetSchemaName(tenantId))} CASCADE");

            // Idle sessions that the pools of every node still hold on the shared database count against the connection
            // limit of the account, which the portal's own database now needs. The switch is done by now: sessions that
            // cannot be ended are logged and close as their pools prune them.
            try
            {
                await EndSessionsAsync(admin, GetRwUser(tenantId), admin.Database);
                await EndSessionsAsync(admin, GetRoUser(tenantId), admin.Database);
            }
            catch (Exception e)
            {
                logger.WarnFormsDbSessionsNotEnded(e, tenantId, GetRwUser(tenantId));
            }
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

        // The copy reads the tables through this membership.
        await GrantToSelfAsync(admin, GetRwUser(tenantId));
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

    // On the move's own session: if that session was lost, and the advisory lock with it, the mark fails. The shared
    // schema must still be fenced, otherwise rows written there after the copy would go with it when the switch drops it.
    private static async Task MarkLiveAsync(NpgsqlConnection moveLock, int tenantId, string databaseName)
    {
        if (!await IsSharedSchemaFencedAsync(moveLock, tenantId))
        {
            throw new FormsDbProvisioningException($"The shared schema of tenant {tenantId} was opened again during the move.");
        }

        await ExecuteAsync(moveLock, $"COMMENT ON DATABASE {Quote(databaseName)} IS '{LiveDatabaseComment}'");
    }

    public async Task DeprovisionAsync(int tenantId)
    {
        await using (var connection = await OpenAdminAsync(null))
        {
            await DropServerObjectsAsync(connection, tenantId, logger);
        }

        await settingsManager.SaveAsync(new BuiltinFormsDbSettings(), tenantId);
        await _cache.RemoveAndNotifyAsync(GetCacheKey(tenantId));

        logger.InfoFormsDbDeprovisioned(tenantId, GetSchemaName(tenantId));
    }

    /// <summary>
    /// What the portal has on the server: its accounts first lose their sign-in and their open sessions, so an outside
    /// client (a BI tool on the read-only account) stops reading even when dropping the rest fails; then the schema, the
    /// portal's own database and the accounts go. Run as the administrator of the forms database, never a superuser.
    /// </summary>
    internal static async Task DropServerObjectsAsync(NpgsqlConnection admin, int tenantId, ILogger<FormsDbProvisioningService> logger)
    {
        var roUser = GetRoUser(tenantId);
        var rwUser = GetRwUser(tenantId);

        foreach (var role in new[] { roUser, rwUser })
        {
            if (await RoleExistsAsync(admin, role))
            {
                await ExecuteAsync(admin, $"ALTER ROLE {Quote(role)} NOLOGIN");

                // The role no longer signs in; sessions that cannot be ended here end with the database below, or with
                // the role once it is dropped.
                try
                {
                    await EndSessionsAsync(admin, role);
                }
                catch (Exception e)
                {
                    logger.WarnFormsDbSessionsNotEnded(e, tenantId, role);
                }
            }
        }

        await ExecuteAsync(admin, $"DROP SCHEMA IF EXISTS {Quote(GetSchemaName(tenantId))} CASCADE");
        await DropDatabaseAsync(admin, GetDatabaseName(tenantId));
        await DropRoleAsync(admin, tenantId, roUser, logger);
        await DropRoleAsync(admin, tenantId, rwUser, logger);
    }

    // Ends the role's sessions on every database of the server, or on the one given. Ending another role's session takes
    // that role's privileges, which its creator holds only through a membership it grants itself; a role that cannot sign
    // in any more does not come back.
    private static async Task EndSessionsAsync(NpgsqlConnection admin, string role, string? database = null)
    {
        await GrantToSelfAsync(admin, role);

        await using var cmd = admin.CreateCommand();
        cmd.CommandText = "SELECT count(pg_terminate_backend(pid)) FROM pg_stat_activity WHERE usename = @role AND (@database IS NULL OR datname = @database)";
        cmd.Parameters.Add(new NpgsqlParameter("@role", role));
        cmd.Parameters.Add(new NpgsqlParameter("@database", NpgsqlDbType.Text) { Value = (object?)database ?? DBNull.Value });
        await cmd.ExecuteScalarAsync();
    }

    // A database that sessions still use cannot be dropped. PostgreSQL 13 and later end them itself (FORCE); before it,
    // the sessions the administrator may end are ended first: its own, and those of the portal's accounts.
    private static async Task DropDatabaseAsync(NpgsqlConnection admin, string databaseName)
    {
        if (admin.PostgreSqlVersion.Major >= 13)
        {
            await ExecuteAsync(admin, $"DROP DATABASE IF EXISTS {Quote(databaseName)} WITH (FORCE)");
            return;
        }

        await using (var cmd = admin.CreateCommand())
        {
            cmd.CommandText =
                "SELECT count(pg_terminate_backend(pid)) FROM pg_stat_activity " +
                "WHERE datname = @database AND pid <> pg_backend_pid() AND pg_has_role(usesysid, 'USAGE')";
            cmd.Parameters.Add(new NpgsqlParameter("@database", databaseName));
            await cmd.ExecuteScalarAsync();
        }

        await ExecuteAsync(admin, $"DROP DATABASE IF EXISTS {Quote(databaseName)}");
    }

    // Since PostgreSQL 16 a repeated GRANT by the same grantor keeps the options of the existing one, so a membership
    // granted earlier WITH INHERIT FALSE (an administrator looking at the data) would stay without the privileges.
    private static async Task GrantToSelfAsync(NpgsqlConnection admin, string role)
    {
        await ExecuteAsync(admin, admin.PostgreSqlVersion.Major >= 16
            ? $"GRANT {Quote(role)} TO CURRENT_USER WITH INHERIT TRUE"
            : $"GRANT {Quote(role)} TO CURRENT_USER");
    }

    private static async Task SetPasswordAsync(NpgsqlConnection admin, string role, string password)
    {
        await ExecuteAsync(admin, $"ALTER ROLE {Quote(role)} WITH PASSWORD '{password.Replace("'", "''")}'");
    }

    /// <summary>
    /// Removes what portal removal or a broken move left behind: the databases, schemas and roles of portals that no
    /// longer exist, a database the portal's settings do not name, and the shared schema of a portal that moved out.
    /// <paramref name="activeTenants"/> are the active portals with built-in database settings, read in one query: one of
    /// them that still lives in the shared schema needs no look at all, so the pass costs a query per portal only for
    /// the ones that moved or may be gone.
    /// </summary>
    public async Task ReconcileAsync(IReadOnlySet<int> activeTenants, CancellationToken cancellationToken)
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

            var active = activeTenants.Contains(tenantId);
            var hasDatabase = databases.Contains(tenantId);
            if (active && !hasDatabase)
            {
                continue;
            }

            try
            {
                await ReconcileTenantAsync(tenantId, active, hasDatabase, schemas.Contains(tenantId));
            }
            catch (Exception e)
            {
                logger.WarnFormsDbNotReconciled(e, tenantId);
            }
        }
    }

    private async Task ReconcileTenantAsync(int tenantId, bool active, bool hasDatabase, bool hasSchema)
    {
        if (!active)
        {
            var tenant = await tenantManager.GetTenantAsync(tenantId);
            if (tenant == null || tenant.Status == TenantStatus.RemovePending)
            {
                await DeprovisionAsync(tenantId);
                logger.InfoFormsDbOrphanRemoved(tenantId, GetDatabaseName(tenantId));
                return;
            }
        }

        // A portal still in the shared schema has nothing to reconcile.
        if (!hasDatabase)
        {
            return;
        }

        // A move that completed, the usual case, is seen without the locks and the session they take: a live database
        // the settings name, and no shared schema. Settings read from a stale cache can only miss the switch, and then
        // the check below runs.
        var databaseName = GetDatabaseName(tenantId);
        if (!hasSchema && await IsLiveAsync(databaseName) &&
            (await settingsManager.LoadAsync<BuiltinFormsDbSettings>(tenantId)).DatabaseName == databaseName)
        {
            return;
        }

        // A move in progress holds these locks, so its database is not taken for a leftover; the advisory one also when the
        // Redis lease of the other lapsed. What to drop is decided by the mark on the database, never by the settings,
        // which this process may read from a stale cache.
        var switched = false;
        await using (await distributedLockProvider.TryAcquireFairLockAsync(GetProvisionLockKey(tenantId)))
        {
            await using var moveLock = await TryLockMoveAsync(tenantId, shared: true);
            if (moveLock == null)
            {
                return;
            }

            if (await IsLiveAsync(databaseName))
            {
                // A move interrupted after the copy: the settings and the shared schema are brought in line.
                var settings = await settingsManager.LoadAsync<BuiltinFormsDbSettings>(tenantId);
                if (hasSchema || settings.DatabaseName != databaseName)
                {
                    await CompleteSwitchAsync(tenantId);
                    logger.InfoFormsDbOrphanRemoved(tenantId, GetSchemaName(tenantId));
                    switched = true;
                }
            }
            else
            {
                await DropLeftoverDatabaseAsync(tenantId, databaseName, hasSchema);
            }
        }

        if (switched)
        {
            await SyncMovedTablesAsync(tenantId);
        }
    }

    // Under the provision lock: a database never marked live is a broken-off copy.
    private async Task DropLeftoverDatabaseAsync(int tenantId, string databaseName, bool hasSchema)
    {
        await using (var admin = await OpenAdminAsync(null))
        {
            await DropDatabaseAsync(admin, databaseName);
        }

        logger.InfoFormsDbOrphanRemoved(tenantId, databaseName);

        // A move interrupted during the copy left the shared schema closed to the portal.
        if (hasSchema)
        {
            await UnfenceSharedSchemaAsync(tenantId);
        }
    }

    // Best effort, after the switch: a submission refused while the tables were copied is in the search index, and the
    // copied table lacks it. Tables that were not copied are filled from the index when they are created on first use.
    private async Task SyncMovedTablesAsync(int tenantId)
    {
        try
        {
            List<string> tables;
            await using (var admin = await OpenAdminAsync(GetDatabaseName(tenantId)))
            await using (var cmd = admin.CreateCommand())
            {
                cmd.CommandText = "SELECT tablename FROM pg_tables WHERE schemaname = @schema AND tablename ~ '^form_[0-9]+_v[0-9]+$'";
                cmd.Parameters.Add(new NpgsqlParameter("@schema", GetSchemaName(tenantId)));

                tables = [];
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    tables.Add(reader.GetString(0));
                }
            }

            await using var scope = serviceScopeFactory.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<TenantManager>().SetCurrentTenantAsync(tenantId);

            // Runs in the background too, which must never provision: the report creator shares this client in the scope.
            if (!await scope.ServiceProvider.GetRequiredService<BuiltinFormsDatabaseClient>().UseProvisionedOnlyAsync())
            {
                logger.WarnFormsDbMovedTablesSkipped(tenantId);
                return;
            }

            var reportCreator = scope.ServiceProvider.GetRequiredService<FormFillingReportCreator>();

            foreach (var table in tables)
            {
                try
                {
                    await reportCreator.SyncBuiltinDbTableAsync(table);
                }
                catch (Exception e)
                {
                    logger.WarnFormsDbTableNotSynced(e, tenantId, table);
                }
            }
        }
        catch (Exception e)
        {
            logger.WarnFormsDbMovedTablesNotSynced(e, tenantId);
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
    // Logged only where provisioning follows: a move that fenced the account makes the stored credentials unusable too.
    private async Task<FormsDbCredentials?> TryGetProvisionedAsync(int tenantId, bool logUnusable = false)
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
            if (logUnusable)
            {
                logger.WarnFormsDbUnusable(tenantId, settings.SchemaName);
            }

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

                // A database of the administrator's server, not one this service made for a portal: roles that reached it
                // through PUBLIC (monitoring, backups, replication) are now refused until granted CONNECT themselves.
                if (database == new NpgsqlConnectionStringBuilder(AdminConnectionString).Database)
                {
                    logger.WarnFormsDbPublicConnectRevoked(database);
                }
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
    private static async Task DropRoleAsync(NpgsqlConnection connection, int tenantId, string role, ILogger<FormsDbProvisioningService> logger)
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

    // A portal that moved into its own database is provisioned there again while that database is live, also when the
    // settings do not name it: a move broken off before they were saved, or a backup taken before the move restored.
    // Back in the shared schema, its submissions would be dropped with that schema when the switch is completed.
    // The grants on the shared schema would lift the fence of a running move, so they are given only under the move's
    // advisory lock: the caller's own (a move provisioning first), or one taken here, which a running move refuses.
    private async Task<FormsDbCredentials> ProvisionAsync(int tenantId, NpgsqlConnection? moveLock)
    {
        var schemaName = GetSchemaName(tenantId);
        var rwUser = GetRwUser(tenantId);
        var roUser = GetRoUser(tenantId);
        var rwPassword = UserManagerWrapper.GeneratePassword();
        var roPassword = UserManagerWrapper.GeneratePassword();

        var databaseName = await IsLiveAsync(GetDatabaseName(tenantId)) ? GetDatabaseName(tenantId) : null;

        NpgsqlConnection? ownLock = null;
        if (databaseName == null && moveLock == null)
        {
            ownLock = await TryLockMoveAsync(tenantId, shared: true) ?? throw new FormsDbMovingException(tenantId);
        }

        await using (ownLock)
        {
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
        }

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
        var roBuilder = CreateRoConnectionStringBuilder(rwBuilder, roUser, roPassword);

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

    // What a portal administrator gets, so only what reaching the portal's tables needs: nothing of the server's own
    // connection (certificate trust, timeouts, error details) is handed out. TLS is kept as the server requires it.
    private static NpgsqlConnectionStringBuilder CreateRoConnectionStringBuilder(NpgsqlConnectionStringBuilder rwBuilder, string user, string password)
    {
        return new NpgsqlConnectionStringBuilder
        {
            Host = rwBuilder.Host,
            Port = rwBuilder.Port,
            Database = rwBuilder.Database,
            Username = user,
            Password = password,
            SearchPath = rwBuilder.SearchPath,
            SslMode = rwBuilder.SslMode
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

    [LoggerMessage(LogLevel.Warning, "Built-in forms DB: table {tableName} of tenant {tenantId} was not completed from the search index after the move")]
    public static partial void WarnFormsDbTableNotSynced(this ILogger<FormsDbProvisioningService> logger, Exception exception, int tenantId, string tableName);

    [LoggerMessage(LogLevel.Warning, "Built-in forms DB: the tables of tenant {tenantId} were not completed from the search index after the move")]
    public static partial void WarnFormsDbMovedTablesNotSynced(this ILogger<FormsDbProvisioningService> logger, Exception exception, int tenantId);

    [LoggerMessage(LogLevel.Warning, "Built-in forms DB: leftovers of tenant {tenantId} could not be checked")]
    public static partial void WarnFormsDbNotReconciled(this ILogger<FormsDbProvisioningService> logger, Exception exception, int tenantId);

    [LoggerMessage(LogLevel.Warning, "Built-in forms DB: the connection limit of role {role} was not changed, the next cleanup pass tries again")]
    public static partial void WarnFormsDbConnectionLimitNotSet(this ILogger<FormsDbProvisioningService> logger, Exception exception, string role);

    [LoggerMessage(LogLevel.Warning, "Built-in forms DB: the shared schema of tenant {tenantId} was not given back after a failed move, the next provisioning or the reconciliation gives it back")]
    public static partial void WarnFormsDbNotUnfenced(this ILogger<FormsDbProvisioningService> logger, Exception exception, int tenantId);

    [LoggerMessage(LogLevel.Warning, "Built-in forms DB: the tables of tenant {tenantId} were not completed from the search index after the move, the portal has no usable credentials; missing rows are added when each table is next used")]
    public static partial void WarnFormsDbMovedTablesSkipped(this ILogger<FormsDbProvisioningService> logger, int tenantId);

    [LoggerMessage(LogLevel.Error, "Built-in forms DB: the read-only password of tenant {tenantId} was changed but neither saved nor set back; the account signs in with no stored password until the next reset")]
    public static partial void ErrorFormsDbPasswordNotRestored(this ILogger<FormsDbProvisioningService> logger, Exception exception, int tenantId);

    [LoggerMessage(LogLevel.Warning, "Built-in forms DB: the open sessions of role {role} of tenant {tenantId} were not ended")]
    public static partial void WarnFormsDbSessionsNotEnded(this ILogger<FormsDbProvisioningService> logger, Exception exception, int tenantId, string role);

    [LoggerMessage(LogLevel.Warning, "Built-in forms DB: the failed move of tenant {tenantId} was not recorded, so the next call may start another one at once")]
    public static partial void WarnFormsDbIsolationFailureNotRecorded(this ILogger<FormsDbProvisioningService> logger, Exception exception, int tenantId);

    [LoggerMessage(LogLevel.Warning, "Built-in forms DB: tenant {tenantId} moved, but the mark of its earlier failed moves was not cleared")]
    public static partial void WarnFormsDbIsolationFailureNotCleared(this ILogger<FormsDbProvisioningService> logger, Exception exception, int tenantId);

    [LoggerMessage(LogLevel.Warning, "Built-in forms DB: CONNECT on database {databaseName} was revoked from PUBLIC so that portal accounts reach only their own data; grant CONNECT to the server's own roles (monitoring, backups, replication) that used it")]
    public static partial void WarnFormsDbPublicConnectRevoked(this ILogger<FormsDbProvisioningService> logger, string databaseName);
}

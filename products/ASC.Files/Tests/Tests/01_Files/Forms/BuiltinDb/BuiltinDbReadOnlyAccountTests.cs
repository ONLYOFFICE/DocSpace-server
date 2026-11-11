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

namespace ASC.Files.Tests.Tests._01_Files.Forms.BuiltinDb;

/// <summary>
/// <c>GET /files/builtinDb/connectionString</c> — the read-only account of the built-in forms database, on the server
/// the AppHost sets up as one shared by portals of different owners needs it: it reads its own portal's schema and
/// cannot load the server. The DocSpace.API.SDK 4.1.0 does not carry the endpoint yet, so it is called over raw HTTP.
/// </summary>
[Trait("Category", "Security")]
[Trait("Feature", "Forms")]
public class BuiltinDbReadOnlyAccountTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    // The limit the integration-test profile gives the read-only roles (Program.cs, AddFormsPostgres).
    private static readonly TimeSpan _queryLimit = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task ReadOnlyAccount_ReadsItsSchema_AndCannotWrite()
    {
        await using var connection = await OpenAsync((await GetCredentialsAsync()).ConnectionString);

        (await ScalarAsync(connection, "SELECT current_schema()")).Should().StartWith("tenant_");

        var exception = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(connection, "CREATE TABLE ro_probe (id int)"));
        exception.SqlState.Should().Be(PostgresErrorCodes.InsufficientPrivilege);
    }

    [Fact]
    public async Task ReadOnlyAccount_CannotWriteLargeObjects()
    {
        await using var connection = await OpenAsync((await GetCredentialsAsync()).ConnectionString);

        var exception = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(connection, "SELECT lo_from_bytea(0, 'x')"));
        exception.SqlState.Should().Be(PostgresErrorCodes.InsufficientPrivilege);
    }

    [Fact]
    public async Task ReadOnlyAccount_CannotConnectToOtherDatabases()
    {
        var credentials = await GetCredentialsAsync();
        var other = new NpgsqlConnectionStringBuilder(credentials.ConnectionString) { Database = "postgres" };

        var exception = await Assert.ThrowsAsync<PostgresException>(() => OpenAsync(other.ConnectionString));
        exception.SqlState.Should().Be(PostgresErrorCodes.InsufficientPrivilege);
    }

    [Fact]
    public async Task ReadOnlyAccount_LongQuery_IsEndedFromOutside()
    {
        await using var connection = await OpenAsync((await GetCredentialsAsync()).ConnectionString);

        // The role lifts its own timeout; pg_cron still ends the query once it runs past the limit.
        await ExecuteAsync(connection, "SET statement_timeout = 0");
        var started = Stopwatch.StartNew();

        var exception = await Assert.ThrowsAnyAsync<NpgsqlException>(() => ExecuteAsync(connection, "SELECT pg_sleep(120)"));

        // pg_terminate_backend ends the session: the server reports it, or the connection drops before it can.
        if (exception is PostgresException postgres)
        {
            postgres.SqlState.Should().Be(PostgresErrorCodes.AdminShutdown);
        }

        connection.FullState.Should().HaveFlag(ConnectionState.Broken);
        started.Elapsed.Should().BeGreaterThan(_queryLimit).And.BeLessThan(_queryLimit * 4);
    }

    [Fact]
    public async Task Credentials_PointToThePortalsOwnDatabase_AndRepeatedCallKeepsIt()
    {
        var first = await GetCredentialsAsync();
        var second = await GetCredentialsAsync();

        first.Database.Should().Be($"forms_t{TenantId}");
        second.Database.Should().Be(first.Database);
        second.Password.Should().Be(first.Password);
    }

    [Fact]
    public async Task ReadOnlyAccount_SeesNothingOfAnotherPortal()
    {
        var own = await GetCredentialsAsync();

        using var other = await Fixture.CreatePortalAsync(TestContext.Current.CancellationToken);
        await other.FilesHttpClient.Authenticate(other.Owner);
        var foreign = await GetCredentialsAsync(other.FilesHttpClient);

        // Its database cannot be entered...
        var toForeign = new NpgsqlConnectionStringBuilder(own.ConnectionString) { Database = foreign.Database };
        var exception = await Assert.ThrowsAsync<PostgresException>(() => OpenAsync(toForeign.ConnectionString));
        exception.SqlState.Should().Be(PostgresErrorCodes.InsufficientPrivilege);

        // ...nor the shared one, whose catalog lists the tables of the portals still living there.
        var toShared = new NpgsqlConnectionStringBuilder(own.ConnectionString) { Database = "forms" };
        exception = await Assert.ThrowsAsync<PostgresException>(() => OpenAsync(toShared.ConnectionString));
        exception.SqlState.Should().BeOneOf(PostgresErrorCodes.InsufficientPrivilege, PostgresErrorCodes.InvalidCatalogName);

        // The catalog of its own database holds no other portal's schema.
        await using var connection = await OpenAsync(own.ConnectionString);
        (await ScalarAsync(connection,
                "SELECT count(*)::text FROM pg_namespace WHERE nspname LIKE 'tenant_%' AND nspname <> current_schema()"))
            .Should().Be("0");
    }

    private Task<BuiltinDbCredentials> GetCredentialsAsync() => GetCredentialsAsync(_filesClient, Owner);

    private async Task<BuiltinDbCredentials> GetCredentialsAsync(HttpClient client, User? owner = null)
    {
        if (owner != null)
        {
            await client.Authenticate(owner);
        }

        // The first call answers 202 while the portal's own database is being prepared.
        var files = new RawApiClient(client);
        var deadline = DateTime.UtcNow.AddSeconds(60);
        while (true)
        {
            using var response = await files.GetAsync("api/2.0/files/builtinDb/connectionString", TestContext.Current.CancellationToken);
            if (response.StatusCode != HttpStatusCode.Accepted)
            {
                return await files.ReadAsync<BuiltinDbCredentials>(response, TestContext.Current.CancellationToken);
            }

            DateTime.UtcNow.Should().BeBefore(deadline, "the portal's own database must be ready within a minute");
            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }

    private static async Task<NpgsqlConnection> OpenAsync(string connectionString)
    {
        var connection = new NpgsqlConnection(connectionString);
        try
        {
            await connection.OpenAsync(TestContext.Current.CancellationToken);

            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection) { CommandTimeout = 0 };
        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<string> ScalarAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);

        return (string)(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken))!;
    }

    private sealed record BuiltinDbCredentials(string Host, int Port, string Database, string User, string Password, string ConnectionString);
}

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
/// The read-only account of the built-in forms database is used from outside DocSpace (a BI tool keeps a session open),
/// so taking access away must end what is already open: a reset password and a removed portal both close the account's
/// sessions, and a move of the portal running elsewhere is never undone by provisioning the shared schema meanwhile.
/// </summary>
[Trait("Category", "Security")]
[Trait("Feature", "Forms")]
public class BuiltinDbAccessRevocationTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task ResetPassword_OldPasswordAndOpenSessionStopReading()
    {
        await _filesClient.Authenticate(Owner);
        var issued = await BuiltinDbApi.GetReadyAsync(_filesClient);
        await using var open = await BuiltinDbApi.OpenAsync(issued.ConnectionString);

        var reset = await BuiltinDbApi.GetReadyAsync(_filesClient, resetPassword: true);

        await Assert.ThrowsAnyAsync<NpgsqlException>(() => BuiltinDbApi.ExecuteAsync(open, "SELECT 1"));
        var refused = await Assert.ThrowsAsync<PostgresException>(() => BuiltinDbApi.OpenAsync(issued.ConnectionString));
        refused.SqlState.Should().Be(PostgresErrorCodes.InvalidPassword);

        await using var renewed = await BuiltinDbApi.OpenAsync(reset.ConnectionString);
        (await BuiltinDbApi.ScalarAsync(renewed, "SELECT current_database()")).Should().Be(issued.Database);

        // The stored password is the new one: the next caller gets it.
        (await BuiltinDbApi.GetReadyAsync(_filesClient)).Password.Should().Be(reset.Password);
    }

    // The portal is removed the way its owner removes it, and the built-in database is deprovisioned behind the message
    // bus. Dropping the portal's database can fail: here a session of the server's superuser keeps it open, which the
    // administrator DocSpace signs in as cannot end. The read-only account loses its access all the same.
    [Fact]
    public async Task PortalRemoved_ReadOnlyAccountLosesAccess_EvenWhenItsDatabaseCannotBeDropped()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await _filesClient.Authenticate(Owner);
        var issued = await BuiltinDbApi.GetReadyAsync(_filesClient);
        await using var open = await BuiltinDbApi.OpenAsync(issued.ConnectionString);
        await using var keepsTheDatabase = await Fixture.OpenFormsServerAsync(issued.Database, cancellationToken);
        await using var server = await Fixture.OpenFormsServerAsync(BuiltinDbApi.SharedDatabase, cancellationToken);

        await RemovePortalAsync();

        var sql = $"SELECT coalesce(bool_or(rolcanlogin), false)::text FROM pg_roles WHERE rolname = '{issued.UserName}'";
        var deadline = DateTime.UtcNow.AddSeconds(60);
        string canSignIn;
        while ((canSignIn = await BuiltinDbApi.ScalarAsync(server, sql)) != "false" && DateTime.UtcNow < deadline)
        {
            await Task.Delay(500, cancellationToken);
        }

        canSignIn.Should().Be("false");
        await Assert.ThrowsAnyAsync<NpgsqlException>(() => BuiltinDbApi.ExecuteAsync(open, "SELECT 1"));
        await Assert.ThrowsAsync<PostgresException>(() => BuiltinDbApi.OpenAsync(issued.ConnectionString));
        (await BuiltinDbApi.ScalarAsync(server, $"SELECT count(*)::text FROM pg_database WHERE datname = '{issued.Database}'"))
            .Should().Be("1", "the superuser's session kept the database, so access was taken first");
    }

    // A move holds the portal's advisory lock on the shared database exclusively for as long as it copies, also past a
    // lapsed Redis lease of its provision lock. Held here by another session, it stands for a move of another node: the
    // portal is neither provisioned into the shared schema nor moved a second time. That a write skips the wait for the
    // Redis provision lock while the move holds it is not covered: no test can hold that lock.
    [Fact]
    public async Task MoveRunningElsewhere_NothingIsProvisioned_UntilItEnds()
    {
        await using (var move = await Fixture.OpenFormsServerAsync(BuiltinDbApi.SharedDatabase, TestContext.Current.CancellationToken))
        {
            await BuiltinDbApi.ExecuteAsync(move, $"SELECT pg_advisory_lock({FormsDbProvisioningService.MoveLockClass}, {TenantId})");

            await _filesClient.Authenticate(Owner);
            for (var call = 0; call < 3; call++)
            {
                using var response = await BuiltinDbApi.GetConnectionAsync(_filesClient);

                response.StatusCode.Should().Be(HttpStatusCode.Accepted);
            }

            (await BuiltinDbApi.ScalarAsync(move, $"SELECT count(*)::text FROM pg_namespace WHERE nspname = 'tenant_{TenantId}'")).Should().Be("0");
            (await BuiltinDbApi.ScalarAsync(move, $"SELECT count(*)::text FROM pg_database WHERE datname = 'forms_t{TenantId}'")).Should().Be("0");
        }

        // The session that held the lock is gone, and with it the move it stood for.
        (await BuiltinDbApi.GetReadyAsync(_filesClient)).Database.Should().Be($"forms_t{TenantId}");
    }

    // DELETE api/2.0/portal/delete with the link the removal letter carries, signed by the services' machine key: the
    // confirmation travels in the "confirm" header, set on the client the SDK call goes through.
    private async Task RemovePortalAsync()
    {
        var builder = new ConfigurationBuilder();
        Microsoft.Extensions.Configuration.MemoryConfigurationBuilderExtensions.AddInMemoryCollection(
            builder, new Dictionary<string, string?> { ["core:machinekey"] = Fixture.GetAppHostSetting("core:machinekey") });
        var configuration = builder.Build();

        var provider = new ASC.Security.Cryptography.EmailValidationKeyProvider(
            new MachinePseudoKeys(configuration),
            null!,
            configuration,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<ASC.Security.Cryptography.EmailValidationKeyProvider>.Instance);
        var key = provider.GetEmailKey(Owner.Email + "PortalRemove", TenantId);

        await _webApiClient.Authenticate(null);
        _webApiClient.DefaultRequestHeaders.Add("confirm", $"type=PortalRemove&email={Uri.EscapeDataString(Owner.Email)}&key={Uri.EscapeDataString(key)}");
        try
        {
            await _portalSettingsApi.DeletePortalAsync(TestContext.Current.CancellationToken);
        }
        finally
        {
            _webApiClient.DefaultRequestHeaders.Remove("confirm");
        }
    }
}

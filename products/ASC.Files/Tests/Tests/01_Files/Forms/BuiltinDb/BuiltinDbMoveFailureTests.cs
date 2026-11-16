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
/// A move into the portal's own database that fails is reported with 503 and the wait before the next attempt, and the
/// portal goes on in the shared schema meanwhile. Failures in a row are counted, and after the last short wait the next
/// move waits a day.
/// </summary>
[Trait("Category", "Forms")]
[Trait("Feature", "Forms")]
public class BuiltinDbMoveFailureTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    // The failure is planted on the server, as no call can make a move fail. First the portal is provisioned in the
    // shared schema while another session shares the move's lock, which lets provisioning through and keeps a move
    // out. Then a table is added there whose column type exists in the shared database only, so copying it into the
    // portal's own database fails: after the shared schema was fenced, which the failed move has to give back.
    [Fact]
    public async Task MoveFails_CallReturns503WithTheWait_AndThePortalGetsTheSharedSchemaBack()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var schema = $"tenant_{TenantId}";
        await using var server = await Fixture.OpenFormsServerAsync(BuiltinDbApi.SharedDatabase, cancellationToken);

        await _filesClient.Authenticate(Owner);

        await using (var provisioning = await Fixture.OpenFormsServerAsync(BuiltinDbApi.SharedDatabase, cancellationToken))
        {
            await BuiltinDbApi.ExecuteAsync(provisioning, $"SELECT pg_advisory_lock_shared({FormsDbProvisioningService.MoveLockClass}, {TenantId})");

            using var first = await BuiltinDbApi.GetConnectionAsync(_filesClient);
            first.StatusCode.Should().Be(HttpStatusCode.Accepted);
            (await BuiltinDbApi.ScalarAsync(server, $"SELECT count(*)::text FROM pg_namespace WHERE nspname = '{schema}'")).Should().Be("1");

            await BuiltinDbApi.ExecuteAsync(server, $"CREATE DOMAIN \"{schema}\".shared_only AS integer");
            await BuiltinDbApi.ExecuteAsync(server, $"CREATE TABLE \"{schema}\".form_1_v1 (answer \"{schema}\".shared_only)");
        }

        var retryAfter = await WaitForFailedMoveAsync();

        // The integration-test profile waits a couple of seconds between the first failures, not five minutes.
        retryAfter.Should().BeGreaterThan(TimeSpan.Zero).And.BeLessThanOrEqualTo(ASC.AppHost.Configuration.Constants.BuiltinFormsDbTestMoveRetryDelay);

        // The move failed after the fence: the shared schema is given back, and the half-built database is not live.
        (await BuiltinDbApi.ScalarAsync(server, $"SELECT has_schema_privilege('forms_t{TenantId}_rw', '{schema}', 'USAGE')::text"))
            .Should().Be("true");
        (await BuiltinDbApi.ScalarAsync(server,
                $"SELECT coalesce(shobj_description(oid, 'pg_database'), '') FROM pg_database WHERE datname = 'forms_t{TenantId}'"))
            .Should().NotBe("docspace forms: live");

        // The failures in a row are counted from one attempt to the next: after the last short wait comes a day.
        for (var failure = 2; failure <= FormsDbProvisioningService.IsolationAttempts; failure++)
        {
            await Task.Delay(retryAfter + TimeSpan.FromSeconds(1), cancellationToken);
            retryAfter = await WaitForFailedMoveAsync();
        }

        retryAfter.Should().BeGreaterThan(TimeSpan.FromHours(1));
    }

    // Repeats the call, which starts a move and answers 202 while it runs, until the failed move answers 503; the wait it
    // gives.
    private async Task<TimeSpan> WaitForFailedMoveAsync()
    {
        var deadline = DateTime.UtcNow.AddSeconds(60);
        while (true)
        {
            using var response = await BuiltinDbApi.GetConnectionAsync(_filesClient);
            if (response.StatusCode != HttpStatusCode.Accepted || DateTime.UtcNow >= deadline)
            {
                response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
                response.Headers.RetryAfter.Should().NotBeNull();
                response.Headers.RetryAfter!.Delta.Should().NotBeNull();

                return response.Headers.RetryAfter!.Delta!.Value;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }
}

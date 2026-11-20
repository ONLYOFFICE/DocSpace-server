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

namespace ASC.Files.Tests.Tests._03_Rooms.ThirdParty;

/// <summary>
/// A file operation that the worker runs against a Nextcloud account must act as that account, whichever account
/// the worker reached on the same host before.
/// </summary>
/// <remarks>
/// <para>
/// Bug 83399 was fixed in <c>WebDavStorage</c> by asking for the cookie-less <c>customHttpClientNoCookie</c>
/// client. Only the API hosts registered that name, though, and <c>IHttpClientFactory</c> does not refuse a name
/// that was never registered. Files.Worker therefore got the default handler, which keeps the session cookie
/// Nextcloud issues on a successful sign-in. Every client created for that name within one handler's lifetime (two
/// minutes by default) shares that cookie, and <c>WebDavStorage</c> creates a client per operation. After a copy
/// into one account, the worker's next request to the same host
/// carried that account's session along with the second account's credentials. Nextcloud ran the request as the
/// first account, and the write into the second account failed. Fixed by registering the standard named clients
/// in the workers too (<c>AddStandardHttpClients</c>).
/// </para>
/// <para>
/// The pair of copies runs twice, each time with a new file, so both pairs create the file rather than overwrite it.
/// Without the fix, a handler that expires between the two copies of one pair takes the cookie with it and lets that
/// pair pass; the copies are seconds apart, so it cannot expire between the copies of both pairs.
/// </para>
/// <para>
/// The second account is a Nextcloud user that the test creates through the provisioning API with the configured
/// account's credentials (the admin of the container the <c>integration-test</c> profile starts) and removes
/// afterwards. A Nextcloud account that may not create users skips the test.
/// </para>
/// </remarks>
[Trait("Category", "Rooms")]
[Trait("Requires", "Nextcloud")]
[Collection("Nextcloud")]
public class ThirdPartyWorkerSessionIsolationTests(
    AspireAppFixture fixture)
    : ThirdPartyTestBase(fixture)
{
    private const int Pairs = 2;

    private readonly List<string> _nextcloudUsersToDelete = [];

    [Fact]
    [Trait("Bug", "83399")]
    public async Task CopyBatchItems_IntoSecondAccountAfterFirst_WritesAsSecondAccount()
    {
        // Arrange
        RequireNextcloud();
        await _filesClient.Authenticate(Owner);
        var secondAccount = await CreateNextcloudUser();
        var (_, _, secondAccountRoomId) = await CreateNextcloudRoom(UniqueTitle("TP Worker Session Room"), account: secondAccount);
        var (_, firstAccountFolder) = await CreateWorkFolder("TP Worker Session");

        // Act: the copy into the configured account signs the worker in to the host as that account, and Nextcloud
        // answers with a session cookie; the copy into the second account follows at once
        var copies = new List<(string Title, string? Error)>();

        for (var pair = 0; pair < Pairs; pair++)
        {
            var source = await CreateFileInMy($"{UniqueTitle("TP Worker Session")}.docx", Owner);

            var first = await CopyInto(source.Id, firstAccountFolder.Id);
            first.Error.Should().BeNullOrEmpty("the copy into the configured account is the arrange step of each pair");

            copies.Add((source.Title, (await CopyInto(source.Id, secondAccountRoomId)).Error));
        }

        // Assert
        copies.Should().AllSatisfy(copy => copy.Error.Should().BeNullOrEmpty());

        foreach (var (title, _) in copies)
        {
            (await PropfindStatus($"{secondAccount.DavUrl}{Uri.EscapeDataString(title)}", secondAccount))
                .Should().Be(HttpStatusCode.MultiStatus, "the copy of {0} must land in the second account's own storage", title);
        }
    }

    /// <summary>
    /// Copies a file into a folder through the batch operation that Files.Worker runs, and returns the finished
    /// operation.
    /// </summary>
    private async Task<FileOperationDto> CopyInto(int fileId, string destFolderId)
    {
        var results = (await _filesOperationsApi.CopyBatchItemsAsync(new BatchRequestDto
        {
            ReturnSingleOperation = true,
            FileIds = [new(fileId)],
            DestFolderId = new(destFolderId),
            ConflictResolveType = FileConflictResolveType.Overwrite,
            DeleteAfter = false
        }, TestContext.Current.CancellationToken)).Response;
        var finished = await WaitLongOperation(results.FirstOrDefault()?.Id);

        finished.Should().NotBeNull();
        var operation = finished!.Should().ContainSingle().Subject;
        operation.Finished.Should().BeTrue();

        return operation;
    }

    /// <summary>
    /// Creates a Nextcloud user with its home already prepared.
    /// </summary>
    private async Task<NextcloudAccount> CreateNextcloudUser()
    {
        var login = $"dsworker{Guid.NewGuid():N}"[..20];
        var account = new NextcloudAccount($"{NextcloudOrigin}/remote.php/dav/files/{login}/", login, $"Qx!{Guid.NewGuid():N}");

        using var admin = CreateNextcloudClient(NextcloudLogin, NextcloudPassword);

        using var created = await admin.PostAsync(
            "ocs/v2.php/cloud/users?format=json",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["userid"] = account.Login, ["password"] = account.Password }),
            TestContext.Current.CancellationToken);

        // Only a refusal of the right itself (a non-admin account set through NEXTCLOUD_*) is an environment this
        // test cannot run in. On the integration-test stack the account is the container's admin, so a wrong
        // password, a disabled provisioning API or a Nextcloud that is down is a broken harness: red.
        Assert.SkipWhen(created.StatusCode == HttpStatusCode.Forbidden,
            "The configured Nextcloud account may not create users (403), so there is no second account.");
        created.IsSuccessStatusCode.Should().BeTrue(
            "the Nextcloud admin must be able to create a user: {0} {1}",
            (int)created.StatusCode, await created.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        _nextcloudUsersToDelete.Add(account.Login);

        // The first sign-in prepares the user's home, so that the connection finds it.
        (await PropfindStatus(account.DavUrl, account)).Should().Be(HttpStatusCode.MultiStatus, "the new Nextcloud user must be able to sign in");

        return account;
    }

    /// <summary>
    /// The status of a depth-0 PROPFIND that the test process sends to Nextcloud directly as the given account.
    /// Nothing of the worker's cookies reaches this request, so the answer is about the account's own storage.
    /// </summary>
    private async Task<HttpStatusCode> PropfindStatus(string url, NextcloudAccount account)
    {
        using var client = CreateNextcloudClient(account.Login, account.Password);
        using var propfind = new HttpRequestMessage(new HttpMethod("PROPFIND"), url);
        propfind.Headers.Add("Depth", "0");

        using var response = await client.SendAsync(propfind, TestContext.Current.CancellationToken);

        return response.StatusCode;
    }

    public override async ValueTask DisposeAsync()
    {
        try
        {
            // A test skipped for want of Nextcloud created nothing, and has no address to build a client for.
            if (_nextcloudUsersToDelete.Count == 0)
            {
                return;
            }

            using var admin = CreateNextcloudClient(NextcloudLogin, NextcloudPassword);

            foreach (var login in _nextcloudUsersToDelete)
            {
                // Best effort: a user left behind only costs a row in the test container, so neither a refused
                // delete, a Nextcloud that stopped answering nor a cancelled run fails a test that has passed.
                try
                {
                    using var _ = await admin.DeleteAsync($"ocs/v2.php/cloud/users/{login}?format=json", TestContext.Current.CancellationToken);
                }
                catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
                {
                    // dropped on purpose: the cleanup must not mask the test's own outcome
                }
            }
        }
        finally
        {
            await base.DisposeAsync();
        }
    }
}

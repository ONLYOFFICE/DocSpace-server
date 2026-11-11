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
/// A write into a connected storage whose own account is out of space: the user reads why, as a 400
/// on a direct call and as the error text of a file operation, instead of the provider's error code
/// (Dropbox answered <c>path/insufficient_space/</c>) or a server error.
/// </summary>
/// <remarks>
/// Only the WebDAV path can be driven here: Nextcloud is the one storage the stack runs, and a user
/// with a one-byte quota makes it answer 507 to any upload. Every test creates such a user through the
/// Nextcloud provisioning API with the account's credentials (the admin of the container the
/// <c>integration-test</c> profile starts) and removes it afterwards; a Nextcloud account that may not
/// create users skips the tests. The OAuth providers (Dropbox, Google Drive, OneDrive, Box) recognise
/// the same refusal in <c>ThirdPartyStorageQuota</c>, but no test can mint their tokens.
/// </remarks>
[Trait("Category", "Rooms")]
[Trait("Requires", "Nextcloud")]
[Collection("Nextcloud")]
public class ThirdPartyStorageQuotaTests(
    AspireAppFixture fixture)
    : ThirdPartyTestBase(fixture)
{
    // FilesCommonResource.ErrorMessage_ThirdPartyStorageFull as the owner's (English) portal shows it, in full, so a
    // text that only starts with it — the provider's error glued on — fails the check
    private const string OutOfSpaceText = "There is not enough free space in the connected third-party storage. Free up space in it and try again.";

    private readonly List<string> _nextcloudUsersToDelete = [];

    [Fact]
    public async Task CreateFile_InFullThirdPartyStorage_Returns400WithReadableReason()
    {
        // Arrange
        RequireNextcloud();
        await _filesClient.Authenticate(Owner);
        var roomId = await CreateRoomOnFullStorage("TP Full Create");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.CreateFileAsync(
                roomId, new CreateFileRequest($"{UniqueTitle("TP Full Create")}.docx"), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent!.ToString().Should().Contain(OutOfSpaceText);
    }

    [Fact]
    public async Task CopyBatchItems_IntoFullThirdPartyStorage_ReportsReadableReason()
    {
        // Arrange
        RequireNextcloud();
        await _filesClient.Authenticate(Owner);
        var roomId = await CreateRoomOnFullStorage("TP Full Copy");
        var source = await CreateFileInMy($"{UniqueTitle("TP Full Copy")}.docx", Owner);

        // Act
        var results = (await _filesOperationsApi.CopyBatchItemsAsync(new BatchRequestDto
        {
            ReturnSingleOperation = true,
            FileIds = [new(source.Id)],
            DestFolderId = new(roomId),
            ConflictResolveType = FileConflictResolveType.Overwrite,
            DeleteAfter = false
        }, TestContext.Current.CancellationToken)).Response;
        var finished = await WaitLongOperation(results.FirstOrDefault()?.Id);

        // Assert
        finished.Should().NotBeNull();
        var operation = finished!.Should().ContainSingle().Subject;
        operation.Finished.Should().BeTrue();
        operation.Error.Should().Be(OutOfSpaceText);
    }

    /// <summary>
    /// The upload the UI makes for every file opens a session with <c>upload/create_session</c> and sends
    /// the bytes to <c>ChunkedUploader.ashx</c>, which answers an error with 200 and <c>success: false</c>.
    /// The handler used to prefer the inner exception's text, which for this refusal is the provider's own
    /// error — "Insufficient Storage" from WebDAV, <c>path/insufficient_space/</c> from Dropbox — so the
    /// readable message never reached the user there.
    /// </summary>
    /// <remarks>
    /// Raw HTTP on both steps, within the carve-outs of the tests rule: the SDK types the folder id of
    /// <c>create_session</c> as an integer, which cannot address a third-party room, and the handler is not
    /// a controller, so it has no SDK signature at all.
    /// </remarks>
    [Fact]
    public async Task ChunkedUpload_IntoFullThirdPartyStorage_ReportsReadableReason()
    {
        // Arrange
        RequireNextcloud();
        await _filesClient.Authenticate(Owner);
        var roomId = await CreateRoomOnFullStorage("TP Full Upload");
        var title = $"{UniqueTitle("TP Full Upload")}.txt";
        var content = Encoding.UTF8.GetBytes("content that does not fit into the storage");

        using var created = await _filesClient.PostAsJsonAsync(
            $"api/2.0/files/{Uri.EscapeDataString(roomId)}/upload/create_session",
            new { fileName = title, fileSize = content.Length },
            TestContext.Current.CancellationToken);
        var sessionBody = await created.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        created.IsSuccessStatusCode.Should().BeTrue("opening a session writes nothing to the storage yet: {0} {1}", (int)created.StatusCode, sessionBody);
        using var session = JsonDocument.Parse(sessionBody);
        var uploadId = session.RootElement.GetProperty("response").GetProperty("data").GetProperty("id").GetString();

        // Act: one chunk holding the whole file, so the handler commits it to the storage at once
        using var form = new MultipartFormDataContent();
        using var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        form.Add(file, "file", title);

        using var uploaded = await _filesClient.PostAsync($"ChunkedUploader.ashx?uid={uploadId}", form, TestContext.Current.CancellationToken);
        using var answer = JsonDocument.Parse(await uploaded.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        // Assert
        answer.RootElement.GetProperty("success").GetBoolean().Should().BeFalse();
        answer.RootElement.GetProperty("message").GetString().Should().Be(OutOfSpaceText);
    }

    /// <summary>
    /// Creates a Nextcloud user whose quota is one byte, connects it and turns the connection into a
    /// room rooted at the account. Any upload into the room is then refused with 507.
    /// </summary>
    private async Task<string> CreateRoomOnFullStorage(string prefix)
    {
        var (davUrl, login, password) = await CreateNextcloudUserWithoutSpace();

        var connection = (await _thirdPartyApi.SaveThirdPartyAsync(
            new ThirdPartyRequestDto(
                url: davUrl,
                login: login,
                password: password,
                customerTitle: UniqueTitle($"{prefix} (storage)"),
                providerKey: "Nextcloud"),
            TestContext.Current.CancellationToken)).Response;

        var room = await _roomsApi.CreateRoomThirdPartyAsync(
            connection.Id,
            new CreateThirdPartyRoomRequest(title: UniqueTitle($"{prefix} Room"), roomType: RoomType.CustomRoom),
            TestContext.Current.CancellationToken);

        return room.Response.Id;
    }

    private async Task<(string DavUrl, string Login, string Password)> CreateNextcloudUserWithoutSpace()
    {
        var login = $"dsquota{Guid.NewGuid():N}"[..20];
        var password = $"Qx!{Guid.NewGuid():N}";
        var davUrl = $"{NextcloudOrigin}/remote.php/dav/files/{login}/";

        using var admin = CreateNextcloudClient(NextcloudLogin, NextcloudPassword);

        using var created = await admin.PostAsync(
            "ocs/v2.php/cloud/users?format=json",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["userid"] = login, ["password"] = password }),
            TestContext.Current.CancellationToken);

        // Only a refusal of the right itself (a non-admin account set through NEXTCLOUD_*) is an environment
        // this test cannot run in. On the integration-test stack the account is the container's admin, so a
        // wrong password, a disabled provisioning API or a Nextcloud that is down is a broken harness: red.
        Assert.SkipWhen(created.StatusCode == HttpStatusCode.Forbidden,
            "The configured Nextcloud account may not create users (403), so no account can be filled up.");
        created.IsSuccessStatusCode.Should().BeTrue(
            "the Nextcloud admin must be able to create a user: {0} {1}",
            (int)created.StatusCode, await created.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        _nextcloudUsersToDelete.Add(login);

        // The first sign-in prepares the user's home (and copies the skeleton files, if any) while the
        // quota is still unlimited; only then is the quota cut, so the home exists and has no space left.
        using (var user = CreateNextcloudClient(login, password))
        using (var propfind = new HttpRequestMessage(new HttpMethod("PROPFIND"), davUrl))
        {
            propfind.Headers.Add("Depth", "0");
            using var home = await user.SendAsync(propfind, TestContext.Current.CancellationToken);
            home.IsSuccessStatusCode.Should().BeTrue("the new Nextcloud user must be able to sign in");
        }

        using var quota = await admin.PutAsync(
            $"ocs/v2.php/cloud/users/{login}?format=json",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["key"] = "quota", ["value"] = "1 B" }),
            TestContext.Current.CancellationToken);

        quota.IsSuccessStatusCode.Should().BeTrue("the admin must be able to set the quota of the user it created");

        return (davUrl, login, password);
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
                // delete, a Nextcloud that stopped answering nor a cancelled run fails a test that has passed,
                // and one failed delete does not keep the others from running.
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

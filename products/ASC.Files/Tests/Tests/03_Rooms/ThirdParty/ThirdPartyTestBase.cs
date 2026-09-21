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
/// Shared setup for the third-party (WebDAV/Nextcloud) suites.
/// </summary>
/// <remarks>
/// Nextcloud is the only provider we have real credentials for — the OAuth providers
/// (Box, GoogleDrive, OneDrive, Dropbox) need a token no test can mint. The credentials come
/// from the environment under the same names the TypeScript suite uses
/// (<c>NEXTCLOUD_URL</c> / <c>NEXTCLOUD_LOGIN</c> / <c>NEXTCLOUD_PASSWORD</c>). The
/// <c>integration-test</c> profile starts a Nextcloud container of its own and the fixture fills
/// those variables from it unless they are already set, so by default the tests run against a
/// fresh local account; set the variables to point them at a real Nextcloud instead. Without
/// either they skip rather than fail.
/// <para>
/// Everything a third-party room holds lives in the one real Nextcloud account, which every test
/// class of every run shares, and deleting the room (or the connection behind it) only detaches
/// the account — the remote files stay. The account root is therefore large, and the host answers
/// in about a second per request, so a test never works in the account root: it takes a room that
/// is a fresh subfolder of its own, and its own uniquely named folder inside that, from
/// <see cref="CreateWorkFolder"/>. The base class deletes the folder through the room after the
/// test, and the room's folder through a second connection, since the room's own connection cannot
/// delete the folder it is rooted in.
/// </para>
/// <para>
/// The suites that actually write to the storage share the <c>Nextcloud</c> test collection and so
/// run one after another: the account is a single external host with a 10-second request timeout
/// on the portal side, and three classes creating and deleting folders in it at once trip that
/// timeout on plain folder creation.
/// </para>
/// </remarks>
public abstract class ThirdPartyTestBase(AspireAppFixture fixture) : BaseTest(fixture)
{
    /// <summary>
    /// A password that is never correct, used to assert that credentials are actually verified.
    /// </summary>
    protected const string WrongPassword = "definitely-wrong-password";

    /// <summary>
    /// The provider key the API reports a Nextcloud connection under: "Nextcloud" is a labelled
    /// preset over plain WebDAV.
    /// </summary>
    protected const string NextcloudProviderKey = "WebDav";

    private static string NextcloudUrl => Environment.GetEnvironmentVariable("NEXTCLOUD_URL") ?? "";
    private static string NextcloudLogin => Environment.GetEnvironmentVariable("NEXTCLOUD_LOGIN") ?? "";
    private static string NextcloudPassword => Environment.GetEnvironmentVariable("NEXTCLOUD_PASSWORD") ?? "";

    private readonly List<string> _foldersToDelete = [];
    private readonly List<string> _roomFolderTitlesToDelete = [];

    /// <summary>
    /// Skips the current test unless a reachable Nextcloud is configured in the environment.
    /// </summary>
    protected static void RequireNextcloud()
    {
        Assert.SkipWhen(
            string.IsNullOrEmpty(NextcloudUrl) || string.IsNullOrEmpty(NextcloudLogin) || string.IsNullOrEmpty(NextcloudPassword),
            "Nextcloud is not configured: set NEXTCLOUD_URL, NEXTCLOUD_LOGIN and NEXTCLOUD_PASSWORD.");
    }

    /// <summary>
    /// A title that no other test, run or portal can produce, for anything created in the shared
    /// Nextcloud account.
    /// </summary>
    protected static string UniqueTitle(string prefix)
    {
        return $"Autotest {prefix} {Guid.NewGuid():N}";
    }

    /// <summary>
    /// Builds a connect request for the configured Nextcloud account.
    /// </summary>
    /// <param name="customerTitle">The title the connection is saved under.</param>
    /// <param name="password">The password to authenticate with; defaults to the correct one.</param>
    protected static ThirdPartyRequestDto NextcloudRequest(string customerTitle, string? password = null)
    {
        // The generated model validates its required properties inside the constructor, so
        // customerTitle and providerKey have to be passed as arguments — an object initializer
        // throws ArgumentNullException before it ever assigns them.
        return new ThirdPartyRequestDto(
            url: NextcloudUrl,
            login: NextcloudLogin,
            password: password ?? NextcloudPassword,
            customerTitle: customerTitle,
            providerKey: "Nextcloud");
    }

    /// <summary>
    /// Connects the configured Nextcloud account and returns the third-party folder it created.
    /// </summary>
    protected async Task<ThirdPartyFolderDto> ConnectNextcloud(string customerTitle)
    {
        var response = await _thirdPartyApi.SaveThirdPartyAsync(
            NextcloudRequest(customerTitle), TestContext.Current.CancellationToken);

        return response.Response;
    }

    /// <summary>
    /// Returns the titles of every currently connected third-party account.
    /// </summary>
    protected async Task<List<string>> ConnectedAccountTitles()
    {
        var accounts = await _thirdPartyApi.GetThirdPartyAccountsAsync(TestContext.Current.CancellationToken);

        return accounts.Response.Select(a => a.CustomerTitle).ToList();
    }

    /// <summary>
    /// Connects a fresh Nextcloud account and immediately turns it into a room. A single
    /// third-party connection can only ever back one room (a second attempt is rejected with
    /// "This provider is already connected to the room"), so every room needs its own connection —
    /// mirrors the TypeScript suite's <c>createNextcloudRoom</c> helper.
    /// </summary>
    /// <remarks>
    /// By default the room is the connected folder itself, so its id is that folder's string id
    /// (<c>sbox-N</c>): a third-party room never gets an integer id, and it is addressed through
    /// the string overloads of the SDK. With <paramref name="createAsNewFolder"/> the room is a new
    /// subfolder of the account named after <paramref name="title"/>; that keeps the room small, but
    /// the subfolder stays in the storage once the room is gone, so use it only where the room root
    /// itself is what the test acts on.
    /// </remarks>
    protected async Task<(int ProviderId, string FolderId, string RoomId)> CreateNextcloudRoom(
        string title, RoomType roomType = RoomType.CustomRoom, bool createAsNewFolder = false)
    {
        var connection = await ConnectNextcloud($"{title} (storage)");

        var room = await _roomsApi.CreateRoomThirdPartyAsync(
            connection.Id,
            new CreateThirdPartyRoom(title: title, roomType: roomType, createAsNewFolder: createAsNewFolder),
            TestContext.Current.CancellationToken);

        if (createAsNewFolder)
        {
            _roomFolderTitlesToDelete.Add(title);
        }

        return (connection.ProviderId!.Value, connection.Id, room.Response.Id);
    }

    /// <summary>
    /// Creates a Nextcloud room that is a fresh subfolder of the account and, inside it, the folder
    /// the test works in. Both get unique titles and both are removed from the storage after the test.
    /// </summary>
    protected async Task<(string RoomId, ThirdPartyFolderDto Folder)> CreateWorkFolder(string prefix)
    {
        var (_, _, roomId) = await CreateNextcloudRoom(UniqueTitle($"{prefix} Room"), createAsNewFolder: true);
        var folder = await CreateThirdPartyFolder(roomId, UniqueTitle(prefix));

        return (roomId, folder);
    }

    /// <summary>
    /// Schedules a third-party folder the test created or renamed itself for deletion after the test.
    /// </summary>
    protected void DeleteAfterTest(string folderId)
    {
        _foldersToDelete.Add(folderId);
    }

    /// <summary>
    /// The id of a folder of a WebDAV connection, as the provider builds it: the connection's folder
    /// id followed by the base64url-encoded path. It lets a folder created through one connection be
    /// addressed through another connection to the same account.
    /// </summary>
    protected static string WebDavFolderId(string connectionId, string path)
    {
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(path))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        return $"{connectionId}-{encoded}";
    }

    /// <summary>
    /// Creates a folder in a third-party folder and schedules it for deletion after the test.
    /// </summary>
    protected async Task<ThirdPartyFolderDto> CreateThirdPartyFolder(string parentId, string title)
    {
        var folder = (await _foldersApi.CreateFolderAsync(
            parentId, new CreateFolder(title), TestContext.Current.CancellationToken)).Response;

        _foldersToDelete.Add(folder.Id);

        return folder;
    }

    /// <summary>
    /// Creates a file in a third-party folder.
    /// </summary>
    protected async Task<ThirdPartyFileDto> CreateThirdPartyFile(string folderId, string title)
    {
        return (await _filesApi.CreateFileAsync(
            folderId, new CreateFileJsonElement(title), TestContext.Current.CancellationToken)).Response;
    }

    /// <summary>
    /// The titles of the files and folders a third-party folder lists.
    /// </summary>
    protected async Task<(List<string> Files, List<string> Folders)> ListThirdPartyFolder(string folderId)
    {
        var content = (await _foldersApi.GetFolderByFolderIdAsync(
            folderId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        return (content.Files.Select(f => f.Title).ToList(), content.Folders.Select(f => f.Title).ToList());
    }

    /// <summary>
    /// Deletes a third-party folder from the storage and waits for the operation to finish. A
    /// folder that is already gone is not an error.
    /// </summary>
    protected async Task DeleteThirdPartyFolder(string folderId)
    {
        try
        {
            var results = (await _foldersApi.DeleteFolderAsync(
                folderId, new DeleteFolder(false, true), TestContext.Current.CancellationToken)).Response;

            await WaitLongOperation(results.FirstOrDefault()?.Id);
        }
        catch (ApiException e) when (e.ErrorCode == 404)
        {
            // Deleted by the test itself.
        }
    }

    /// <summary>
    /// Polls a third-party folder until the API answers "not found" and returns the last status it
    /// saw: 200 while the folder is still served, otherwise the error code. A delete runs in the
    /// worker process and the API process learns of it through a cache notification, so a read
    /// straight after the operation has finished can still be answered from the cached folder.
    /// </summary>
    protected async Task<int> WaitForFolderGone(string folderId)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);

        while (true)
        {
            try
            {
                await _foldersApi.GetFolderInfoAsync(folderId, TestContext.Current.CancellationToken);
            }
            catch (ApiException e)
            {
                return e.ErrorCode;
            }

            if (DateTime.UtcNow >= deadline)
            {
                return 200;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }

    public override async ValueTask DisposeAsync()
    {
        // The remote account outlives the portal, so the working folders are removed while the
        // clients are still bound to it. A failure here must not mask the test's own outcome.
        foreach (var folderId in _foldersToDelete)
        {
            try
            {
                await DeleteThirdPartyFolder(folderId);
            }
            catch (ApiException)
            {
                // Cleanup is best effort: the connection may already be gone (a test deleted the room).
            }
        }

        if (_roomFolderTitlesToDelete.Count > 0)
        {
            try
            {
                // A room cannot delete the folder it is rooted in, and its connection may already be
                // gone, so the room folders are removed through one more connection to the account.
                await _filesClient.Authenticate(Owner);
                var cleanup = await ConnectNextcloud(UniqueTitle("Cleanup"));

                foreach (var title in _roomFolderTitlesToDelete)
                {
                    await DeleteThirdPartyFolder(WebDavFolderId(cleanup.Id, $"/{title}"));
                }
            }
            catch (ApiException)
            {
                // Best effort as above.
            }
        }

        await base.DisposeAsync();
    }
}

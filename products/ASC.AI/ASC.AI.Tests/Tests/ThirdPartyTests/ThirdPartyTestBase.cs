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

namespace ASC.AI.Tests.Tests.ThirdPartyTests;

/// <summary>
/// Shared setup of the third-party room suites ported from thirdparty.spec.ts: a Nextcloud (WebDAV) account connected
/// and turned into a room, so that its string id (<c>sbox-N</c>) can be used as the entity of AI calls.
/// </summary>
/// <remarks>
/// Nextcloud is the only third-party provider with credentials a test can have. They come from the environment under
/// the names the Files suite and the TypeScript suite use (<c>NEXTCLOUD_URL</c>, <c>NEXTCLOUD_LOGIN</c>,
/// <c>NEXTCLOUD_PASSWORD</c>); without them a test skips rather than fails. The <c>integration-test</c> profile starts a
/// Nextcloud container, the AI suite's fixture lists it among its lazy resources and fills the variables from it unless
/// they are set already, and these classes wait for it before the portal is registered (<see cref="BaseTest.NeedsLazyResources"/>).
/// <para>
/// One connection backs one room, so every room needs its own connection. The room is a fresh subfolder of the account
/// (<c>createAsNewFolder</c>) with a unique title: the account root is large and slow. The subfolder stays in the remote
/// storage once the test is over.
/// </para>
/// </remarks>
public abstract class ThirdPartyTestBase(AspireAppFixture fixture) : McpServerStorageTests.PublicToolsTestBase(fixture)
{
    /// <summary>The marker the attachment tests put into a stored document and look for in what the chat service read.</summary>
    protected const string Marker = "TP-ATTACH-MARKER";

    protected override bool NeedsLazyResources => true;

    private static string NextcloudUrl => Environment.GetEnvironmentVariable("NEXTCLOUD_URL") ?? "";

    private static string NextcloudLogin => Environment.GetEnvironmentVariable("NEXTCLOUD_LOGIN") ?? "";

    private static string NextcloudPassword => Environment.GetEnvironmentVariable("NEXTCLOUD_PASSWORD") ?? "";

    /// <summary>Skips the current test unless a reachable Nextcloud is configured in the environment.</summary>
    protected static void RequireNextcloud()
    {
        Assert.SkipWhen(
            string.IsNullOrEmpty(NextcloudUrl) || string.IsNullOrEmpty(NextcloudLogin) || string.IsNullOrEmpty(NextcloudPassword),
            "Nextcloud is not configured: set NEXTCLOUD_URL, NEXTCLOUD_LOGIN and NEXTCLOUD_PASSWORD.");
    }

    /// <summary>
    /// Connects a fresh Nextcloud account and turns it into a room (a new subfolder of the account); returns the room's
    /// string id. The owner is signed in on the Files client afterwards.
    /// </summary>
    protected async Task<string> CreateNextcloudRoomAsync(string prefix)
    {
        RequireNextcloud();
        await FilesHttpClient.Authenticate(Owner);

        var title = $"Autotest {prefix} {Guid.NewGuid():N}";

        // The generated model validates its required properties in the constructor, so they are passed as arguments.
        var connection = (await ThirdPartyIntegrationApi.SaveThirdPartyAsync(
            new ThirdPartyRequestDto(url: NextcloudUrl, login: NextcloudLogin, password: NextcloudPassword, customerTitle: $"{title} (storage)", providerKey: "Nextcloud"),
            TestContext.Current.CancellationToken)).Response;

        var room = await RoomsApi.CreateRoomThirdPartyAsync(
            connection.Id,
            new CreateThirdPartyRoomRequest(title: title, roomType: RoomType.CustomRoom, createAsNewFolder: true),
            TestContext.Current.CancellationToken);

        return room.Response.Id;
    }

    /// <summary>Gives a member the access in a third-party room, as the owner.</summary>
    protected async Task ShareThirdPartyRoomAsync(string roomId, Guid userId, FileShare access)
    {
        await FilesHttpClient.Authenticate(Owner);
        await RoomsApi.SetRoomSecurityAsync(
            roomId,
            new RoomInvitationRequest([new RoomInvitation(userId, access)], notify: false),
            TestContext.Current.CancellationToken);
    }

    /// <summary>An ordinary room of the owner's, for the cases that compare a third-party room with an internal one.</summary>
    protected async Task<int> CreateCustomRoomAsync(string prefix)
    {
        await FilesHttpClient.Authenticate(Owner);

        var room = await RoomsApi.CreateRoomAsync(
            new CreateRoomRequestDto($"Autotest {prefix} {Guid.NewGuid():N}", roomType: RoomType.CustomRoom),
            TestContext.Current.CancellationToken);

        return room.Response.Id;
    }

    protected Task<List<AiThread>> ListThreadsForAsync(string? entityId) =>
        _aiThreads.AiThreadsListAsync(entityId, cancellationToken: TestContext.Current.CancellationToken);

    protected Task<Dictionary<string, object>> ListCustomServersForAsync(string? entityId) =>
        _aiTools.AiToolsListCustomServersAsync(entityId, TestContext.Current.CancellationToken);
}

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


namespace ASC.Files.Tests.Tests._08_Privacy.ExternalLinks;

/// <summary>
/// What the clients of the portal hear about external links of private items: a link set or revoked on an item updates
/// that item for the members who watch its parent, and the portal setting for such links reaches every client. Both
/// are best-effort and sent after the change took effect.
/// </summary>
[Trait("Category", "Socket")]
[Trait("Feature", "PrivacyRoom")]
public class PrivateLinkSocketTests(AspireAppFixture fixture) : PrivateLinkTestBase(fixture)
{
    private static Func<JsonElement, bool> FolderUpdated(int folderId) =>
        payload => payload.GetProperty("cmd").GetString() == "update"
                   && payload.GetProperty("type").GetString() == "folder"
                   && payload.GetProperty("id").GetRawText() == folderId.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// A folder with no file in it: nothing else updates it, so every update counted is one of the link. A file would
    /// also be updated by its thumbnail.
    /// </summary>
    [Fact]
    public async Task SettingAndRevokingALink_UpdatesTheFolder_ForAMemberWatchingTheRoom()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var room = await CreateRoomAsOwner();
        await _filesClient.Authenticate(Owner);
        var folderId = await CreateFolderIn(room.Id);

        await using var socket = await ConnectSocket(Owner);
        await socket.SubscribeAsync(true, cancellationToken, $"DIR-{room.Id}");

        await _filesClient.Authenticate(Owner);
        var linkId = await StoreFolderLink(folderId);
        await socket.WaitForCountAsync("s:modify-folder", FolderUpdated(folderId), 1, cancellationToken: cancellationToken);

        await RevokeFolderLink(folderId, linkId);
        await socket.WaitForCountAsync("s:modify-folder", FolderUpdated(folderId), 2, cancellationToken: cancellationToken);
    }

    [Fact]
    public async Task ThePortalSettingForTheLinks_ReachesTheQuotaRoom_WithItsValue()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var socket = await ConnectSocket(Owner);
        await socket.SubscribeAsync(false, cancellationToken, "quota");

        await _filesClient.Authenticate(Owner);
        using (var off = await new RawApiClient(_filesClient).PutAsync("api/2.0/files/settings/external/private", new { set = false }, cancellationToken))
        {
            ((int)off.StatusCode).Should().Be(200);
        }

        await socket.WaitForAsync(
            "s:change-private-room-links-settings",
            payload => payload.GetProperty("enabled").ValueKind == JsonValueKind.False,
            cancellationToken: cancellationToken);
    }
}

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

namespace ASC.Files.Tests.Tests._03_Rooms.Links;

/// <summary>
/// PUT /files/rooms/{id}/links with an invitation link whose <c>linkId</c> is the id of the room creator's account.
/// The sharing code never turns that id into a link and sets nothing, and the invitation path then read the missing
/// result, a 500. Nothing is changed, so the call now answers 200 with what that id already holds, as the external
/// link path does for the same id, and the room gets no invitation link.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomInvitationLinkCreatorIdTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task SetRoomLink_InvitationWithCreatorId_SetsNothing()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Invitation Creator Id");

        // Act
        await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(linkId: room.CreatedBy.Id, access: FileShare.Read, linkType: LinkType.Invitation, title: "Autotest Creator Id Invitation"),
            TestContext.Current.CancellationToken);

        // Assert
        var links = (await _roomsApi.GetRoomLinksAsync(room.Id, LinkType.Invitation, TestContext.Current.CancellationToken)).Response;
        links.Should().BeEmpty();
    }

    /// <summary>
    /// The same id on the external link path was already answered without an error; it stays so.
    /// </summary>
    [Fact]
    public async Task SetRoomLink_ExternalWithCreatorId_SetsNothing()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest External Creator Id");
        var before = (await _roomsApi.GetRoomLinksAsync(room.Id, LinkType.External, TestContext.Current.CancellationToken)).Response
            .Select(l => l.SharedLink.Id)
            .ToList();

        // Act
        await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(linkId: room.CreatedBy.Id, access: FileShare.Read, linkType: LinkType.External, title: "Autotest Creator Id External"),
            TestContext.Current.CancellationToken);

        // Assert
        var after = (await _roomsApi.GetRoomLinksAsync(room.Id, LinkType.External, TestContext.Current.CancellationToken)).Response
            .Select(l => l.SharedLink.Id);
        after.Should().BeEquivalentTo(before);
        after.Should().NotContain(room.CreatedBy.Id);
    }
}

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
/// GET /files/rooms/{id}/link — the room's single, always-present primary External link. Returns a
/// single FileShareWrapper (not an array, unlike GetRoomLinks) and auto-creates the link on first
/// read for room types that support it.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomLinkPrimaryTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task GetPrimaryLink_PublicRoom_ReturnsAutoCreatedExternalLink()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreatePublicRoom("Autotest Primary Link Room");

        // Act
        var link = (await _roomsApi.GetRoomsPrimaryExternalLinkAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        link.SharedLink.Id.Should().NotBeEmpty();
        link.SharedLink.ShareLink.Should().NotBeNullOrEmpty();
        link.SharedLink.LinkType.Should().Be(LinkType.External);
    }

    [Fact]
    public async Task GetPrimaryLink_FillingFormsRoom_ReturnsAutoCreatedPrimaryLink()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateFillingFormsRoom("Autotest Primary Link FillingForms");

        // Act
        var link = (await _roomsApi.GetRoomsPrimaryExternalLinkAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        link.SharedLink.Id.Should().NotBeEmpty();
        link.SharedLink.ShareLink.Should().NotBeNullOrEmpty();
        link.SharedLink.LinkType.Should().Be(LinkType.External);
        link.SharedLink.Primary.Should().BeTrue();
    }

    [Fact]
    public async Task GetPrimaryLink_ResponseShape_MatchesExpected()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreatePublicRoom("Autotest Primary Link Shape");

        // Act
        var response = (await _roomsApi.GetRoomsPrimaryExternalLinkAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var link = response.SharedLink;

        // Assert — a single object (not an array, unlike GetRoomLinks)
        link.Id.Should().NotBeEmpty();
        link.ShareLink.Should().StartWith("http");
        link.RequestToken.Should().NotBeNullOrEmpty();
        link.LinkType.Should().Be(LinkType.External);
        link.Primary.Should().BeTrue();
        link.DenyDownload.Should().NotBeNull();
        response.Access.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPrimaryLink_MatchesTheExternalLinkFromGetRoomLinks()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreatePublicRoom("Autotest Primary Link Matches List");

        // Act
        var primary = (await _roomsApi.GetRoomsPrimaryExternalLinkAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var links = (await _roomsApi.GetRoomLinksAsync(room.Id, LinkType.External, TestContext.Current.CancellationToken)).Response;

        // Assert
        links.Should().HaveCount(1);
        primary.SharedLink.Id.Should().Be(links[0].SharedLink.Id);
        primary.SharedLink.RequestToken.Should().Be(links[0].SharedLink.RequestToken);
    }

    [Fact]
    public async Task GetPrimaryLink_ReturnsTheExternalPrimaryLink_NotAnInvitationLink()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreatePublicRoom("Autotest Primary Link Not Invitation");

        var invitation = (await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(access: FileShare.Read, linkType: LinkType.Invitation, title: "Autotest Primary Not Invitation", denyDownload: false),
            TestContext.Current.CancellationToken)).Response;

        // Act
        var link = (await _roomsApi.GetRoomsPrimaryExternalLinkAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        link.SharedLink.LinkType.Should().Be(LinkType.External);
        link.SharedLink.Id.Should().NotBe(invitation.SharedLink.Id);
    }

    [Fact]
    public async Task GetPrimaryLink_RepeatedCalls_ReturnTheSameLink()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreatePublicRoom("Autotest Primary Link Stable");

        // Act
        var first = (await _roomsApi.GetRoomsPrimaryExternalLinkAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var second = (await _roomsApi.GetRoomsPrimaryExternalLinkAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        second.SharedLink.Id.Should().Be(first.SharedLink.Id);
        second.SharedLink.RequestToken.Should().Be(first.SharedLink.RequestToken);
    }

    /// <summary>
    /// A CustomRoom has no external link in GetRoomLinks (empty list), but the primary-link endpoint
    /// still returns a primary External link for it.
    /// </summary>
    [Fact]
    public async Task GetPrimaryLink_CustomRoom_ReturnsPrimaryExternalLink()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Primary Link Custom");

        // Act
        var link = (await _roomsApi.GetRoomsPrimaryExternalLinkAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        link.SharedLink.LinkType.Should().Be(LinkType.External);
        link.SharedLink.Primary.Should().BeTrue();
    }

    [Fact]
    public async Task GetPrimaryLink_EditingRoom_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCollaborationRoom("Autotest Primary Link Editing");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetRoomsPrimaryExternalLinkAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetPrimaryLink_ArchivedPublicRoom_StillReturnsPrimaryLink()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreatePublicRoom("Autotest Primary Link Archived");

        await _roomsApi.ArchiveRoomAsync(room.Id, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Act
        var link = (await _roomsApi.GetRoomsPrimaryExternalLinkAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        link.SharedLink.LinkType.Should().Be(LinkType.External);
    }
}

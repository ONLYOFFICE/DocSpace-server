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
/// GET /files/rooms/{id}/links — the list of a room's External and Invitation links. A complete
/// non-member gets 403, same as GetRoomsPrimaryExternalLink — <c>FileSharing.CheckAccessAsync</c>
/// enforces <c>CanReadAsync</c> (room membership) before it even considers link visibility. Only
/// once someone IS a room member does GetRoomLinks diverge: it answers 200 with an empty list when
/// the member lacks link-management access (<c>FileSecurity</c> requires exactly
/// <c>FileShare.RoomManager</c> for the <c>ReadLinks</c> action), rather than 403.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomLinkReadTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    [Fact]
    public async Task GetRoomLinks_PublicRoom_ReturnsAllLinksWithExpectedShape()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreatePublicRoom("Autotest Links Room");

        // Act
        var links = (await _roomsApi.GetRoomLinksAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        links.Should().NotBeEmpty();
        links[0].SharedLink.Id.Should().NotBeEmpty();
        links[0].SharedLink.ShareLink.Should().NotBeNullOrEmpty();
        links[0].SharedLink.LinkType.Should().Be(LinkType.External);
    }

    [Fact]
    public async Task GetRoomLinks_AutoCreatedExternalLinkOfPublicRoom_HasPrimaryTrue()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreatePublicRoom("Autotest Links Primary Flag Room");

        // Act
        var links = (await _roomsApi.GetRoomLinksAsync(room.Id, LinkType.External, TestContext.Current.CancellationToken)).Response;

        // Assert
        links.Should().HaveCount(1);
        links[0].SharedLink.Primary.Should().BeTrue();
        links[0].SharedLink.LinkType.Should().Be(LinkType.External);
    }

    [Fact]
    public async Task GetRoomLinks_CustomRoom_ReturnsEmptyListByDefault()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Links Empty Custom");

        // Act
        var links = (await _roomsApi.GetRoomLinksAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        links.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRoomLinks_ExternalLinkDenyDownloadTrue_IsReflected()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreatePublicRoom("Autotest Links denyDownload");

        var created = (await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(access: FileShare.Read, linkType: LinkType.External, title: "Autotest denyDownload Link", denyDownload: true),
            TestContext.Current.CancellationToken)).Response;
        var linkId = created.SharedLink.Id;

        // Act
        var links = (await _roomsApi.GetRoomLinksAsync(room.Id, LinkType.External, TestContext.Current.CancellationToken)).Response;

        // Assert
        var link = links.Single(l => l.SharedLink.Id == linkId);
        link.SharedLink.DenyDownload.Should().BeTrue();
    }

    [Fact]
    public async Task GetRoomLinks_InvitationLink_TitleIsPreserved()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Links Title Preserved");

        var created = (await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(access: FileShare.Read, linkType: LinkType.Invitation, title: "My Invitation Title", denyDownload: false),
            TestContext.Current.CancellationToken)).Response;
        var linkId = created.SharedLink.Id;

        // Act
        var links = (await _roomsApi.GetRoomLinksAsync(room.Id, LinkType.Invitation, TestContext.Current.CancellationToken)).Response;

        // Assert
        links.Single(l => l.SharedLink.Id == linkId).SharedLink.Title.Should().Be("My Invitation Title");
    }

    [Fact]
    public async Task GetRoomLinks_InvitationLink_MaxUseCountIsPreserved()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Links maxUseCount");

        var created = (await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(access: FileShare.Read, linkType: LinkType.Invitation, title: "Autotest maxUseCount Link", denyDownload: false, maxUseCount: 5),
            TestContext.Current.CancellationToken)).Response;
        var linkId = created.SharedLink.Id;

        // Act
        var links = (await _roomsApi.GetRoomLinksAsync(room.Id, LinkType.Invitation, TestContext.Current.CancellationToken)).Response;

        // Assert
        links.Single(l => l.SharedLink.Id == linkId).SharedLink.MaxUseCount.Should().Be(5);
    }

    [Fact]
    public async Task GetRoomLinks_ExternalLink_HasNonEmptyRequestToken()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreatePublicRoom("Autotest Links requestToken");

        // Act
        var links = (await _roomsApi.GetRoomLinksAsync(room.Id, LinkType.External, TestContext.Current.CancellationToken)).Response;

        // Assert
        links.Should().HaveCount(1);
        links[0].SharedLink.RequestToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetRoomLinks_TypeExternal_ReturnsOnlyExternalLinks()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreatePublicRoom("Autotest Links Filter External");

        await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(access: FileShare.Read, linkType: LinkType.Invitation, title: "Autotest Filter Invitation", denyDownload: false),
            TestContext.Current.CancellationToken);

        // Act
        var links = (await _roomsApi.GetRoomLinksAsync(room.Id, LinkType.External, TestContext.Current.CancellationToken)).Response;

        // Assert
        links.Should().HaveCount(1);
        links[0].SharedLink.LinkType.Should().Be(LinkType.External);
    }

    [Fact]
    public async Task GetRoomLinks_TypeInvitation_ReturnsOnlyInvitationLinks()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreatePublicRoom("Autotest Links Filter Invitation");

        await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(access: FileShare.Read, linkType: LinkType.Invitation, title: "Autotest Filter Invitation Link", denyDownload: false),
            TestContext.Current.CancellationToken);

        // Act
        var links = (await _roomsApi.GetRoomLinksAsync(room.Id, LinkType.Invitation, TestContext.Current.CancellationToken)).Response;

        // Assert
        links.Should().HaveCount(1);
        links[0].SharedLink.LinkType.Should().Be(LinkType.Invitation);
    }

    [Fact]
    public async Task GetRoomLinks_WithoutType_ReturnsBothExternalAndInvitationLinks()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreatePublicRoom("Autotest Links No Filter");

        await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(access: FileShare.Read, linkType: LinkType.Invitation, title: "Autotest No Filter Invitation", denyDownload: false),
            TestContext.Current.CancellationToken);

        // Act
        var links = (await _roomsApi.GetRoomLinksAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        links.Should().HaveCount(2);
        links.Select(l => l.SharedLink.LinkType).Should().Contain([LinkType.External, LinkType.Invitation]);
    }

    [Fact]
    public async Task GetRoomLinks_PublicRoomWithNoInvitations_TypeInvitation_ReturnsEmpty()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreatePublicRoom("Autotest Links Public NoInvitations");

        // Act
        var links = (await _roomsApi.GetRoomLinksAsync(room.Id, LinkType.Invitation, TestContext.Current.CancellationToken)).Response;

        // Assert
        links.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRoomLinks_CustomRoom_TypeExternal_ReturnsEmpty()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Links Custom NoExternal");

        // Act
        var links = (await _roomsApi.GetRoomLinksAsync(room.Id, LinkType.External, TestContext.Current.CancellationToken)).Response;

        // Assert
        links.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRoomLinks_SetRoomLinkAccessNone_RemovesInvitationLink()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Links Remove Invitation");

        var created = (await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(access: FileShare.Read, linkType: LinkType.Invitation, title: "Autotest Remove Invitation", denyDownload: false),
            TestContext.Current.CancellationToken)).Response;
        var linkId = created.SharedLink.Id;

        var beforeLinks = (await _roomsApi.GetRoomLinksAsync(room.Id, LinkType.Invitation, TestContext.Current.CancellationToken)).Response;
        beforeLinks.Select(l => l.SharedLink.Id).Should().Contain(linkId);

        // Act
        await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(linkId: linkId, access: FileShare.None, linkType: LinkType.Invitation, title: "Autotest Remove Invitation", denyDownload: false),
            TestContext.Current.CancellationToken);

        // Assert
        var afterLinks = (await _roomsApi.GetRoomLinksAsync(room.Id, LinkType.Invitation, TestContext.Current.CancellationToken)).Response;
        afterLinks.Should().BeEmpty();
        afterLinks.Select(l => l.SharedLink.Id).Should().NotContain(linkId);
    }

    [Fact]
    public async Task GetRoomLinks_MultipleExternalLinks_AllReturnedWithUniqueIds()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreatePublicRoom("Autotest Links Multiple Externals");
        var expectedTitles = new List<string>();

        for (var i = 0; i < 4; i++)
        {
            var title = $"Autotest Bulk External {i}";
            await _roomsApi.SetRoomLinkAsync(
                room.Id,
                new RoomLinkRequest(access: FileShare.Read, linkType: LinkType.External, title: title, denyDownload: false),
                TestContext.Current.CancellationToken);
            expectedTitles.Add(title);
        }

        // Act
        var links = (await _roomsApi.GetRoomLinksAsync(room.Id, LinkType.External, TestContext.Current.CancellationToken)).Response;

        // Assert
        var returnedIds = links.Select(l => l.SharedLink.Id).ToList();
        links.Should().HaveCount(5);
        returnedIds.Distinct().Should().HaveCount(5);
        links.Select(l => l.SharedLink.Title).Should().Contain(expectedTitles);
    }

    [Fact]
    public async Task GetRoomLinks_RepeatedGet_ReturnsSameSetOfLinkIds()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreatePublicRoom("Autotest Links Repeated GET");

        await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(access: FileShare.Read, linkType: LinkType.Invitation, title: "Autotest Repeated Invitation", denyDownload: false),
            TestContext.Current.CancellationToken);

        // Act
        var first = (await _roomsApi.GetRoomLinksAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var second = (await _roomsApi.GetRoomLinksAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        first.Should().HaveCount(2);
        second.Should().HaveCount(2);
        second.Select(l => l.SharedLink.Id).Should().BeEquivalentTo(first.Select(l => l.SharedLink.Id));
    }

    [Fact]
    public async Task GetRoomLinks_EditingRoom_HasNoExternalLinksByDefault()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCollaborationRoom("Autotest Links Editing NoExternal");

        // Act
        var links = (await _roomsApi.GetRoomLinksAsync(room.Id, LinkType.External, TestContext.Current.CancellationToken)).Response;

        // Assert
        links.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRoomLinks_VirtualDataRoom_HasNoExternalLinksByDefault()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateVDRRoom("Autotest Links VDR NoExternal");

        // Act
        var links = (await _roomsApi.GetRoomLinksAsync(room.Id, LinkType.External, TestContext.Current.CancellationToken)).Response;

        // Assert
        links.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRoomLinks_FillingFormsRoom_HasOneAutoCreatedExternalLinkByDefault()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateFillingFormsRoom("Autotest Links Form AutoExternal");

        // Act
        var links = (await _roomsApi.GetRoomLinksAsync(room.Id, LinkType.External, TestContext.Current.CancellationToken)).Response;

        // Assert
        links.Should().HaveCount(1);
        links[0].SharedLink.LinkType.Should().Be(LinkType.External);
        links[0].SharedLink.Primary.Should().BeTrue();
    }

    [Fact]
    public async Task GetRoomLinks_TwoRooms_ReturnOnlyTheirOwnLinks()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var roomA = await CreatePublicRoom("Autotest Links Isolation A");
        var roomB = await CreatePublicRoom("Autotest Links Isolation B");

        var linkA = (await _roomsApi.SetRoomLinkAsync(
            roomA.Id,
            new RoomLinkRequest(access: FileShare.Read, linkType: LinkType.Invitation, title: "Autotest Isolation Invite A", denyDownload: false),
            TestContext.Current.CancellationToken)).Response;
        var linkB = (await _roomsApi.SetRoomLinkAsync(
            roomB.Id,
            new RoomLinkRequest(access: FileShare.Read, linkType: LinkType.Invitation, title: "Autotest Isolation Invite B", denyDownload: false),
            TestContext.Current.CancellationToken)).Response;

        // Act
        var linksA = (await _roomsApi.GetRoomLinksAsync(roomA.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var linksB = (await _roomsApi.GetRoomLinksAsync(roomB.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        var idsA = linksA.Select(l => l.SharedLink.Id).ToList();
        var idsB = linksB.Select(l => l.SharedLink.Id).ToList();

        idsA.Should().Contain(linkA.SharedLink.Id).And.NotContain(linkB.SharedLink.Id);
        idsB.Should().Contain(linkB.SharedLink.Id).And.NotContain(linkA.SharedLink.Id);
    }

    [Fact]
    public async Task GetRoomLinks_DocSpaceAdminInvitedAsRoomManager_SeesExternalLinkOfPublicRoom()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreatePublicRoom("Autotest Links Admin Reads");

        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);
        await InviteToRoom(room.Id, admin, FileShare.RoomManager);

        await _filesClient.Authenticate(admin);

        // Act
        var links = (await _roomsApi.GetRoomLinksAsync(room.Id, LinkType.External, TestContext.Current.CancellationToken)).Response;

        // Assert
        links.Should().HaveCount(1);
        links[0].SharedLink.LinkType.Should().Be(LinkType.External);
    }

    /// <summary>
    /// A completely non-member — no share record on the room at all — cannot even read the room,
    /// so <c>FileSharing.CheckAccessAsync</c> throws <c>SecurityException</c> (403 "Access denied")
    /// before the "does this member manage links" check ever runs — see
    /// <c>ASC.Files.Core/Utils/FileSharing.cs</c>, <c>GetPureSharesAsync</c> / <c>CheckAccessAsync</c>.
    /// That is the same 403 <c>GetRoomsPrimaryExternalLink</c> gives a non-member; the TypeScript
    /// suite's assumption of 200 + empty for every non-member does not hold in this build. The
    /// 200 + empty-list behaviour is reserved for someone who CAN read the room (was invited) but
    /// lacks link-management access — see
    /// <see cref="GetRoomLinks_RoomAdminInvitedWithoutLinkAccess_ReturnsEmptyList"/> below.
    /// </summary>
    [Fact]
    public async Task GetRoomLinks_RoomAdminNotInvited_Forbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreatePublicRoom("Autotest Links RoomAdmin NotInvited");

        var roomAdmin = await InviteMember(EmployeeType.RoomAdmin);
        await _filesClient.Authenticate(roomAdmin);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetRoomLinksAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    /// <inheritdoc cref="GetRoomLinks_RoomAdminNotInvited_Forbidden"/>
    [Fact]
    public async Task GetRoomLinks_UserNotInvited_Forbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreatePublicRoom("Autotest Links User NotInvited");

        var user = await InviteMember(EmployeeType.User);
        await _filesClient.Authenticate(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetRoomLinksAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    /// <inheritdoc cref="GetRoomLinks_RoomAdminNotInvited_Forbidden"/>
    [Fact]
    public async Task GetRoomLinks_GuestNotInvited_Forbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreatePublicRoom("Autotest Links Guest NotInvited");

        var guest = await InviteMember(EmployeeType.Guest);
        await _filesClient.Authenticate(guest);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetRoomLinksAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    /// <summary>
    /// Unlike <see cref="GetRoomLinks_RoomAdminNotInvited_Forbidden"/>, this member was actually
    /// invited into the room — with <c>ContentCreator</c>, which is enough to read the room but not
    /// enough to manage its links (<c>FileSecurity</c> requires exactly <c>RoomManager</c> for the
    /// <c>ReadLinks</c> action). That is the case the 200 + empty-list response is actually for.
    /// </summary>
    [Fact]
    public async Task GetRoomLinks_RoomAdminInvitedWithoutLinkAccess_ReturnsEmptyList()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreatePublicRoom("Autotest Links RoomAdmin Invited NoLinkAccess");

        var roomAdmin = await InviteMember(EmployeeType.RoomAdmin);
        await InviteToRoom(room.Id, roomAdmin, FileShare.ContentCreator);
        await _filesClient.Authenticate(roomAdmin);

        // Act
        var links = (await _roomsApi.GetRoomLinksAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        links.Should().BeEmpty();
    }
}

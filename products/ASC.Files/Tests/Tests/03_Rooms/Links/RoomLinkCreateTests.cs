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
/// PUT /files/rooms/{id}/links — creating a new External or Invitation link (no <c>linkId</c>).
/// External links may be created multiple times per room; an Invitation link is a singleton — a
/// second create without <c>linkId</c> is rejected. Room-type support for each link kind is covered
/// at the bottom.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomLinkCreateTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    private Task<FolderDtoInteger> MkRoom(string title, RoomType roomType = RoomType.CustomRoom)
    {
        return roomType switch
        {
            RoomType.PublicRoom => CreatePublicRoom(title),
            RoomType.EditingRoom => CreateCollaborationRoom(title),
            RoomType.VirtualDataRoom => CreateVDRRoom(title),
            RoomType.FillingFormsRoom => CreateFillingFormsRoom(title),
            _ => CreateCustomRoom(title)
        };
    }

    [Fact]
    public async Task SetRoomLink_OwnerCreatesExternalLinkWithoutLinkId()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await MkRoom("Autotest setLink Create External");

        // Act
        var created = (await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(access: FileShare.Read, linkType: LinkType.External, title: "Autotest New External", denyDownload: false),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        created.SharedLink.Id.Should().NotBeEmpty();
        created.SharedLink.LinkType.Should().Be(LinkType.External);
        created.SharedLink.ShareLink.Should().NotBeNullOrEmpty();

        var links = (await _roomsApi.GetRoomLinksAsync(room.Id, LinkType.External, TestContext.Current.CancellationToken)).Response;
        links.Select(l => l.SharedLink.Id).Should().Contain(created.SharedLink.Id);
    }

    [Fact]
    public async Task SetRoomLink_SecondInvitationLinkWithoutLinkId_IsRejectedAsSingleton()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await MkRoom("Autotest setLink Invitation Single");

        var first = (await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(access: FileShare.Read, linkType: LinkType.Invitation, title: "First Invitation", denyDownload: false),
            TestContext.Current.CancellationToken)).Response;

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.SetRoomLinkAsync(
                room.Id,
                new RoomLinkRequest(access: FileShare.Read, linkType: LinkType.Invitation, title: "Second Invitation", denyDownload: false),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        var links = (await _roomsApi.GetRoomLinksAsync(room.Id, LinkType.Invitation, TestContext.Current.CancellationToken)).Response;
        links.Select(l => l.SharedLink.Id).Should().Contain(first.SharedLink.Id);
    }

    [Fact]
    public async Task SetRoomLink_InvitationLinkCanBeRecreatedAfterDeletion()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await MkRoom("Autotest setLink Recreate Invitation");

        var first = (await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(access: FileShare.Read, linkType: LinkType.Invitation, title: "Invitation V1", denyDownload: false),
            TestContext.Current.CancellationToken)).Response;

        await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(linkId: first.SharedLink.Id, access: FileShare.None, linkType: LinkType.Invitation, title: "Invitation V1", denyDownload: false),
            TestContext.Current.CancellationToken);

        // Act
        var recreated = (await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(access: FileShare.Read, linkType: LinkType.Invitation, title: "Invitation V2", denyDownload: false),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        recreated.SharedLink.Id.Should().NotBeEmpty();
        var links = (await _roomsApi.GetRoomLinksAsync(room.Id, LinkType.Invitation, TestContext.Current.CancellationToken)).Response;
        links.Should().HaveCount(1);
    }

    [Theory]
    [InlineData(FileShare.Read)]
    [InlineData(FileShare.Review)]
    [InlineData(FileShare.Comment)]
    [InlineData(FileShare.Editing)]
    public async Task SetRoomLink_InvitationLinkAcceptsAccess(FileShare access)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await MkRoom($"Autotest setLink Access {access}");

        // Act
        var created = (await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(access: access, linkType: LinkType.Invitation, title: $"Invitation Access {access}", denyDownload: false),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        var links = (await _roomsApi.GetRoomLinksAsync(room.Id, LinkType.Invitation, TestContext.Current.CancellationToken)).Response;
        var link = links.Single(l => l.SharedLink.Id == created.SharedLink.Id);
        link.Access.Should().Be(access);
    }

    [Fact]
    public async Task SetRoomLink_UnknownLinkId_IsUpsertedAsCreatedWithThatId()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await MkRoom("Autotest setLink Unknown LinkId");
        var fakeLinkId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        // Act
        var created = (await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(linkId: fakeLinkId, access: FileShare.Read, linkType: LinkType.External, title: "Upserted Link", denyDownload: false),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        created.SharedLink.Id.Should().Be(fakeLinkId);
    }

    [Fact]
    public async Task SetRoomLink_LinkIdFromAnotherRoom_CreatesSeparateLink_LeavingTheOriginalIntact()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var roomA = await MkRoom("Autotest setLink Cross Room A");
        var roomB = await MkRoom("Autotest setLink Cross Room B");

        var linkA = (await _roomsApi.SetRoomLinkAsync(
            roomA.Id,
            new RoomLinkRequest(access: FileShare.Read, linkType: LinkType.External, title: "Cross Room Link A", denyDownload: false),
            TestContext.Current.CancellationToken)).Response;

        // Act — reusing room A's linkId while creating a link on room B
        await _roomsApi.SetRoomLinkAsync(
            roomB.Id,
            new RoomLinkRequest(linkId: linkA.SharedLink.Id, access: FileShare.Read, linkType: LinkType.External, title: "Cross Room Link On B", denyDownload: false),
            TestContext.Current.CancellationToken);

        // Assert — room A's own link is untouched
        var linksA = (await _roomsApi.GetRoomLinksAsync(roomA.Id, LinkType.External, TestContext.Current.CancellationToken)).Response;
        linksA.Select(l => l.SharedLink.Id).Should().Contain(linkA.SharedLink.Id);
    }

    [Fact]
    public async Task SetRoomLink_MissingLinkType_DefaultsToAnInvitationLink()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await MkRoom("Autotest setLink No LinkType");

        // Act
        var created = (await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(access: FileShare.Read, title: "No LinkType", denyDownload: false),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        created.SharedLink.LinkType.Should().Be(LinkType.Invitation);
    }

    [Fact]
    public async Task SetRoomLink_MissingAccess_CreatesNoLink()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await MkRoom("Autotest setLink No Access");

        // Act — access omitted, treated as None
        await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(linkType: LinkType.Invitation, title: "No Access", denyDownload: false),
            TestContext.Current.CancellationToken);

        // Assert
        var links = (await _roomsApi.GetRoomLinksAsync(room.Id, LinkType.Invitation, TestContext.Current.CancellationToken)).Response;
        links.Should().BeEmpty();
    }

    [Fact]
    public async Task SetRoomLink_MissingTitle_IsAccepted()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await MkRoom("Autotest setLink No Title");

        // Act
        var created = (await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(access: FileShare.Read, linkType: LinkType.Invitation, denyDownload: false),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        created.SharedLink.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SetRoomLink_EmptyTitle_IsAcceptedAndAutoNamed()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await MkRoom("Autotest setLink Empty Title");

        // Act
        var created = (await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(access: FileShare.Read, linkType: LinkType.Invitation, title: "", denyDownload: false),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        created.SharedLink.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SetRoomLink_WhitespaceOnlyTitle_IsAccepted()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await MkRoom("Autotest setLink Whitespace Title");

        // Act
        var created = (await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(access: FileShare.Read, linkType: LinkType.Invitation, title: "   ", denyDownload: false),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        created.SharedLink.Id.Should().NotBeEmpty();
    }

    #region Room type coverage

    /// <summary>
    /// Most room types support the full Invitation-link lifecycle. A FillingFormsRoom is the
    /// exception — see <see cref="SetRoomLink_FillingFormsRoom_RejectsInvitationLinkCreation"/>.
    /// </summary>
    [Theory]
    [InlineData(RoomType.CustomRoom)]
    [InlineData(RoomType.PublicRoom)]
    [InlineData(RoomType.EditingRoom)]
    [InlineData(RoomType.VirtualDataRoom)]
    public async Task SetRoomLink_SupportsInvitationCreateUpdateDelete(RoomType roomType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await MkRoom($"Autotest setLink Type {roomType}", roomType);

        // Act — create
        var created = (await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(access: FileShare.Read, linkType: LinkType.Invitation, title: $"{roomType} Invitation", denyDownload: false),
            TestContext.Current.CancellationToken)).Response;

        // Act — update
        await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(linkId: created.SharedLink.Id, access: FileShare.Read, linkType: LinkType.Invitation, title: $"{roomType} Invitation Updated", denyDownload: false),
            TestContext.Current.CancellationToken);

        // Act — delete
        await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(linkId: created.SharedLink.Id, access: FileShare.None, linkType: LinkType.Invitation, title: $"{roomType} Invitation Updated", denyDownload: false),
            TestContext.Current.CancellationToken);

        // Assert
        var links = (await _roomsApi.GetRoomLinksAsync(room.Id, LinkType.Invitation, TestContext.Current.CancellationToken)).Response;
        links.Select(l => l.SharedLink.Id).Should().NotContain(created.SharedLink.Id);
    }

    /// <summary>
    /// Only CustomRoom and PublicRoom let an owner manage External links through setRoomLink.
    /// FillingFormsRoom exposes only its single auto-created External link, while EditingRoom and
    /// VirtualDataRoom have no External-link feature at all.
    /// </summary>
    [Theory]
    [InlineData(RoomType.CustomRoom)]
    [InlineData(RoomType.PublicRoom)]
    public async Task SetRoomLink_SupportsExternalCreateUpdateDelete(RoomType roomType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await MkRoom($"Autotest setLink External Type {roomType}", roomType);

        // Act — create
        var created = (await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(access: FileShare.Read, linkType: LinkType.External, title: $"{roomType} External", denyDownload: false),
            TestContext.Current.CancellationToken)).Response;

        // Act — update
        await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(linkId: created.SharedLink.Id, access: FileShare.Read, linkType: LinkType.External, title: $"{roomType} External Updated", denyDownload: true),
            TestContext.Current.CancellationToken);

        // Act — delete
        await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(linkId: created.SharedLink.Id, access: FileShare.None, linkType: LinkType.External, title: $"{roomType} External Updated", denyDownload: false),
            TestContext.Current.CancellationToken);

        // Assert
        var links = (await _roomsApi.GetRoomLinksAsync(room.Id, LinkType.External, TestContext.Current.CancellationToken)).Response;
        links.Select(l => l.SharedLink.Id).Should().NotContain(created.SharedLink.Id);
    }

    /// <summary>FillingFormsRoom, EditingRoom and VirtualDataRoom reject External-link creation with 403.</summary>
    [Theory]
    [InlineData(RoomType.FillingFormsRoom)]
    [InlineData(RoomType.EditingRoom)]
    [InlineData(RoomType.VirtualDataRoom)]
    public async Task SetRoomLink_RejectsExternalLinkCreation(RoomType roomType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await MkRoom($"Autotest setLink External Reject {roomType}", roomType);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.SetRoomLinkAsync(
                room.Id,
                new RoomLinkRequest(access: FileShare.Read, linkType: LinkType.External, title: $"{roomType} External", denyDownload: false),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    /// <summary>A FillingFormsRoom exposes only its auto-created External link; Invitation links are rejected.</summary>
    [Fact]
    public async Task SetRoomLink_FillingFormsRoom_RejectsInvitationLinkCreation()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateFillingFormsRoom("Autotest setLink FillingForms NoInvitation");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.SetRoomLinkAsync(
                room.Id,
                new RoomLinkRequest(access: FileShare.Read, linkType: LinkType.Invitation, title: "FillingForms Invitation", denyDownload: false),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        var links = (await _roomsApi.GetRoomLinksAsync(room.Id, LinkType.Invitation, TestContext.Current.CancellationToken)).Response;
        links.Should().BeEmpty();
    }

    #endregion
}

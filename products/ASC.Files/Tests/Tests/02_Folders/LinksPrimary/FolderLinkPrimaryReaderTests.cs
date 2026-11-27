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

namespace ASC.Files.Tests.Tests._02_Folders.LinksPrimary;

/// <summary>
/// <c>PUT files/folder/{id}/links</c> with <c>primary</c> and <c>access</c> Read in a public room, by a member who can
/// read the folder but may not manage its links (a content creator). Such a member may create the first primary link
/// of a folder that has none; a link that exists, the primary one or another, is changed only with the right to
/// manage the links. A second primary link of a public room is refused for any caller by the room's limit of one
/// primary link, which would hide the reader's check, so these tests change links that exist instead.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "Folders")]
public class FolderLinkPrimaryReaderTests(
    AspireAppFixture fixture)
    : FolderLinkPrimaryTestBase(fixture)
{
    [Fact]
    public async Task SetPrimaryLink_ContentCreatorChangesTheExistingPrimaryLinkOfAPublicRoom_Returns403AndTheLinkStaysAsItWas()
    {
        // Arrange: a public room gets its primary link when it is created
        await _filesClient.Authenticate(Owner);
        var room = await CreatePublicRoom("Autotest Public Room Primary Link Change");
        var before = (await _foldersApi.GetFolderPrimaryExternalLinkAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response.SharedLink;

        var member = await InviteContact(EmployeeType.User);
        await InviteToRoom(room.Id, member, FileShare.ContentCreator);
        await _filesClient.Authenticate(member);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.SetFolderPrimaryExternalLinkAsync(
                room.Id,
                new FolderLinkRequest(linkId: before.Id, access: FileShare.Read, title: "Autotest Taken Over", denyDownload: true, primary: true),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);

        await _filesClient.Authenticate(Owner);
        var after = (await _foldersApi.GetFolderPrimaryExternalLinkAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response.SharedLink;
        after.Id.Should().Be(before.Id);
        after.Title.Should().Be(before.Title);
        after.DenyDownload.Should().NotBe(true);
    }

    [Fact]
    public async Task SetPrimaryLink_ContentCreatorSendsTheIdOfAnotherLinkOfAPublicRoom_Returns403AndTheLinkStaysAsItWas()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreatePublicRoom("Autotest Public Room Second Link Change");
        var second = (await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(access: FileShare.Read, linkType: LinkType.External, title: "Autotest Second Link", denyDownload: false),
            TestContext.Current.CancellationToken)).Response.SharedLink;

        var member = await InviteContact(EmployeeType.User);
        await InviteToRoom(room.Id, member, FileShare.ContentCreator);
        await _filesClient.Authenticate(member);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.SetFolderPrimaryExternalLinkAsync(
                room.Id,
                new FolderLinkRequest(linkId: second.Id, access: FileShare.Read, title: "Autotest Taken Over", denyDownload: true, primary: true),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);

        await _filesClient.Authenticate(Owner);
        var links = (await _foldersApi.GetFolderLinksAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        var stored = links.Select(l => l.SharedLink).Single(l => l.Id == second.Id);
        stored.Title.Should().Be("Autotest Second Link");
        stored.DenyDownload.Should().NotBe(true);
    }

    /// <summary>
    /// A folder inside the room: the reader's exception finds the room through the folder's parents, so this is the
    /// path the room's own link does not take.
    /// </summary>
    [Fact]
    public async Task SetPrimaryLink_ContentCreatorChangesTheExistingPrimaryLinkOfAFolderInAPublicRoom_Returns403AndTheLinkStaysAsItWas()
    {
        // Arrange: the owner's read creates the folder's primary link
        await _filesClient.Authenticate(Owner);
        var room = await CreatePublicRoom("Autotest Public Room Folder Link Change");
        var folder = await CreateFolder("Autotest Folder With A Primary Link", room.Id);
        var before = (await _foldersApi.GetFolderPrimaryExternalLinkAsync(folder.Id, cancellationToken: TestContext.Current.CancellationToken)).Response.SharedLink;

        var member = await InviteContact(EmployeeType.User);
        await InviteToRoom(room.Id, member, FileShare.ContentCreator);
        await _filesClient.Authenticate(member);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.SetFolderPrimaryExternalLinkAsync(
                folder.Id,
                new FolderLinkRequest(linkId: before.Id, access: FileShare.Read, title: "Autotest Taken Over", denyDownload: true, primary: true),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);

        await _filesClient.Authenticate(Owner);
        var after = (await _foldersApi.GetFolderPrimaryExternalLinkAsync(folder.Id, cancellationToken: TestContext.Current.CancellationToken)).Response.SharedLink;
        after.Id.Should().Be(before.Id);
        after.Title.Should().Be(before.Title);
        after.DenyDownload.Should().NotBe(true);
    }

    [Fact]
    public async Task SetPrimaryLink_ContentCreatorCreatesTheFirstPrimaryLinkOfAFolderInAPublicRoom_Returns200()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreatePublicRoom("Autotest Public Room First Folder Link");
        var folder = await CreateFolder("Autotest First Link Folder", room.Id);

        var member = await InviteContact(EmployeeType.User);
        await InviteToRoom(room.Id, member, FileShare.ContentCreator);
        await _filesClient.Authenticate(member);

        // Act
        var created = (await _foldersApi.SetFolderPrimaryExternalLinkAsync(
            folder.Id,
            new FolderLinkRequest(access: FileShare.Read, primary: true),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        created.SharedLink.Primary.Should().BeTrue();
        created.Access.Should().Be(FileShare.Read);
    }
}

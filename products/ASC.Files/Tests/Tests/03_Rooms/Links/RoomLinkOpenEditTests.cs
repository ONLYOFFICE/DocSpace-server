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
/// <c>GET files/file/{fileId}/openedit</c> on an ordinary document of a room, opened with the request token
/// of an external link of that room: the access the file gets is the link's for a visitor who has no role
/// in the room, the member's own role for a member, and it follows the token of each request. A file inside
/// a room is the one place where the security filter takes a link's record from its per-request cache keyed
/// by the parent folder, so the cases put files next to each other in a subfolder as well.
/// </summary>
[Trait("Category", "Security")]
[Trait("Feature", "Rooms")]
public class RoomLinkOpenEditTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    public static TheoryData<RoomType, FileShare> LinkAccesses => new()
    {
        { RoomType.CustomRoom, FileShare.Read },
        { RoomType.CustomRoom, FileShare.Comment },
        { RoomType.CustomRoom, FileShare.Editing },
        { RoomType.PublicRoom, FileShare.Read },
        { RoomType.PublicRoom, FileShare.Comment },
        { RoomType.PublicRoom, FileShare.Editing }
    };

    [Theory]
    [MemberData(nameof(LinkAccesses))]
    public async Task OpenEditFile_AnonymousThroughRoomLink_GetsTheAccessOfTheLink(RoomType roomType, FileShare linkAccess)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await MkRoom(roomType, $"Autotest OpenEdit Room Link {roomType} {linkAccess}");
        var file = await CreateFile("Autotest Room Link Document.docx", room.Id);
        var token = await CreateRoomLink(room.Id, linkAccess);

        // Act
        await _filesClient.Authenticate(null);
        var opened = await OpenEditWithToken(file.Id, token);

        // Assert
        opened.Access.Should().Be(linkAccess);
        opened.Security.Edit.Should().Be(linkAccess == FileShare.Editing);
        opened.Security.Comment.Should().Be(linkAccess is FileShare.Editing or FileShare.Comment);
    }

    [Theory]
    [InlineData(FileShare.Read, FileShare.Editing)]
    [InlineData(FileShare.Editing, FileShare.Read)]
    public async Task OpenEditFile_MemberThroughRoomLink_KeepsTheirOwnRole(FileShare memberRole, FileShare linkAccess)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom($"Autotest OpenEdit Member Link {memberRole} {linkAccess}");
        var file = await CreateFile("Autotest Member Link Document.docx", room.Id);
        var token = await CreateRoomLink(room.Id, linkAccess);

        var member = await InviteContact(EmployeeType.User);
        await InviteToRoom(room.Id, member, memberRole);

        // Act
        await _filesClient.Authenticate(member);
        var withLink = await OpenEditWithToken(file.Id, token);
        var withoutLink = (await _filesApi.OpenEditFileAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response.File;

        // Assert
        withoutLink.Access.Should().Be(memberRole);
        withLink.Access.Should().Be(memberRole, "the member's own record comes before the link of the request");
        withLink.Security.Edit.Should().Be(memberRole == FileShare.Editing);
    }

    /// <summary>
    /// A signed-in account outside the room gets the link's access once it has sent the link's token. The portal
    /// then remembers the link for that account (a <c>RecentByLink</c> tag), so later requests without the token
    /// keep the same access; before the first request with the token the account has none.
    /// </summary>
    [Fact]
    public async Task OpenEditFile_NonMemberThroughRoomLink_GetsTheLinkFromTheFirstRequestWithItsToken()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest OpenEdit NonMember Link");
        var file = await CreateFile("Autotest NonMember Link Document.docx", room.Id);
        var token = await CreateRoomLink(room.Id, FileShare.Editing);

        var outsider = await InviteContact(EmployeeType.User);

        // Act
        await _filesClient.Authenticate(outsider);
        var before = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.OpenEditFileAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken));
        var withLink = await OpenEditWithToken(file.Id, token);
        var afterwards = (await _filesApi.OpenEditFileAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response.File;

        // Assert
        before.ErrorCode.Should().Be(403);
        withLink.Access.Should().Be(FileShare.Editing);
        withLink.Security.Edit.Should().BeTrue();
        afterwards.Access.Should().Be(FileShare.Editing);
    }

    [Fact]
    public async Task OpenEditAndListing_TwoLinksOfOneRoom_EachRequestGetsTheAccessOfItsOwnToken()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreatePublicRoom("Autotest OpenEdit Two Room Links");
        var folder = await CreateFolder("Autotest Two Links Folder", room.Id);
        var first = await CreateFile("Autotest Two Links First.docx", folder.Id);
        var second = await CreateFile("Autotest Two Links Second.docx", folder.Id);
        var readToken = await CreateRoomLink(room.Id, FileShare.Read);
        var editingToken = await CreateRoomLink(room.Id, FileShare.Editing);

        // Act
        await _filesClient.Authenticate(null);
        var throughRead = await OpenEditWithToken(first.Id, readToken);
        var throughEditing = await OpenEditWithToken(first.Id, editingToken);
        var listedThroughRead = await ListWithToken(folder.Id, readToken);
        var listedThroughEditing = await ListWithToken(folder.Id, editingToken);

        // Assert
        throughRead.Access.Should().Be(FileShare.Read);
        throughRead.Security.Edit.Should().BeFalse();
        throughEditing.Access.Should().Be(FileShare.Editing);
        throughEditing.Security.Edit.Should().BeTrue();

        listedThroughRead.Select(f => f.Id).Should().BeEquivalentTo([first.Id, second.Id]);
        listedThroughRead.Should().AllSatisfy(f => f.Access.Should().Be(FileShare.Read));
        listedThroughEditing.Select(f => f.Id).Should().BeEquivalentTo([first.Id, second.Id]);
        listedThroughEditing.Should().AllSatisfy(f => f.Access.Should().Be(FileShare.Editing));
    }

    private Task<FolderDto> MkRoom(RoomType roomType, string title)
    {
        return roomType == RoomType.PublicRoom ? CreatePublicRoom(title) : CreateCustomRoom(title);
    }

    private async Task<string> CreateRoomLink(int roomId, FileShare access)
    {
        var link = (await _roomsApi.SetRoomLinkAsync(
            roomId,
            new RoomLinkRequest(access: access, linkType: LinkType.External, title: $"Autotest {access} Link", denyDownload: false),
            TestContext.Current.CancellationToken)).Response;

        return link.SharedLink.RequestToken;
    }

    private async Task<FileDto> OpenEditWithToken(int fileId, string token)
    {
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, token);

        try
        {
            return (await _filesApi.OpenEditFileAsync(fileId, cancellationToken: TestContext.Current.CancellationToken)).Response.File;
        }
        finally
        {
            _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);
        }
    }

    private async Task<List<FileDto>> ListWithToken(int folderId, string token)
    {
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, token);

        try
        {
            var content = (await _foldersApi.GetFolderByFolderIdAsync(folderId, cancellationToken: TestContext.Current.CancellationToken)).Response;

            return content.Files.OfType<FileDto>().ToList();
        }
        finally
        {
            _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);
        }
    }
}

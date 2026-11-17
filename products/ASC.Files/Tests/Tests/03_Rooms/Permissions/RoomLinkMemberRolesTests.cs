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

namespace ASC.Files.Tests.Tests._03_Rooms.Permissions;

/// <summary>
/// Room members below Room manager manage no links: a Content creator cannot share a file it created,
/// and an Editor, Reviewer, Commentator or Form filler is refused both the room link and a link on a
/// file of the room (<c>FileSharingHelper.CanSetAccessAsync</c> asks for the room's manage right).
/// </summary>
[Trait("Category", "Rooms")]
public class RoomLinkMemberRolesTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    [Fact]
    public async Task SetFileExternalLink_ContentCreatorOnOwnFileInCustomRoom_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Content Creator File Link");
        var creator = await InviteMember(EmployeeType.RoomAdmin);
        await InviteToRoom(room.Id, creator, FileShare.ContentCreator);

        await _filesClient.Authenticate(creator);
        var file = await CreateFile("Autotest Content Creator Own File.docx", room.Id);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.SetFileExternalLinkAsync(file.Id, FileLink(FileShare.Read), TestContext.Current.CancellationToken));

        // Assert
        file.CreatedBy.Id.Should().Be(creator.Id);
        exception.ErrorCode.Should().Be(403);
    }

    [Theory]
    [InlineData(FileShare.Editing)]
    [InlineData(FileShare.Review)]
    [InlineData(FileShare.Comment)]
    public async Task SetRoomLink_CustomRoomMemberWithoutManageRight_Returns403(FileShare access)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom($"Autotest Room Link {access}");
        var member = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, member, access);

        await _filesClient.Authenticate(member);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.SetRoomLinkAsync(room.Id, BuildExternalLink(), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Theory]
    [InlineData(FileShare.Editing)]
    [InlineData(FileShare.Review)]
    [InlineData(FileShare.Comment)]
    public async Task SetFileExternalLink_CustomRoomMemberWithoutManageRight_Returns403(FileShare access)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom($"Autotest File Link {access}");
        var file = await CreateFile("Autotest Room File.docx", room.Id);
        var member = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, member, access);

        await _filesClient.Authenticate(member);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.SetFileExternalLinkAsync(file.Id, FileLink(FileShare.Read), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task SetRoomLinkAndFileLink_FormFillerInFormFillingRoom_Return403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateFillingFormsRoom("Autotest Form Filler Links");
        var file = await CreateFile("Autotest Form Filler Form.pdf", room.Id);
        var member = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, member, FileShare.FillForms);

        await _filesClient.Authenticate(member);

        // Act
        var roomLink = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.SetRoomLinkAsync(
                room.Id,
                new RoomLinkRequest(access: FileShare.FillForms, title: "Autotest Perm Link", linkType: LinkType.External),
                TestContext.Current.CancellationToken));
        var fileLink = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.SetFileExternalLinkAsync(file.Id, new FileLinkRequest(access: FileShare.FillForms, primary: true), TestContext.Current.CancellationToken));

        // Assert
        roomLink.ErrorCode.Should().Be(403);
        fileLink.ErrorCode.Should().Be(403);
    }

    private static FileLinkRequest FileLink(FileShare access)
    {
        return new FileLinkRequest(access: access, primary: false, title: "Autotest Perm File Link");
    }
}

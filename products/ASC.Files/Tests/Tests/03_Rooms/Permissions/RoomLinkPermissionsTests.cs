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

[Trait("Category", "Rooms")]
public class RoomLinkPermissionsTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    #region GET /files/rooms/{id}/link - access control

    // The endpoint is link-management scoped: only the room owner, portal admins (DocSpaceAdmin),
    // or a member invited with link-management access (RoomManager / ContentCreator) get 200. Lower
    // access levels (Editing / Read) and non-members get 403 even on a PublicRoom; anonymous requests
    // get 401. This differs from GetRoomLinks, which returns 200 + an empty list for non-members.

    [Fact]
    public async Task GetPrimaryExternalLink_OwnerOwnRoom_ReturnsLink()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreatePublicRoom("Autotest Primary Link Owner");

        // Act
        var link = (await _roomsApi.GetRoomsPrimaryExternalLinkAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        link.SharedLink.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetPrimaryExternalLink_DocSpaceAdminForeignRoom_ReturnsLink()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreatePublicRoom("Autotest Primary Link Admin");

        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);

        // Act
        var link = (await _roomsApi.GetRoomsPrimaryExternalLinkAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        link.SharedLink.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetPrimaryExternalLink_RoomAdminNotInvited_Forbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreatePublicRoom("Autotest Primary Link RoomAdmin NotInvited");

        var roomAdmin = await InviteMember(EmployeeType.RoomAdmin);
        await _filesClient.Authenticate(roomAdmin);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetRoomsPrimaryExternalLinkAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    /// <summary>RoomManager / ContentCreator grant link management; Editing does not.</summary>
    [Theory]
    [MemberData(nameof(RoomAccessData.PrimaryLinkAccesses), MemberType = typeof(RoomAccessData))]
    public async Task GetPrimaryExternalLink_InvitedRoomAdmin_MatchesExpectation(FileShare access, int expectedStatus)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreatePublicRoom($"Autotest Primary Link RoomAdmin {access}");

        var roomAdmin = await InviteMember(EmployeeType.RoomAdmin);
        await InviteToRoom(room.Id, roomAdmin, access);

        await _filesClient.Authenticate(roomAdmin);

        // Act & Assert
        if (expectedStatus == 200)
        {
            var link = (await _roomsApi.GetRoomsPrimaryExternalLinkAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
            link.SharedLink.Id.Should().NotBeEmpty();
        }
        else
        {
            var exception = await Assert.ThrowsAsync<ApiException>(
                async () => await _roomsApi.GetRoomsPrimaryExternalLinkAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken));

            exception.ErrorCode.Should().Be(expectedStatus);
        }
    }

    [Fact]
    public async Task GetPrimaryExternalLink_UserInvitedWithRead_Forbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreatePublicRoom("Autotest Primary Link User Invited");

        var user = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, user, FileShare.Read);

        await _filesClient.Authenticate(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetRoomsPrimaryExternalLinkAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Theory]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task GetPrimaryExternalLink_NotInvitedMember_Forbidden(EmployeeType employeeType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreatePublicRoom($"Autotest Primary Link {employeeType} NotInvited");

        var member = await InviteMember(employeeType);
        await _filesClient.Authenticate(member);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetRoomsPrimaryExternalLinkAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetPrimaryExternalLink_Anonymous_Unauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreatePublicRoom("Autotest Primary Link Anonymous");

        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetRoomsPrimaryExternalLinkAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    #endregion

    #region PUT /files/rooms/{id}/links - access control

    // Creating, updating or deleting a room link is a write action and is scoped more tightly than
    // the read endpoints: only the room owner and members invited with RoomManager access may run it.
    // Unlike GetRoomsPrimaryExternalLink, a portal admin (DocSpaceAdmin) does NOT get access to
    // another owner's room (403), and ContentCreator is not enough either. Lower access levels
    // (Editing / Read), non-members and guests get 403; anonymous gets 401; terminated users get 401.

    /// <remarks>
    /// BUG 81840: with external sharing restricted and existing links still allowed, creating a NEW
    /// external link must be rejected.
    /// </remarks>
    [Fact]
    [Trait("Bug", "81840")]
    public async Task SetRoomLink_ExternalSharingRestricted_Forbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreatePublicRoom("Autotest External Link Restriction Room");

        // Enable the external sharing restriction, keeping the existing links allowed
        await _filesSettingsApi.ChangeExternalSharingSettingsAsync(
            new ExternalSharingSettingsRequestDto(
                externalShare: false,
                defaultShareLinkInternal: false,
                externalShareApplyToDocuments: false,
                externalShareApplyToRooms: true,
                blockExistingLinksOnRestrict: false),
            TestContext.Current.CancellationToken);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.SetRoomLinkAsync(room.Id, BuildExternalLink(), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Theory]
    [InlineData(null)]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    public async Task SetRoomLink_OwnRoom_LinkCreated(EmployeeType? employeeType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        if (employeeType.HasValue)
        {
            var member = await InviteMember(employeeType.Value);
            await _filesClient.Authenticate(member);
        }

        var room = await CreatePublicRoom($"Autotest setLink Perm {employeeType?.ToString() ?? "Owner"}");

        // Act
        var link = (await _roomsApi.SetRoomLinkAsync(room.Id, BuildExternalLink(), TestContext.Current.CancellationToken)).Response;

        // Assert
        link.SharedLink.Id.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task SetRoomLink_ForeignRoomWithoutInvitation_Forbidden(EmployeeType employeeType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreatePublicRoom($"Autotest setLink Perm {employeeType} NotInvited");

        var member = await InviteMember(employeeType);
        await _filesClient.Authenticate(member);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.SetRoomLinkAsync(room.Id, BuildExternalLink(), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    /// <summary>Only RoomManager access grants link management; ContentCreator and Editing do not.</summary>
    [Theory]
    [MemberData(nameof(RoomAccessData.SetRoomLinkAccesses), MemberType = typeof(RoomAccessData))]
    public async Task SetRoomLink_InvitedRoomAdmin_MatchesExpectation(FileShare access, int expectedStatus)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreatePublicRoom($"Autotest setLink Perm RoomAdmin {access}");

        var roomAdmin = await InviteMember(EmployeeType.RoomAdmin);
        await InviteToRoom(room.Id, roomAdmin, access);

        await _filesClient.Authenticate(roomAdmin);

        // Act & Assert
        if (expectedStatus == 200)
        {
            var link = (await _roomsApi.SetRoomLinkAsync(room.Id, BuildExternalLink(), TestContext.Current.CancellationToken)).Response;
            link.SharedLink.Id.Should().NotBeEmpty();
        }
        else
        {
            var exception = await Assert.ThrowsAsync<ApiException>(
                async () => await _roomsApi.SetRoomLinkAsync(room.Id, BuildExternalLink(), TestContext.Current.CancellationToken));

            exception.ErrorCode.Should().Be(expectedStatus);
        }
    }

    [Fact]
    public async Task SetRoomLink_UserInvitedWithRead_Forbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreatePublicRoom("Autotest setLink Perm User Invited");

        var user = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, user, FileShare.Read);

        await _filesClient.Authenticate(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.SetRoomLinkAsync(room.Id, BuildExternalLink(), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task SetRoomLink_Anonymous_Unauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreatePublicRoom("Autotest setLink Perm Anonymous");

        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.SetRoomLinkAsync(room.Id, BuildExternalLink(), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task SetRoomLink_TerminatedUser_Unauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreatePublicRoom("Autotest setLink Perm Terminated");

        var user = await InviteMember(EmployeeType.User);
        await _filesClient.Authenticate(user);

        await TerminateUser(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.SetRoomLinkAsync(room.Id, BuildExternalLink(), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    #endregion
}

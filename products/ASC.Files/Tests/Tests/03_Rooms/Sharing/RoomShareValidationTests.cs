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

namespace ASC.Files.Tests.Tests._03_Rooms.Sharing;

/// <summary>
/// Input validation of <c>PUT /files/rooms/{id}/share</c>: malformed invitation payloads, no-op
/// bodies and the room id/state itself.
/// </summary>
/// <remarks>
/// The TS suite has a case here for <c>id: 0 as unknown as string</c> expecting 400. That is a
/// JSON-type mismatch (a number where the schema expects a GUID string), almost certainly caught
/// by model binding before the controller runs - and it is unreachable through the typed .NET SDK,
/// since <see cref="RoomInvitation.Id"/> is a compile-time <see cref="Guid"/>. <see cref="Guid.Empty"/>
/// looked like the nearest substitute, but it is not equivalent: it is a syntactically valid subject
/// id that the product currently accepts (traced through
/// <c>FileSharing.SetAceObjectAsync</c>: a lookup miss resolves to <c>Constants.LostUser</c>, which is
/// treated as <see cref="EmployeeType.Guest"/> and passes the access-level check, so the ace is written
/// for the literal empty subject rather than being rejected). Whether an empty/non-existent subject id
/// should be rejected isn't specified anywhere - asserting either the current 200 or a guessed 400 would
/// mean inventing a contract, so the case is dropped rather than ported.
/// </remarks>
[Trait("Category", "Rooms")]
public class RoomShareValidationTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    [Fact]
    public async Task SetRoomSecurity_UndefinedAccessValue_IsRejectedWith403()
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);
        var room = await CreateCustomRoom("Autotest Share BadAccess");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.SetRoomSecurityAsync(
                room.Id,
                new RoomInvitationRequest { Invitations = [new RoomInvitation { Id = user.Id, Access = (FileShare)999 }], Notify = false },
                TestContext.Current.CancellationToken));

        // Assert: side-effect first - nothing was added
        var info = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        info.Should().NotContain(s => s.SharedToUser != null && s.SharedToUser.Id == user.Id);

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task SetRoomSecurity_EmptyInvitationsArray_Returns200NoOp()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Share NoOp Empty Array");

        // Act
        await _roomsApi.SetRoomSecurityAsync(
            room.Id,
            new RoomInvitationRequest { Invitations = [], Notify = false },
            TestContext.Current.CancellationToken);

        // Assert
        var info = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        info.Should().HaveCount(1);
        info[0].IsOwner.Should().BeTrue();
    }

    /// <summary>
    /// The TS suite has two separate no-op cases here ("invitations: null" and "{}"), but
    /// <see cref="RoomInvitationRequest.Invitations"/> serialises with <c>EmitDefaultValue = true</c>,
    /// so a default-constructed request and one with <c>Invitations</c> explicitly set to null
    /// produce the exact same JSON body. There is only one request to test through the typed SDK.
    /// </summary>
    [Fact]
    public async Task SetRoomSecurity_EmptyBody_Returns200NoOp()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Share NoOp Empty Body");

        // Act
        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest(), TestContext.Current.CancellationToken);

        // Assert
        var info = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        info.Should().HaveCount(1);
        info[0].IsOwner.Should().BeTrue();
    }

    [Fact]
    public async Task SetRoomSecurity_InvitationWithoutAccess_IsIgnored()
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);
        var room = await CreateCustomRoom("Autotest Share NoAccess");

        // Act
        await _roomsApi.SetRoomSecurityAsync(
            room.Id,
            new RoomInvitationRequest { Invitations = [new RoomInvitation { Id = user.Id }], Notify = false },
            TestContext.Current.CancellationToken);

        // Assert: the user without an access level is not added
        var info = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        info.Should().NotContain(s => s.SharedToUser != null && s.SharedToUser.Id == user.Id);
    }

    public static TheoryData<int> NonExistingRoomIds => [0, -1, 999999999];

    [Theory]
    [MemberData(nameof(NonExistingRoomIds))]
    public async Task SetRoomSecurity_NonExistingRoomId_Returns404(int roomId)
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.SetRoomSecurityAsync(
                roomId,
                BuildInvitation(user, FileShare.Read),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task SetRoomSecurity_DeletedRoom_Returns404()
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);
        var room = await CreateCustomRoom("Autotest Share Deleted");
        await _roomsApi.DeleteRoomAsync(room.Id, new DeleteRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.SetRoomSecurityAsync(
                room.Id,
                BuildInvitation(user, FileShare.Read),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task SetRoomSecurity_ArchivedRoom_Returns403()
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);
        var room = await CreateCustomRoom("Autotest Share Archived");
        await ArchiveRoom(room.Id);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.SetRoomSecurityAsync(
                room.Id,
                BuildInvitation(user, FileShare.Read),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }
}

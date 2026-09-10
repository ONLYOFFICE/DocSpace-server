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
/// Which <see cref="FileShare"/> levels <c>PUT /files/rooms/{id}/share</c> accepts for a plain
/// <c>User</c> subject, per room type. Mirrors <c>FileSecurity.AvailableRoomAccesses</c> for
/// <see cref="SubjectType.User"/> - see the table in <c>.claude/rules/tests.md</c>. Not covered
/// there: <see cref="FileShare.RoomManager"/> is technically listed for
/// <see cref="RoomType.FillingFormsRoom"/> and <see cref="RoomType.PublicRoom"/> too, but a plain
/// <c>User</c> can never receive it - only a RoomAdmin can, which is why the FillingFormsRoom
/// case below expects it to be rejected all the same.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomShareAccessLevelTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    /// <summary>Room type, the access level offered to a User, and whether it must be accepted.</summary>
    public static TheoryData<RoomType, FileShare, bool> RoomTypeAccessLevels => new()
    {
        // FillingFormsRoom: User subject accepts only ContentCreator/FillForms/None.
        // RoomManager is rejected here for a different reason than the room type - it can only
        // ever be granted to a RoomAdmin - but the observable outcome is the same 403.
        { RoomType.FillingFormsRoom, FileShare.FillForms, true },
        { RoomType.FillingFormsRoom, FileShare.Editing, false },
        { RoomType.FillingFormsRoom, FileShare.Read, false },
        { RoomType.FillingFormsRoom, FileShare.RoomManager, false },

        // EditingRoom (Collaboration room)
        { RoomType.EditingRoom, FileShare.Read, true },
        { RoomType.EditingRoom, FileShare.Editing, true },
        { RoomType.EditingRoom, FileShare.ContentCreator, true },
        { RoomType.EditingRoom, FileShare.FillForms, false },
        { RoomType.EditingRoom, FileShare.Review, false },
        { RoomType.EditingRoom, FileShare.Comment, false },

        // PublicRoom
        { RoomType.PublicRoom, FileShare.ContentCreator, true },
        { RoomType.PublicRoom, FileShare.Read, false },
        { RoomType.PublicRoom, FileShare.Editing, false },

        // VirtualDataRoom
        { RoomType.VirtualDataRoom, FileShare.Read, true },
        { RoomType.VirtualDataRoom, FileShare.Editing, true },
        { RoomType.VirtualDataRoom, FileShare.FillForms, true },
        { RoomType.VirtualDataRoom, FileShare.ContentCreator, true },
        { RoomType.VirtualDataRoom, FileShare.Review, false },
        { RoomType.VirtualDataRoom, FileShare.Comment, false }
    };

    [Theory]
    [MemberData(nameof(RoomTypeAccessLevels))]
    public async Task SetRoomSecurity_UserSubject_RoomTypeAccessLevel(RoomType roomType, FileShare access, bool accepted)
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);
        var room = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto($"Autotest Share Type {roomType} {access}", roomType: roomType),
            TestContext.Current.CancellationToken)).Response;

        if (accepted)
        {
            // Act
            await _roomsApi.SetRoomSecurityAsync(room.Id, BuildInvitation(user, access), TestContext.Current.CancellationToken);

            // Assert
            var info = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
            var entry = info.Find(s => s.SharedToUser?.Id == user.Id);
            entry.Should().NotBeNull();
            entry!.Access.Should().Be(access);
        }
        else
        {
            // Act
            var exception = await Assert.ThrowsAsync<ApiException>(
                async () => await _roomsApi.SetRoomSecurityAsync(room.Id, BuildInvitation(user, access), TestContext.Current.CancellationToken));

            // Assert
            exception.ErrorCode.Should().Be(403);

            var info = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
            info.Should().NotContain(s => s.SharedToUser != null && s.SharedToUser.Id == user.Id);
        }
    }
}

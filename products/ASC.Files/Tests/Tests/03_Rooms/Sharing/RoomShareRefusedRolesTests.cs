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
/// The roles a room type does not offer are refused for a User subject: a Public room takes neither Reviewer,
/// Commentator nor Form Filler, a Form filling room neither Reviewer nor Commentator, a Custom room no Form Filler.
/// The cases <see cref="RoomShareAccessLevelTests"/> does not cover; both mirror <c>FileSecurity.AvailableRoomAccesses</c>.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomShareRefusedRolesTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    public static TheoryData<RoomType, FileShare> RefusedRoles => new()
    {
        { RoomType.PublicRoom, FileShare.Review },
        { RoomType.PublicRoom, FileShare.Comment },
        { RoomType.PublicRoom, FileShare.FillForms },
        { RoomType.FillingFormsRoom, FileShare.Review },
        { RoomType.FillingFormsRoom, FileShare.Comment },
        { RoomType.CustomRoom, FileShare.FillForms }
    };

    [Theory]
    [MemberData(nameof(RefusedRoles))]
    public async Task SetRoomSecurity_RoleTheRoomTypeDoesNotOffer_Returns403AndMemberAbsent(RoomType roomType, FileShare access)
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);
        var room = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto($"Autotest Refused Role {roomType} {access}", roomType: roomType),
            TestContext.Current.CancellationToken)).Response;

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.SetRoomSecurityAsync(room.Id, BuildInvitation(user, access), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent!.ToString().Should().Contain("The role is not available for this user type");

        var info = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        info.Should().NotContain(s => s.SharedToUser != null && s.SharedToUser.Id == user.Id);
    }
}

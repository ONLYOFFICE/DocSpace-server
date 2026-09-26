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

namespace ASC.People.Tests.Tests._06_Photos;

/// <summary>
/// <c>GET /people/{userid}/photo</c> — the restrictions on reading someone else's photo: a
/// RoomAdmin cannot view a Guest's, and a User or a Guest cannot view anyone else's. The
/// permissions that ARE granted (Owner/DocSpaceAdmin/RoomAdmin viewing most other roles) are
/// covered as positive cases in <see cref="PhotoGetTests"/>.
/// </summary>
[Trait("Category", "Photos")]
public class PhotoGetPermissionsTests(
    AspireAppFixture fixture)
    : PhotosTestBase(fixture)
{
    [Fact]
    public async Task GetMemberPhoto_RoomAdminGetsGuestPhoto_ThrowsAccessDenied()
    {
        // Arrange
        var guest = await ResolveUserAsync(Role.Guest);
        var roomAdmin = await ResolveUserAsync(Role.RoomAdmin);

        await _peopleClient.Authenticate(roomAdmin);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _photosApi.GetMemberPhotoAsync(guest.Id.ToString(), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    public static TheoryData<Role, Role> RestrictedViewerTargetPairs()
    {
        var data = new TheoryData<Role, Role>();

        foreach (var target in OthersOf(Role.User))
        {
            data.Add(Role.User, target);
        }

        foreach (var target in OthersOf(Role.Guest))
        {
            data.Add(Role.Guest, target);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(RestrictedViewerTargetPairs))]
    public async Task GetMemberPhoto_UserOrGuestGetsOtherUsersPhoto_ThrowsAccessDenied(Role viewer, Role target)
    {
        // Arrange
        var viewerUser = await ResolveUserAsync(viewer);
        var targetUser = await ResolveUserAsync(target);

        await _peopleClient.Authenticate(viewerUser);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _photosApi.GetMemberPhotoAsync(targetUser.Id.ToString(), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }
}

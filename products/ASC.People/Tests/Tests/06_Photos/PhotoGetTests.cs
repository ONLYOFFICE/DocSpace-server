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
/// <c>GET /people/{userid}/photo</c> — reading a member's own photo, and the roles that are
/// allowed to read someone else's (Owner, DocSpaceAdmin and RoomAdmin all can; the restrictions on
/// User, Guest and RoomAdmin-vs-Guest live in <see cref="PhotoGetPermissionsTests"/>).
/// Unauthenticated/not-found cases live in <see cref="PhotoEdgeCaseTests"/>.
/// </summary>
[Trait("Category", "Photos")]
public class PhotoGetTests(
    AspireAppFixture fixture)
    : PhotosTestBase(fixture)
{
    [Theory]
    [MemberData(nameof(AllRolesData))]
    public async Task GetMemberPhoto_OwnPhoto_Succeeds(Role role)
    {
        // Arrange
        var member = await ResolveUserAsync(role);

        // Act
        await _peopleClient.Authenticate(member);
        var result = await _photosApi.GetMemberPhotoAsync(member.Id.ToString(), TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
    }

    public static TheoryData<Role, Role> AllowedViewerTargetPairs()
    {
        // Owner and DocSpaceAdmin can view anyone; RoomAdmin can view anyone except Guest
        // (that restriction is covered separately in PhotoGetPermissionsTests).
        var data = new TheoryData<Role, Role>();

        foreach (var target in OthersOf(Role.Owner))
        {
            data.Add(Role.Owner, target);
        }

        foreach (var target in OthersOf(Role.DocSpaceAdmin))
        {
            data.Add(Role.DocSpaceAdmin, target);
        }

        foreach (var target in OthersOf(Role.RoomAdmin).Where(target => target != Role.Guest))
        {
            data.Add(Role.RoomAdmin, target);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(AllowedViewerTargetPairs))]
    public async Task GetMemberPhoto_OtherUsersPhoto_Succeeds(Role viewer, Role target)
    {
        // Arrange
        var viewerUser = await ResolveUserAsync(viewer);
        var targetUser = await ResolveUserAsync(target);

        // Act
        await _peopleClient.Authenticate(viewerUser);
        var result = await _photosApi.GetMemberPhotoAsync(targetUser.Id.ToString(), TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
    }
}

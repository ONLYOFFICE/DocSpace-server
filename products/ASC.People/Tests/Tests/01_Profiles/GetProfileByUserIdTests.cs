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

namespace ASC.People.Tests.Tests._01_Profiles;

/// <summary>
/// <c>GET /people/:userId</c> — reading another member's detailed profile by ID.
/// </summary>
public class GetProfileByUserIdTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    public static readonly TheoryData<EmployeeType?> ViewerRoles = new() { (EmployeeType?)null, EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin };

    [Theory]
    [MemberData(nameof(ViewerRoles))]
    public async Task GetProfileByUserId_ByOwnerOrAdmin_ShouldReturnTheTargetProfile(EmployeeType? viewerType)
    {
        var target = await InviteContact(EmployeeType.RoomAdmin);

        await _peopleClient.Authenticate(viewerType is null ? Owner : await InviteContact(viewerType.Value));

        var profile = await _profilesApi.GetProfileByUserIdAsync(target.Id.ToString(), TestContext.Current.CancellationToken);

        profile.Response.Id.Should().Be(target.Id);
        profile.Response.Email.Should().Be(target.Email);
    }

    [Fact]
    public async Task GetProfileByUserId_ByUser_ShouldThrowAccessDenied()
    {
        var user = await InviteContact(EmployeeType.User);
        await _peopleClient.Authenticate(user);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _profilesApi.GetProfileByUserIdAsync(Owner.Id.ToString(), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task GetProfileByUserId_ByGuest_ShouldThrowAccessDenied()
    {
        var guest = await InviteGuest();
        await _peopleClient.Authenticate(guest);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _profilesApi.GetProfileByUserIdAsync(Owner.Id.ToString(), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task GetProfileByUserId_NonExistentUser_ShouldThrowNotFound()
    {
        var target = await InviteContact(EmployeeType.RoomAdmin);
        await TerminateUser(target);
        await _profilesApi.RemoveUsersAsync(new UpdateMembersRequestDto([target.Id]), TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _profilesApi.GetProfileByUserIdAsync(target.Id.ToString(), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
        exception.ErrorContent?.ToString().Should().Contain("The user could not be found");
    }

    [Fact]
    public async Task GetProfileByUserId_Anonymous_ShouldThrowUnauthorized()
    {
        var target = await InviteContact(EmployeeType.RoomAdmin);
        await _peopleClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _profilesApi.GetProfileByUserIdAsync(target.Id.ToString(), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }
}

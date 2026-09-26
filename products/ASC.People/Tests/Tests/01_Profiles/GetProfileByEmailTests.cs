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
/// <c>GET /people/email?email=</c> — looking a profile up by email address, both one's own and
/// (for Owner/DocSpace admin/Room admin) someone else's.
/// </summary>
public class GetProfileByEmailTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    public static readonly TheoryData<EmployeeType?> SelfLookupRoles = new()
    {
        (EmployeeType?)null, EmployeeType.DocSpaceAdmin, EmployeeType.User, EmployeeType.Guest
    };

    [Theory]
    [MemberData(nameof(SelfLookupRoles))]
    public async Task GetProfileByEmail_LookingUpOwnEmail_ShouldReturnOwnProfile(EmployeeType? actorType)
    {
        var actor = actorType switch
        {
            null => Owner,
            EmployeeType.Guest => await InviteGuest(),
            _ => await InviteContact(actorType.Value)
        };
        await _peopleClient.Authenticate(actor);

        var self = await _profilesApi.GetSelfProfileAsync(TestContext.Current.CancellationToken);

        var byEmail = await _profilesApi.GetProfileByEmailAsync(self.Response.Email, cancellationToken: TestContext.Current.CancellationToken);

        byEmail.Response.Id.Should().Be(self.Response.Id);
        byEmail.Response.FirstName.Should().Be(self.Response.FirstName);
        byEmail.Response.LastName.Should().Be(self.Response.LastName);
        byEmail.Response.Email.Should().Be(self.Response.Email);
    }

    public static readonly TheoryData<EmployeeType?, EmployeeType> LookupAnotherCases = new()
    {
        { null, EmployeeType.DocSpaceAdmin },
        { EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin },
        { EmployeeType.RoomAdmin, EmployeeType.User },
    };

    [Theory]
    [MemberData(nameof(LookupAnotherCases))]
    public async Task GetProfileByEmail_LookingUpAnotherMembersEmail_ShouldSucceedForOwnerOrAdmin(EmployeeType? viewerType, EmployeeType targetType)
    {
        var target = await InviteContact(targetType);

        await _peopleClient.Authenticate(viewerType is null ? Owner : await InviteContact(viewerType.Value));

        var byEmail = await _profilesApi.GetProfileByEmailAsync(target.Email, cancellationToken: TestContext.Current.CancellationToken);

        byEmail.Response.Id.Should().Be(target.Id);
        byEmail.Response.Email.Should().Be(target.Email);
    }

    [Fact]
    public async Task GetProfileByEmail_Anonymous_ShouldThrowUnauthorized()
    {
        await _peopleClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _profilesApi.GetProfileByEmailAsync(Owner.Email, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task GetProfileByEmail_NonExistentUser_ShouldThrowNotFound()
    {
        var target = await InviteContact(EmployeeType.RoomAdmin);
        await TerminateUser(target);
        await _profilesApi.RemoveUsersAsync(new UpdateMembersRequestDto([target.Id]), TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _profilesApi.GetProfileByEmailAsync(target.Email, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
        exception.ErrorContent?.ToString().Should().Contain("The user could not be found");
    }

    [Fact]
    public async Task GetProfileByEmail_ByUserLookingUpAnother_ShouldThrowAccessDenied()
    {
        var target = await InviteContact(EmployeeType.RoomAdmin);
        var user = await InviteContact(EmployeeType.User);
        await _peopleClient.Authenticate(user);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _profilesApi.GetProfileByEmailAsync(target.Email, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task GetProfileByEmail_ByGuestLookingUpAnother_ShouldThrowAccessDenied()
    {
        var target = await InviteContact(EmployeeType.RoomAdmin);
        var guest = await InviteGuest();
        await _peopleClient.Authenticate(guest);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _profilesApi.GetProfileByEmailAsync(target.Email, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }
}

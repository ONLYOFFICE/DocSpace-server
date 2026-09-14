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
/// <c>PUT /people/:userId/culture</c> — every role may change its own display culture; per
/// BUG 65478 no role (including the Owner) is currently allowed to change someone else's, which
/// this suite documents as a single permission boundary rather than "owner-only".
/// </summary>
public class UpdateMemberCultureTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    public static readonly TheoryData<EmployeeType?> SelfUpdaters = new()
    {
        (EmployeeType?)null, EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User, EmployeeType.Guest
    };

    [Theory]
    [MemberData(nameof(SelfUpdaters))]
    public async Task UpdateMemberCulture_UpdatingOwnCulture_ShouldPersist(EmployeeType? actorType)
    {
        var actor = actorType switch
        {
            null => Owner,
            EmployeeType.Guest => await InviteGuest(),
            _ => await InviteContact(actorType.Value)
        };
        await _peopleClient.Authenticate(actor);

        var updated = await _profilesApi.UpdateMemberCultureAsync(
            actor.Id.ToString(),
            new Culture("es"),
            TestContext.Current.CancellationToken);

        updated.Response.Id.Should().Be(actor.Id);
        updated.Response.Email.Should().Be(actor.Email);
        updated.Response.CultureName.Should().Be("es");
    }

    /// <summary>
    /// One actor (<paramref name="actorType"/>, null = Owner) attempting to change the culture of
    /// four different targets — Owner, DocSpace admin, Room admin, User — every one refused.
    /// </summary>
    private async Task AssertActorCannotChangeAnyonesElseCultureAsync(EmployeeType? actorType)
    {
        var owner = Owner;
        var docSpaceAdmin = await InviteContact(EmployeeType.DocSpaceAdmin);
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        var user = await InviteContact(EmployeeType.User);

        var actor = actorType is null ? owner : await InviteContact(actorType.Value);
        await _peopleClient.Authenticate(actor);

        foreach (var target in new[] { owner, docSpaceAdmin, roomAdmin, user })
        {
            if (target.Id == actor.Id)
            {
                continue;
            }

            var exception = await Assert.ThrowsAsync<ApiException>(async () =>
                await _profilesApi.UpdateMemberCultureAsync(
                    target.Id.ToString(),
                    new Culture("es"),
                    TestContext.Current.CancellationToken));

            exception.ErrorCode.Should().Be(403);
            exception.ErrorContent?.ToString().Should().Contain("Access denied");
        }
    }

    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "65478")]
    public Task UpdateMemberCulture_OwnerChangingAnotherMembersCulture_ShouldThrowAccessDenied()
        => AssertActorCannotChangeAnyonesElseCultureAsync(null);

    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "65478")]
    public Task UpdateMemberCulture_DocSpaceAdminChangingAnotherMembersCulture_ShouldThrowAccessDenied()
        => AssertActorCannotChangeAnyonesElseCultureAsync(EmployeeType.DocSpaceAdmin);

    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "65478")]
    public Task UpdateMemberCulture_RoomAdminChangingAnotherMembersCulture_ShouldThrowAccessDenied()
        => AssertActorCannotChangeAnyonesElseCultureAsync(EmployeeType.RoomAdmin);

    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "65478")]
    public Task UpdateMemberCulture_UserChangingAnotherMembersCulture_ShouldThrowAccessDenied()
        => AssertActorCannotChangeAnyonesElseCultureAsync(EmployeeType.User);

    [Fact]
    public async Task UpdateMemberCulture_GuestChangingAnotherMembersCulture_ShouldThrowAccessDenied()
    {
        var owner = Owner;
        var docSpaceAdmin = await InviteContact(EmployeeType.DocSpaceAdmin);
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        var user = await InviteContact(EmployeeType.User);

        var guest = await InviteGuest();
        await _peopleClient.Authenticate(guest);

        foreach (var target in new[] { owner, docSpaceAdmin, roomAdmin, user })
        {
            var exception = await Assert.ThrowsAsync<ApiException>(async () =>
                await _profilesApi.UpdateMemberCultureAsync(
                    target.Id.ToString(),
                    new Culture("es"),
                    TestContext.Current.CancellationToken));

            exception.ErrorCode.Should().Be(403);
            exception.ErrorContent?.ToString().Should().Contain("Access denied");
        }
    }

    [Fact]
    public async Task UpdateMemberCulture_Anonymous_ShouldThrowUnauthorized()
    {
        await _peopleClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _profilesApi.UpdateMemberCultureAsync(
                Owner.Id.ToString(),
                new Culture("es"),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task UpdateMemberCulture_NonExistentUser_ShouldThrowNotFound()
    {
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _profilesApi.UpdateMemberCultureAsync(
                Guid.NewGuid().ToString(),
                new Culture("es"),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
        exception.ErrorContent?.ToString().Should().Contain("The user could not be found");
    }

    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79918")]
    public async Task UpdateMemberCulture_LongCultureName_ShouldReturnValidationError()
    {
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _profilesApi.UpdateMemberCultureAsync(
                Owner.Id.ToString(),
                new Culture(new string('a', 260)),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent?.ToString().Should().Contain("CultureName must be a string with a maximum length of 85");
    }
}

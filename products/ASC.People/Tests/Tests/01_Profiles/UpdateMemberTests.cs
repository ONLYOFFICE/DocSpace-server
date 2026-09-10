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
/// <c>PUT /people/:userId</c> — updating a member's own basic profile fields (name), the
/// permission boundary around who may edit whom, and request-level validation.
/// </summary>
public class UpdateMemberTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    public static readonly TheoryData<EmployeeType?> SelfUpdaters = new()
    {
        (EmployeeType?)null, EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.Guest
    };

    [Theory]
    [MemberData(nameof(SelfUpdaters))]
    public async Task UpdateMember_UpdatingOwnProfile_ShouldPersistNameChange(EmployeeType? actorType)
    {
        var actor = actorType switch
        {
            null => Owner,
            EmployeeType.Guest => await InviteGuest(),
            _ => await InviteContact(actorType.Value)
        };
        await _peopleClient.Authenticate(actor);

        // Letters only on purpose: DisplayName comes back HTML-encoded while FirstName and LastName
        // do not, so a faker surname like "O'Hara" reads as "O&#39;Hara" in DisplayName alone. That
        // inconsistency is the server's, not the test's, and is not what this case is about.
        var firstName = Initializer.Faker.Random.String2(8, "abcdefghijklmnopqrstuvwxyz");
        var lastName = Initializer.Faker.Random.String2(8, "abcdefghijklmnopqrstuvwxyz");

        var updated = await _profilesApi.UpdateMemberAsync(
            actor.Id.ToString(),
            new UpdateMemberRequestDto { FirstName = firstName, LastName = lastName },
            TestContext.Current.CancellationToken);

        updated.Response.Id.Should().Be(actor.Id);
        updated.Response.FirstName.Should().Be(firstName);
        updated.Response.LastName.Should().Be(lastName);
        updated.Response.DisplayName.Should().Be($"{firstName} {lastName}");
    }

    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79994")]
    public async Task UpdateMember_UserUpdatingOwnProfile_ShouldPersistNameChange()
    {
        var user = await InviteContact(EmployeeType.User);
        await _peopleClient.Authenticate(user);

        // Letters only on purpose: DisplayName comes back HTML-encoded while FirstName and LastName
        // do not, so a faker surname like "O'Hara" reads as "O&#39;Hara" in DisplayName alone. That
        // inconsistency is the server's, not the test's, and is not what this case is about.
        var firstName = Initializer.Faker.Random.String2(8, "abcdefghijklmnopqrstuvwxyz");
        var lastName = Initializer.Faker.Random.String2(8, "abcdefghijklmnopqrstuvwxyz");

        var updated = await _profilesApi.UpdateMemberAsync(
            user.Id.ToString(),
            new UpdateMemberRequestDto { FirstName = firstName, LastName = lastName },
            TestContext.Current.CancellationToken);

        updated.Response.Id.Should().Be(user.Id);
        updated.Response.FirstName.Should().Be(firstName);
        updated.Response.LastName.Should().Be(lastName);
        updated.Response.IsCollaborator.Should().BeTrue();
    }

    public static readonly TheoryData<EmployeeType> ForbiddenOwnerUpdaters = new()
    {
        EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User, EmployeeType.Guest
    };

    [Theory]
    [MemberData(nameof(ForbiddenOwnerUpdaters))]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79724")]
    public async Task UpdateMember_UpdatingOwner_ShouldThrowAccessDeniedForEveryoneElse(EmployeeType actorType)
    {
        var actor = actorType == EmployeeType.Guest ? await InviteGuest() : await InviteContact(actorType);
        await _peopleClient.Authenticate(actor);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _profilesApi.UpdateMemberAsync(
                Owner.Id.ToString(),
                new UpdateMemberRequestDto { FirstName = Initializer.Faker.Person.FirstName, LastName = Initializer.Faker.Person.LastName },
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task UpdateMember_IncorrectName_ShouldReturnValidationError()
    {
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _profilesApi.UpdateMemberAsync(
                Owner.Id.ToString(),
                new UpdateMemberRequestDto { FirstName = "12345", LastName = Initializer.Faker.Person.LastName },
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent?.ToString().Should().Contain("Incorrect firstname or lastname");
    }

    [Fact]
    public async Task UpdateMember_UserWithLongNames_ShouldReturnValidationError()
    {
        var user = await InviteContact(EmployeeType.User);
        await _peopleClient.Authenticate(user);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _profilesApi.UpdateMemberAsync(
                user.Id.ToString(),
                new UpdateMemberRequestDto { FirstName = new string('a', 260), LastName = new string('a', 260) },
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent?.ToString().Should().Contain("FirstName must be a string with a maximum length of 255");
        exception.ErrorContent?.ToString().Should().Contain("LastName must be a string with a maximum length of 255");
    }

    [Fact]
    public async Task UpdateMember_GuestWithLongNames_ShouldReturnValidationError()
    {
        var guest = await InviteGuest();
        await _peopleClient.Authenticate(guest);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _profilesApi.UpdateMemberAsync(
                guest.Id.ToString(),
                new UpdateMemberRequestDto { FirstName = new string('a', 260), LastName = new string('a', 260) },
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent?.ToString().Should().Contain("FirstName must be a string with a maximum length of 255");
        exception.ErrorContent?.ToString().Should().Contain("LastName must be a string with a maximum length of 255");
    }

    [Fact]
    public async Task UpdateMember_Anonymous_ShouldThrowUnauthorized()
    {
        await _peopleClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _profilesApi.UpdateMemberAsync(
                Owner.Id.ToString(),
                new UpdateMemberRequestDto { FirstName = Initializer.Faker.Person.FirstName, LastName = Initializer.Faker.Person.LastName },
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task UpdateMember_NonExistentUser_ShouldThrowNotFound()
    {
        var target = await InviteContact(EmployeeType.RoomAdmin);
        await TerminateUser(target);
        await _profilesApi.RemoveUsersAsync(new UpdateMembersRequestDto([target.Id]), TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _profilesApi.UpdateMemberAsync(
                target.Id.ToString(),
                new UpdateMemberRequestDto { FirstName = Initializer.Faker.Person.FirstName, LastName = Initializer.Faker.Person.LastName },
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
        exception.ErrorContent?.ToString().Should().Contain("The user could not be found");
    }
}

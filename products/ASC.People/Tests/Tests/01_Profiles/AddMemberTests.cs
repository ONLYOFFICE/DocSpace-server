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
/// <c>POST /people</c> — creating a portal member directly (as opposed to inviting one by email
/// through <see cref="InviteUsersTests"/>). Covers who may create whom, the resulting employee
/// flags, and the request-level validation and SSRF protection on the optional avatar URL.
/// </summary>
public class AddMemberTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    public static readonly TheoryData<EmployeeType?, EmployeeType, bool, bool, bool, bool, bool> SuccessCases = new()
    {
        // actor (null = Owner), target, isCollaborator, isOwner, isVisitor, isAdmin, isRoomAdmin
        { null, EmployeeType.RoomAdmin, false, false, false, false, true },
        { null, EmployeeType.DocSpaceAdmin, false, false, false, true, false },
        { EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, false, false, false, false, true },
        { EmployeeType.DocSpaceAdmin, EmployeeType.User, true, false, false, false, false },
        { EmployeeType.RoomAdmin, EmployeeType.User, true, false, false, false, false },
    };

    [Theory]
    [MemberData(nameof(SuccessCases))]
    public async Task AddMember_ByAllowedActor_ShouldCreateMemberWithExpectedFlags(
        EmployeeType? actorType, EmployeeType targetType,
        bool expectedIsCollaborator, bool expectedIsOwner, bool expectedIsVisitor, bool expectedIsAdmin, bool expectedIsRoomAdmin)
    {
        if (actorType is not null)
        {
            var actor = await InviteContact(actorType.Value);
            await _peopleClient.Authenticate(actor);
        }

        var fakeMember = Initializer.FakerMember.Generate();

        var created = await _profilesApi.AddMemberAsync(new MemberRequestDto
        {
            CultureName = "en-US",
            Spam = false,
            Type = targetType,
            Email = fakeMember.Email,
            PasswordHash = Initializer.GetClientPassword(fakeMember.Password),
            FirstName = fakeMember.FirstName,
            LastName = fakeMember.LastName
        }, TestContext.Current.CancellationToken);

        created.Response.IsCollaborator.Should().Be(expectedIsCollaborator);
        created.Response.IsOwner.Should().Be(expectedIsOwner);
        created.Response.IsVisitor.Should().Be(expectedIsVisitor);
        created.Response.IsAdmin.Should().Be(expectedIsAdmin);
        created.Response.IsRoomAdmin.Should().Be(expectedIsRoomAdmin);
        created.Response.IsLDAP.Should().BeFalse();
        created.Response.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "80020")]
    public async Task AddMember_OwnerCreatesUser_ShouldStampCreatedByWithOwner()
    {
        var ownerProfile = await _profilesApi.GetSelfProfileAsync(TestContext.Current.CancellationToken);
        var ownerDisplayName = ownerProfile.Response.DisplayName;

        var fakeMember = Initializer.FakerMember.Generate();

        var created = await _profilesApi.AddMemberAsync(new MemberRequestDto
        {
            CultureName = "en-US",
            Spam = false,
            Type = EmployeeType.User,
            Email = fakeMember.Email,
            PasswordHash = Initializer.GetClientPassword(fakeMember.Password),
            FirstName = fakeMember.FirstName,
            LastName = fakeMember.LastName
        }, TestContext.Current.CancellationToken);

        created.Response.IsCollaborator.Should().BeTrue();
        created.Response.CreatedBy.DisplayName.Should().Be(ownerDisplayName);
    }

    [Fact]
    public async Task AddMember_LongFirstAndLastName_ShouldReturnValidationError()
    {
        var fakeMember = Initializer.FakerMember.Generate();

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _profilesApi.AddMemberAsync(new MemberRequestDto
            {
                PasswordHash = Initializer.GetClientPassword(fakeMember.Password),
                Email = fakeMember.Email,
                FirstName = new string('a', 260),
                LastName = new string('a', 260),
                Type = EmployeeType.User
            }, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent?.ToString().Should().Contain("FirstName must be a string with a maximum length of 255");
        exception.ErrorContent?.ToString().Should().Contain("LastName must be a string with a maximum length of 255");
    }

    [Fact]
    public async Task AddMember_LongEmail_ShouldReturnValidationError()
    {
        var fakeMember = Initializer.FakerMember.Generate();
        var longEmail = new string('a', 250) + "@test.com";

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _profilesApi.AddMemberAsync(new MemberRequestDto
            {
                PasswordHash = Initializer.GetClientPassword(fakeMember.Password),
                Email = longEmail,
                FirstName = fakeMember.FirstName,
                LastName = fakeMember.LastName,
                Type = EmployeeType.User
            }, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent?.ToString().Should().Contain("Email must be a string with a maximum length of 255");
    }

    [Fact]
    public async Task AddMember_ByDocSpaceAdminCreatingDocSpaceAdmin_ShouldThrowAccessDenied()
    {
        var docSpaceAdmin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _peopleClient.Authenticate(docSpaceAdmin);

        var fakeMember = Initializer.FakerMember.Generate();

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _profilesApi.AddMemberAsync(new MemberRequestDto
            {
                PasswordHash = Initializer.GetClientPassword(fakeMember.Password),
                Email = fakeMember.Email,
                FirstName = fakeMember.FirstName,
                LastName = fakeMember.LastName,
                Type = EmployeeType.DocSpaceAdmin
            }, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task AddMember_ByRoomAdminCreatingRoomAdmin_ShouldThrowAccessDenied()
    {
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        await _peopleClient.Authenticate(roomAdmin);

        var fakeMember = Initializer.FakerMember.Generate();

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _profilesApi.AddMemberAsync(new MemberRequestDto
            {
                PasswordHash = Initializer.GetClientPassword(fakeMember.Password),
                Email = fakeMember.Email,
                FirstName = fakeMember.FirstName,
                LastName = fakeMember.LastName,
                Type = EmployeeType.RoomAdmin
            }, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task AddMember_ByUserCreatingUser_ShouldThrowAccessDenied()
    {
        var user = await InviteContact(EmployeeType.User);
        await _peopleClient.Authenticate(user);

        var fakeMember = Initializer.FakerMember.Generate();

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _profilesApi.AddMemberAsync(new MemberRequestDto
            {
                PasswordHash = Initializer.GetClientPassword(fakeMember.Password),
                Email = fakeMember.Email,
                FirstName = fakeMember.FirstName,
                LastName = fakeMember.LastName,
                Type = EmployeeType.User
            }, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task AddMember_Anonymous_ShouldThrowUnauthorized()
    {
        await _peopleClient.Authenticate(null);

        var fakeMember = Initializer.FakerMember.Generate();

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _profilesApi.AddMemberAsync(new MemberRequestDto
            {
                PasswordHash = Initializer.GetClientPassword(fakeMember.Password),
                Email = fakeMember.Email,
                FirstName = fakeMember.FirstName,
                LastName = fakeMember.LastName,
                Type = EmployeeType.User
            }, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    /// <summary>
    /// CWE-918 SSRF: the <c>files</c> parameter accepts an avatar photo URL. The server must
    /// validate it against a blacklist that includes the link-local/metadata range before ever
    /// attempting to fetch it.
    /// </summary>
    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "81491")]
    public async Task AddMember_LinkLocalAvatarUrl_ShouldBeRejectedForSsrfProtection()
    {
        var fakeMember = Initializer.FakerMember.Generate();

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _profilesApi.AddMemberAsync(new MemberRequestDto
            {
                PasswordHash = Initializer.GetClientPassword(fakeMember.Password),
                Email = fakeMember.Email,
                FirstName = fakeMember.FirstName,
                LastName = fakeMember.LastName,
                Type = EmployeeType.User,
                Files = "http://169.254.169.254/"
            }, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task AddMember_ExternalAvatarUrl_ShouldSucceed()
    {
        var fakeMember = Initializer.FakerMember.Generate();

        var created = await _profilesApi.AddMemberAsync(new MemberRequestDto
        {
            PasswordHash = Initializer.GetClientPassword(fakeMember.Password),
            Email = fakeMember.Email,
            FirstName = fakeMember.FirstName,
            LastName = fakeMember.LastName,
            Type = EmployeeType.User,
            Files = "https://www.google.com/images/branding/googlelogo/2x/googlelogo_color_272x92dp.png"
        }, TestContext.Current.CancellationToken);

        created.Response.Id.Should().NotBe(Guid.Empty);
    }

    /// <remarks>
    /// <c>POST /api/2.0/people</c> refuses <see cref="EmployeeType.Guest"/> outright — guests are
    /// created through <c>POST /api/2.0/people/active</c> (<see cref="BaseTest.InviteGuest"/>).
    /// The TypeScript suite hides this behind a helper that picks the endpoint by type, so it reads
    /// as if this endpoint accepted guests; here the refusal is asserted directly.
    /// </remarks>
    [Fact]
    public async Task AddMember_GuestType_ShouldThrowAccessDenied()
    {
        var fakeMember = Initializer.FakerMember.Generate();

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _profilesApi.AddMemberAsync(new MemberRequestDto
            {
                CultureName = "en-US",
                Spam = false,
                Type = EmployeeType.Guest,
                Email = fakeMember.Email,
                PasswordHash = Initializer.GetClientPassword(fakeMember.Password),
                FirstName = fakeMember.FirstName,
                LastName = fakeMember.LastName
            }, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }
}

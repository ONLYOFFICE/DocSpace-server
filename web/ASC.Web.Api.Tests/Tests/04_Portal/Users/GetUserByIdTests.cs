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

namespace ASC.Web.Api.Tests.Tests._04_Portal.Users;

/// <summary>
/// GET /api/2.0/portal/users/{userID} — reading a portal user's profile by id.
/// </summary>
[Trait("Category", "Portal")]
public class GetUserByIdTests(
    AspireAppFixture fixture)
    : UsersTestBase(fixture)
{
    /// <summary>
    /// Creates a member as the portal owner, capturing the first/last name the SDK's
    /// <see cref="ASC.Tests.Common.Data.User"/> record does not carry, so the test can assert them
    /// against what <c>GetUserById</c> reports back.
    /// </summary>
    private async Task<(User User, string FirstName, string LastName)> CreateMemberAsOwnerAsync(EmployeeType employeeType)
    {
        await _peopleClient.Authenticate(Owner);

        var fake = Initializer.FakerMember.Generate();

        var created = await _profilesApi.AddMemberAsync(new MemberRequestDto
        {
            CultureName = "en-US",
            Spam = false,
            Email = fake.Email,
            Password = fake.Password,
            FirstName = fake.FirstName,
            LastName = fake.LastName,
            Type = employeeType,
        }, TestContext.Current.CancellationToken);

        var user = new User(fake.Email, fake.Password) { Id = created.Response.Id };
        return (user, fake.FirstName, fake.LastName);
    }

    [Theory]
    [InlineData(null, EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.DocSpaceAdmin, EmployeeType.User)]
    [InlineData(EmployeeType.RoomAdmin, EmployeeType.User)]
    public async Task GetUserById_ByRole_ReturnsUser(EmployeeType? actingRole, EmployeeType targetType)
    {
        // Arrange — the owner always creates the target user; only the caller's role varies.
        var (target, firstName, lastName) = await CreateMemberAsOwnerAsync(targetType);
        await ActAsAsync(actingRole);

        // Act
        var result = await _portalUsersApi.GetUserByIdAsync(target.Id, TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().NotBeNull();
        result.Response.Id.Should().Be(target.Id);
        result.Response.FirstName.Should().Be(firstName);
        result.Response.LastName.Should().Be(lastName);
        result.Response.UserName.Should().NotBeNullOrEmpty();
        result.Response.Email.Should().Be(target.Email);
        result.Response.CreatedBy.Should().Be(Owner.Id);
    }

    [Fact]
    public async Task GetUserById_Anonymous_ReturnsUnauthorized()
    {
        // Arrange
        var (target, _, _) = await CreateMemberAsOwnerAsync(EmployeeType.RoomAdmin);
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _portalUsersApi.GetUserByIdAsync(target.Id, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Theory]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task GetUserById_ByRole_ReturnsAccessDenied(EmployeeType actingRole)
    {
        // Arrange
        var (target, _, _) = await CreateMemberAsOwnerAsync(EmployeeType.RoomAdmin);
        await ActAsAsync(actingRole);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _portalUsersApi.GetUserByIdAsync(target.Id, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Trait("Bug", "81212")]
    [Fact]
    public async Task GetUserById_Owner_NonExistentUser_ReturnsNotFound()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _portalUsersApi.GetUserByIdAsync(Guid.Empty, TestContext.Current.CancellationToken));

        // Assert — the endpoint should report a proper 404, not surface an internal error as 200/500.
        exception.ErrorCode.Should().Be(404);
        exception.ErrorContent?.ToString().Should().Contain("The user could not be found");
    }
}

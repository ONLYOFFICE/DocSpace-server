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

namespace ASC.People.Tests.Tests._04_Email;

/// <summary>
/// POST /people/email — access rights, and the request-shape edge cases (missing user, malformed
/// or oversized email, no authentication).
/// </summary>
public class EmailChangeInstructionsPermissionsTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    /// <summary>
    /// Every pair the API refuses: a DocSpace admin may not touch the Owner or another DocSpace
    /// admin, and no non-admin may target anyone but themselves.
    /// </summary>
    public static readonly TheoryData<EmployeeType?, EmployeeType?> DeniedPairs = new()
    {
        { EmployeeType.DocSpaceAdmin, EmployeeType.DocSpaceAdmin },
        { EmployeeType.DocSpaceAdmin, null },

        { EmployeeType.RoomAdmin, null },
        { EmployeeType.RoomAdmin, EmployeeType.DocSpaceAdmin },
        { EmployeeType.RoomAdmin, EmployeeType.RoomAdmin },
        { EmployeeType.RoomAdmin, EmployeeType.User },

        { EmployeeType.User, null },
        { EmployeeType.Guest, null },
        { EmployeeType.User, EmployeeType.DocSpaceAdmin },
        { EmployeeType.Guest, EmployeeType.DocSpaceAdmin },
        { EmployeeType.User, EmployeeType.RoomAdmin },
        { EmployeeType.Guest, EmployeeType.RoomAdmin },
        { EmployeeType.User, EmployeeType.User },
        { EmployeeType.User, EmployeeType.Guest },
        { EmployeeType.Guest, EmployeeType.User },
    };

    [Theory]
    [MemberData(nameof(DeniedPairs))]
    public async Task SendEmailChangeInstructions_ForDisallowedPairs_ReturnsAccessDenied(EmployeeType? viewerType, EmployeeType? targetType)
    {
        var viewer = viewerType is null ? Owner : await InviteMember(viewerType.Value);
        var target = targetType is null ? Owner : await InviteMember(targetType.Value);

        await _peopleClient.Authenticate(viewer);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _emailApi.SendEmailChangeInstructionsAsync(
                new UpdateMemberRequestDto(userId: target.Id.ToString(), email: Initializer.FakerMember.Generate().Email),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task SendEmailChangeInstructions_ForNonExistentUser_ReturnsNotFound()
    {
        await _peopleClient.Authenticate(Owner);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _emailApi.SendEmailChangeInstructionsAsync(
                new UpdateMemberRequestDto(userId: Guid.NewGuid().ToString(), email: Initializer.FakerMember.Generate().Email),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
        exception.ErrorContent?.ToString().Should().Contain("The user could not be found");
    }

    [Fact]
    public async Task SendEmailChangeInstructions_WithMalformedEmail_ReturnsValidationError()
    {
        await _peopleClient.Authenticate(Owner);
        var user = await InviteContact(EmployeeType.User);
        var malformedEmail = Initializer.Faker.Random.String2(20, "abcdefghijklmnopqrstuvwxyz");

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _emailApi.SendEmailChangeInstructionsAsync(
                new UpdateMemberRequestDto(userId: user.Id.ToString(), email: malformedEmail),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent?.ToString().Should().Contain("One or more validation errors occurred.");
        exception.ErrorContent?.ToString().Should().Contain("The Email field is not a valid e-mail address.");
    }

    [Fact]
    public async Task SendEmailChangeInstructions_WithEmailLongerThan255Characters_ReturnsValidationError()
    {
        await _peopleClient.Authenticate(Owner);
        var docSpaceAdmin = await InviteContact(EmployeeType.DocSpaceAdmin);
        var veryLongEmail = new string('a', 250) + "@ab.com";

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _emailApi.SendEmailChangeInstructionsAsync(
                new UpdateMemberRequestDto(userId: docSpaceAdmin.Id.ToString(), email: veryLongEmail),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent?.ToString().Should().Contain("One or more validation errors occurred.");
        exception.ErrorContent?.ToString().Should().Contain("The field Email must be a string with a maximum length of 255.");
    }

    [Fact]
    public async Task SendEmailChangeInstructions_Anonymous_ReturnsUnauthorized()
    {
        await _peopleClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _emailApi.SendEmailChangeInstructionsAsync(
                new UpdateMemberRequestDto(userId: Owner.Id.ToString(), email: Initializer.FakerMember.Generate().Email),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }
}

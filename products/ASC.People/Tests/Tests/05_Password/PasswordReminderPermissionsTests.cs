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

namespace ASC.People.Tests.Tests._05_Password;

/// <summary>
/// POST /people/password — access rights. A password reminder for someone else is only allowed for
/// a DocSpace admin reminding a non-owner, non-admin member; every other pairing is refused.
/// </summary>
public class PasswordReminderPermissionsTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    /// <summary>
    /// A DocSpace admin (not the Owner) may not remind the Owner, nor another DocSpace admin, of
    /// their password — both were once bug 80157 and are kept here as regression coverage now that
    /// the server correctly refuses them.
    /// </summary>
    public static readonly TheoryData<EmployeeType?> DocSpaceAdminDeniedTargets = new()
    {
        (EmployeeType?)null, // the Owner
        EmployeeType.DocSpaceAdmin,
    };

    [Theory]
    [Trait("Category", "Bug")]
    [Trait("Bug", "80157")]
    [MemberData(nameof(DocSpaceAdminDeniedTargets))]
    public async Task SendUserPassword_DocSpaceAdminTargetingOwnerOrAdmin_ReturnsAccessDenied(EmployeeType? targetType)
    {
        var docSpaceAdmin = await InviteContact(EmployeeType.DocSpaceAdmin);
        var target = targetType is null ? Owner : await InviteContact(targetType.Value);

        await _peopleClient.Authenticate(docSpaceAdmin);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _passwordApi.SendUserPasswordAsync(
                new EmailMemberRequestDto(email: target.Email),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    public static readonly TheoryData<EmployeeType, EmployeeType?> OtherDeniedPairs = new()
    {
        { EmployeeType.RoomAdmin, null },
        { EmployeeType.User, null },
        { EmployeeType.Guest, null },

        { EmployeeType.RoomAdmin, EmployeeType.RoomAdmin },
        { EmployeeType.User, EmployeeType.User },
        { EmployeeType.Guest, EmployeeType.Guest },

        { EmployeeType.RoomAdmin, EmployeeType.User },
        { EmployeeType.RoomAdmin, EmployeeType.Guest },
    };

    [Theory]
    [MemberData(nameof(OtherDeniedPairs))]
    public async Task SendUserPassword_ForDisallowedPairs_ReturnsAccessDenied(EmployeeType viewerType, EmployeeType? targetType)
    {
        var viewer = await InviteMember(viewerType);
        var target = targetType is null ? Owner : await InviteMember(targetType.Value);

        await _peopleClient.Authenticate(viewer);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _passwordApi.SendUserPasswordAsync(
                new EmailMemberRequestDto(email: target.Email),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }
}

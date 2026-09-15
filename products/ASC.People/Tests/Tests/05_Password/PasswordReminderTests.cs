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
/// POST /people/password — functional coverage. An authenticated caller reminding themselves, or a
/// DocSpace admin reminding a non-owner, gets one message; an anonymous caller (the login page) gets
/// a deliberately vaguer one that does not confirm whether the address exists.
/// </summary>
public class PasswordReminderTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    public static readonly TheoryData<EmployeeType?, EmployeeType?> AllowedPairs = new()
    {
        // Everyone may remind themselves.
        { null, null },
        { EmployeeType.DocSpaceAdmin, null },
        { EmployeeType.RoomAdmin, null },
        { EmployeeType.User, null },
        { EmployeeType.Guest, null },

        // A DocSpace admin may also remind a non-owner, non-admin member.
        { EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin },
        { EmployeeType.DocSpaceAdmin, EmployeeType.User },
        { EmployeeType.DocSpaceAdmin, EmployeeType.Guest },
    };

    [Theory]
    [MemberData(nameof(AllowedPairs))]
    public async Task SendUserPassword_ForAllowedPairs_ReturnsSuccessMessage(EmployeeType? viewerType, EmployeeType? targetType)
    {
        var viewer = viewerType is null ? Owner : await InviteMember(viewerType.Value);
        var target = targetType is null ? viewer : await InviteMember(targetType.Value);

        await _peopleClient.Authenticate(viewer);

        var result = await _passwordApi.SendUserPasswordAsync(
            new EmailMemberRequestDto(email: target.Email),
            TestContext.Current.CancellationToken);

        result.StatusCode.Should().Be(200);
        result.Response.Should().Be($"The password change instruction has been sent to {target.Email} email address.");
    }

    [Fact]
    public async Task SendUserPassword_Anonymous_ReturnsGenericSuccessMessage()
    {
        await _peopleClient.Authenticate(null);

        var result = await _passwordApi.SendUserPasswordAsync(
            new EmailMemberRequestDto(email: Owner.Email),
            TestContext.Current.CancellationToken);

        result.StatusCode.Should().Be(200);
        result.Response.Should().Be(
            $"If a user with the {Owner.Email} email exists, the password change instruction has been sent to this email address.");
    }
}

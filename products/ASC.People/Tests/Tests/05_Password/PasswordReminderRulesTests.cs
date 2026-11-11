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
/// POST /people/password — the owner may remind a DocSpace administrator (an administrator may not), and the
/// sign-in page gets the same answer whether or not an account with the address exists. The CAPTCHA branch needs
/// CAPTCHA keys the stand does not configure.
/// </summary>
public class PasswordReminderRulesTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task SendUserPassword_OwnerForDocSpaceAdmin_ReturnsSuccessMessage()
    {
        // Arrange
        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);
        await _peopleClient.Authenticate(Owner);

        // Act
        var result = await _passwordApi.SendUserPasswordAsync(
            new EmailMemberRequestDto(email: admin.Email),
            TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(200);
        result.Response.Should().Be($"The password change instruction has been sent to {admin.Email} email address.");
    }

    [Fact]
    public async Task SendUserPassword_AnonymousForUnknownAddress_ReturnsSameGenericMessage()
    {
        // Arrange
        await _peopleClient.Authenticate(null);
        var unknown = $"nobody-{Guid.NewGuid():N}@example.com";

        // Act
        var result = await _passwordApi.SendUserPasswordAsync(
            new EmailMemberRequestDto(email: unknown),
            TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(200);
        result.Response.Should().Be(
            $"If a user with the {unknown} email exists, the password change instruction has been sent to this email address.");
    }
}

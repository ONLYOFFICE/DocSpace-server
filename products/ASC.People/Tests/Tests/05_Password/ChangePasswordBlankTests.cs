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
/// PUT /api/2.0/people/{userId}/password with a password of spaces, or a password hash of spaces and no password. Both
/// pass the action's own "nothing sent" check and reach the password policy check empty after trimming, which threw a
/// plain <c>Exception</c> that nothing maps, so the call answered 500. The confirmation link is the one
/// PUT security/activeconnections/logoutallchangepassword returns for the caller.
/// </summary>
public class ChangePasswordBlankTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Theory]
    [InlineData("   ", null)]
    [InlineData(null, "   ")]
    public async Task ChangeUserPassword_BlankPassword_Returns400(string? password, string? passwordHash)
    {
        // Arrange
        var member = await InviteMember(EmployeeType.User);
        var confirm = await GetPasswordChangeTokenAsync(member);

        await _peopleClient.Authenticate(null);
        _peopleClient.DefaultRequestHeaders.Add("confirm", confirm);

        try
        {
            // Act
            var exception = await Assert.ThrowsAsync<ApiException>(async () =>
                await _passwordApi.ChangeUserPasswordAsync(
                    member.Id,
                    new ChangePasswordRequest(password!, passwordHash!),
                    TestContext.Current.CancellationToken));

            // Assert
            exception.ErrorCode.Should().Be(400);
        }
        finally
        {
            _peopleClient.DefaultRequestHeaders.Remove("confirm");
        }
    }

    /// <summary>
    /// Signs in as <paramref name="member"/>, asks for the password change link and returns its query string, which is
    /// the value of the <c>confirm</c> header.
    /// </summary>
    private async Task<string> GetPasswordChangeTokenAsync(User member)
    {
        await _webApiClient.Authenticate(member);

        using var response = await _webApiClient.PutAsync(
            "api/2.0/security/activeconnections/logoutallchangepassword", null, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var url = JsonDocument.Parse(body).RootElement.GetProperty("response").GetString()!;

        url.Should().Contain("type=PasswordChange");

        return url[(url.IndexOf('?') + 1)..];
    }
}

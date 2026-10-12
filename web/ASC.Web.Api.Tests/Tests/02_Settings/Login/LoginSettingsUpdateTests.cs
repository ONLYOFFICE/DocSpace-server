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

namespace ASC.Web.Api.Tests.Tests._02_Settings.Login;

/// <summary>
/// PUT /api/2.0/settings/security/loginsettings — updating the portal's brute-force login
/// protection settings. Writable only by Owner and DocSpaceAdmin; the request DTO's
/// AttemptCount/BlockTime/CheckPeriod are all range-checked server-side against 1-9999.
/// </summary>
[Trait("Category", "Settings")]
public class LoginSettingsUpdateTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task UpdateLoginSettings_Owner_SavesCustomValues()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var request = new LoginSettingsRequestDto(attemptCount: 3, blockTime: 15, checkPeriod: 60);

        try
        {
            // Act
            var updated = await _loginSettingsApi.UpdateLoginSettingsAsync(request, TestContext.Current.CancellationToken);

            // Assert
            updated.StatusCode.Should().Be(200);
            updated.Response.AttemptCount.Should().Be(3);
            updated.Response.BlockTime.Should().Be(15);
            updated.Response.CheckPeriod.Should().Be(60);
            updated.Response.IsDefault.Should().BeFalse();

            var fetched = await _loginSettingsApi.GetLoginSettingsAsync(TestContext.Current.CancellationToken);
            fetched.Response.AttemptCount.Should().Be(3);
            fetched.Response.BlockTime.Should().Be(15);
            fetched.Response.CheckPeriod.Should().Be(60);
            fetched.Response.IsDefault.Should().BeFalse();
        }
        finally
        {
            await _loginSettingsApi.SetDefaultLoginSettingsAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task UpdateLoginSettings_DocSpaceAdmin_SavesCustomValues()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(admin);
        var request = new LoginSettingsRequestDto(attemptCount: 7, blockTime: 20, checkPeriod: 120);

        try
        {
            // Act
            var updated = await _loginSettingsApi.UpdateLoginSettingsAsync(request, TestContext.Current.CancellationToken);

            // Assert
            updated.StatusCode.Should().Be(200);
            updated.Response.AttemptCount.Should().Be(7);
            updated.Response.BlockTime.Should().Be(20);
            updated.Response.CheckPeriod.Should().Be(120);
            updated.Response.IsDefault.Should().BeFalse();

            var fetched = await _loginSettingsApi.GetLoginSettingsAsync(TestContext.Current.CancellationToken);
            fetched.Response.AttemptCount.Should().Be(7);
            fetched.Response.BlockTime.Should().Be(20);
            fetched.Response.CheckPeriod.Should().Be(120);
        }
        finally
        {
            // The admin can also reset it, but switch back to Owner so the portal is left in a
            // known state regardless of which role happens to run last.
            await _webApiClient.Authenticate(Owner);
            await _loginSettingsApi.SetDefaultLoginSettingsAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task UpdateLoginSettings_Anonymous_ThrowsUnauthorized()
    {
        // Arrange
        await _webApiClient.Authenticate(null);
        var request = new LoginSettingsRequestDto(attemptCount: 3, blockTime: 10, checkPeriod: 60);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _loginSettingsApi.UpdateLoginSettingsAsync(request, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task UpdateLoginSettings_Member_ThrowsForbidden(EmployeeType employeeType)
    {
        // Arrange
        var member = await InviteMember(employeeType);
        await _webApiClient.Authenticate(member);
        var request = new LoginSettingsRequestDto(attemptCount: 3, blockTime: 10, checkPeriod: 60);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _loginSettingsApi.UpdateLoginSettingsAsync(request, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Theory]
    [InlineData(99999, 60, 60, "AttemptCount")]
    [InlineData(5, 99999, 60, "BlockTime")]
    [InlineData(5, 60, 99999, "CheckPeriod")]
    public async Task UpdateLoginSettings_ValueExceedsUiLimit_ThrowsValidationError(
        int attemptCount, int blockTime, int checkPeriod, string field)
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var request = new LoginSettingsRequestDto(attemptCount, blockTime, checkPeriod);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _loginSettingsApi.UpdateLoginSettingsAsync(request, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent?.ToString().Should().Contain($"The field {field} must be between 1 and 9999.");
    }
}

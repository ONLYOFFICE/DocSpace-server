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

namespace ASC.Web.Api.Tests.Tests._02_Settings.Cookies;

/// <summary>
/// PUT /api/2.0/settings/cookiesettings — updating the portal's cookie lifetime. A successful
/// update invalidates the current session (the tenant's cookie version is bumped), so every
/// passing case here re-authenticates its own client afterward. Writable only by Owner and
/// DocSpaceAdmin.
/// </summary>
[Trait("Category", "Settings")]
public class CookieSettingsUpdateTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task UpdateCookieSettings_Owner_SavesSettings()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var updated = await _cookiesApi.UpdateCookieSettingsAsync(
            new CookieSettingsRequestsDto(lifeTime: 720, enabled: true), TestContext.Current.CancellationToken);

        // Assert
        updated.StatusCode.Should().Be(200);
        updated.Response.Should().Be("Settings have been successfully updated");

        // The update bumped the tenant's cookie version and invalidated this session.
        await _webApiClient.Authenticate(Owner);
    }

    [Fact]
    public async Task UpdateCookieSettings_DocSpaceAdmin_SavesSettings()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(admin);

        // Act
        var updated = await _cookiesApi.UpdateCookieSettingsAsync(
            new CookieSettingsRequestsDto(lifeTime: 720, enabled: true), TestContext.Current.CancellationToken);

        // Assert
        updated.StatusCode.Should().Be(200);
        updated.Response.Should().Be("Settings have been successfully updated");

        // Every session on the tenant was invalidated, including the owner's — restore it.
        await _webApiClient.Authenticate(Owner);
    }

    [Fact]
    public async Task UpdateCookieSettings_Anonymous_ThrowsUnauthorized()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _cookiesApi.UpdateCookieSettingsAsync(
                new CookieSettingsRequestsDto(lifeTime: 720, enabled: true), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task UpdateCookieSettings_Member_ThrowsForbidden(EmployeeType employeeType)
    {
        // Arrange
        var member = await InviteMember(employeeType);
        await _webApiClient.Authenticate(member);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _cookiesApi.UpdateCookieSettingsAsync(
                new CookieSettingsRequestsDto(lifeTime: 720, enabled: true), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    // The controller clamps an out-of-range lifetime to the maximum instead of rejecting it
    // (CookiesManager.SetLifeTimeAsync) — not a validated DTO, so this is the actual behaviour.
    [Fact]
    public async Task UpdateCookieSettings_LifeTimeExceedsMaximum_IsClampedTo9999()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var updated = await _cookiesApi.UpdateCookieSettingsAsync(
            new CookieSettingsRequestsDto(lifeTime: 100000, enabled: true), TestContext.Current.CancellationToken);

        // Assert
        updated.StatusCode.Should().Be(200);
        updated.Response.Should().Be("Settings have been successfully updated");

        // The update bumps the cookie version and invalidates every issued token, including the
        // cached one — force a fresh sign-in instead of reusing it.
        await _webApiClient.Authenticate(Owner, forceRefresh: true);

        var fetched = await _cookiesApi.GetCookieSettingsAsync(TestContext.Current.CancellationToken);
        fetched.Response.LifeTime.Should().Be(9999);
        fetched.Response.Enabled.Should().BeTrue();
    }
}

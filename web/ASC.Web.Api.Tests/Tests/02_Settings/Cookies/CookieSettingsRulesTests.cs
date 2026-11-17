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
/// PUT /api/2.0/settings/cookiesettings beyond the role matrix (<c>CookiesManager.SetLifeTimeAsync</c>): a lifetime of
/// 0 or less clears the number, the change is audited, and a positive lifetime makes every token issued before the
/// call stop working, whatever <c>enabled</c> says. <see cref="User.Token"/> keeps the token handed out before the
/// call, so re-using it after the change is exactly the client that held it.
/// </summary>
[Trait("Category", "Settings")]
public class CookieSettingsRulesTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task UpdateCookieSettings_LifeTimeZeroOrLess_ClearsTheLifeTime(int lifeTime)
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        await _cookiesApi.UpdateCookieSettingsAsync(new CookieSettingsRequestDto(lifeTime: 720, enabled: true), TestContext.Current.CancellationToken);
        await _webApiClient.Authenticate(Owner, forceRefresh: true);

        // Act
        var updated = await _cookiesApi.UpdateCookieSettingsAsync(
            new CookieSettingsRequestDto(lifeTime: lifeTime, enabled: true), TestContext.Current.CancellationToken);

        // Assert
        updated.StatusCode.Should().Be(200);
        var fetched = (await _cookiesApi.GetCookieSettingsAsync(TestContext.Current.CancellationToken)).Response;
        fetched.LifeTime.Should().Be(0);
        fetched.Enabled.Should().BeTrue();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task UpdateCookieSettings_PositiveLifeTime_TokenIssuedBeforeIsRejected(bool enabled)
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var tokenBefore = Owner.Token;

        // Act
        await _cookiesApi.UpdateCookieSettingsAsync(new CookieSettingsRequestDto(lifeTime: 60, enabled: enabled), TestContext.Current.CancellationToken);

        // Assert
        _webApiClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenBefore);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _cookiesApi.GetCookieSettingsAsync(TestContext.Current.CancellationToken));
        exception.ErrorCode.Should().Be(401);

        await _webApiClient.Authenticate(Owner, forceRefresh: true);
        var fetched = await _cookiesApi.GetCookieSettingsAsync(TestContext.Current.CancellationToken);
        fetched.Response.LifeTime.Should().Be(60);
    }

    [Fact]
    public async Task UpdateCookieSettings_ChangedByDocSpaceAdmin_OwnersEarlierTokenIsRejected()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var ownerToken = Owner.Token;
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(admin);

        // Act
        await _cookiesApi.UpdateCookieSettingsAsync(new CookieSettingsRequestDto(lifeTime: 60, enabled: false), TestContext.Current.CancellationToken);

        // Assert
        _webApiClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", ownerToken);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _cookiesApi.GetCookieSettingsAsync(TestContext.Current.CancellationToken));
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task UpdateCookieSettings_Saved_WritesCookieSettingsUpdatedAuditEvent()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        await _cookiesApi.UpdateCookieSettingsAsync(new CookieSettingsRequestDto(lifeTime: 0, enabled: false), TestContext.Current.CancellationToken);

        // Assert
        await _webApiClient.Authenticate(Owner, forceRefresh: true);
        var events = await SettingsAudit.PollAsync(_auditTrailDataApi, MessageAction.CookieSettingsUpdated);
        events.Should().NotBeEmpty();
    }
}

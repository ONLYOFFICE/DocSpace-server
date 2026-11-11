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
/// GET /api/2.0/settings/security/loginSettings answers 304 when the sign-in limits have not changed since the
/// <c>Last-Modified</c> it handed out.
/// </summary>
[Trait("Category", "Settings")]
public class LoginSettingsConditionalGetTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    private const string LoginSettingsPath = "api/2.0/settings/security/loginSettings";

    [Fact]
    public async Task GetLoginSettings_IfModifiedSinceIsTheLastModifiedHandedOut_ReturnsNotModified()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        await _loginSettingsApi.UpdateLoginSettingsAsync(
            new LoginSettingsRequestDto(attemptCount: 7, blockTime: 30, checkPeriod: 30), TestContext.Current.CancellationToken);

        using var first = await ConditionalGet.GetAsync(_webApiClient, LoginSettingsPath);
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        var lastModified = ConditionalGet.Header(first, "Last-Modified");
        lastModified.Should().NotBeNullOrEmpty();

        // Act
        using var second = await ConditionalGet.SendAsync(_webApiClient, LoginSettingsPath, "If-Modified-Since", lastModified!);

        // Assert
        second.StatusCode.Should().Be(HttpStatusCode.NotModified);
    }

    [Fact]
    public async Task GetLoginSettings_IfModifiedSinceOlderThanTheChange_ReturnsSettings()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        await _loginSettingsApi.UpdateLoginSettingsAsync(
            new LoginSettingsRequestDto(attemptCount: 7, blockTime: 30, checkPeriod: 30), TestContext.Current.CancellationToken);

        // Act
        using var response = await ConditionalGet.SendAsync(
            _webApiClient, LoginSettingsPath, "If-Modified-Since", "01/01/2001 00:00:00");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

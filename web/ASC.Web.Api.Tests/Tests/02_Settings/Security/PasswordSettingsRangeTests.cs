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

namespace ASC.Web.Api.Tests.Tests._02_Settings.Security;

/// <summary>
/// The edges of the password policy: <c>minLength</c> must lie between the installation key <c>web:password:min</c>
/// (unset on the stand, so the default 8) and the fixed upper end 30
/// (<c>PasswordSettingsManager.CheckLengthInRange</c>), and the read answers 304 when the policy has not changed
/// since the <c>Last-Modified</c> it handed out.
/// </summary>
[Trait("Category", "Settings")]
public class PasswordSettingsRangeTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    private const string PasswordPath = "api/2.0/settings/security/password";

    [Theory]
    [InlineData(8)]
    [InlineData(30)]
    public async Task UpdatePasswordSettings_MinLengthAtEitherEndOfRange_IsStored(int minLength)
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var updated = await _securityApi.UpdatePasswordSettingsAsync(
            new PasswordSettingsRequestDto(minLength, false, false, false), TestContext.Current.CancellationToken);

        // Assert
        updated.Response.MinLength.Should().Be(minLength);
        var after = await _securityApi.GetPasswordSettingsAsync(TestContext.Current.CancellationToken);
        after.Response.MinLength.Should().Be(minLength);
    }

    [Theory]
    [InlineData(7)]
    [InlineData(31)]
    public async Task UpdatePasswordSettings_MinLengthJustOutsideRange_ThrowsBadRequestNamingMinLength(int minLength)
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _securityApi.UpdatePasswordSettingsAsync(
                new PasswordSettingsRequestDto(minLength, false, false, false), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent!.ToString().Should().Contain("MinLength");
    }

    [Fact]
    public async Task GetPasswordSettings_IfModifiedSinceIsTheLastModifiedHandedOut_ReturnsNotModified()
    {
        // Arrange: a stored policy has a LastModified; the default one has none and is never 304
        await _webApiClient.Authenticate(Owner);
        await _securityApi.UpdatePasswordSettingsAsync(
            new PasswordSettingsRequestDto(10, false, false, false), TestContext.Current.CancellationToken);

        using var first = await ConditionalGet.GetAsync(_webApiClient, PasswordPath);
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        var lastModified = ConditionalGet.Header(first, "Last-Modified");
        lastModified.Should().NotBeNullOrEmpty();

        // Act
        using var second = await ConditionalGet.SendAsync(_webApiClient, PasswordPath, "If-Modified-Since", lastModified!);

        // Assert
        second.StatusCode.Should().Be(HttpStatusCode.NotModified);
    }
}

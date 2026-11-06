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

namespace ASC.Web.Api.Tests.Tests._02_Settings.Common;

/// <summary>
/// PUT /api/2.0/settings/emailactivation (a per-user preference, so any signed-in role may call
/// it) and PUT /api/2.0/settings/dns. DNS mapping is Standalone-only and a successful save would
/// re-map the domain of the test portal itself, so only the rejection that changes nothing is covered.
/// </summary>
[Trait("Category", "Settings")]
public class EmailActivationAndDnsSettingsTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task UpdateEmailActivationSettings_Owner_ReturnsSettingsAsSubmitted()
    {
        // Act
        var saved = await _commonSettingsApi.UpdateEmailActivationSettingsAsync(
            new EmailActivationSettingsRequestDto(false), TestContext.Current.CancellationToken);

        // Assert
        saved.StatusCode.Should().Be(200);
        saved.Response.Show.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateEmailActivationSettings_User_Succeeds()
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);
        await _webApiClient.Authenticate(user);

        // Act
        var saved = await _commonSettingsApi.UpdateEmailActivationSettingsAsync(
            new EmailActivationSettingsRequestDto(true), TestContext.Current.CancellationToken);

        // Assert
        saved.StatusCode.Should().Be(200);
        saved.Response.Show.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateEmailActivationSettings_Anonymous_ThrowsUnauthorized()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _commonSettingsApi.UpdateEmailActivationSettingsAsync(
                new EmailActivationSettingsRequestDto(true), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task SaveDnsSettings_Anonymous_ThrowsUnauthorized()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _commonSettingsApi.SaveDnsSettingsAsync(
                new DnsSettingsRequestDto("custom.example.com", true), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }
}

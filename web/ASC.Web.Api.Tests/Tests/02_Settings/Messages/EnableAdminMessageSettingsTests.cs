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

namespace ASC.Web.Api.Tests.Tests._02_Settings.Messages;

/// <summary>
/// POST /api/2.0/settings/messagesettings — toggling the administrator contact form shown on
/// the Sign In page. No cleanup is needed: the portal belongs to this test alone.
/// </summary>
[Trait("Category", "Settings")]
public class EnableAdminMessageSettingsTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task EnableAdminMessageSettings_Owner_UpdatesSetting(bool turnOn)
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var result = await _messagesApi.EnableAdminMessageSettingsAsync(
            new TurnOnAdminMessageSettingsRequestDto(turnOn), TestContext.Current.CancellationToken);

        // Assert
        AssertUpdated(result);
    }

    [Fact]
    public async Task EnableAdminMessageSettings_Owner_TogglesOnThenOff()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var enabled = await _messagesApi.EnableAdminMessageSettingsAsync(
            new TurnOnAdminMessageSettingsRequestDto(true), TestContext.Current.CancellationToken);
        var disabled = await _messagesApi.EnableAdminMessageSettingsAsync(
            new TurnOnAdminMessageSettingsRequestDto(false), TestContext.Current.CancellationToken);

        // Assert
        AssertUpdated(enabled);
        AssertUpdated(disabled);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task EnableAdminMessageSettings_DocSpaceAdmin_UpdatesSetting(bool turnOn)
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(admin);

        // Act
        var result = await _messagesApi.EnableAdminMessageSettingsAsync(
            new TurnOnAdminMessageSettingsRequestDto(turnOn), TestContext.Current.CancellationToken);

        // Assert
        AssertUpdated(result);
    }

    private static void AssertUpdated(StringWrapper result)
    {
        result.StatusCode.Should().Be(200);
        result.Status.Should().Be(0);
        result.Response.Should().Be("Settings have been successfully updated");
        result.Count.Should().Be(1);
        result.Links.Should().NotBeNullOrEmpty();
    }
}

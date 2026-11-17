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

namespace ASC.Web.Api.Tests.Tests._02_Settings.Webplugins;

/// <summary>
/// DELETE /api/2.0/settings/webplugins/{name} — who may delete, the lookup, and what is left afterwards.
/// </summary>
[Trait("Category", "Settings")]
public class WebPluginRemovalTests(
    AspireAppFixture fixture)
    : WebPluginTestBase(fixture)
{
    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task Delete_MemberWithoutPortalSettingsRight_ThrowsAccessDeniedAndKeepsThePlugin(EmployeeType employeeType)
    {
        // Arrange
        await UploadAsOwnerAsync("del-denied");
        var member = await InviteMember(employeeType);
        await _webApiClient.Authenticate(member);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _webpluginsApi.DeleteWebPluginWithHttpInfoAsync("del-denied", TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent!.ToString().Should().Contain("Access denied");
        (await GetPluginAsync("del-denied")).Name.Should().Be("del-denied");
    }

    [Fact]
    public async Task Delete_DocSpaceAdmin_RemovesThePlugin()
    {
        // Arrange
        await UploadAsOwnerAsync("del-admin");
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(admin);

        // Act
        var result = await _webpluginsApi.DeleteWebPluginWithHttpInfoAsync("del-admin", TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ListPluginsAsync()).Select(p => p.Name).Should().NotContain("del-admin");
    }

    [Fact]
    public async Task Delete_NameInAnotherCase_RemovesThePluginAndItsLookup()
    {
        // Arrange
        await UploadAsOwnerAsync("del-case");

        // Act
        await _webpluginsApi.DeleteWebPluginWithHttpInfoAsync("DEL-Case", TestContext.Current.CancellationToken);

        // Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _webpluginsApi.GetWebPluginAsync("del-case", TestContext.Current.CancellationToken));
        exception.ErrorCode.Should().Be(404);
        (await ListPluginsAsync()).Select(p => p.Name).Should().NotContain("del-case");
    }

    [Fact]
    public async Task Delete_UnknownName_ThrowsNotFound()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _webpluginsApi.DeleteWebPluginWithHttpInfoAsync("del-unknown", TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
        exception.ErrorContent!.ToString().Should().Contain("Plugin not found");
    }

    [Fact]
    public async Task Delete_RemovesTheStoredState_SoAReuploadStartsWithoutSettings()
    {
        // Arrange
        await UploadAsOwnerAsync("del-state");
        await UpdatePluginAsync("del-state", false, "old-settings");

        // Act
        await _webpluginsApi.DeleteWebPluginWithHttpInfoAsync("del-state", TestContext.Current.CancellationToken);
        await UploadAsOwnerAsync("del-state");

        // Assert
        var plugin = await GetPluginAsync("del-state");
        plugin.Enabled.Should().BeTrue();
        plugin.Settings.Should().BeNull();
    }
}

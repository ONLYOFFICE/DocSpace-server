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
/// PUT /api/2.0/settings/security/security and PUT /api/2.0/settings/security/access — writing
/// per-module and bulk web-item security. Only a portal owner or a DocSpaceAdmin may change it.
/// </summary>
[Trait("Category", "Settings")]
public class WebItemSecurityWriteTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task SetWebItemSecurity_Owner_DisablesWebItem()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var id = Guid.NewGuid().ToString();

        // Act
        var result = await _securityApi.SetWebItemSecurityAsync(
            new WebItemSecurityRequestsDto(id, false), TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(200);
        result.Response.Should().ContainSingle(s => s.WebItemId == id && !s.Enabled);

        var after = await _securityApi.GetWebItemSettingsSecurityInfoAsync([id], TestContext.Current.CancellationToken);
        after.Response.Should().ContainSingle(s => s.WebItemId == id && !s.Enabled);
    }

    [Fact]
    public async Task SetWebItemSecurity_DocSpaceAdmin_CanSetSecurity()
    {
        // Arrange
        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(admin);
        var id = Guid.NewGuid().ToString();

        // Act
        var result = await _securityApi.SetWebItemSecurityAsync(
            new WebItemSecurityRequestsDto(id, false), TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(200);
        result.Response.Should().ContainSingle(s => s.WebItemId == id && !s.Enabled);
    }

    // BUG 83187: a malformed (non-GUID) id crashes this endpoint with an unhandled 500
    // instead of a clean 400, the same root cause as BUG 83186 on the read side.
    [Trait("Bug", "83187")]
    [Fact]
    public async Task SetWebItemSecurity_MalformedId_ThrowsValidationError()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _securityApi.SetWebItemSecurityAsync(
                new WebItemSecurityRequestsDto("not-a-guid", false), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task SetWebItemSecurity_NonAdminMember_ThrowsAccessDenied(EmployeeType employeeType)
    {
        // Arrange
        var member = await InviteMember(employeeType);
        await _webApiClient.Authenticate(member);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _securityApi.SetWebItemSecurityAsync(
                new WebItemSecurityRequestsDto(Guid.NewGuid().ToString(), false), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task SetAccessToWebItems_Owner_SetsMultipleItems()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var firstId = Guid.NewGuid().ToString();
        var secondId = Guid.NewGuid().ToString();

        var request = new WebItemsSecurityRequestsDto(
        [
            new ItemKeyValuePairStringBoolean(firstId, false),
            new ItemKeyValuePairStringBoolean(secondId, true),
        ]);

        // Act
        var result = await _securityApi.SetAccessToWebItemsAsync(request, TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(200);
        result.Response.Should().Contain(s => s.WebItemId == firstId && !s.Enabled);
        result.Response.Should().Contain(s => s.WebItemId == secondId && s.Enabled);
    }

    // BUG 83190: a malformed (non-GUID) id crashes this endpoint with an unhandled 500
    // instead of a clean 400, the same root cause as BUG 83186/83187.
    [Trait("Bug", "83190")]
    [Fact]
    public async Task SetAccessToWebItems_MalformedId_ThrowsValidationError()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var request = new WebItemsSecurityRequestsDto([new ItemKeyValuePairStringBoolean("not-a-guid", true)]);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _securityApi.SetAccessToWebItemsAsync(request, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task SetAccessToWebItems_User_ThrowsAccessDenied()
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);
        await _webApiClient.Authenticate(user);
        var request = new WebItemsSecurityRequestsDto([new ItemKeyValuePairStringBoolean(Guid.NewGuid().ToString(), true)]);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _securityApi.SetAccessToWebItemsAsync(request, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }
}

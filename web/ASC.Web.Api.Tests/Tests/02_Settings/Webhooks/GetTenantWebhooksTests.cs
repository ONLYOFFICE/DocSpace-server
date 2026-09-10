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

namespace ASC.Web.Api.Tests.Tests._02_Settings.Webhooks;

/// <summary>
/// GET /api/2.0/settings/webhook — listing the tenant's webhook configurations. Any portal
/// member except a Guest may see the whole tenant list, not just their own webhooks.
/// </summary>
[Trait("Category", "Settings")]
public class GetTenantWebhooksTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task GetTenantWebhooks_Owner_ContainsCreatedWebhook()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var dto = WebhooksTestData.CreateWebhookDto();
        var created = await _webhooksApi.CreateWebhookAsync(dto, TestContext.Current.CancellationToken);

        // Act
        var list = await _webhooksApi.GetTenantWebhooksAsync(TestContext.Current.CancellationToken);

        // Assert
        list.StatusCode.Should().Be(200);
        var found = list.Response.Should().ContainSingle(w => w.Configs.Id == created.Response.Id).Subject;
        found.Configs.Name.Should().Be(dto.Name);
        found.Configs.Uri.Should().Be(dto.Uri);
    }

    [Fact]
    public async Task GetTenantWebhooks_Anonymous_ThrowsUnauthorized()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _webhooksApi.GetTenantWebhooksAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    public async Task GetTenantWebhooks_RoomAdminOrUser_ReturnsList(EmployeeType employeeType)
    {
        // Arrange
        var member = await InviteContact(employeeType);
        await _webApiClient.Authenticate(member);

        // Act
        var list = await _webhooksApi.GetTenantWebhooksAsync(TestContext.Current.CancellationToken);

        // Assert
        list.StatusCode.Should().Be(200);
        list.Response.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTenantWebhooks_Guest_ThrowsAccessDenied()
    {
        // Arrange
        var guest = await InviteGuest();
        await _webApiClient.Authenticate(guest);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _webhooksApi.GetTenantWebhooksAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }
}

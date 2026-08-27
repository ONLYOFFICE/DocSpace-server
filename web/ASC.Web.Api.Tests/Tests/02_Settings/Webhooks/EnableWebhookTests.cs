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
/// PUT /api/2.0/settings/webhook/enable — toggling a webhook's enabled state. Only the webhook's
/// own creator (or an admin) may toggle it; a plain member cannot touch another member's webhook.
/// </summary>
[Trait("Category", "Settings")]
public class EnableWebhookTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task EnableWebhook_Owner_TogglesEnabledState()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var dto = WebhooksTestData.CreateWebhookDto(enabled: true);
        var created = await _webhooksApi.CreateWebhookAsync(dto, TestContext.Current.CancellationToken);

        var toggleRequest = new UpdateWebhooksConfigRequestsDto(created.Response.Id)
        {
            Name = dto.Name,
            Uri = dto.Uri,
            SecretKey = dto.SecretKey,
            Enabled = false
        };

        // Act
        var result = await _webhooksApi.EnableWebhookAsync(toggleRequest, TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(200);
        result.Response.Id.Should().Be(created.Response.Id);
        result.Response.Name.Should().Be(dto.Name);
        result.Response.Uri.Should().Be(dto.Uri);
        result.Response.Enabled.Should().BeFalse();
        result.Response.ModifiedOn.Should().NotBeNull();
    }

    [Fact]
    public async Task EnableWebhook_AnotherUsersWebhook_ThrowsAccessDenied()
    {
        // Arrange
        var userA = await InviteContact(EmployeeType.User);
        var userB = await InviteContact(EmployeeType.User);

        await _webApiClient.Authenticate(userB);
        var dto = WebhooksTestData.CreateWebhookDto();
        var created = await _webhooksApi.CreateWebhookAsync(dto, TestContext.Current.CancellationToken);

        await _webApiClient.Authenticate(userA);
        var toggleRequest = new UpdateWebhooksConfigRequestsDto(created.Response.Id)
        {
            Name = dto.Name,
            Uri = dto.Uri,
            SecretKey = dto.SecretKey,
            Enabled = false
        };

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _webhooksApi.EnableWebhookAsync(toggleRequest, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }
}

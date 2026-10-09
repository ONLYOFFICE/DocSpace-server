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
/// PUT /api/2.0/settings/webhook/{id}/retry — re-queuing one webhook delivery.
/// </summary>
[Trait("Category", "Settings")]
public class RetryWebhookTests(
    AspireAppFixture fixture)
    : WebhookDeliveryTestBase(fixture)
{
    [Fact]
    public async Task RetryWebhook_AttemptedDelivery_KeepsIdAndClearsOutcome()
    {
        // Arrange
        var delivery = await CreateAttemptedDeliveryAsync();

        // Act
        var result = await _webhooksApi.RetryWebhookAsync(delivery.Id, TestContext.Current.CancellationToken);

        // Assert: the same record is requeued, so the receiver gets the same event.id again
        result.Response.Id.Should().Be(delivery.Id);
        result.Response.Status.Should().Be(0);
        result.Response.Delivery.Should().BeNull();
        result.Response.RequestPayload.Should().Be(delivery.RequestPayload);
    }

    [Fact]
    public async Task RetryWebhook_AttemptedDelivery_IsAttemptedAgainUnderTheSameId()
    {
        // Arrange
        var delivery = await CreateAttemptedDeliveryAsync();

        // Act
        await _webhooksApi.RetryWebhookAsync(delivery.Id, TestContext.Current.CancellationToken);

        // Assert
        var record = await WaitForAttemptAsync(delivery.Id);

        record.Should().NotBeNull();
        record!.Status.Should().NotBe(0);
        record.Delivery.Should().NotBeNull();
    }

    [Fact]
    public async Task RetryWebhook_UnknownId_ThrowsNotFound()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _webhooksApi.RetryWebhookAsync(int.MaxValue, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task RetryWebhook_ZeroId_ThrowsBadRequest()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _webhooksApi.RetryWebhookAsync(0, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task RetryWebhook_AnotherMembersSubscription_ThrowsAccessDenied()
    {
        // Arrange
        var delivery = await CreateAttemptedDeliveryAsync();
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        await _webApiClient.Authenticate(roomAdmin);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _webhooksApi.RetryWebhookAsync(delivery.Id, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task RetryWebhook_Guest_ThrowsAccessDenied()
    {
        // Arrange
        var delivery = await CreateAttemptedDeliveryAsync();
        var guest = await InviteGuest();
        await _webApiClient.Authenticate(guest);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _webhooksApi.RetryWebhookAsync(delivery.Id, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }
}

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

namespace ASC.Web.Api.Tests.Tests._04_Portal.Payments;

/// <summary>
/// POST /api/2.0/portal/payment/request — the "contact sales" form. The action only calls
/// <c>PaymentHelper.DemandAdminAsync</c>, never <c>DemandConfigured</c>, so it is fully exercisable
/// without a configured tariff service: it queues an internal notification, not an external
/// billing call.
/// </summary>
[Trait("Category", "Portal")]
public class PaymentRequestTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    private static SalesRequestsDto ValidRequest => new("nctTest", "nct@email.com", "autoTest");

    [Fact]
    public async Task SendPaymentRequest_Owner_Succeeds()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act & Assert
        await _paymentApi.SendPaymentRequestAsync(ValidRequest, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SendPaymentRequest_DocSpaceAdmin_Succeeds()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(admin);

        // Act & Assert
        await _paymentApi.SendPaymentRequestAsync(ValidRequest, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SendPaymentRequest_Anonymous_ThrowsUnauthorized()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.SendPaymentRequestAsync(ValidRequest, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task SendPaymentRequest_RoomAdmin_ThrowsAccessDenied()
    {
        // Arrange
        var roomAdmin = await InviteMember(EmployeeType.RoomAdmin);
        await _webApiClient.Authenticate(roomAdmin);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.SendPaymentRequestAsync(ValidRequest, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task SendPaymentRequest_User_ThrowsAccessDenied()
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);
        await _webApiClient.Authenticate(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.SendPaymentRequestAsync(ValidRequest, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task SendPaymentRequest_Guest_ThrowsAccessDenied()
    {
        // Arrange
        var guest = await InviteGuest();
        await _webApiClient.Authenticate(guest);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.SendPaymentRequestAsync(ValidRequest, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task SendPaymentRequest_UserNameTooLong_ThrowsBadRequest()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var request = new SalesRequestsDto(new string('a', 256), "nct@email.com", "autoTest");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.SendPaymentRequestAsync(request, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent?.ToString().Should().Contain("UserName");
    }

    [Fact]
    public async Task SendPaymentRequest_EmailTooLong_ThrowsBadRequest()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var request = new SalesRequestsDto("nctTest", new string('a', 55) + "@email.com", "autoTest");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.SendPaymentRequestAsync(request, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent?.ToString().Should().Contain("Email");
    }

    [Fact]
    public async Task SendPaymentRequest_MessageTooLong_ThrowsBadRequest()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var request = new SalesRequestsDto("nctTest", "nct@email.com", new string('a', 256));

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.SendPaymentRequestAsync(request, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent?.ToString().Should().Contain("Message");
    }

    [Fact]
    public async Task SendPaymentRequest_EmptyUserName_ThrowsBadRequest()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var request = new SalesRequestsDto("", "nct@email.com", "autoTest");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.SendPaymentRequestAsync(request, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent?.ToString().Should().Contain("Incorrect firstname or lastname");
    }

    [Fact]
    public async Task SendPaymentRequest_EmptyEmail_ThrowsBadRequest()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var request = new SalesRequestsDto("nctTest", "", "autoTest");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.SendPaymentRequestAsync(request, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent?.ToString().Should().Contain("Incorrect email");
    }

    [Fact]
    public async Task SendPaymentRequest_EmptyMessage_ThrowsBadRequest()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var request = new SalesRequestsDto("nctTest", "nct@email.com", "");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.SendPaymentRequestAsync(request, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent?.ToString().Should().Contain("Message text is empty");
    }
}

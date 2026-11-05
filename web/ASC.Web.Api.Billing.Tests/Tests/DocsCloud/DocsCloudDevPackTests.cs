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


namespace ASC.Web.Api.Billing.Tests.Tests.DocsCloud;

/// <summary>
/// Moving a DocsCloud subscription to the developer pack: the price billing calculates for it
/// (<c>settings/docscloud/calculatedevpack</c>) and who may ask for it or make the switch
/// (<c>settings/docscloud/switchtodevpack</c>).
/// </summary>
public class DocsCloudDevPackTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task CalculateDevPack_DocsCloudSubscriber_ReturnsTheSwitchBillingCalculates()
    {
        // Arrange
        Payments.Customer(Owner.Email)
            .Paid(Plans.DocsCloudProductId, quantity: 5, DateTime.UtcNow.Date.AddMonths(1))
            .On(PaymentService.Billing, "POST", "/billing/CalculateSwitchSubscription",
                """{"OperationId":77,"Amount":42.5,"Currency":"USD","Quantity":12}""");

        // The calculation reads the cached tariff, so it is re-read once the subscription is paid, as a client does.
        await _portalQuotaApi.GetPortalTariffAsync(true, TestContext.Current.CancellationToken);

        // Act
        var calculation = (await _docsCloudApi.CalculateDevPackAsync(new DocsCloudDevPackRequestDto(12), TestContext.Current.CancellationToken)).Response;

        // Assert
        calculation.OperationId.Should().Be(77);
        calculation.Amount.Should().Be(42.5);
        calculation.Quantity.Should().Be(12);

        var request = PaymentCalls.Should().ContainSingle(call => call.Route == "/billing/CalculateSwitchSubscription").Subject;
        BillingValue(request, "FromProductId").Should().Be(Plans.DocsCloudProductId.ToString(CultureInfo.InvariantCulture));
        BillingValue(request, "ToProductId").Should().Be(Plans.DocsCloudDevPackProductId.ToString(CultureInfo.InvariantCulture));
        BillingValue(request, "ProductQty").Should().Be("12");
    }

    [Fact]
    public async Task CalculateDevPack_NoDocsCloudSubscription_ThrowsBadRequest()
    {
        // Arrange
        Payments.Customer(Owner.Email);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _docsCloudApi.CalculateDevPackAsync(new DocsCloudDevPackRequestDto(12), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent!.ToString().Should().Contain("DocsCloud subscription is not active");
    }

    [Fact]
    public async Task CalculateDevPack_FewerSeatsThanTheMinimum_ThrowsBadRequest()
    {
        // Arrange
        Payments.Customer(Owner.Email).Paid(Plans.DocsCloudProductId, quantity: 5, DateTime.UtcNow.Date.AddMonths(1));
        await _portalQuotaApi.GetPortalTariffAsync(true, TestContext.Current.CancellationToken);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _docsCloudApi.CalculateDevPackAsync(new DocsCloudDevPackRequestDto(1), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent!.ToString().Should().Contain("Invalid quantity");
        PaymentCalls.Should().NotContain(call => call.Route == "/billing/CalculateSwitchSubscription");
    }

    // On the stack without payment services these were refused as "not configured" before the role was looked at.
    [Theory]
    [MemberData(nameof(NonAdministratorRoles), MemberType = typeof(BaseTest))]
    public async Task CalculateDevPack_NotAnAdministrator_ThrowsForbidden(EmployeeType employeeType)
    {
        // Arrange
        Payments.Customer(Owner.Email).Paid(Plans.DocsCloudProductId, quantity: 5, DateTime.UtcNow.Date.AddMonths(1));
        var member = await InviteMember(employeeType);
        await _webApiClient.Authenticate(member);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _docsCloudApi.CalculateDevPackAsync(new DocsCloudDevPackRequestDto(12), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Theory]
    [MemberData(nameof(NonAdministratorRoles), MemberType = typeof(BaseTest))]
    public async Task SwitchToDevPack_NotAnAdministrator_ThrowsForbiddenWithoutCharging(EmployeeType employeeType)
    {
        // Arrange
        Payments.Customer(Owner.Email).Paid(Plans.DocsCloudProductId, quantity: 5, DateTime.UtcNow.Date.AddMonths(1));
        var member = await InviteMember(employeeType);
        await _webApiClient.Authenticate(member);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _docsCloudApi.SwitchToDevPackAsync(new DocsCloudDevPackRequestDto(12), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        PaymentCalls.Should().NotContain(call => call.Route == "/billing/SwitchSubscription");
    }
}

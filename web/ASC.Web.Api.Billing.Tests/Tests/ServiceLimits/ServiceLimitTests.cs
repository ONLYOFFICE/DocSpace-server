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


namespace ASC.Web.Api.Billing.Tests.Tests.ServiceLimits;

/// <summary>
/// The spending limits on the AI services (<c>payment/servicelimit</c>): created, read, changed and deleted through
/// accounting for a billing customer, and only ever the portal's own - a limit on another customer's account is
/// answered as missing.
/// </summary>
public class ServiceLimitTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task CreateServiceLimit_Total_IsReadBackAsThePortalsLimit()
    {
        // Arrange
        Payments.Customer(Owner.Email).Balance(10m);

        // Act
        var created = (await _paymentApi.CreateServiceLimitAsync(
            new CreateServiceLimitRequestDto(ServiceLimitScope.Total, amountValue: 50, period: ServiceLimitPeriod.Day),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        created.Scope.Should().Be(ServiceLimitScope.Total);
        created.AmountValue.Should().Be(50);
        created.Period.Should().Be(ServiceLimitPeriod.Day);
        created.CustomerAccountNumber.Should().Be(Payments.AccountNumber);

        var total = (await _paymentApi.GetTotalServiceLimitAsync(TestContext.Current.CancellationToken)).Response;
        total.Id.Should().Be(created.Id);

        var byId = (await _paymentApi.GetServiceLimitAsync(created.Id, TestContext.Current.CancellationToken)).Response;
        byId.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task CreateServiceLimit_Global_IsReadBackAsTheLimitForEveryUser()
    {
        // Arrange
        Payments.Customer(Owner.Email).Balance(10m);

        // Act
        var created = (await _paymentApi.CreateServiceLimitAsync(
            new CreateServiceLimitRequestDto(ServiceLimitScope.Global, amountValue: 20, period: ServiceLimitPeriod.Week),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        var global = (await _paymentApi.GetGlobalServiceLimitAsync(TestContext.Current.CancellationToken)).Response;
        global.Id.Should().Be(created.Id);
        global.Scope.Should().Be(ServiceLimitScope.Global);
        global.Period.Should().Be(ServiceLimitPeriod.Week);
    }

    [Fact]
    public async Task CreateServiceLimit_Personal_IsListedForTheUser()
    {
        // Arrange
        Payments.Customer(Owner.Email).Balance(10m);
        var user = await InviteMember(EmployeeType.User);

        // Act
        var created = (await _paymentApi.CreateServiceLimitAsync(
            new CreateServiceLimitRequestDto(ServiceLimitScope.Custom, userId: user.Id, amountValue: 5, period: ServiceLimitPeriod.Day),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        created.UserId.Should().Be(user.Id);

        var participants = (await _paymentApi.GetParticipantServiceLimitsAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        participants.Collection.Should().Contain(limit => limit.Id == created.Id && limit.UserId == user.Id);
    }

    [Fact]
    public async Task GetTotalServiceLimit_ComesWithTheSpendingOfTheCurrentPeriod()
    {
        // Arrange
        Payments.Customer(Owner.Email).Balance(10m);
        await _paymentApi.CreateServiceLimitAsync(
            new CreateServiceLimitRequestDto(ServiceLimitScope.Total, amountValue: 50, period: ServiceLimitPeriod.Day),
            TestContext.Current.CancellationToken);

        // Act
        var total = (await _paymentApi.GetTotalServiceLimitAsync(TestContext.Current.CancellationToken)).Response;

        // Assert: nothing has been spent yet, so the whole threshold is left for the day under way.
        total.Usage.Should().NotBeNull("the limit of the portal is read with its spending");
        total.Usage.AmountConsumed.Should().Be(0);
        total.Usage.AmountAvailable.Should().Be(50);
        total.Usage.PeriodStart.ToUniversalTime().Should().BeOnOrBefore(DateTime.UtcNow);
        total.Usage.PeriodEnd!.Value.ToUniversalTime().Should().BeOnOrAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task GetParticipantServiceLimits_ComeWithTheSpendingOfEachLimit()
    {
        // Arrange
        Payments.Customer(Owner.Email).Balance(10m);
        var user = await InviteMember(EmployeeType.User);
        await _paymentApi.SetParticipantServiceLimitsAsync(
            new SetParticipantServiceLimitsRequestDto([user.Id], quantityValue: 100, period: ServiceLimitPeriod.Month),
            TestContext.Current.CancellationToken);

        // Act
        var participants = (await _paymentApi.GetParticipantServiceLimitsAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        var limit = participants.Collection.Should().ContainSingle().Subject;
        limit.Usage.Should().NotBeNull("the list of the personal limits is read with their spending");
        limit.Usage.QuantityConsumed.Should().Be(0);
        limit.Usage.QuantityAvailable.Should().Be(100);
        limit.Usage.AmountAvailable.Should().BeNull("the limit has no money threshold");
    }

    [Fact]
    public async Task UpdateAndDeleteServiceLimit_OwnLimit_ChangesItThenRemovesIt()
    {
        // Arrange
        Payments.Customer(Owner.Email).Balance(10m);
        var created = (await _paymentApi.CreateServiceLimitAsync(
            new CreateServiceLimitRequestDto(ServiceLimitScope.Total, amountValue: 50, period: ServiceLimitPeriod.Day),
            TestContext.Current.CancellationToken)).Response;

        // Act
        var updated = (await _paymentApi.UpdateServiceLimitAsync(new UpdateServiceLimitRequestDto(created.Id, amountValue: 75),
            TestContext.Current.CancellationToken)).Response;
        await _paymentApi.DeleteServiceLimitAsync(created.Id, TestContext.Current.CancellationToken);

        // Assert
        updated.AmountValue.Should().Be(75);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.GetServiceLimitAsync(created.Id, TestContext.Current.CancellationToken));
        exception.ErrorCode.Should().Be(404);
        exception.ErrorContent!.ToString().Should().Contain("Service limit could not be found");
    }

    [Fact]
    public async Task GetServiceLimit_LimitOfAnotherCustomer_ThrowsNotFound()
    {
        // Arrange
        Payments.Customer(Owner.Email).Balance(10m);
        var foreignId = PaymentServices.AddServiceLimit(accountNumber: 1);

        // Act
        var read = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.GetServiceLimitAsync(foreignId, TestContext.Current.CancellationToken));
        var delete = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.DeleteServiceLimitAsync(foreignId, TestContext.Current.CancellationToken));

        // Assert
        read.ErrorCode.Should().Be(404);
        read.ErrorContent!.ToString().Should().Contain("Service limit could not be found");
        delete.ErrorCode.Should().Be(404);
        delete.ErrorContent!.ToString().Should().Contain("Service limit could not be found");
        PaymentServices.HasServiceLimit(foreignId).Should().BeTrue("a limit of another customer is never deleted");
    }

    [Fact]
    public async Task CreateServiceLimit_PortalIsNotACustomer_ThrowsNotFound()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.CreateServiceLimitAsync(
                new CreateServiceLimitRequestDto(ServiceLimitScope.Total, amountValue: 50, period: ServiceLimitPeriod.Day),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
        exception.ErrorContent!.ToString().Should().Contain("Customer could not be found");
    }
}

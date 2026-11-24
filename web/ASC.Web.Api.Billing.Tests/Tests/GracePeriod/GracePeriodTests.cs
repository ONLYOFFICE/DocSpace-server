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

namespace ASC.Web.Api.Billing.Tests.Tests.GracePeriod;

/// <summary>
/// The grace period of a portal whose billing customer pays by a delayed payment method (SEPA): the money of the
/// renewal reaches billing days after the charge, so the portal keeps working as on a paid tariff while
/// <c>GET portal/tariff</c> still reports the grace period. A card, no customer, or a grace period that ran out restrict
/// the portal as before.
/// </summary>
/// <remarks>
/// The counted action checked here is the invitation link of a Room admin (<c>POST portal/users/invitationlink</c>),
/// which asks the paid-user checker; this suite starts no Files service, so the room checker, which takes the same
/// decision (<c>Tariff.IsRestricted</c>), is covered by the unit test of that decision.
/// </remarks>
public class GracePeriodTests(AspireAppFixture fixture) : GracePeriodTestBase(fixture)
{
    private PortalUsersApi PortalUsers => field ??= new PortalUsersApi(_webApiClient,
        new Configuration { BasePath = _webApiClient.BaseAddress!.ToString().TrimEnd('/') });

    [Fact]
    public async Task GracePeriod_DelayedPaymentMethod_IsNotRestrictedAndAllowsCountedActions()
    {
        // Arrange
        await StartGracePeriodAsync(delayedPaymentMethod: true);

        // Act
        var (state, restricted) = await ReadTariffAsync();
        var link = (await PortalUsers.CreateInvitationLinkAsync(
            new InvitationLinkCreateRequestDto(EmployeeType.RoomAdmin), TestContext.Current.CancellationToken)).Response;
        var roomAdmin = await InviteMember(EmployeeType.RoomAdmin);

        // Assert
        state.Should().Be(TariffState.Delay);
        restricted.Should().BeFalse();
        link.EmployeeType.Should().Be(EmployeeType.RoomAdmin);
        link.Url.Should().NotBeNullOrEmpty();
        await _peopleClient.Authenticate(Owner);
        (await _profilesApi.GetProfileByUserIdAsync(roomAdmin.Id.ToString(), cancellationToken: TestContext.Current.CancellationToken))
            .Response.IsRoomAdmin.Should().BeTrue("a counted member added in this grace period keeps its paid type");
    }

    [Fact]
    public async Task GracePeriod_Card_IsRestrictedAndRefusesCountedActions()
    {
        // Arrange
        await StartGracePeriodAsync(delayedPaymentMethod: false);

        // Act
        var (state, restricted) = await ReadTariffAsync();
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await PortalUsers.CreateInvitationLinkAsync(
            new InvitationLinkCreateRequestDto(EmployeeType.RoomAdmin), TestContext.Current.CancellationToken));

        // Assert
        state.Should().Be(TariffState.Delay);
        restricted.Should().BeTrue();
        exception.ErrorCode.Should().Be(402);
        exception.ErrorContent!.ToString().Should().Contain("Your pricing plan does not support this option");
    }

    [Fact]
    public async Task GracePeriodOver_DelayedPaymentMethod_IsNotPaidAndTheGateAnswersPaymentRequired()
    {
        // Arrange
        Payments.Paid(Plans.AdminProductId, quantity: 10, DateTime.UtcNow.Date.AddYears(-1), Owner.Email)
            .Customer(Owner.Email, delayedPaymentMethod: true);
        await RefreshCustomerAsync();

        // Act
        var (state, restricted) = await ReadTariffAsync();
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _commonSettingsApi.GetPortalLogoAsync(TestContext.Current.CancellationToken));

        // Assert
        state.Should().Be(TariffState.NotPaid);
        restricted.Should().BeTrue();
        exception.ErrorCode.Should().Be(402);
    }

    [Fact]
    public async Task GracePeriod_PortalIsNotACustomer_IsRestricted()
    {
        // Arrange: billing answers "cannot find" for the customer of a portal nothing is scripted for
        Payments.Paid(Plans.AdminProductId, quantity: 10, DateTime.UtcNow.Date.AddDays(-2), Owner.Email);
        await RefreshCustomerAsync();

        // Act
        var (state, restricted) = await ReadTariffAsync();

        // Assert
        state.Should().Be(TariffState.Delay);
        restricted.Should().BeTrue();
        PaymentCalls.Should().Contain(call => call.Route == CustomerRoute);
    }

    [Fact]
    public async Task GracePeriod_BillingFailsAfterADelayedAnswer_StaysUnrestricted()
    {
        // Arrange
        await StartGracePeriodAsync(delayedPaymentMethod: true);
        (await ReadTariffAsync()).Restricted.Should().BeFalse();

        Payments.CustomerUnavailable();
        var customerCallsBefore = CustomerCalls();
        await RefreshCustomerAsync();

        // Act
        var (state, restricted) = await ReadTariffAsync();
        var link = (await PortalUsers.CreateInvitationLinkAsync(
            new InvitationLinkCreateRequestDto(EmployeeType.RoomAdmin), TestContext.Current.CancellationToken)).Response;

        // Assert
        CustomerCalls().Should().BeGreaterThan(customerCallsBefore, "the failing customer request reached billing");
        state.Should().Be(TariffState.Delay);
        restricted.Should().BeFalse("the last answer of billing still decides");
        link.EmployeeType.Should().Be(EmployeeType.RoomAdmin);
    }

    [Fact]
    public async Task GracePeriod_BillingFailsWithNoEarlierAnswer_IsRestricted()
    {
        // Arrange
        Payments.Paid(Plans.AdminProductId, quantity: 10, DateTime.UtcNow.Date.AddDays(-2), Owner.Email).CustomerUnavailable();
        await RefreshCustomerAsync();

        // Act
        var (state, restricted) = await ReadTariffAsync();

        // Assert
        PaymentCalls.Should().Contain(call => call.Route == CustomerRoute);
        state.Should().Be(TariffState.Delay);
        restricted.Should().BeTrue();
    }

    [Fact]
    public async Task GracePeriod_CustomerSwitchesToADelayedPaymentMethod_LiftsTheRestrictionOfTheCachedTariff()
    {
        // Arrange
        await StartGracePeriodAsync(delayedPaymentMethod: false);
        (await ReadTariffAsync()).Restricted.Should().BeTrue();

        // Act
        Payments.Customer(Owner.Email, delayedPaymentMethod: true);
        await RefreshCustomerAsync();

        // Assert: read from the cached tariff, as the restriction is worked out again on every read. This stack runs no
        // socket service, so the change-tariff event the switch sends is checked by the unit test of TariffService.
        var (state, restricted) = await ReadTariffAsync(refresh: false);
        state.Should().Be(TariffState.Delay);
        restricted.Should().BeFalse();
    }

    [Fact]
    public async Task GracePeriod_CustomerSwitchesToADelayedPaymentMethod_RefreshOfTheTariffLiftsTheRestriction()
    {
        // Arrange
        await StartGracePeriodAsync(delayedPaymentMethod: false);
        (await ReadTariffAsync()).Restricted.Should().BeTrue();
        var customerCallsBefore = CustomerCalls();

        // Act: only the tariff is read again; the customer is not
        Payments.Customer(Owner.Email, delayedPaymentMethod: true);
        var (state, restricted) = await ReadTariffAsync(refresh: true);

        // Assert
        CustomerCalls().Should().BeGreaterThan(customerCallsBefore, "a refresh of the tariff reads the customer again");
        state.Should().Be(TariffState.Delay);
        restricted.Should().BeFalse();
    }

    [Fact]
    public async Task GracePeriod_EnteredByTheRefreshAfterASwitchToADelayedPaymentMethod_IsNotRestricted()
    {
        // Arrange: a paid plan whose customer pays by card, both read and cached. The tariff goes first: saving it
        // drops the cached customer, so the customer read after it is the one in the cache when the Act starts.
        Payments.Paid(Plans.AdminProductId, quantity: 10, DateTime.UtcNow.Date.AddYears(1), Owner.Email).Customer(Owner.Email);
        (await ReadTariffAsync()).State.Should().Be(TariffState.Paid);
        await RefreshCustomerAsync();
        var customerCallsBefore = CustomerCalls();
        await _paymentApi.GetCustomerInfoAsync(false, TestContext.Current.CancellationToken);
        CustomerCalls().Should().Be(customerCallsBefore, "the card is cached");

        // Act: the plan lapsed and the customer moved to SEPA. The stored tariff is still paid, so the read enters the
        // grace period only once it has read the payments again; saving that tariff drops the cached card, so the
        // calculation that needs the customer reads it from billing.
        Payments.Paid(Plans.AdminProductId, quantity: 10, DateTime.UtcNow.Date.AddDays(-2), Owner.Email)
            .Customer(Owner.Email, delayedPaymentMethod: true);
        var (state, restricted) = await ReadTariffAsync(refresh: true);

        // Assert
        CustomerCalls().Should().Be(customerCallsBefore + 1, "the refresh reads the customer when the tariff enters the grace period");
        state.Should().Be(TariffState.Delay);
        restricted.Should().BeFalse();
    }

    [Fact]
    public async Task PaidPlan_IsNotRestricted()
    {
        // Arrange
        Payments.Paid(Plans.AdminProductId, quantity: 10, DateTime.UtcNow.Date.AddYears(1), Owner.Email).Customer(Owner.Email);

        // Act
        var (state, restricted) = await ReadTariffAsync();

        // Assert
        state.Should().Be(TariffState.Paid);
        restricted.Should().BeFalse();
    }
}

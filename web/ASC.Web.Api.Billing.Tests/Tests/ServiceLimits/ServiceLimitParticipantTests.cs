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
/// The personal spending limits set for many users at once (<c>PUT payment/servicelimit/participants</c>) and switched
/// off all together (<c>PUT payment/servicelimit/participants/disable</c>), as the list of the personal limits shows them.
/// </summary>
public class ServiceLimitParticipantTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task SetParticipantServiceLimits_Users_GivesEachTheLimit()
    {
        // Arrange
        Payments.Customer(Owner.Email).Balance(10m);
        var first = await InviteMember(EmployeeType.User);
        var second = await InviteMember(EmployeeType.RoomAdmin);

        // Act
        var count = (await _paymentApi.SetParticipantServiceLimitsAsync(
            new SetParticipantServiceLimitsRequestDto([first.Id, second.Id, first.Id], amountValue: 5, period: ServiceLimitPeriod.Week),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        count.Should().Be(2, "a user listed twice is limited once");

        var limits = await ParticipantLimitsAsync();
        limits.Select(limit => limit.UserId).Should().BeEquivalentTo([first.Id, second.Id]);
        limits.Should().AllSatisfy(limit =>
        {
            limit.AmountValue.Should().Be(5);
            limit.Period.Should().Be(ServiceLimitPeriod.Week);
            limit.Enabled.Should().BeTrue();
        });
    }

    [Fact]
    public async Task SetParticipantServiceLimits_UserWithASwitchedOffLimit_ReplacesItAndSwitchesItOn()
    {
        // Arrange
        Payments.Customer(Owner.Email).Balance(10m);
        var user = await InviteMember(EmployeeType.User);
        var created = (await _paymentApi.CreateServiceLimitAsync(
            new CreateServiceLimitRequestDto(ServiceLimitScope.Custom, userId: user.Id, amountValue: 5, period: ServiceLimitPeriod.Day),
            TestContext.Current.CancellationToken)).Response;
        await _paymentApi.UpdateServiceLimitAsync(new UpdateServiceLimitRequestDto(created.Id, enabled: false), TestContext.Current.CancellationToken);

        // Act
        await _paymentApi.SetParticipantServiceLimitsAsync(
            new SetParticipantServiceLimitsRequestDto([user.Id], quantityValue: 100, period: ServiceLimitPeriod.Month),
            TestContext.Current.CancellationToken);

        // Assert: the whole limit is set, so the money threshold left out is gone.
        var limit = (await ParticipantLimitsAsync()).Should().ContainSingle().Subject;
        limit.Id.Should().Be(created.Id);
        limit.Enabled.Should().BeTrue();
        limit.AmountValue.Should().BeNull();
        limit.QuantityValue.Should().Be(100);
        limit.Period.Should().Be(ServiceLimitPeriod.Month);
    }

    [Fact]
    public async Task DisableParticipantServiceLimits_SwitchesOffThePersonalLimitsOnly()
    {
        // Arrange
        Payments.Customer(Owner.Email).Balance(10m);
        var first = await InviteMember(EmployeeType.User);
        var second = await InviteMember(EmployeeType.User);
        await _paymentApi.SetParticipantServiceLimitsAsync(
            new SetParticipantServiceLimitsRequestDto([first.Id, second.Id], amountValue: 5, period: ServiceLimitPeriod.Day),
            TestContext.Current.CancellationToken);
        var total = (await _paymentApi.CreateServiceLimitAsync(
            new CreateServiceLimitRequestDto(ServiceLimitScope.Total, amountValue: 50, period: ServiceLimitPeriod.Day),
            TestContext.Current.CancellationToken)).Response;

        // Act
        var disabled = (await _paymentApi.DisableParticipantServiceLimitsAsync(TestContext.Current.CancellationToken)).Response;
        var disabledAgain = (await _paymentApi.DisableParticipantServiceLimitsAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        disabled.Should().Be(2);
        disabledAgain.Should().Be(0, "a repeated call switches off nothing more");
        (await ParticipantLimitsAsync()).Should().HaveCount(2).And.AllSatisfy(limit => limit.Enabled.Should().BeFalse());

        var portalLimit = (await _paymentApi.GetTotalServiceLimitAsync(TestContext.Current.CancellationToken)).Response;
        portalLimit.Id.Should().Be(total.Id);
        portalLimit.Enabled.Should().BeTrue("the limit of the portal as a whole is no personal limit");
    }

    private async Task<List<ServiceLimitDto>> ParticipantLimitsAsync()
    {
        return (await _paymentApi.GetParticipantServiceLimitsAsync(cancellationToken: TestContext.Current.CancellationToken)).Response.Collection;
    }
}

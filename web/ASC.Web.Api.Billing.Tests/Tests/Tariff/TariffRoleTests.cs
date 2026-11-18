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


namespace ASC.Web.Api.Billing.Tests.Tests.Tariff;

/// <summary>
/// What of a paid tariff (<c>GET portal/tariff</c>) each role reads: the state for everybody, the due dates from Room
/// admin up, the payment id, the customer and the quotas only with the portal-settings right.
/// </summary>
public class TariffRoleTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    private static readonly DateTime _endDate = DateTime.UtcNow.Date.AddYears(1);

    [Fact]
    public async Task GetPortalTariff_Owner_ReadsTheCustomerAndTheQuotas()
    {
        // Arrange
        Payments.Paid(Plans.AdminProductId, quantity: 10, _endDate, Owner.Email);

        // Act
        var tariff = (await _portalQuotaApi.GetPortalTariffAsync(true, TestContext.Current.CancellationToken)).Response;

        // Assert
        tariff.State.Should().Be(TariffState.Paid);
        tariff.Id.Should().NotBe(0);
        tariff.CustomerId.Should().Be(Owner.Email);
        tariff.DueDate.UtcTime.Date.Should().Be(_endDate);
        tariff.Quotas.Should().Contain(quota => quota.Id == Plans.AdminQuotaId);
    }

    [Fact]
    public async Task GetPortalTariff_RoomAdmin_ReadsTheDueDateButNotTheCustomer()
    {
        // Arrange
        Payments.Paid(Plans.AdminProductId, quantity: 10, _endDate, Owner.Email);
        var roomAdmin = await InviteMember(EmployeeType.RoomAdmin);
        await _webApiClient.Authenticate(roomAdmin);

        // Act
        var tariff = (await _portalQuotaApi.GetPortalTariffAsync(true, TestContext.Current.CancellationToken)).Response;

        // Assert
        tariff.State.Should().Be(TariffState.Paid);
        tariff.DueDate.Should().NotBeNull();
        tariff.DueDate.UtcTime.Date.Should().Be(_endDate);
        tariff.Id.Should().Be(0);
        tariff.CustomerId.Should().BeNull();
        tariff.Quotas.Should().BeNullOrEmpty();
    }

    [Theory]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task GetPortalTariff_BelowRoomAdmin_ReadsTheStateOnly(EmployeeType employeeType)
    {
        // Arrange
        Payments.Paid(Plans.AdminProductId, quantity: 10, _endDate, Owner.Email);
        var member = await InviteMember(employeeType);
        await _webApiClient.Authenticate(member);

        // Act
        var tariff = (await _portalQuotaApi.GetPortalTariffAsync(true, TestContext.Current.CancellationToken)).Response;

        // Assert
        tariff.State.Should().Be(TariffState.Paid);
        tariff.DueDate.Should().BeNull();
        tariff.DelayDueDate.Should().BeNull();
        tariff.CustomerId.Should().BeNull();
        tariff.Quotas.Should().BeNullOrEmpty();
    }
}

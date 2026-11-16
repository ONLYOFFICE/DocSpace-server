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
/// GET /api/2.0/portal/payment/customer/info on the stack without a billing service. The operation is for DocSpace
/// administrators: on a billing portal the roles below get 403 (<c>ASC.Web.Api.Billing.Tests</c>,
/// <c>CustomerPermissionsTests</c>), so they get it here too.
/// </summary>
/// <remarks>
/// Suspected defect W2 of the billing spec: without billing the action answers an empty result before any role check,
/// so every signed-in member, a guest included, gets 200.
/// </remarks>
[Trait("Category", "Portal")]
public class PaymentCustomerInfoAccessTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task GetCustomerInfo_OwnerWithoutBilling_ReturnsAnEmptyResult()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var info = (await _paymentApi.GetCustomerInfoAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        (info?.Email).Should().BeNullOrEmpty();
        (info?.Payer).Should().BeNull();
    }

    [Theory]
    [Trait("Category", "Bug")]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task GetCustomerInfo_NonAdministratorWithoutBilling_ThrowsForbidden(EmployeeType employeeType)
    {
        // Arrange
        var member = await InviteMember(employeeType);
        await _webApiClient.Authenticate(member);

        // Act
        var exception = await Record.ExceptionAsync(
            async () => await _paymentApi.GetCustomerInfoAsync(cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.Should().BeOfType<ApiException>("the access rule of an operation does not depend on the installation")
            .Which.ErrorCode.Should().Be(403);
    }
}

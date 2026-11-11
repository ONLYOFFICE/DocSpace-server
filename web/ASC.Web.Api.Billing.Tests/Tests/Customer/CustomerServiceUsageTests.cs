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

namespace ASC.Web.Api.Billing.Tests.Tests.Customer;

/// <summary>
/// The consumption of the wallet services added up per service (<c>GET payment/customer/usage</c>): each total with the
/// split of its amount between the money paid into the wallet and the bonuses, as accounting reports it.
/// </summary>
public class CustomerServiceUsageTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    private const string UsageRoute = "/customer/{portalId}/usage";

    [Fact]
    public async Task GetCustomerServiceUsage_TotalWithASplit_ReturnsItWithTheBonusPartMarked()
    {
        // Arrange
        Payments.Customer(Owner.Email)
            .On(PaymentService.Accounting, "GET", UsageRoute, """
                {"collection":[
                  {"service":"backup","currency":"USD","totalQuantity":1,"totalAmount":10,"operationCount":1,
                   "sources":[{"subAccount":53,"amount":4},{"subAccount":0,"amount":6}]},
                  {"service":"disk-storage","currency":"USD","totalQuantity":5,"totalAmount":2,"operationCount":1}
                ],"offset":0,"limit":25,"totalQuantity":2,"totalPage":1,"currentPage":1}
                """);

        // Act
        var report = (await _paymentApi.GetCustomerServiceUsageAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        var backup = report.Collection.Should().ContainSingle(u => u.Service == "backup").Subject;
        backup.Sources.Select(s => (s.SubAccount, s.IsBonus, s.Amount)).Should().Equal((53, true, 4.0), (0, false, 6.0));

        report.Collection.Should().ContainSingle(u => u.Service == "disk-storage")
            .Which.Sources.Should().BeNull("accounting reported that total without the split");
    }

    [Fact]
    public async Task GetCustomerServiceUsage_MergeByok_AddsTheSplitsUpPerSubAccount()
    {
        // Arrange
        Payments.Customer(Owner.Email)
            .On(PaymentService.Accounting, "GET", UsageRoute, """
                {"collection":[
                  {"service":"ai-tools","currency":"USD","totalQuantity":100,"totalAmount":15,"operationCount":2,
                   "sources":[{"subAccount":53,"amount":5},{"subAccount":0,"amount":10}]},
                  {"service":"ai-tools-byok","currency":"USD","totalQuantity":40,"totalAmount":3,"operationCount":1,
                   "sources":[{"subAccount":0,"amount":3}]}
                ],"offset":0,"limit":1000,"totalQuantity":2,"totalPage":1,"currentPage":1}
                """);

        // Act
        var report = (await _paymentApi.GetCustomerServiceUsageAsync(mergeByok: true, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        var aiTools = report.Collection.Should().ContainSingle().Subject;
        aiTools.Service.Should().Be("ai-tools");
        aiTools.TotalAmount.Should().Be(18);
        aiTools.Sources.Select(s => (s.SubAccount, s.Amount)).Should().BeEquivalentTo([(53, 5.0), (0, 13.0)]);
    }

    [Fact]
    public async Task GetCustomerServiceUsage_MergeByokWithATotalWithoutASplit_ReturnsNoSplit()
    {
        // Arrange
        Payments.Customer(Owner.Email)
            .On(PaymentService.Accounting, "GET", UsageRoute, """
                {"collection":[
                  {"service":"ai-tools","currency":"USD","totalQuantity":100,"totalAmount":15,"operationCount":2,
                   "sources":[{"subAccount":53,"amount":5},{"subAccount":0,"amount":10}]},
                  {"service":"ai-tools-byok","currency":"USD","totalQuantity":40,"totalAmount":3,"operationCount":1}
                ],"offset":0,"limit":1000,"totalQuantity":2,"totalPage":1,"currentPage":1}
                """);

        // Act
        var report = (await _paymentApi.GetCustomerServiceUsageAsync(mergeByok: true, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert: a split of one of the two totals would not add up to the merged amount.
        var aiTools = report.Collection.Should().ContainSingle().Subject;
        aiTools.TotalAmount.Should().Be(18);
        aiTools.Sources.Should().BeNull();
    }
}

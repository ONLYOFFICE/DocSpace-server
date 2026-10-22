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

namespace ASC.Core.Common.Tests;

/// <summary>
/// The reads of <see cref="TariffService"/> for the limit of the portal, the limit for every user and the page of
/// the user limits, including a portal the accounting service does not know yet.
/// </summary>
public class TariffServiceServiceLimitReadTests : TariffServiceServiceLimitTestBase
{
    [Fact]
    public async Task GetTotalServiceLimit_ReadsThePortalLimitWithItsUsage()
    {
        var (tariffService, handler) = await CreateTariffServiceAsync(new Balance { AccountNumber = OwnAccountNumber, SubAccounts = [] },
            _ => AccountingClientTests.Json(HttpStatusCode.OK,
                """{"id":41,"serviceGroupName":"ai-services","scope":"Total","amountValue":500,"currency":"USD","period":"Month","enabled":true,"usage":{"periodStart":"2026-10-01T00:00:00Z","amountConsumed":312.4,"amountAvailable":187.6,"quantityConsumed":0}}"""));

        var serviceLimit = await tariffService.GetTotalServiceLimitAsync(TenantId, "ai-services");

        serviceLimit.Scope.Should().Be(ServiceLimitScope.Total);
        serviceLimit.Usage.AmountAvailable.Should().Be(187.6m);
        handler.LastUri!.AbsolutePath.Should().Be($"/api/serviceLimit/by-customer/{PaymentRegion}{TenantId}/by-group/ai-services/total");
        AccountingClientTests.ParseQuery(handler.LastUri)["includeUsage"].Should().Be("true");
    }

    [Fact]
    public async Task GetTotalServiceLimit_NoneSet_ReturnsNull()
    {
        var (tariffService, _) = await CreateTariffServiceAsync(new Balance { AccountNumber = OwnAccountNumber, SubAccounts = [] },
            _ => AccountingClientTests.Json(HttpStatusCode.NotFound, """{"title":"Resource not found","status":404}"""));

        var serviceLimit = await tariffService.GetTotalServiceLimitAsync(TenantId, "ai-services");

        serviceLimit.Should().BeNull();
    }

    [Fact]
    public async Task GetGlobalServiceLimit_NoneSet_ReturnsNull()
    {
        var (tariffService, handler) = await CreateTariffServiceAsync(new Balance { AccountNumber = OwnAccountNumber, SubAccounts = [] },
            _ => AccountingClientTests.Json(HttpStatusCode.NotFound, """{"title":"Resource not found","status":404}"""));

        var serviceLimit = await tariffService.GetGlobalServiceLimitAsync(TenantId, "ai-services");

        serviceLimit.Should().BeNull();
        handler.LastUri!.AbsolutePath.Should().Be($"/api/serviceLimit/by-customer/{PaymentRegion}{TenantId}/by-group/ai-services/global");
    }

    [Fact]
    public async Task TotalAndGlobalLimits_PortalUnknownToTheAccountingService_ReturnNull()
    {
        // The accounting service answers an unknown customer with 400; for the portal that simply means no limit.
        var (tariffService, _) = await CreateTariffServiceAsync(new Balance(),
            _ => AccountingClientTests.Json(HttpStatusCode.BadRequest, """{"title":"Bad request","status":400,"detail":"Customer 'portal-1' not found"}"""));

        var total = await tariffService.GetTotalServiceLimitAsync(TenantId, "ai-services");
        var global = await tariffService.GetGlobalServiceLimitAsync(TenantId, "ai-services");

        total.Should().BeNull();
        global.Should().BeNull();
    }

    [Fact]
    public async Task GetParticipantServiceLimits_PortalUnknownToTheAccountingService_ReturnsAnEmptyPage()
    {
        var (tariffService, _) = await CreateTariffServiceAsync(new Balance(),
            _ => AccountingClientTests.Json(HttpStatusCode.BadRequest, """{"title":"Bad request","status":400,"detail":"Customer 'portal-1' not found"}"""));

        var report = await tariffService.GetParticipantServiceLimitsAsync(TenantId, "ai-services", new ServiceLimitFilter { Offset = 50, Limit = 10 });

        report.Collection.Should().BeEmpty();
        report.TotalQuantity.Should().Be(0);
        report.Offset.Should().Be(50);
        report.Limit.Should().Be(10);
    }
}

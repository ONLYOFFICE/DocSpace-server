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
/// The operations of <see cref="TariffService"/> that create or change service limits for the whole portal or for a
/// set of its users, on a portal that is a billing customer but not yet known to the accounting service - one whose
/// wallet was never topped up. The reads answer such a portal as one without limits; a change has nothing to act on
/// and is refused as a missing customer, which the API answers with 404, the same as for a portal without billing.
/// </summary>
/// <remarks>
/// Bug 84837: the accounting service's "customer not found" escaped from these operations unhandled, so
/// <c>POST servicelimit</c> and <c>PUT servicelimit/participants/disable</c> answered 500. The three operations now turn
/// it into <see cref="ItemNotFoundException"/>.
/// </remarks>
public class TariffServiceServiceLimitWriteTests : TariffServiceServiceLimitTestBase
{
    private const string CustomerNotFoundBody = """{"title":"Bad request","status":400,"detail":"Customer 'portal-1' not found"}""";

    [Fact]
    [Trait("Bug", "84837")]
    public async Task CreateServiceLimit_PortalUnknownToTheAccountingService_ThrowsCustomerNotFound()
    {
        var (tariffService, _) = await CreateTariffServiceAsync(new Balance(),
            _ => Reply(HttpStatusCode.BadRequest, CustomerNotFoundBody));

        var act = async () => await tariffService.CreateServiceLimitAsync(TenantId, "ai-services", ServiceLimitScope.Total, null, 1m, null, ServiceLimitPeriod.Month);

        await act.Should().ThrowExactlyAsync<ItemNotFoundException>().WithMessage("Customer could not be found");
    }

    [Fact]
    [Trait("Bug", "84837")]
    public async Task SetParticipantServiceLimits_PortalUnknownToTheAccountingService_ThrowsCustomerNotFound()
    {
        var (tariffService, _) = await CreateTariffServiceAsync(new Balance(),
            _ => Reply(HttpStatusCode.BadRequest, CustomerNotFoundBody));

        var act = async () => await tariffService.SetParticipantServiceLimitsAsync(TenantId, "ai-services", [UserA], 1m, null, ServiceLimitPeriod.Month);

        await act.Should().ThrowExactlyAsync<ItemNotFoundException>().WithMessage("Customer could not be found");
    }

    [Fact]
    [Trait("Bug", "84837")]
    public async Task DisableParticipantServiceLimits_PortalUnknownToTheAccountingService_ThrowsCustomerNotFound()
    {
        var (tariffService, _) = await CreateTariffServiceAsync(new Balance(),
            _ => Reply(HttpStatusCode.BadRequest, CustomerNotFoundBody));

        var act = async () => await tariffService.DisableParticipantServiceLimitsAsync(TenantId, "ai-services");

        await act.Should().ThrowExactlyAsync<ItemNotFoundException>().WithMessage("Customer could not be found");
    }

    [Fact]
    public async Task CreateServiceLimit_ServiceGroupUnknownToTheAccountingService_IsNotTakenForAMissingCustomer()
    {
        // A 400 "not found" that names something other than the customer stays the caller's mistake, answered with 400.
        var (tariffService, _) = await CreateTariffServiceAsync(new Balance { AccountNumber = OwnAccountNumber, SubAccounts = [] },
            _ => Reply(HttpStatusCode.BadRequest, """{"title":"Bad request","status":400,"detail":"Service group 'ai-services' not found"}"""));

        var act = async () => await tariffService.CreateServiceLimitAsync(TenantId, "ai-services", ServiceLimitScope.Total, null, 1m, null, ServiceLimitPeriod.Month);

        await act.Should().ThrowExactlyAsync<AccountingBadRequestException>();
    }
}

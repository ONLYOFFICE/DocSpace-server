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
/// The owner check of <see cref="TariffService"/> for service limits addressed by ID. The accounting service
/// looks a limit up by its global ID, so a limit of another portal must never be returned or changed: the
/// limit's <c>customerAccountNumber</c> has to match the account number of the tenant's billing customer.
/// </summary>
public class TariffServiceServiceLimitOwnerTests : TariffServiceServiceLimitTestBase
{
    [Fact]
    public async Task GetServiceLimit_LimitOfThisPortal_ReturnsIt()
    {
        var (tariffService, _) = await CreateTariffServiceAsync(new Balance { AccountNumber = OwnAccountNumber, SubAccounts = [] },
            _ => ServiceLimitResponse(OwnAccountNumber));

        var serviceLimit = await tariffService.GetServiceLimitAsync(TenantId, 42);

        serviceLimit.Should().NotBeNull();
        serviceLimit.Id.Should().Be(42);
    }

    [Fact]
    public async Task GetServiceLimit_LimitOfAnotherPortal_ReturnsNull()
    {
        var (tariffService, _) = await CreateTariffServiceAsync(new Balance { AccountNumber = OwnAccountNumber, SubAccounts = [] },
            _ => ServiceLimitResponse(OtherAccountNumber));

        var serviceLimit = await tariffService.GetServiceLimitAsync(TenantId, 42);

        serviceLimit.Should().BeNull();
    }

    [Fact]
    public async Task GetServiceLimit_PortalWithoutBillingCustomer_ReturnsNull()
    {
        // An empty balance is what TariffService caches for a portal the accounting service does not know.
        var (tariffService, _) = await CreateTariffServiceAsync(new Balance(),
            _ => ServiceLimitResponse(OwnAccountNumber));

        var serviceLimit = await tariffService.GetServiceLimitAsync(TenantId, 42);

        serviceLimit.Should().BeNull();
    }

    [Fact]
    public async Task GetServiceLimit_UnknownId_ReturnsNull()
    {
        var (tariffService, _) = await CreateTariffServiceAsync(new Balance { AccountNumber = OwnAccountNumber, SubAccounts = [] },
            _ => AccountingClientTests.Json(HttpStatusCode.NotFound, """{"title":"Resource not found","status":404}"""));

        var serviceLimit = await tariffService.GetServiceLimitAsync(TenantId, 999999);

        serviceLimit.Should().BeNull();
    }

    [Fact]
    public async Task UpdateServiceLimit_LimitOfThisPortal_SendsUpdate()
    {
        var (tariffService, handler) = await CreateTariffServiceAsync(new Balance { AccountNumber = OwnAccountNumber, SubAccounts = [] },
            _ => ServiceLimitResponse(OwnAccountNumber));

        var serviceLimit = await tariffService.UpdateServiceLimitAsync(TenantId, 42, 100m, null, null);

        serviceLimit.Should().NotBeNull();
        handler.CallCount.Should().Be(2);
        handler.LastMethod.Should().Be(HttpMethod.Put);
    }

    [Fact]
    public async Task UpdateServiceLimit_LimitOfAnotherPortal_ReturnsNullWithoutSendingUpdate()
    {
        var (tariffService, handler) = await CreateTariffServiceAsync(new Balance { AccountNumber = OwnAccountNumber, SubAccounts = [] },
            _ => ServiceLimitResponse(OtherAccountNumber));

        var serviceLimit = await tariffService.UpdateServiceLimitAsync(TenantId, 42, 100m, null, false);

        serviceLimit.Should().BeNull();
        handler.CallCount.Should().Be(1);
        handler.LastMethod.Should().Be(HttpMethod.Get);
    }

    [Fact]
    public async Task DeleteServiceLimit_LimitOfThisPortal_DeletesAndReturnsIt()
    {
        var (tariffService, handler) = await CreateTariffServiceAsync(new Balance { AccountNumber = OwnAccountNumber, SubAccounts = [] },
            request => request.Method == HttpMethod.Delete ? new HttpResponseMessage(HttpStatusCode.NoContent) : ServiceLimitResponse(OwnAccountNumber));

        var deleted = await tariffService.DeleteServiceLimitAsync(TenantId, 42);

        deleted.Should().NotBeNull();
        deleted.Id.Should().Be(42);
        handler.CallCount.Should().Be(2);
        handler.LastMethod.Should().Be(HttpMethod.Delete);
    }

    [Fact]
    public async Task DeleteServiceLimit_LimitOfAnotherPortal_ReturnsNullWithoutDeleting()
    {
        // A limit is deleted by its global ID, so the owner is checked first and a foreign limit is never deleted.
        var (tariffService, handler) = await CreateTariffServiceAsync(new Balance { AccountNumber = OwnAccountNumber, SubAccounts = [] },
            request => request.Method == HttpMethod.Delete ? new HttpResponseMessage(HttpStatusCode.NoContent) : ServiceLimitResponse(OtherAccountNumber));

        var deleted = await tariffService.DeleteServiceLimitAsync(TenantId, 42);

        deleted.Should().BeNull();
        handler.CallCount.Should().Be(1);
        handler.LastMethod.Should().Be(HttpMethod.Get);
    }

    [Fact]
    public async Task DeleteServiceLimit_AlreadyDeletedByAConcurrentRequest_ReturnsNull()
    {
        // The accounting service answers a deletion of a limit it does not have with 400, not 404.
        var (tariffService, handler) = await CreateTariffServiceAsync(new Balance { AccountNumber = OwnAccountNumber, SubAccounts = [] },
            request => request.Method == HttpMethod.Delete
                ? AccountingClientTests.Json(HttpStatusCode.BadRequest, ServiceLimitNotFoundBody)
                : ServiceLimitResponse(OwnAccountNumber));

        var deleted = await tariffService.DeleteServiceLimitAsync(TenantId, 42);

        deleted.Should().BeNull();
        handler.CallCount.Should().Be(2);
    }

    [Fact]
    public async Task UpdateServiceLimit_AlreadyDeletedByAConcurrentRequest_ReturnsNull()
    {
        // The accounting service answers an update of a limit it does not have with 400, not 404.
        var (tariffService, handler) = await CreateTariffServiceAsync(new Balance { AccountNumber = OwnAccountNumber, SubAccounts = [] },
            request => request.Method == HttpMethod.Put
                ? AccountingClientTests.Json(HttpStatusCode.BadRequest, ServiceLimitNotFoundBody)
                : ServiceLimitResponse(OwnAccountNumber));

        var updated = await tariffService.UpdateServiceLimitAsync(TenantId, 42, 100m, null, null);

        updated.Should().BeNull();
        handler.CallCount.Should().Be(2);
    }

    [Fact]
    public async Task UpdateServiceLimit_RejectedThreshold_ThrowsBadRequest()
    {
        // A 400 that explains a mistake of the caller must reach the caller, not turn into "limit not found".
        var (tariffService, _) = await CreateTariffServiceAsync(new Balance { AccountNumber = OwnAccountNumber, SubAccounts = [] },
            request => request.Method == HttpMethod.Put
                ? AccountingClientTests.Json(HttpStatusCode.BadRequest,
                    """{"title":"Bad request","status":400,"detail":"Services of group 'ai-services' have different units, a quantity threshold can not be set"}""")
                : ServiceLimitResponse(OwnAccountNumber));

        var act = async () => await tariffService.UpdateServiceLimitAsync(TenantId, 42, null, 1000, null);

        await act.Should().ThrowExactlyAsync<AccountingBadRequestException>();
    }
}

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
/// POST /api/2.0/portal/payment/servicelimit — validation of the limit to create. The action itself needs a
/// configured billing service, which this environment does not have, but the body is validated before the action
/// runs, so a rejected body answers 400 here just as it does on a billing portal, while an accepted one only gets
/// as far as the configuration check.
/// </summary>
[Trait("Category", "Portal")]
public class PaymentServiceLimitValidationTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    /// <summary>
    /// A body without `scope` used to pass the validation and was read as a limit for the whole portal, because the
    /// request field defaulted to `Total`, so a client that forgot the field set a spending ceiling for every user.
    /// The field has no default now and is required, so such a body is refused with 400 before the action runs.
    /// </summary>
    [Fact]
    [Trait("Bug", "84839")]
    public async Task CreateServiceLimit_ScopeMissing_ReturnsBadRequest()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.CreateServiceLimitAsync(
                new CreateServiceLimitRequestDto(amountValue: 5, period: ServiceLimitPeriod.Month),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent?.ToString().Should().Contain("Scope");
    }

    [Theory]
    [InlineData(ServiceLimitScope.Total)]
    [InlineData(ServiceLimitScope.Global)]
    public async Task CreateServiceLimit_ScopeGiven_PassesValidation(ServiceLimitScope scope)
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.CreateServiceLimitAsync(
                new CreateServiceLimitRequestDto(scope: scope, amountValue: 5, period: ServiceLimitPeriod.Month),
                TestContext.Current.CancellationToken));

        // Assert
        // The body is accepted and the request reaches the configuration check, which this environment without a
        // billing service refuses.
        exception.ErrorCode.Should().Be(403);
    }
}

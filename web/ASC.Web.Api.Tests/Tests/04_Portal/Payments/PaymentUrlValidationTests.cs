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
/// PUT /api/2.0/portal/payment/url — validation of the request body. The action itself first demands a
/// configured billing service, which this environment does not have, but the body is validated before the
/// action runs, so a rejected body answers 400 here just as it does on a billing portal, while an accepted
/// one only gets as far as the configuration check (403).
/// </summary>
/// <remarks>
/// Divergences from the TypeScript suite, which runs against a billing backend: the "Invalid quantity"
/// checks for a quantity of 0 and a negative quantity (bug 81436) and the per-role "Access denied" checks
/// live in the action body after <c>PaymentHelper.DemandConfigured</c>, so here every caller, Owner included,
/// gets 403 "Tariff service is not configured" and those cases cannot be reproduced. The skipped
/// open-redirect case (an external <c>backUrl</c>, also filed under 81433) is not ported either: the product
/// deliberately lets a client supply its own thank-you page.
/// </remarks>
[Trait("Category", "Portal")]
public class PaymentUrlValidationTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    private const string ValidUrl = "https://example.com/payment";

    private static Dictionary<string, int> OneProduct => new() { ["admin"] = 1 };

    /// <summary>
    /// <c>backUrl</c> had no format check, so a string that is not a URL was carried into the checkout link.
    /// It must be a fully-qualified http, https or ftp URL.
    /// </summary>
    [Fact]
    [Trait("Bug", "81433")]
    public async Task GetPaymentUrl_BackUrlNotAUrl_ReturnsBadRequest()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var request = new PaymentUrlRequestDto("notaurl", ValidUrl, OneProduct);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.GetPaymentUrlAsync(request, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent?.ToString().Should().Contain("BackUrl");
    }

    /// <summary>
    /// <c>backUrl</c> had no length limit, so an 18000-character value (above the nginx 16k header buffer) was
    /// accepted. It is limited to 255 characters.
    /// </summary>
    [Fact]
    [Trait("Bug", "81434")]
    public async Task GetPaymentUrl_BackUrlLongerThan255_ReturnsBadRequest()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var longUrl = "https://" + string.Concat(Enumerable.Repeat("evil", 4497)) + ".com";
        var request = new PaymentUrlRequestDto(longUrl, ValidUrl, OneProduct);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.GetPaymentUrlAsync(request, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent?.ToString().Should().Contain("BackUrl");
    }

    /// <summary>
    /// A 255-character <c>backUrl</c> is exactly on the limit and must still pass the validation.
    /// </summary>
    [Fact]
    [Trait("Bug", "81434")]
    public async Task GetPaymentUrl_BackUrlOf255Chars_PassesValidation()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        const string prefix = "https://example.com/";
        var request = new PaymentUrlRequestDto(prefix + new string('a', 255 - prefix.Length), ValidUrl, OneProduct);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.GetPaymentUrlAsync(request, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetPaymentUrl_SuccessUrlNotAUrl_ReturnsBadRequest()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var request = new PaymentUrlRequestDto(ValidUrl, "notaurl", OneProduct);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.GetPaymentUrlAsync(request, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent?.ToString().Should().Contain("SuccessUrl");
    }

    [Fact]
    public async Task GetPaymentUrl_SuccessUrlLongerThan255_ReturnsBadRequest()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var request = new PaymentUrlRequestDto(ValidUrl, "https://example.com/" + new string('a', 256), OneProduct);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.GetPaymentUrlAsync(request, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent?.ToString().Should().Contain("SuccessUrl");
    }

    [Fact]
    public async Task GetPaymentUrl_EmptyQuantity_ReturnsBadRequest()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var request = new PaymentUrlRequestDto(ValidUrl, ValidUrl, []);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.GetPaymentUrlAsync(request, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent?.ToString().Should().Contain("Quantity");
    }

    [Fact]
    public async Task GetPaymentUrl_TwoProducts_ReturnsBadRequest()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var request = new PaymentUrlRequestDto(
            ValidUrl,
            ValidUrl,
            new Dictionary<string, int> { ["admin"] = 1, ["startup"] = 1 });

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.GetPaymentUrlAsync(request, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent?.ToString().Should().Contain("Quantity");
    }

    [Fact]
    public async Task GetPaymentUrl_ValidBody_PassesValidation()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var request = new PaymentUrlRequestDto(ValidUrl, ValidUrl, OneProduct);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.GetPaymentUrlAsync(request, TestContext.Current.CancellationToken));

        // Assert
        // The body is accepted and the request reaches the configuration check, which this environment
        // without a billing service refuses.
        exception.ErrorCode.Should().Be(403);
    }
}

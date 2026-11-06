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


namespace ASC.Web.Api.Billing.Tests.Tests;

/// <summary>
/// The payment services stub checks the signature the services put on every request, as the real services do: a stack
/// given another key or secret, or a client that signs another way, fails here instead of passing unnoticed. That the
/// stack signs right is shown by every other test of the suite, whose requests would all be refused otherwise.
/// </summary>
public class PaymentServiceStubTests(AspireAppFixture fixture)
{
    public static TheoryData<PaymentService> Services => [PaymentService.Billing, PaymentService.Accounting, PaymentService.DocsCloud];

    [Fact]
    public async Task Request_WithoutASignature_IsRefusedWithUnauthorized()
    {
        // Arrange
        using var client = fixture.Payments.CreateClient();

        // Act
        using var response = await client.GetAsync(Constants.PaymentStubAccountingPath + "/currency/all", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [MemberData(nameof(Services))]
    public void Signature_OfTheStacksKeyAndSecret_IsAccepted(PaymentService service)
    {
        PaymentServiceStub.IsSigned(service, Sign(service, Constants.PaymentStubKey, Constants.PaymentStubSecret)).Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(Services))]
    public void Signature_OfAnotherSecret_IsRefused(PaymentService service)
    {
        PaymentServiceStub.IsSigned(service, Sign(service, Constants.PaymentStubKey, "another-secret")).Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(Services))]
    public void Signature_OfAnotherKey_IsRefused(PaymentService service)
    {
        PaymentServiceStub.IsSigned(service, Sign(service, "another-key", Constants.PaymentStubSecret)).Should().BeFalse();
    }

    [Fact]
    public void Signature_InTheAlphabetOfAnotherService_IsRefused()
    {
        // Billing and accounting sign in Base64Url, docscloud in standard Base64 with its padding.
        PaymentServiceStub.IsSigned(PaymentService.Billing, Sign(PaymentService.DocsCloud, Constants.PaymentStubKey, Constants.PaymentStubSecret))
            .Should().BeFalse();
    }

    // As the AuthHandler of the service's client signs a request.
    private static string Sign(PaymentService service, string key, string secret)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        var hash = HMACSHA1.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes($"{timestamp}\n{key}"));

        return $"ASC {key}:{timestamp}:{(service == PaymentService.DocsCloud ? Convert.ToBase64String(hash) : Base64Url.EncodeToString(hash))}";
    }
}

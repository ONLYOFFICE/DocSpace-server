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
/// PUT /api/2.0/portal/payment/ai-model/restrictions — validation of the restricted model set. The action
/// itself needs a configured billing service and AI gateway, which this environment does not have, but the
/// body is validated before the action runs, so a rejected body answers 400 here just as it does on a billing
/// portal, while an accepted one only gets as far as the configuration check.
/// </summary>
[Trait("Category", "Portal")]
public class PaymentAiModelRestrictionsValidationTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    /// <summary>
    /// A null element in `models` used to pass the binding, unlike the other wrong-typed elements
    /// (`123`, `true`, `{}`), and was then dropped on the way to the AI gateway, so the request replaced
    /// the stored restrictions with an empty set and answered 200. The request body now rejects a null entry
    /// with 400 before the action runs.
    /// </summary>
    [Fact]
    [Trait("Bug", "84317")]
    public async Task SetRestrictedAiModels_NullElement_ReturnsBadRequest()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.SetRestrictedAiModelsAsync(
                new SetRestrictedAiModelsRequestDto([null!]),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent?.ToString().Should().Contain("Models");
    }

    [Fact]
    [Trait("Bug", "84317")]
    public async Task SetRestrictedAiModels_NullElementBesideValidId_ReturnsBadRequest()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.SetRestrictedAiModelsAsync(
                new SetRestrictedAiModelsRequestDto(["openai/gpt-4o", null!]),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task SetRestrictedAiModels_ValidSet_PassesValidation()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.SetRestrictedAiModelsAsync(
                new SetRestrictedAiModelsRequestDto(["openai/gpt-4o"]),
                TestContext.Current.CancellationToken));

        // Assert
        // The body is accepted and the request reaches the configuration check, which this environment
        // without a billing service and an AI gateway refuses.
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task SetRestrictedAiModels_EmptySet_PassesValidation()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.SetRestrictedAiModelsAsync(
                new SetRestrictedAiModelsRequestDto([]),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }
}

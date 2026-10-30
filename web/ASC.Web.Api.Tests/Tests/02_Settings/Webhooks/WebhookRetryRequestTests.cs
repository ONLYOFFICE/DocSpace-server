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

namespace ASC.Web.Api.Tests.Tests._02_Settings.Webhooks;

/// <summary>
/// PUT /api/2.0/settings/webhook/retry — the request body itself. A null or missing <c>ids</c> is read as a list of
/// nothing to retry, as an empty list is, instead of failing the call.
/// </summary>
[Trait("Category", "Settings")]
public class WebhookRetryRequestTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task RetryWebhooks_NullIds_ReturnsEmptyResult()
    {
        // Arrange: the SDK model sends an unset list as "ids": null
        await _webApiClient.Authenticate(Owner);

        // Act
        var result = await _webhooksApi.RetryWebhooksAsync(new WebhookRetryRequestDto(), TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(200);
        result.Response.Should().BeEmpty();
    }

    [Fact]
    public async Task RetryWebhooks_100Ids_IsAccepted()
    {
        // Arrange: the most one call takes; none of them names a record, so nothing is queued
        await _webApiClient.Authenticate(Owner);
        var ids = Enumerable.Range(int.MaxValue - 99, 100).ToList();

        // Act
        var result = await _webhooksApi.RetryWebhooksAsync(new WebhookRetryRequestDto(ids), TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(200);
        result.Response.Should().BeEmpty();
    }

    [Fact]
    public async Task RetryWebhooks_MoreThan100Ids_ThrowsBadRequest()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var ids = Enumerable.Range(int.MaxValue - 100, 101).ToList();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _webhooksApi.RetryWebhooksAsync(new WebhookRetryRequestDto(ids), TestContext.Current.CancellationToken));

        // Assert: a longer batch has to be split into several calls
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task RetryWebhooks_BodyWithoutIds_ReturnsEmptyResult()
    {
        // Arrange: a body with no ids property at all, which the typed model always writes (tests.md, raw carve-outs)
        await _webApiClient.Authenticate(Owner);

        // Act
        using var response = await _webApi.PutRawAsync("api/2.0/settings/webhook/retry", "{}", TestContext.Current.CancellationToken);

        // Assert
        var records = await _webApi.ReadEnvelopeAsync<List<JsonElement>>(response, TestContext.Current.CancellationToken);
        records.Response.Should().BeEmpty();
    }
}

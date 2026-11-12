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
/// Contract tests for <see cref="AccountingClient.CreditBonusAsync"/>: the request to <c>operation/bonus-credit</c>, the
/// operation read from the answer, and a refused credit told apart from the other failures.
/// </summary>
public class AccountingClientBonusCreditTests
{
    [Fact]
    public async Task CreditBonus_PostsTheCreditAndReadsTheOperation()
    {
        var (client, handler) = CreateAccountingClient(_ => Reply(HttpStatusCode.OK,
            """{"operationId":987654,"quantity":1,"amount":10.5,"currency":"USD"}"""));

        var result = await client.CreditBonusAsync("portal-1", 10.5m, "USD", "promo-K7QX",
            new DateTime(2027, 2, 7, 12, 0, 0, DateTimeKind.Utc), new Dictionary<string, string> { ["source"] = "promo" });

        result.OperationId.Should().Be(987654);
        result.Amount.Should().Be(10.5m);
        result.Currency.Should().Be("USD");

        handler.LastMethod.Should().Be(HttpMethod.Post);
        handler.LastUri!.AbsolutePath.Should().Be("/api/operation/bonus-credit");

        using var body = JsonDocument.Parse(handler.LastRequestBody!);
        var root = body.RootElement;
        root.GetProperty("customerName").GetString().Should().Be("portal-1");
        root.GetProperty("externalRef").GetString().Should().Be("promo-K7QX");
        root.GetProperty("sum").GetDecimal().Should().Be(10.5m);
        root.GetProperty("currency").GetString().Should().Be("USD");
        root.GetProperty("expiresAt").GetDateTime().ToUniversalTime().Should().Be(new DateTime(2027, 2, 7, 12, 0, 0, DateTimeKind.Utc));
        root.GetProperty("metadata").GetProperty("source").GetString().Should().Be("promo");
    }

    [Fact]
    public async Task CreditBonus_RefusedAsConflicting_ThrowsConflictWithTheReason()
    {
        var (client, _) = CreateAccountingClient(_ => Reply(HttpStatusCode.Conflict,
            """{"title":"Conflict","status":409,"detail":"Operation with external reference 'promo-K7QX' already exists","instance":"/api/operation/bonus-credit"}"""));

        var act = async () => await client.CreditBonusAsync("portal-1", 10m, "USD", "promo-K7QX");

        (await act.Should().ThrowExactlyAsync<AccountingConflictException>())
            .Which.Message.Should().Be("Operation with external reference 'promo-K7QX' already exists");
    }

    [Fact]
    public async Task CreditBonus_ServerError_StaysAPlainFailure()
    {
        // Whether the bonus was credited is unknown after a 5xx, so it is no refusal the caller could show.
        var (client, _) = CreateAccountingClient(_ => Reply(HttpStatusCode.InternalServerError,
            """{"title":"Internal Server Error","status":500}"""));

        var act = async () => await client.CreditBonusAsync("portal-1", 10m, "USD", "promo-K7QX");

        await act.Should().ThrowExactlyAsync<AccountingException>();
    }
}

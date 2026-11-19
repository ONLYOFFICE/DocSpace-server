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
/// The decisions of the wallet bonus that the billing stack cannot reach: which bonus pays (an ended credit does not),
/// how the bonus part of a movement splits into the report columns, and which promo code answer has a new balance.
/// </summary>
public class WalletBonusDecisionsTests
{
    private static readonly DateTime _now = new(2026, 11, 17, 12, 0, 0, DateTimeKind.Utc);

    // The end of a credit against now: ended yesterday, an hour ago, ends in an hour, tomorrow, never.
    [Theory]
    [InlineData(-24 * 60, 0)]
    [InlineData(-60, 0)]
    [InlineData(0, 0)]
    [InlineData(60, 50)]
    [InlineData(24 * 60, 50)]
    [InlineData(null, 50)]
    public void SpendableAmount_CountsACreditOnlyUntilItEnds(int? endsInMinutes, double expected)
    {
        var goodwill = Goodwill(50, Lot(50, "USD", endsInMinutes));

        goodwill.SpendableAmount("USD", _now).Should().Be((decimal)expected);
    }

    // An end without an offset is UTC: read as the host's local time, it would move by the host's offset.
    [Theory]
    [InlineData(-60, 0)]
    [InlineData(60, 50)]
    public void SpendableAmount_EndWithoutAnOffset_IsReadAsUtc(int endsInMinutes, double expected)
    {
        var lot = new GoodwillLot
        {
            Amount = 50,
            Remaining = 50,
            Currency = "USD",
            ExpiresAt = DateTime.SpecifyKind(_now.AddMinutes(endsInMinutes), DateTimeKind.Unspecified)
        };

        Goodwill(50, lot).SpendableAmount("USD", _now).Should().Be((decimal)expected);
    }

    [Fact]
    public void SpendableAmount_AddsUpTheCreditsThatHaveNotEnded()
    {
        var goodwill = Goodwill(70, Lot(30, "USD", -60), Lot(25, "USD", 60), Lot(15, "USD", null));

        goodwill.SpendableAmount("USD", _now).Should().Be(40);
    }

    [Fact]
    public void SpendableAmount_NeverMoreThanTheSubAccount()
    {
        var goodwill = Goodwill(10, Lot(30, "USD", null));

        goodwill.SpendableAmount("USD", _now).Should().Be(10);
    }

    [Fact]
    public void SpendableAmount_CreditInAnotherCurrency_IsNotCounted()
    {
        var goodwill = Goodwill(50, Lot(50, "EUR", null));

        goodwill.SpendableAmount("USD", _now).Should().Be(0);
    }

    [Fact]
    public void SpendableAmount_AnswerWithoutCredits_CountsTheSubAccount()
    {
        var goodwill = new GoodwillBalance { SubAccounts = [new SubAccount { Currency = "USD", Amount = 50 }] };

        goodwill.SpendableAmount("USD", _now).Should().Be(50);
    }

    [Fact]
    public void SpendableAmount_NegativeSubAccount_IsNone()
    {
        var goodwill = Goodwill(-20, Lot(10, "USD", null));

        goodwill.SpendableAmount("USD", _now).Should().Be(0);
    }

    // The bonus part goes to each side above 0, and a side of 0 gets 0; a minus from accounting comes out unsigned.
    [Theory]
    [InlineData(10.0, 0.0, -4.0, 4.0, 0.0)]
    [InlineData(0.0, 10.0, -4.0, 0.0, 4.0)]
    [InlineData(0.0, 4.5, -4.5, 0.0, 4.5)]
    [InlineData(10.0, 0.0, null, 0.0, 0.0)]
    [InlineData(10.0, 6.0, -4.0, 4.0, 4.0)]
    [InlineData(0.0, 0.0, -4.0, 0.0, 0.0)]
    public void BonusPartsOf_PutsTheBonusOnTheSideOfTheMovement(double credit, double debit, double? bonus, double expectedCredit, double expectedDebit)
    {
        List<OperationSource>? sources = bonus == null
            ? null
            : [new OperationSource { SubAccount = OperationSource.BonusSubAccount, Amount = (decimal)bonus }, new OperationSource { SubAccount = 0, Amount = -1 }];

        var parts = OperationSource.BonusPartsOf((decimal)credit, (decimal)debit, sources!);

        parts.Should().Be(((decimal)expectedCredit, (decimal)expectedDebit));
    }

    // A wallet never topped up has no sub-account, also with a bonus credited: the bonus pays the purchase all the same,
    // and only a portal with neither has nothing to pay with.
    [Theory]
    [InlineData("USD", null, null, true)]
    [InlineData(null, 5.0, null, true)]
    [InlineData(null, 5.0, -60, false)]
    [InlineData(null, null, null, false)]
    [InlineData("EUR", null, null, false)]
    public void CanPayFromWallet_WalletSubAccountOrABonusThatHasNotEnded(string? walletCurrency, double? bonus, int? bonusEndsInMinutes, bool expected)
    {
        var wallet = new Balance { SubAccounts = walletCurrency == null ? [] : [new SubAccount { Currency = walletCurrency, Amount = 0 }] };
        var goodwill = bonus == null ? null : Goodwill((decimal)bonus, Lot((decimal)bonus, "USD", bonusEndsInMinutes));

        GoodwillBalance.CanPayFromWallet(wallet, goodwill, "USD", _now).Should().Be(expected);
    }

    [Theory]
    [InlineData(PromoCodeStatus.Completed, true)]
    [InlineData(PromoCodeStatus.Pending, false)]
    public void IsCredited_OnlyForABonusAlreadyOnTheBalance(PromoCodeStatus status, bool expected)
    {
        Enum.GetValues<PromoCodeStatus>().Should().BeEquivalentTo([PromoCodeStatus.Completed, PromoCodeStatus.Pending],
            "a new status needs its own decision here");

        new PromoCodeResult { Status = status }.IsCredited.Should().Be(expected);
    }

    private static GoodwillBalance Goodwill(decimal subAccount, params GoodwillLot[] lots)
    {
        return new GoodwillBalance { SubAccounts = [new SubAccount { Currency = "USD", Amount = subAccount }], Lots = [.. lots] };
    }

    private static GoodwillLot Lot(decimal remaining, string currency, int? endsInMinutes)
    {
        return new GoodwillLot
        {
            Amount = remaining,
            Remaining = remaining,
            Currency = currency,
            ExpiresAt = endsInMinutes == null ? null : _now.AddMinutes(endsInMinutes.Value)
        };
    }
}

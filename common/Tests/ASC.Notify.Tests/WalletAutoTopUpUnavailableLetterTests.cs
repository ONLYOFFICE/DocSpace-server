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

namespace ASC.Notify.Tests;

/// <summary>
/// What the payer gets when the payment method cannot support an automatic top-up at all
/// (<c>wallet_auto_top_up_unavailable</c>) - a delayed method such as SEPA credits the wallet only
/// once the transfer settles. Distinct from <see cref="TopUpWalletErrorLetterTests"/>, which covers
/// an attempt that was made and failed. Also goes out over Telegram, from the same pattern.
/// </summary>
public class WalletAutoTopUpUnavailableLetterTests : LetterTestBase<WalletAutoTopUpUnavailableNotifyAction>
{
    /// <summary>The wallet page the button leads to.</summary>
    private static string WalletUrl(LetterScope scope)
    {
        return $"{scope.PortalUrl}/billing/wallet";
    }

    protected override void AssertContent(RenderedLetter letter, LetterScope scope)
    {
        letter.Body.Should().Contain(scope.Recipient.FirstName)
            .And.Contain(Resource("ButtonGoToWalletSettings", scope.Culture))
            .And.Contain(WalletUrl(scope));
    }

    protected override void AssertDefaultCultureText(RenderedLetter letter, LetterScope scope)
    {
        var logoText = LetterEnvironment.LogoText;

        letter.Subject.Should().Be($"Action required: Automatic top-up is unavailable for your {logoText} Wallet");

        // No apostrophes in the expected strings: TextileStyler rewrites them.
        letter.Body.Should().Contain($"Hello, {scope.Recipient.FirstName}!")
            .And.Contain("automatic top-up is not supported for your current payment method")
            .And.Contain($"the money reaches the {logoText} Wallet only after the bank transfer is settled");

        // The reader must learn that auto top-up is off now and that topping up is on them.
        letter.Body.Should().Contain("Automatic top-up has therefore been turned off.")
            .And.Contain("top up your wallet balance manually");

        // The brand no longer carries the DocSpace suffix.
        letter.Body.Should().NotContain("DocSpace");
    }

    protected override Task InitAsync(WalletAutoTopUpUnavailableNotifyAction action, LetterScope scope)
    {
        action.Init(scope.Recipient);

        return Task.CompletedTask;
    }
}

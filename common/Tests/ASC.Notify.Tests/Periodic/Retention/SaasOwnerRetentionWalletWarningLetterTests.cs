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

namespace ASC.Notify.Tests.Periodic.Retention;

/// <summary>
/// The reminder to an unused free portal with money left on its wallet
/// (<c>saas_owner_retention_wallet_warning</c>): the money goes with the portal, the letter says by when,
/// and signing in keeps both. The lapsed portal's variant is <see cref="SaasOwnerRetentionWalletWarningUnpaidLetterTests"/>.
/// </summary>
public class SaasOwnerRetentionWalletWarningLetterTests : PortalRetentionLetterTestBase<SaasOwnerRetentionWalletWarningNotifyAction>
{
    protected override PortalRetentionCategory Category => PortalRetentionCategory.FreeWithBalance;

    protected override PortalRetentionLetter? Letter => PortalRetentionLetter.MonthlyNotice;

    protected override void AssertContent(RenderedLetter letter, LetterScope scope)
    {
        letter.Subject.Should().Contain(ShortDay(BlockOn, scope));

        letter.Body.Should().Contain(Day(BlockOn, scope))
            .And.Contain(ShortDay(BlockOn, scope))
            .And.Contain(Day(DeleteOn, scope))
            .And.Contain(scope.PortalUrl)
            .And.Contain($"{scope.PortalUrl}/billing/wallet")
            .And.Contain(Caption("ButtonKeepSpaceAndFunds", scope));
    }

    protected override void AssertDefaultCultureText(RenderedLetter letter, LetterScope scope)
    {
        letter.Subject.Should().Be($"You still have funds in your {LetterEnvironment.LogoText} space (action needed by {ShortDay(BlockOn, scope)})");

        letter.Body.Should().Contain("since anyone signed in to your")
            .And.Contain("there are still funds left in its")
            .And.Contain("along with the funds in your wallet")
            .And.Contain("Just sign in once before");
    }
}

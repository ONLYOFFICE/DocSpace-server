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
/// The "Get more with useful add-ons" letter (<c>saas_admin_addons_v1</c>), sent in SaaS on day 4 after
/// portal registration to the owner and the DocSpace admins, regardless of the tariff.
/// </summary>
public class SaasAdminAddonsLetterTests : LetterTestBase<SaasAdminAddonsV1NotifyAction>
{
    private static string BillingUrl => LetterEnvironment.PortalLink("billing/overview");
    private static string WalletUrl => LetterEnvironment.PortalLink("billing/wallet");

    /// <summary>Mirrors the day-4 block of <c>StudioPeriodicNotify.SendSaasLettersAsync</c>.</summary>
    protected override IEnumerable<ITagValue> BuildLetterTags(CultureInfo culture)
    {
        return
        [
            OrangeButton("ButtonGetStarted", culture, BillingUrl),
            new TagValue("URL1", BillingUrl),
            new TagValue("URL2", WalletUrl)
        ];
    }

    protected override void AssertContent(RenderedLetter letter, CultureInfo culture)
    {
        letter.Body.Should().Contain(RecipientName)
            .And.Contain(Resource("ButtonGetStarted", culture))
            .And.Contain(BillingUrl)
            .And.Contain(WalletUrl);
    }

    protected override void AssertDefaultCultureText(RenderedLetter letter)
    {
        var logoText = LetterEnvironment.LogoText;

        letter.Subject.Should().Be($"Get more from {logoText} with useful add-ons");

        letter.Body.Should().Contain($"Hello, {RecipientName}!")
            .And.Contain($"Want to do even more with {logoText}?")
            .And.Contain("Docs Connect.")
            .And.Contain("AI features.")
            .And.Contain("AI search.")
            .And.Contain("Additional disk storage.")
            .And.Contain("Backups.")
            .And.Contain("Simple &amp; transparent payments");
    }
}

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
/// The business subscription pitch a new SaaS owner gets (<c>saas_admin_welcome_v1</c>). SaaS only —
/// the Enterprise, whitelabel and Opensource copies of it were never sent and are gone.
/// </summary>
public class SaasAdminWelcomeLetterTests : LetterTestBase
{
    /// <summary>The billing page, used both as the button target and as the <c>$PricingPage</c> tag.</summary>
    private static string BillingUrl => LetterEnvironment.PortalLink("billing/overview");

    protected override string LetterId => "saas_admin_welcome_v1";

    protected override IPattern Pattern => new EmailPattern(
        () => WebstudioNotifyPatternResource.subject_saas_admin_welcome_v1,
        () => WebstudioNotifyPatternResource.pattern_saas_admin_welcome_v1);

    /// <summary>The sending code sets a top image for this letter.</summary>
    protected override string? TopGif => LetterEnvironment.NotificationImageUrl("discover_business_subscription.gif");

    /// <summary>Mirrors <c>SaasAdminWelcomeV1NotifyAction.Init</c>.</summary>
    protected override IEnumerable<ITagValue> BuildLetterTags(CultureInfo culture)
    {
        return
        [
            OrangeButton("ButtonUpgrade", culture, BillingUrl),
            new TagValue(CommonTags.PricingPage, BillingUrl)
        ];
    }

    protected override void AssertContent(RenderedLetter letter, CultureInfo culture)
    {
        letter.Body.Should().Contain(RecipientName)
            .And.Contain(Resource("ButtonUpgrade", culture))
            .And.Contain(BillingUrl);
    }

    protected override void AssertDefaultCultureText(RenderedLetter letter)
    {
        var logoText = LetterEnvironment.LogoText;

        letter.Subject.Should().Be($"Discover business subscription of {logoText}");

        letter.Body.Should().Contain($"Hello, {RecipientName}!")
            .And.Contain("three simple questions")
            .And.Contain($"collaborate with in your {logoText}?")
            .And.Contain($"use {logoText} under your own brand?")
            .And.Contain("BUSINESS tariff plan");

        // The brand no longer carries the DocSpace suffix.
        letter.Body.Should().NotContain("DocSpace");
    }
}

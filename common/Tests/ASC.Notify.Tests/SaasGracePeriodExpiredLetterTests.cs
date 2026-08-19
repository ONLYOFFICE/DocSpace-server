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
/// The letter that closes the grace period (<c>saas_owner_payment_warning_grace_period_expired</c>),
/// sent to the owner and the payer on the day it runs out.
/// </summary>
public class SaasGracePeriodExpiredLetterTests : LetterTestBase
{
    private const string PaymentDelay = "30";

    /// <summary>The billing page the button leads to.</summary>
    private static string BillingUrl => LetterEnvironment.PortalLink("billing/overview");

    protected override string LetterId => "saas_owner_payment_warning_grace_period_expired";

    protected override IPattern Pattern => new EmailPattern(
        () => WebstudioNotifyPatternResource.subject_saas_owner_payment_warning_grace_period_expired,
        () => WebstudioNotifyPatternResource.pattern_saas_owner_payment_warning_grace_period_expired);

    /// <summary>The sending code sets no top image, so the tenant letter logo is rendered instead.</summary>
    protected override string? TopGif => null;

    /// <summary>Textile letter: <c>$TrulyYours</c> is inline, not a table row of its own.</summary>
    protected override bool TrulyYoursAsTableRow => false;

    /// <summary>Mirrors the grace period expired block of <c>StudioPeriodicNotify.SendSaasLettersAsync</c>.</summary>
    protected override IEnumerable<ITagValue> BuildLetterTags(CultureInfo culture)
    {
        return
        [
            OrangeButton("ButtonVisitBillingSection", culture, BillingUrl),
            new TagValue(CommonTags.PaymentDelay, PaymentDelay)
        ];
    }

    protected override void AssertContent(RenderedLetter letter, CultureInfo culture)
    {
        letter.Body.Should().Contain(RecipientName)
            .And.Contain(PaymentDelay)
            .And.Contain(Resource("ButtonVisitBillingSection", culture))
            .And.Contain(BillingUrl);
    }

    protected override void AssertDefaultCultureText(RenderedLetter letter)
    {
        var logoText = LetterEnvironment.LogoText;

        letter.Subject.Should().Be($"Grace period for your {logoText} expired");

        letter.Body.Should().Contain($"Hello, {RecipientName}!")
            .And.Contain($"grace period of {PaymentDelay}")
            .And.Contain("Make sure to pay your Business subscription as soon as possible.");

        // The brand no longer carries the DocSpace suffix.
        letter.Body.Should().NotContain("DocSpace");
    }
}

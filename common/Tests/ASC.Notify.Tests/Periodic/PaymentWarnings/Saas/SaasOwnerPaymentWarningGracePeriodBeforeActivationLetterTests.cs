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

namespace ASC.Notify.Tests.Periodic.PaymentWarnings.Saas;

/// <summary>
/// The heads-up three days before the subscription is debited
/// (<c>saas_owner_payment_warning_grace_period_before_activation</c>). The only letter of the four that
/// carries no button — it links the payment method and the support desk inline instead.
/// </summary>
public class SaasOwnerPaymentWarningGracePeriodBeforeActivationLetterTests : PeriodicLetterTestBase<SaasOwnerPaymentWarningGracePeriodBeforeActivationNotifyAction>
{
    private static string PaymentMethodUrl(LetterScope scope) => $"{scope.PortalUrl}/billing/payment-method";

    protected override void AssertContent(RenderedLetter letter, LetterScope scope)
    {
        // The payment method link belongs to the new copy, which so far exists only in the default
        // scope.Culture — the translations still carry the previous sentence. It is asserted below instead.
        letter.Body.Should().Contain(scope.Recipient.FirstName)
            .And.Contain(LetterEnvironment.SupportUrl);
    }

    protected override void AssertDefaultCultureText(RenderedLetter letter, LetterScope scope)
    {
        letter.Subject.Should().Be($"Upcoming subscription payment for your {LetterEnvironment.LogoText} tariff plan");

        // No apostrophes in the expected strings: TextileStyler rewrites them.
        letter.Body.Should().Contain($"Hello, {scope.Recipient.FirstName}!")
            .And.Contain("Business subscription payment for the chosen number of admins")
            .And.Contain("will be automatically debited in 3 days")
            .And.Contain("payment method")
            .And.Contain(PaymentMethodUrl(scope))
            .And.Contain("support team");

        // The brand no longer carries the DocSpace suffix.
        letter.Body.Should().NotContain("DocSpace");
    }
}

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
/// What the owner and the payer get three days before the wallet is charged for an add-on
/// (<c>upcoming_subscription_payment</c>) — the sibling of <see cref="LowWalletBalanceLetterTests"/>,
/// sent ahead of the charge rather than when the balance is already too low. Email only.
///
/// The subscription name arrives as a delegate, because <c>Init</c> resolves it in the recipient's
/// culture. Naming the features stays with the caller in production too, which is why it stays here.
/// </summary>
public class UpcomingSubscriptionPaymentLetterTests : LetterTestBase<UpcomingSubscriptionPaymentNotifyAction>
{
    protected override Task InitAsync(UpcomingSubscriptionPaymentNotifyAction action, LetterScope scope)
    {
        action.Init(scope.Recipient, SubscriptionName);

        return Task.CompletedTask;
    }

    protected override void AssertContent(RenderedLetter letter, LetterScope scope)
    {
        letter.Subject.Should().Contain(SubscriptionName(scope.Culture));

        letter.Body.Should().Contain(scope.Recipient.FirstName)
            .And.Contain(SubscriptionName(scope.Culture))
            .And.Contain($"{scope.PortalUrl}/billing/wallet")
            .And.Contain(LetterEnvironment.SupportUrl);
    }

    protected override void AssertDefaultCultureText(RenderedLetter letter, LetterScope scope)
    {
        var logoText = LetterEnvironment.LogoText;

        letter.Subject.Should().Be(
            $"Upcoming subscription payment for \"{SubscriptionName(scope.Culture)}\" in your {logoText}");

        // No apostrophes in the expected strings: TextileStyler rewrites them.
        letter.Body.Should().Contain($"Hello, {scope.Recipient.FirstName}!")
            .And.Contain("will be automatically debited from your")
            .And.Contain($"{logoText} Wallet</a> in 3 days.")
            .And.Contain("please confirm that sufficient funds are available")
            .And.Contain("feel free to reach out to our");

        // The brand no longer carries the DocSpace suffix.
        letter.Body.Should().NotContain("DocSpace");
    }

    /// <summary>
    /// The worst case the letter has to render: the wallet is charged for two add-ons at once, so
    /// <c>StudioPeriodicNotify.SendUpcomingSubscriptionPaymentAsync</c> joins their localized names with
    /// a comma — the same names the billing page shows (<c>QuotaHelper.GetFeatures</c> resolves
    /// <c>TariffsFeature_{feature}_wallet</c> too).
    /// </summary>
    private static string SubscriptionName(CultureInfo culture)
    {
        return string.Join(", ", FeatureTitle("total_size", culture), FeatureTitle("docscloud", culture));
    }

    private static string FeatureTitle(string featureName, CultureInfo culture)
    {
        return ASC.Web.Core.PublicResources.Resource.ResourceManager.GetString($"TariffsFeature_{featureName}_wallet", culture)
            ?? throw new InvalidOperationException($"Wallet feature '{featureName}' has no localized title.");
    }
}

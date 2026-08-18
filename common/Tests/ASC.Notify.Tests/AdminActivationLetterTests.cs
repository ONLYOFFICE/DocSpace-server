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
/// What the owner of a brand new portal gets outside SaaS (<c>admin_activation_v1</c>) — one template
/// for Enterprise, Enterprise whitelabel and Opensource, which differ only in the footer. It is the SaaS
/// letter (<see cref="SaasAdminActivationLetterTests"/>) without the STARTUP plan block.
/// </summary>
public class AdminActivationLetterTests : LetterTestBase<EnterpriseAdminActivationV1NotifyAction>
{
    private const string RecipientEmail = "owner@preview.onlyoffice.com";

    /// <summary>The email confirmation link, built from <c>ConfirmType.EmailActivation</c>.</summary>
    private static string ConfirmUrl => LetterEnvironment.PortalLink("confirm/EmailActivation");

    /// <summary>The sending code sets a top image for this letter.</summary>
    protected override string? TopGif => LetterEnvironment.NotificationImageUrl("welcome.gif");

    /// <summary>Mirrors <c>Init</c>, which is now the same in all three non-SaaS activation actions.</summary>
    protected override IEnumerable<ITagValue> BuildLetterTags(CultureInfo culture)
    {
        return
        [
            OrangeButton("ButtonConfirm", culture, ConfirmUrl),
            new TagValue(CommonTags.UserEmail, RecipientEmail)
        ];
    }

    protected override void AssertContent(RenderedLetter letter, CultureInfo culture)
    {
        letter.Body.Should().Contain(RecipientName)
            .And.Contain(RecipientEmail)
            .And.Contain(Resource("ButtonConfirm", culture))
            .And.Contain(ConfirmUrl)
            .And.Contain(LetterEnvironment.PortalUrl);
    }

    protected override void AssertDefaultCultureText(RenderedLetter letter)
    {
        var logoText = LetterEnvironment.LogoText;

        letter.Subject.Should().Be($"Welcome to {logoText}!");

        // No apostrophes in the expected strings: TextileStyler turns "You've" into "You&#8217;ve".
        letter.Body.Should().Contain($"Hello, {RecipientName}!")
            .And.Contain($"just created your {logoText}")
            .And.Contain($"Your {logoText} address")
            .And.Contain("Your login")
            .And.Contain("Please confirm your email (the link is valid for 7 days):")
            .And.Contain("Enjoy your private document collaboration infrastructure!");

        // The STARTUP plan block belongs to the SaaS letter only.
        letter.Body.Should().NotContain("STARTUP");
    }
}

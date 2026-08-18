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
/// What the owner of a brand new SaaS portal gets (<c>saas_admin_activation_v1</c>). It is the
/// Enterprise letter (<see cref="AdminActivationLetterTests"/>) plus the STARTUP plan block, and it
/// carries two mutually exclusive buttons — confirm the email, or change the generated password.
/// </summary>
public class SaasAdminActivationLetterTests : LetterTestBase<SaasAdminActivationV1NotifyAction>
{
    private const string RecipientEmail = "owner@preview.onlyoffice.com";

    /// <summary>The password change link, built from <c>ConfirmType.PasswordChange</c>.</summary>
    private static string PasswordChangeUrl => LetterEnvironment.PortalLink("confirm/PasswordChange");

    /// <summary>The sending code sets a top image for this letter.</summary>
    protected override string? TopGif => LetterEnvironment.NotificationImageUrl("welcome.gif");

    /// <summary>
    /// Mirrors the branch of <c>SaasAdminActivationV1NotifyAction.Init</c> taken for an already
    /// activated owner: the confirm button stays empty and only the password one is rendered.
    /// </summary>
    protected override IEnumerable<ITagValue> BuildLetterTags(CultureInfo culture)
    {
        return
        [
            new TagValue("OrangeButton", string.Empty),
            OrangeButton("ButtonChangePassword", culture, PasswordChangeUrl, "OrangeButtonPwd"),
            new TagValue(CommonTags.UserEmail, RecipientEmail)
        ];
    }

    protected override void AssertContent(RenderedLetter letter, CultureInfo culture)
    {
        letter.Body.Should().Contain(RecipientName)
            .And.Contain(RecipientEmail)
            .And.Contain(Resource("ButtonChangePassword", culture))
            .And.Contain(PasswordChangeUrl)
            .And.Contain(LetterEnvironment.PortalUrl);

        // The confirm-email branch is switched off, so neither its text nor an empty button shows up.
        letter.Body.Should().NotContain("Please confirm your email");
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
            .And.Contain("we recommend changing the automatically generated password")
            .And.Contain("Your current tariff plan is STARTUP")
            .And.Contain("Docs, Files, Rooms, Forms, AI agents")
            .And.Contain("3 admins")
            .And.Contain("Up to 12 rooms")
            .And.Contain("Unlimited number of users and guests")
            .And.Contain("2 GB disk space")
            .And.Contain("Enjoy your private document collaboration infrastructure!");
    }
}

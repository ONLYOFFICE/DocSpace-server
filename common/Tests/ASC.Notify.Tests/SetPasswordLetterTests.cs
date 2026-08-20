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
/// The password setup link (<c>set_password</c>). The shortest letter in the set: no greeting by name
/// and no signature, just the button and the seven-day notice.
/// </summary>
public class SetPasswordLetterTests : LetterTestBase
{
    /// <summary>The password change link, built from <c>ConfirmType.PasswordChange</c>.</summary>
    private static string ConfirmUrl => LetterEnvironment.PortalLink("confirm/PasswordChange");

    protected override string LetterId => "set_password";

    protected override IPattern Pattern => new EmailPattern(
        () => WebstudioNotifyPatternResource.subject_set_password,
        () => WebstudioNotifyPatternResource.pattern_set_password);

    /// <summary>The sending code sets no top image, so the tenant letter logo is rendered instead.</summary>
    protected override string? TopGif => null;

    /// <summary>The letter ends on the seven-day notice, it carries no <c>$TrulyYours</c>.</summary>
    protected override bool HasSignature => false;

    /// <summary>Mirrors <c>PasswordSetNotifyAction.Init</c>, which sets the button and nothing else.</summary>
    protected override IEnumerable<ITagValue> BuildLetterTags(CultureInfo culture)
    {
        return [OrangeButton("ButtonSetPassword", culture, ConfirmUrl)];
    }

    protected override void AssertContent(RenderedLetter letter, CultureInfo culture)
    {
        letter.Body.Should().Contain(Resource("ButtonSetPassword", culture))
            .And.Contain(ConfirmUrl);
    }

    protected override void AssertDefaultCultureText(RenderedLetter letter)
    {
        var logoText = LetterEnvironment.LogoText;

        letter.Subject.Should().Be($"Set up a password for {logoText}");

        letter.Body.Should().Contain($"Please set up a password for your {logoText}.")
            .And.Contain("Just click the button below:")
            .And.Contain("The link is valid for 7 days.");

        // The brand no longer carries the DocSpace suffix, and the letter no longer says "account".
        letter.Body.Should().NotContain("DocSpace").And.NotContain("account");
    }
}

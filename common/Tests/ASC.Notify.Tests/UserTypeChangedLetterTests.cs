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
/// What a user gets when an admin changes their type (<c>user_type_changed</c>).
/// </summary>
public class UserTypeChangedLetterTests : LetterTestBase
{
    private const string UserType = "Room admin";


    /// <summary>The access rights article the letter points at.</summary>
    private static string HelpCenterUrl(CultureInfo culture)
    {
        return LetterEnvironment.ExternalEntry(LetterEnvironment.ExternalResources.Helpcenter, "accessrights", culture, "https://helpcenter.onlyoffice.com");
    }

    protected override string LetterId => "user_type_changed";

    protected override IPattern Pattern => new EmailPattern(
        () => WebstudioNotifyPatternResource.subject_user_type_changed,
        () => WebstudioNotifyPatternResource.pattern_user_type_changed);

    /// <summary>The sending code sets no top image, so the tenant letter logo is rendered instead.</summary>
    protected override string? TopGif => null;

    /// <summary>Textile letter: <c>$TrulyYours</c> is inline, not a table row of its own.</summary>
    protected override bool TrulyYoursAsTableRow => false;

    /// <summary>Mirrors <c>UserTypeChangedNotifyAction.Init</c>.</summary>
    protected override IEnumerable<ITagValue> BuildLetterTags(CultureInfo culture)
    {
        return
        [
            new TagValue("UserType", UserType),
            new TagValue("HelpCenterUrl", HelpCenterUrl(culture))
        ];
    }

    protected override void AssertContent(RenderedLetter letter, CultureInfo culture)
    {
        letter.Body.Should()
            .Contain(UserType)
            .And.Contain(LetterEnvironment.PortalUrl)
            .And.Contain(HelpCenterUrl(culture));
    }

    protected override void AssertDefaultCultureText(RenderedLetter letter)
    {
        var logoText = LetterEnvironment.LogoText;

        letter.Subject.Should().Be($"{logoText}: Your user type has changed");

        letter.Body.Should().Contain("Hello!")
            .And.Contain($"You are assigned a new user type in {logoText}")
            .And.Contain("Learn more about user types and permissions in");

        // The brand no longer carries the DocSpace suffix.
        letter.Body.Should().NotContain("DocSpace");
    }
}

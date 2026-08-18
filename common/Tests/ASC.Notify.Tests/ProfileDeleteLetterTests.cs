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
/// The confirmation a member gets after asking for their account to be disabled
/// (<c>profile_delete</c>). Nothing happens until they follow the button, so a letter that arrives
/// without a working link leaves the reader with no way to finish what they started.
/// </summary>
public class ProfileDeleteLetterTests : LetterTestBase<ProfileDeleteNotifyAction>
{
    /// <summary>What <c>CommonLinkUtility.GetConfirmationEmailUrl</c> builds for <c>ProfileRemove</c>.</summary>
    private static string ConfirmUrl => LetterEnvironment.PortalLink("confirm/ProfileRemove");

    /// <summary>The sending code sets no top image, so the tenant letter logo is rendered instead.</summary>
    protected override string? TopGif => null;

    /// <summary>Textile letter: <c>$TrulyYours</c> is inline, not a table row of its own.</summary>
    protected override bool TrulyYoursAsTableRow => false;

    /// <summary>
    /// The pattern carries no <c>$TrulyYours</c> — it ends on the note about how long the link lives —
    /// even though <c>Init</c> passes the signature in.
    /// </summary>
    protected override bool HasSignature => false;

    /// <summary>Mirrors <c>ProfileDeleteNotifyAction.Init</c>.</summary>
    protected override IEnumerable<ITagValue> BuildLetterTags(CultureInfo culture)
    {
        return
        [
            OrangeButton("ButtonRemoveProfile", culture, ConfirmUrl)
        ];
    }

    protected override void AssertContent(RenderedLetter letter, CultureInfo culture)
    {
        letter.Body.Should()
            .Contain(Resource("ButtonRemoveProfile", culture))
            .And.Contain(ConfirmUrl);

        // The portal address is a textile link twice: in the heading and in the sentence recalling
        // what was requested. A translation that drops a quotation mark around it still renders —
        // as the bare address in the middle of the text, which is what lv used to print. Counting
        // the anchors catches that, where merely looking for the address would not.
        Regex.Matches(letter.Body, $"href=\"{Regex.Escape(LetterEnvironment.PortalUrl)}\"")
            .Should().HaveCount(2, "the portal address is linked in the heading and in the request line");
    }

    protected override void AssertDefaultCultureText(RenderedLetter letter)
    {
        letter.Subject.Should().Be($"Disabling account on {LetterEnvironment.PortalHost}");

        letter.Body.Should().Contain("Disabling account on")
            .And.Contain("You have requested to disable your account in")
            .And.Contain("please follow the link below to confirm the operation")
            .And.Contain("this link is valid for 7 days only");

        // The letter only disables the account; it must not promise deletion.
        letter.Body.Should().NotContain("permanently");
    }
}

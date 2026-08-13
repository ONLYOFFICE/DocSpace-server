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
/// The invitation as a guest receives it. The four guest actions currently render the very same
/// <c>user_activation_v1</c> template as the four user ones, so this class is a deliberate duplicate of
/// <see cref="UserActivationLetterTests"/>: it holds the place for the guest wording and starts failing
/// the day the two letters diverge.
/// </summary>
public class GuestActivationLetterTests : LetterTestBase
{
    /// <summary>The confirmation link, built by the sending code from <c>ConfirmType.Activation</c>.</summary>
    private static string ConfirmUrl => LetterEnvironment.PortalLink("confirm/Activation");

    /// <summary>
    /// Only names the preview file and the MailPit address — the template comes from <see cref="Pattern"/>.
    /// </summary>
    protected override string LetterId => "guest_activation_v1";

    protected override IPattern Pattern => new EmailPattern(
        () => WebstudioNotifyPatternResource.subject_user_activation_v1,
        () => WebstudioNotifyPatternResource.pattern_user_activation_v1);

    /// <summary>The sending code sets a top image for this letter.</summary>
    protected override string? TopGif => LetterEnvironment.NotificationImageUrl("join_docspace.gif");

    /// <summary>The SaaS footer; Enterprise passes <c>null</c> and Opensource <c>opensource</c>.</summary>
    protected override string Footer => "social";

    /// <summary>Textile letter: <c>$TrulyYours</c> is inline, not a table row of its own.</summary>
    protected override bool TrulyYoursAsTableRow => false;

    /// <summary>Mirrors <c>Init</c>, which is now the same in all four guest activation actions.</summary>
    protected override IEnumerable<ITagValue> BuildLetterTags(CultureInfo culture)
    {
        return [OrangeButton("ButtonAccept", culture, ConfirmUrl)];
    }

    protected override void AssertContent(RenderedLetter letter, CultureInfo culture)
    {
        letter.Body.Should().Contain(Resource("ButtonAccept", culture))
            .And.Contain(ConfirmUrl)
            .And.Contain(LetterEnvironment.PortalUrl);
    }

    protected override void AssertDefaultCultureText(RenderedLetter letter)
    {
        letter.Subject.Should().Be($"You are invited to {LetterEnvironment.LogoText}");

        letter.Body.Should().Contain("Hello!")
            .And.Contain("Accept the invitation by clicking the link:")
            .And.Contain("After clicking on the invitation link, please set a new password.");
    }
}

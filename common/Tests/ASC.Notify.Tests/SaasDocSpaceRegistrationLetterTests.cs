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
/// The letter that finishes a SaaS sign-up (<c>saas_docspace_registration</c>). Unlike the marketing
/// letters this one is plain textile — an <c>h1.</c> heading and a <c>"text":"url"</c> link — so it
/// exercises the other half of <c>TextileStyler</c>.
/// </summary>
public class SaasDocSpaceRegistrationLetterTests : LetterTestBase
{
    /// <summary>The confirmation link, passed into <c>Init</c> by the caller.</summary>
    private static string ConfirmUrl => LetterEnvironment.PortalLink("confirm/EmpInvite");

    protected override string LetterId => "saas_docspace_registration";

    protected override IPattern Pattern => new EmailPattern(
        () => WebstudioNotifyPatternResource.subject_saas_docspace_registration,
        () => WebstudioNotifyPatternResource.pattern_saas_docspace_registration);

    /// <summary>The sending code sets no top image, so the tenant letter logo is rendered instead.</summary>
    protected override string? TopGif => null;

    /// <summary>Textile letter: <c>$TrulyYours</c> is inline, not a table row of its own.</summary>
    protected override bool TrulyYoursAsTableRow => false;

    /// <summary>Mirrors <c>SaasDocSpaceRegistrationNotifyAction.Init</c>.</summary>
    protected override IEnumerable<ITagValue> BuildLetterTags(CultureInfo culture)
    {
        return
        [
            OrangeButton("ButtonRegister", culture, ConfirmUrl),
            new TagValue(CommonTags.InviteLink, ConfirmUrl)
        ];
    }

    protected override void AssertContent(RenderedLetter letter, CultureInfo culture)
    {
        letter.Body.Should().Contain(Resource("ButtonRegister", culture))
            .And.Contain(ConfirmUrl)
            .And.Contain(LetterEnvironment.PortalUrl);
    }

    protected override void AssertDefaultCultureText(RenderedLetter letter)
    {
        var logoText = LetterEnvironment.LogoText;

        letter.Subject.Should().Be($"Complete {logoText} registration");

        letter.Body.Should().Contain("Hello!")
            .And.Contain($"Complete {logoText} registration")
            .And.Contain($"You are invited to complete {logoText} registration at")
            .And.Contain("Enter your name and set a password by clicking the link:");
    }
}

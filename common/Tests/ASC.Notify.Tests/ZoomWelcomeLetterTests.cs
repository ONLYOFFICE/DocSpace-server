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
/// What the owner of a portal created from the Zoom app gets (<c>zoom_welcome</c>). Textile with an
/// HTML table dropped in for the plan list, and a plan of its own — 100 admins, not the SaaS STARTUP
/// numbers.
/// </summary>
public class ZoomWelcomeLetterTests : LetterTestBase
{
    protected override string LetterId => "zoom_welcome";

    protected override IPattern Pattern => new EmailPattern(
        () => WebstudioNotifyPatternResource.subject_zoom_welcome,
        () => WebstudioNotifyPatternResource.pattern_zoom_welcome);

    /// <summary>The sending code sets a top image for this letter.</summary>
    protected override string? TopGif => LetterEnvironment.NotificationImageUrl("welcome.gif");

    /// <summary>Textile letter: <c>$TrulyYours</c> is inline, not a table row of its own.</summary>
    protected override bool TrulyYoursAsTableRow => false;

    /// <summary>Mirrors <c>ZoomWelcomeNotifyAction.Init</c>, which sets no letter-specific tags.</summary>
    protected override IEnumerable<ITagValue> BuildLetterTags(CultureInfo culture)
    {
        return [];
    }

    protected override void AssertContent(RenderedLetter letter, CultureInfo culture)
    {
        letter.Body.Should().Contain(RecipientName)
            .And.Contain(LetterEnvironment.PortalUrl);
    }

    protected override void AssertDefaultCultureText(RenderedLetter letter)
    {
        var logoText = LetterEnvironment.LogoText;

        letter.Subject.Should().Be($"Welcome to {logoText}!");

        letter.Body.Should().Contain($"Hello, {RecipientName}!")
            .And.Contain($"You have just created {logoText},")
            .And.Contain("Your current tariff plan is STARTUP")
            .And.Contain("100 admins")
            .And.Contain("Up to 100 rooms")
            .And.Contain("100 GB disk space")
            .And.Contain("Enjoy a new way of document collaboration!");

        // The brand no longer carries the DocSpace suffix.
        letter.Body.Should().NotContain("DocSpace");
    }
}

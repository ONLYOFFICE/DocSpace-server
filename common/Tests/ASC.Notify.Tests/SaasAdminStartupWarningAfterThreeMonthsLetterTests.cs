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
/// The first warning a free portal gets (<c>saas_admin_startup_warning_after_three_months_v1</c>), sent to
/// the owner after three months without activity. The six-month letter and the removal follow.
/// </summary>
public class SaasAdminStartupWarningAfterThreeMonthsLetterTests : LetterTestBase<SaasAdminStartupWarningAfterThreeMonthsV1NotifyAction>
{
    private static string DashboardUrl => LetterEnvironment.PortalLink("dashboard");

    /// <summary>The sending code sets a top image for this letter.</summary>
    protected override string? TopGif => LetterEnvironment.NotificationImageUrl("docspace_deleted.gif");

    /// <summary>Mirrors the three-months-without-activity block of <c>StudioPeriodicNotify.SendSaasLettersAsync</c>.</summary>
    protected override IEnumerable<ITagValue> BuildLetterTags(CultureInfo culture)
    {
        return [OrangeButton("ButtonLogIn", culture, DashboardUrl)];
    }

    protected override void AssertContent(RenderedLetter letter, CultureInfo culture)
    {
        letter.Body.Should()
            .Contain(Resource("ButtonLogIn", culture).Replace("${" + CommonTags.LetterLogoText + "}", LetterEnvironment.LogoText))
            .And.Contain(DashboardUrl)
            .And.Contain(LetterEnvironment.PortalUrl);
    }

    protected override void AssertDefaultCultureText(RenderedLetter letter)
    {
        var logoText = LetterEnvironment.LogoText;

        letter.Subject.Should().Be($"Your {logoText} will be deleted");

        // No apostrophes in the expected strings: TextileStyler turns "haven't" into "haven&#8217;t".
        letter.Body.Should().Contain($"entered your {logoText}")
            .And.Contain("for 3 months.")
            .And.Contain("will be deleted after 6 months of inactivity")
            .And.Contain($"Simply log in now to keep your {logoText} active");
    }
}

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
/// The goodbye letter (<c>portal_delete_success_v1</c>), sent to the owner once the portal has actually
/// been removed. Unlike the warning letters, this one is event-driven, not periodic.
/// </summary>
public class PortalDeleteSuccessLetterTests : LetterTestBase
{
    /// <summary>The feedback form, passed into <c>Init</c> by the caller.</summary>
    private static string FeedbackUrl(CultureInfo culture)
    {
        return LetterEnvironment.ExternalEntry(LetterEnvironment.ExternalResources.Site, "registrationcanceled", culture, "https://www.onlyoffice.com/registration-canceled.aspx");
    }

    private static string LegalTermsUrl(CultureInfo culture)
    {
        return LetterEnvironment.ExternalEntry(LetterEnvironment.ExternalResources.Common, "legalterms", culture, "https://docspace.onlyoffice.com/s/Fj-fVY--ZhHHnv7");
    }

    protected override string LetterId => "portal_delete_success_v1";

    protected override IPattern Pattern => new EmailPattern(
        () => WebstudioNotifyPatternResource.subject_portal_delete_success_v1,
        () => WebstudioNotifyPatternResource.pattern_portal_delete_success_v1);

    /// <summary>The sending code sets a top image for this letter.</summary>
    protected override string? TopGif => LetterEnvironment.NotificationImageUrl("docspace_deactivated.gif");

    /// <summary>Mirrors <c>PortalDeleteSuccessV1NotifyAction.Init</c>.</summary>
    protected override IEnumerable<ITagValue> BuildLetterTags(CultureInfo culture)
    {
        return
        [
            OrangeButton("ButtonLeaveFeedback", culture, FeedbackUrl(culture)),
            new TagValue("URL1", LegalTermsUrl(culture)),
            new TagValue(CommonTags.OwnerName, RecipientName)
        ];
    }

    protected override void AssertContent(RenderedLetter letter, CultureInfo culture)
    {
        letter.Body.Should().Contain(Resource("ButtonLeaveFeedback", culture))
            .And.Contain(FeedbackUrl(culture))
            .And.Contain(LegalTermsUrl(culture));
    }

    protected override void AssertDefaultCultureText(RenderedLetter letter)
    {
        var logoText = LetterEnvironment.LogoText;

        letter.Subject.Should().Be($"{logoText} has been deleted");

        letter.Body.Should().Contain($"Your {logoText} has been successfully deleted.")
            .And.Contain("all of your data is deleted in accordance with our")
            .And.Contain("Privacy Policy")
            .And.Contain("Why have you decided to leave?")
            .And.Contain("Thank you and good luck!");
    }
}

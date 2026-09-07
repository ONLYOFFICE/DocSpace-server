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

namespace ASC.Notify.Tests.Periodic.Tips;

/// <summary>
/// The "Get free apps" letter (<c>saas_admin_user_apps_tips_v1</c>), sent in SaaS on day 14 after portal
/// registration to admins and users, regardless of the tariff. Its Enterprise twin
/// (<c>enterprise_admin_user_apps_tips_v1</c>) must carry the very same text — see
/// <see cref="Letters_AreIdentical"/>.
/// </summary>
public class SaasAdminUserAppsTipsLetterTests : PeriodicLetterTestBase<SaasAdminUserAppsTipsV1NotifyAction>
{
    private static string DesktopUrl(CultureInfo culture)
    {
        return LetterEnvironment.ExternalEntry(LetterEnvironment.ExternalResources.Site, "downloaddesktop", culture, "https://www.onlyoffice.com/download-desktop.aspx#desktop");
    }

    private static string MobileUrl(CultureInfo culture)
    {
        return LetterEnvironment.ExternalEntry(LetterEnvironment.ExternalResources.Site, "downloadmobile", culture, "https://www.onlyoffice.com/download-desktop.aspx#mobile");
    }

    protected override void AssertContent(RenderedLetter letter, LetterScope scope)
    {
        letter.Body.Should().Contain(scope.Recipient.FirstName)
            .And.Contain(DesktopUrl(scope.Culture))
            .And.Contain(MobileUrl(scope.Culture))
            .And.Contain(LetterEnvironment.NotificationImageUrl("windows.png"))
            .And.Contain(LetterEnvironment.NotificationImageUrl("apple.png"))
            .And.Contain(LetterEnvironment.NotificationImageUrl("linux.png"))
            .And.Contain(LetterEnvironment.NotificationImageUrl("android.png"));
    }

    protected override void AssertDefaultCultureText(RenderedLetter letter, LetterScope scope)
    {
        var logoText = LetterEnvironment.LogoText;

        letter.Subject.Should().Be($"Get free {logoText} apps");

        letter.Body.Should().Contain($"Hello, {scope.Recipient.FirstName}!")
            .And.Contain($"Get free {logoText} apps to work on documents from any of your devices.")
            .And.Contain($"To work on documents offline, get {logoText} Desktop Editors")
            .And.Contain($"To edit documents on mobile devices, get {logoText} Documents app")
            .And.Contain("For Windows")
            .And.Contain("For MacOS")
            .And.Contain("For Linux")
            .And.Contain("For iOS")
            .And.Contain("For Android");
    }

    /// <summary>
    /// SaaS and Enterprise send the same letter from two separate actions, so the two resource pairs must
    /// stay word for word identical — a change to one is a change to both.
    /// </summary>
    [Fact]
    public void Letters_AreIdentical()
    {
        WebstudioNotifyPatternResource.subject_enterprise_admin_user_apps_tips_v1
            .Should().Be(WebstudioNotifyPatternResource.subject_saas_admin_user_apps_tips_v1);

        WebstudioNotifyPatternResource.pattern_enterprise_admin_user_apps_tips_v1
            .Should().Be(WebstudioNotifyPatternResource.pattern_saas_admin_user_apps_tips_v1);
    }
}

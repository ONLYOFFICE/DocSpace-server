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
/// The "Configure your ONLYOFFICE" letter (<c>saas_admin_configure_v1</c>), sent in SaaS on day 3 after
/// portal registration to the owner and the DocSpace admins, regardless of the tariff.
/// </summary>
public class SaasAdminConfigureLetterTests : PeriodicLetterTestBase<SaasAdminConfigureV1NotifyAction>
{
    private static string SettingsUrl(LetterScope scope) => $"{scope.PortalUrl}/portal-settings";
    private static string TariffUrl(LetterScope scope) => $"{scope.PortalUrl}/billing/tariff-plan";

    private static string HelpCenterUrl(CultureInfo culture)
    {
        return LetterEnvironment.ExternalDomain(LetterEnvironment.ExternalResources.Helpcenter, culture, "https://helpcenter.onlyoffice.com");
    }

    protected override void AssertContent(RenderedLetter letter, LetterScope scope)
    {
        letter.Body.Should().Contain(scope.Recipient.FirstName)
            .And.Contain(Resource("ButtonConfigureRightNow", scope.Culture))
            .And.Contain(SettingsUrl(scope))
            .And.Contain(TariffUrl(scope))
            .And.Contain(HelpCenterUrl(scope.Culture));
    }

    protected override void AssertDefaultCultureText(RenderedLetter letter, LetterScope scope)
    {
        letter.Subject.Should().Be($"Configure your {LetterEnvironment.LogoText}");

        letter.Body.Should().Contain($"Hello, {scope.Recipient.FirstName}!")
            .And.Contain($"Adjust the settings of your {LetterEnvironment.LogoText}")
            .And.Contain("Set password strength")
            .And.Contain("Enable two-factor authentication and Single Sign-On")
            .And.Contain("Control whether users can create public links")
            .And.Contain("Enable automatic data backup");
    }
}

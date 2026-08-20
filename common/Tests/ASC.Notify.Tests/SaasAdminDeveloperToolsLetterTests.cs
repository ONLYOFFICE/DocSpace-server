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
/// The "Connect, integrate, and build" letter (<c>saas_admin_developer_tools_v1</c>), sent in SaaS on day
/// 10 after portal registration to the owner and the DocSpace admins, regardless of the tariff.
/// </summary>
public class SaasAdminDeveloperToolsLetterTests : PeriodicLetterTestBase<SaasAdminDeveloperToolsV1NotifyAction>
{
    private static string DeveloperToolsUrl(LetterScope scope) => $"{scope.PortalUrl}/developer-tools/overview";

    private static string ConnectorsUrl(CultureInfo culture)
    {
        return LetterEnvironment.ExternalEntry(LetterEnvironment.ExternalResources.Site, "allconnectors", culture, "https://www.onlyoffice.com/all-connectors.aspx");
    }

    private static string ApiUrl(CultureInfo culture)
    {
        return LetterEnvironment.ExternalDomain(LetterEnvironment.ExternalResources.Api, culture, "https://api.onlyoffice.com");
    }

    protected override void AssertContent(RenderedLetter letter, LetterScope scope)
    {
        letter.Body.Should().Contain(scope.Recipient.FirstName)
            .And.Contain(Resource("ButtonGetStarted", scope.Culture))
            .And.Contain(DeveloperToolsUrl(scope))
            .And.Contain(ConnectorsUrl(scope.Culture))
            .And.Contain(ApiUrl(scope.Culture));
    }

    protected override void AssertDefaultCultureText(RenderedLetter letter, LetterScope scope)
    {
        var logoText = LetterEnvironment.LogoText;

        letter.Subject.Should().Be($"Connect, integrate, and build with {logoText} Docs");

        letter.Body.Should().Contain($"Hello, {scope.Recipient.FirstName}!")
            .And.Contain("three simple ways to make it happen")
            .And.Contain("50+ ready connectors")
            .And.Contain("Explore Developer Tools")
            .And.Contain("Embed and Plugin SDK, webhooks, OAuth 2.0");
    }
}

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
/// The "Get more with useful add-ons" letter (<c>saas_admin_addons_v1</c>), sent in SaaS on day 4 after
/// portal registration to the owner and the DocSpace admins, regardless of the tariff.
///
/// Its two links and its button come from the action's own <c>AddTagsAsync</c>, so the test only says
/// which pages they are meant to point at — not what the tag values are. Which day the letter goes out on
/// is checked separately, in <see cref="PeriodicLetterScheduleTests"/>.
/// </summary>
public class SaasAdminAddonsLetterTests : PeriodicLetterTestBase<SaasAdminAddonsV1NotifyAction>
{
    protected override void AssertContent(RenderedLetter letter, LetterScope scope)
    {
        letter.Body.Should().Contain(scope.Recipient.FirstName)
            .And.Contain(Resource("ButtonGetStarted", scope.Culture))
            .And.Contain($"{scope.PortalUrl}/billing/overview")
            .And.Contain($"{scope.PortalUrl}/billing/wallet");
    }

    protected override void AssertDefaultCultureText(RenderedLetter letter, LetterScope scope)
    {
        var logoText = LetterEnvironment.LogoText;

        letter.Subject.Should().Be($"Get more from {logoText} with useful add-ons");

        letter.Body.Should().Contain($"Hello, {scope.Recipient.FirstName}!")
            .And.Contain($"Want to do even more with {logoText}?")
            .And.Contain("Docs Connect.")
            .And.Contain("AI features.")
            .And.Contain("AI search.")
            .And.Contain("Additional disk storage.")
            .And.Contain("Backups.")
            .And.Contain("Simple &amp; transparent payments");
    }
}

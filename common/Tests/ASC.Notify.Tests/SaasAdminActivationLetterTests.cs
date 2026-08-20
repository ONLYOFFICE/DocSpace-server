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
/// The letter a brand-new SaaS owner gets (<c>saas_admin_activation_v1</c>): the portal address, the
/// login and the offer to replace the generated password. The password-change link is shortened by
/// <c>Init</c>, so the letter is asserted on the button and the address, not on the link.
/// </summary>
public class SaasAdminActivationLetterTests : LetterTestBase<SaasAdminActivationV1NotifyAction>
{
    protected override Task InitAsync(SaasAdminActivationV1NotifyAction action, LetterScope scope)
    {
        // This letter has two shapes and Init picks between them: an unactivated owner is asked to
        // confirm their email (that is EnterpriseAdminActivationLetterTests), and an activated one whose
        // password was generated for them is offered the change. Both conditions have to be met for the
        // second, so the recipient is activated here and an audit date is passed in.
        var owner = (UserInfo)scope.Recipient.Clone();
        owner.ActivationStatus = EmployeeActivationStatus.Activated;

        return action.Init(owner, DateTime.UtcNow);
    }

    protected override void AssertContent(RenderedLetter letter, LetterScope scope)
    {
        letter.Body.Should().Contain(scope.Recipient.FirstName)
            .And.Contain(scope.Recipient.Email)
            .And.Contain(Resource("ButtonChangePassword", scope.Culture))
            .And.Contain(scope.PortalUrl);

        // The confirm-email branch is switched off, so neither its text nor an empty button shows up.
        letter.Body.Should().NotContain("Please confirm your email");
    }

    protected override void AssertDefaultCultureText(RenderedLetter letter, LetterScope scope)
    {
        var logoText = LetterEnvironment.LogoText;

        letter.Subject.Should().Be($"Welcome to {logoText}!");

        // No apostrophes in the expected strings: TextileStyler turns "You've" into "You&#8217;ve".
        letter.Body.Should().Contain($"Hello, {scope.Recipient.FirstName}!")
            .And.Contain($"just created your {logoText}")
            .And.Contain($"Your {logoText} address")
            .And.Contain("Your login")
            .And.Contain("we recommend changing the automatically generated password")
            .And.Contain("Your current tariff plan is STARTUP")
            .And.Contain("Docs, Files, Rooms, Forms, AI agents")
            .And.Contain("3 admins")
            .And.Contain("Up to 12 rooms")
            .And.Contain("Unlimited number of users and guests")
            .And.Contain("2 GB disk space")
            .And.Contain("Enjoy your private document collaboration infrastructure!");
    }
}

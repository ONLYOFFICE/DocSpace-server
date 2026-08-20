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
/// The invitation a new user gets (<c>user_activation_v1</c>). One textile template serves all four
/// editions — SaaS, Enterprise, Enterprise whitelabel and Opensource — which differ only in the footer
/// flavour their <c>Init</c> passes in; this class renders the SaaS one.
///
/// Nothing about the tags is stated here: the letter's button, footer, top image and signature all come
/// from <c>SaasUserActivationV1NotifyAction.Init</c>, which is the point of the exercise.
/// </summary>
public class SaasUserActivationLetterTests : LetterTestBase<SaasUserActivationV1NotifyAction>
{
    protected override Task InitAsync(SaasUserActivationV1NotifyAction action, LetterScope scope)
    {
        return action.Init(scope.Recipient);
    }

    protected override void AssertContent(RenderedLetter letter, LetterScope scope)
    {
        // Not the invitation link itself: Init shortens it, and the short key is minted by the database
        // on every call. What can be asserted is that the button the action built arrived — its caption,
        // and a target on the portal, which is what the shortener guarantees.
        letter.Body.Should().Contain(Resource("ButtonAccept", scope.Culture))
            .And.Contain(scope.PortalUrl);
    }

    protected override void AssertDefaultCultureText(RenderedLetter letter, LetterScope scope)
    {
        var logoText = LetterEnvironment.LogoText;

        letter.Subject.Should().Be($"You are invited to {logoText}");

        letter.Body.Should().Contain("Hello!")
            .And.Contain($"You are invited to join {logoText} at")
            .And.Contain("Accept the invitation by clicking the link:")
            .And.Contain("After clicking on the invitation link, please set a new password.");
    }
}

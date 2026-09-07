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

namespace ASC.Notify.Tests.Account;

/// <summary>
/// The link that lets a new account set its first password (<c>set_password</c>). One of the few letters
/// that deliberately carries no signature, which is now simply what its <c>Init</c> does rather than
/// something the test has to declare.
/// </summary>
public class PasswordSetLetterTests : LetterTestBase<PasswordSetNotifyAction>
{
    protected override Task InitAsync(PasswordSetNotifyAction action, LetterScope scope)
    {
        return action.Init(scope.Recipient, DateTime.UtcNow);
    }

    protected override void AssertContent(RenderedLetter letter, LetterScope scope)
    {
        // Not the link itself: Init shortens it, so the short key differs on every call.
        letter.Body.Should().Contain(Resource("ButtonSetPassword", scope.Culture));
    }

    protected override void AssertDefaultCultureText(RenderedLetter letter, LetterScope scope)
    {
        var logoText = LetterEnvironment.LogoText;

        letter.Subject.Should().Be($"Set up a password for {logoText}");

        letter.Body.Should().Contain($"Please set up a password for your {logoText}.")
            .And.Contain("Just click the button below:")
            .And.Contain("The link is valid for 7 days.");

        // The brand no longer carries the DocSpace suffix, and the letter no longer says "account".
        letter.Body.Should().NotContain("DocSpace").And.NotContain("account");
    }
}

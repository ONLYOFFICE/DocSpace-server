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
/// The confirmation a user asks for when disabling their own account (<c>profile_delete</c>). The
/// confirmation link is shortened by <c>Init</c>, so what is asserted is the button and the two places
/// the portal address is linked.
/// </summary>
public class ProfileDeleteLetterTests : LetterTestBase<ProfileDeleteNotifyAction>
{
    protected override Task InitAsync(ProfileDeleteNotifyAction action, LetterScope scope)
    {
        return action.Init(scope.Recipient);
    }

    protected override void AssertContent(RenderedLetter letter, LetterScope scope)
    {
        letter.Body.Should().Contain(Resource("ButtonRemoveProfile", scope.Culture));

        // The portal address is a textile link twice: in the heading and in the sentence recalling
        // what was requested. A translation that drops a quotation mark around it still renders —
        // as the bare address in the middle of the text, which is what lv used to print. Counting
        // the anchors catches that, where merely looking for the address would not.
        Regex.Matches(letter.Body, $"href=\"{Regex.Escape(scope.PortalUrl)}\"")
            .Should().HaveCount(2, "the portal address is linked in the heading and in the request line");
    }

    protected override void AssertDefaultCultureText(RenderedLetter letter, LetterScope scope)
    {
        letter.Subject.Should().Be($"Disabling account on {new Uri(scope.PortalUrl).Host}");

        letter.Body.Should().Contain("Disabling account on")
            .And.Contain("You have requested to disable your account in")
            .And.Contain("please follow the link below to confirm the operation")
            .And.Contain("this link is valid for 7 days only");

        // The letter only disables the account; it must not promise deletion.
        letter.Body.Should().NotContain("permanently");
    }
}

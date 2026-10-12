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
/// The notice a user gets when one of their API keys expires and is deactivated
/// (<c>api_key_expired</c>). The key name is the only caller-supplied value in it, and the API
/// stores it exactly as typed, so this letter is the place that has to make it safe to render.
/// </summary>
[Trait("Bug", "82910")]
public class ApiKeyExpiredLetterTests : LetterTestBase<ApiKeyExpiredNotifyAction>
{
    // Every payload fits the 30-character name limit the API enforces, so all of them are names a
    // user can actually create.
    private const string KeyName = "<a href=//evil.com>LINK</a>";
    private const string EncodedKeyName = "&lt;a href=//evil.com&gt;LINK&lt;/a&gt;";

    protected override Task InitAsync(ApiKeyExpiredNotifyAction action, LetterScope scope)
    {
        action.Init(scope.Recipient, KeyName);

        return Task.CompletedTask;
    }

    protected override void AssertContent(RenderedLetter letter, LetterScope scope)
    {
        letter.Body.Should().Contain(scope.Recipient.FirstName);

        // BUG 82910: the name went into the pattern verbatim, so a key called "<a href=//evil.com>LINK</a>"
        // turned into a live link in the recipient's mailbox — the same opening for CSS injection and
        // tracking pixels. It is escaped here rather than on the way into the database, so what every
        // screen shows the user stays the name they typed.
        letter.Body.Should().Contain(EncodedKeyName).And.NotContain(KeyName);
    }

    protected override void AssertDefaultCultureText(RenderedLetter letter, LetterScope scope)
    {
        letter.Subject.Should().Contain("Your API key is expired");

        letter.Body.Should().Contain("has expired and has been deactivated")
            .And.Contain("Developer Tools");
    }
}

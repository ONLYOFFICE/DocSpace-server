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
/// The receipt a user gets after their password changed (<c>password_changed</c>): when it happened, from
/// which device and where, and a link to lock the session out if it was not them. The audit event is the
/// input the sending code passes in — everything the letter says about it is formatted by <c>Init</c>.
/// </summary>
public class PasswordChangedLetterTests : LetterTestBase<PasswordChangedNotifyAction>
{
    private static readonly DateTime _changedOn = new(2026, 8, 14, 11, 20, 0, DateTimeKind.Utc);

    private const string Ip = "203.0.113.7";
    private const string Device = "Windows";
    private const string Country = "Latvia";
    private const string City = "Riga";

    // The audit event reads the browser off the request's User-Agent, so it is whatever the client chose
    // to send — and this letter prints it. Escaped by Init, like every other value that arrives from
    // outside; see ApiKeyExpiredLetterTests for the same check on a name the user types.
    private const string Browser = "<a href=//evil.com>Chrome 140</a>";
    private const string EncodedBrowser = "&lt;a href=//evil.com&gt;Chrome 140&lt;/a&gt;";

    protected override Task InitAsync(PasswordChangedNotifyAction action, LetterScope scope)
    {
        action.Init(scope.Recipient, new AuditEvent
        {
            Date = _changedOn,
            IP = Ip,
            Platform = Device,
            Browser = Browser,
            Country = Country,
            City = City
        });

        return Task.CompletedTask;
    }

    protected override void AssertContent(RenderedLetter letter, LetterScope scope)
    {
        // Init writes the moment with ToShortDateString/ToShortTimeString, i.e. in the recipient's
        // culture — so the expected text has to be built the same way rather than spelled out. A test
        // that spelled it out would be asserting a format no culture actually produces.
        var changedOn = _changedOn.ToShortDateString() + " " + _changedOn.ToShortTimeString();

        letter.Body.Should().Contain(scope.Recipient.FirstName)
            .And.Contain(scope.Recipient.Email)
            .And.Contain(changedOn)
            .And.Contain(Ip)
            .And.Contain(Device)
            .And.Contain($"{Country}, {City}")
            .And.Contain(Resource("ButtonOpenDocSpace", scope.Culture)
                .Replace("${" + CommonTags.LetterLogoText + "}", LetterEnvironment.LogoText));

        letter.Body.Should().Contain(EncodedBrowser).And.NotContain(Browser);
    }

    protected override void AssertDefaultCultureText(RenderedLetter letter, LetterScope scope)
    {
        var logoText = LetterEnvironment.LogoText;

        letter.Subject.Should().Be("Your password was changed successfully");

        letter.Body.Should().Contain("Password changed successfully")
            .And.Contain("was successfully changed on")
            .And.Contain($"in {logoText}:")
            .And.Contain("no further steps are required")
            .And.Contain($"disable access to {logoText} for this device");

        // The brand no longer carries the DocSpace suffix.
        letter.Body.Should().NotContain("DocSpace");
    }
}

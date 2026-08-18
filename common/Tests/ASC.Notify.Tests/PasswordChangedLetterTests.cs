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
/// The security notice sent after a password change (<c>password_changed</c>). It reports the audit
/// event behind the change, so it carries the widest set of tags of any letter — who, when, from which
/// IP, device and browser.
/// </summary>
public class PasswordChangedLetterTests : LetterTestBase
{
    private const string RecipientEmail = "owner@preview.onlyoffice.com";
    private const string ChangedOn = "14.08.2026 11:20";
    private const string Ip = "203.0.113.7";
    private const string Device = "Windows";
    private const string Browser = "Chrome 140";
    private const string Location = "Latvia, Riga";

    /// <summary>The sign-in link, built from <c>ConfirmType.Auth</c>.</summary>
    private static string ConfirmUrl => LetterEnvironment.PortalLink("confirm/Auth");

    protected override string LetterId => "password_changed";

    protected override IPattern Pattern => new EmailPattern(
        () => WebstudioNotifyPatternResource.subject_password_changed,
        () => WebstudioNotifyPatternResource.pattern_password_changed);

    /// <summary>The sending code sets no top image, so the tenant letter logo is rendered instead.</summary>
    protected override string? TopGif => null;

    /// <summary>Textile letter: <c>$TrulyYours</c> is inline, not a table row of its own.</summary>
    protected override bool TrulyYoursAsTableRow => false;

    /// <summary>Mirrors <c>PasswordChangedNotifyAction.Init</c>.</summary>
    protected override IEnumerable<ITagValue> BuildLetterTags(CultureInfo culture)
    {
        return
        [
            OrangeButton("ButtonOpenDocSpace", culture, ConfirmUrl),
            new TagValue(CommonTags.UserEmail, RecipientEmail),
            new TagValue(CommonTags.Date, ChangedOn),
            new TagValue(CommonTags.Device, Device),
            new TagValue(CommonTags.Location, Location),
            new TagValue(CommonTags.Browser, Browser),
            new TagValue(CommonTags.IP, Ip)
        ];
    }

    protected override void AssertContent(RenderedLetter letter, CultureInfo culture)
    {
        letter.Body.Should().Contain(RecipientName)
            .And.Contain(RecipientEmail)
            .And.Contain(ChangedOn)
            .And.Contain(Ip)
            .And.Contain(Device)
            .And.Contain(Browser)
            .And.Contain(Location)
            .And.Contain(Resource("ButtonOpenDocSpace", culture).Replace("${" + CommonTags.LetterLogoText + "}", LetterEnvironment.LogoText))
            .And.Contain(ConfirmUrl);
    }

    protected override void AssertDefaultCultureText(RenderedLetter letter)
    {
        var logoText = LetterEnvironment.LogoText;

        letter.Subject.Should().Be("Your password was changed successfully");

        letter.Body.Should().Contain("Password changed successfully")
            .And.Contain($"was successfully changed on")
            .And.Contain($"in {logoText}:")
            .And.Contain("no further steps are required")
            .And.Contain($"disable access to {logoText} for this device");

        // The brand no longer carries the DocSpace suffix.
        letter.Body.Should().NotContain("DocSpace");
    }
}

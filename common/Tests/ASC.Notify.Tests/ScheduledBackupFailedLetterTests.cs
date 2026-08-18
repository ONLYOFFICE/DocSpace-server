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
/// What the owner gets when a scheduled backup fails (<c>scheduled_backup_failed</c>). Unlike most
/// letters it has a Telegram twin of its own, <c>pattern_scheduled_backup_failed_tg</c>, so the two are
/// checked to stay in step — see <see cref="EmailAndTelegram_AgreeOnLinks"/>.
/// </summary>
public class ScheduledBackupFailedLetterTests : LetterTestBase<ScheduledBackupFailedNotifyAction>
{
    /// <summary>The two settings pages the letter points at, relative to the portal root.</summary>
    private const string BackupPath = "/portal-settings/backup/data-backup";
    private const string AddonsPath = "/billing/addons";

    /// <summary>The sending code sets no top image, so the tenant letter logo is rendered instead.</summary>
    protected override string? TopGif => null;

    /// <summary>Textile letter: <c>$TrulyYours</c> is inline, not a table row of its own.</summary>
    protected override bool TrulyYoursAsTableRow => false;

    /// <summary>The backup letters sign off with "Best regards", not "Truly Yours".</summary>
    protected override string SignatureKey => "BestRegardsText";

    /// <summary>
    /// Mirrors <c>ScheduledBackupFailedNotifyAction.Init</c>. Both settings links and the support one
    /// come from common tags, so the letter needs no tags of its own.
    /// </summary>
    protected override IEnumerable<ITagValue> BuildLetterTags(CultureInfo culture)
    {
        return [new TagValue(CommonTags.Message, "Disk quota exceeded")];
    }

    protected override void AssertContent(RenderedLetter letter, CultureInfo culture)
    {
        letter.Body.Should().Contain(RecipientName)
            .And.Contain(LetterEnvironment.PortalUrl + BackupPath)
            .And.Contain(LetterEnvironment.PortalUrl + AddonsPath)
            .And.Contain(LetterEnvironment.SupportUrl);
    }

    protected override void AssertDefaultCultureText(RenderedLetter letter)
    {
        var logoText = LetterEnvironment.LogoText;

        letter.Subject.Should().Be($"Auto backup for your {logoText} failed");

        // No apostrophes in the expected strings: TextileStyler rewrites them.
        letter.Body.Should().Contain($"Hello, {RecipientName}!")
            .And.Contain($"The scheduled backup process for your {logoText}")
            .And.Contain("has failed.")
            .And.Contain("Backup")
            .And.Contain("Addons");

        // The brand no longer carries the DocSpace suffix.
        letter.Body.Should().NotContain("DocSpace");
    }

    /// <summary>
    /// The Telegram copy is a separate resource, so it can drift. It must point at the same two settings
    /// pages and carry no leftover brand either.
    /// </summary>
    [Fact]
    public void EmailAndTelegram_AgreeOnLinks()
    {
        // Read it for the default culture explicitly: the static property would follow whatever culture
        // the thread happens to carry.
        var telegram = Resource("pattern_scheduled_backup_failed_tg", CultureInfo.GetCultureInfo(LetterCultures.DefaultCultureName));

        telegram.Should().Contain(BackupPath)
            .And.Contain(AddonsPath)
            .And.NotContain("DocSpace")
            .And.NotContain("/portal-settings/payments/services");
    }
}

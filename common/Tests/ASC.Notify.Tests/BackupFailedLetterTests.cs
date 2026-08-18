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
/// What the owner gets when a backup they started by hand fails (<c>backup_failed</c>). The shorter
/// sibling of <see cref="ScheduledBackupFailedLetterTests"/>: no settings links, just the support one.
/// It has a Telegram twin of its own, checked by <see cref="EmailAndTelegram_AgreeOnText"/>.
/// </summary>
public class BackupFailedLetterTests : LetterTestBase<BackupFailedNotifyAction>
{

    /// <summary>The sending code sets no top image, so the tenant letter logo is rendered instead.</summary>
    protected override string? TopGif => null;

    /// <summary>Textile letter: <c>$TrulyYours</c> is inline, not a table row of its own.</summary>
    protected override bool TrulyYoursAsTableRow => false;

    /// <summary>The backup letters sign off with "Best regards", not "Truly Yours".</summary>
    protected override string SignatureKey => "BestRegardsText";

    /// <summary>Mirrors <c>BackupFailedNotifyAction.Init</c>.</summary>
    protected override IEnumerable<ITagValue> BuildLetterTags(CultureInfo culture)
    {
        return [new TagValue(CommonTags.Message, "Disk quota exceeded")];
    }

    protected override void AssertContent(RenderedLetter letter, CultureInfo culture)
    {
        letter.Body.Should().Contain(RecipientName)
            .And.Contain(LetterEnvironment.PortalUrl)
            .And.Contain(LetterEnvironment.SupportUrl);
    }

    protected override void AssertDefaultCultureText(RenderedLetter letter)
    {
        var logoText = LetterEnvironment.LogoText;

        letter.Subject.Should().Be($"Backup for your {logoText} failed");

        // No apostrophes in the expected strings: TextileStyler rewrites them.
        letter.Body.Should().Contain(RecipientName)
            .And.Contain($"The backup process for your {logoText}")
            .And.Contain("has failed.")
            .And.Contain("hesitate to contact us via");

        // The brand no longer carries the DocSpace suffix.
        letter.Body.Should().NotContain("DocSpace");
    }

    /// <summary>
    /// The Telegram copy is a separate resource, so it can drift. It must carry the same sentence and no
    /// leftover brand either.
    /// </summary>
    [Fact]
    public void EmailAndTelegram_AgreeOnText()
    {
        // Read it for the default culture explicitly: the static property would follow whatever culture
        // the thread happens to carry.
        var telegram = Resource("pattern_backup_failed_tg", CultureInfo.GetCultureInfo(LetterCultures.DefaultCultureName));

        telegram.Should().Contain("The backup process for your ${LetterLogoText}")
            .And.Contain("has failed.")
            .And.NotContain("DocSpace");
    }
}

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
/// What the owner gets when the nightly backup fails (<c>scheduled_backup_failed</c>). Unlike the manual
/// one it points at the backup settings and at the add-ons page, since the usual cause is a full quota.
/// </summary>
public class ScheduledBackupFailedLetterTests : LetterTestBase<ScheduledBackupFailedNotifyAction>
{
    private const string BackupPath = "/portal-settings/backup/data-backup";
    private const string AddonsPath = "/billing/addons";

    /// <summary>The failure the sending code passes in — a real one, from a full disk.</summary>
    private const string ErrorMessage = "Disk quota exceeded";

    protected override Task InitAsync(ScheduledBackupFailedNotifyAction action, LetterScope scope)
    {
        action.Init(scope.Recipient, ErrorMessage);

        return Task.CompletedTask;
    }

    protected override void AssertContent(RenderedLetter letter, LetterScope scope)
    {
        letter.Body.Should().Contain(scope.DisplayName)
            .And.Contain(scope.PortalUrl + BackupPath)
            .And.Contain(scope.PortalUrl + AddonsPath)
            .And.Contain(LetterEnvironment.SupportUrl);
    }

    protected override void AssertDefaultCultureText(RenderedLetter letter, LetterScope scope)
    {
        var logoText = LetterEnvironment.LogoText;

        letter.Subject.Should().Be($"Auto backup for your {logoText} failed");

        // No apostrophes in the expected strings: TextileStyler rewrites them.
        letter.Body.Should().Contain($"Hello, {scope.DisplayName}!")
            .And.Contain($"The scheduled backup process for your {logoText}")
            .And.Contain("has failed.")
            .And.Contain("Backup")
            .And.Contain("Addons");

        // The brand no longer carries the DocSpace suffix.
        letter.Body.Should().NotContain("DocSpace");
    }

    /// <summary>
    /// The Telegram copy is a separate resource, so it can drift. Both settings links have to be in it,
    /// and the payments page it used to point at must not come back.
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

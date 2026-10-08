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

namespace ASC.Notify.Tests.Periodic.Retention;

/// <summary>
/// The last reminder that a blocked portal is about to be deleted
/// (<c>saas_owner_retention_deletion_reminder</c>), here for a lapsed portal that may still be unblocked
/// from it. The earlier reminder is <see cref="SaasOwnerRetentionDeletionReminderEarlyLetterTests"/>.
/// </summary>
public class SaasOwnerRetentionDeletionReminderLetterTests : PortalRetentionLetterTestBase<SaasOwnerRetentionDeletionReminderNotifyAction>
{
    protected override PortalRetentionCategory Category => PortalRetentionCategory.FormerPayingWithBalance;

    protected override PortalRetentionLetter? Letter => PortalRetentionLetter.FinalDeletionNotice;

    protected override void AssertContent(RenderedLetter letter, LetterScope scope)
    {
        letter.Subject.Should().Contain(ShortDay(DeleteOn, scope));

        letter.Body.Should().Contain(Day(DeleteOn, scope))
            .And.Contain(ShortDay(DeleteOn, scope))
            .And.Contain(scope.PortalUrl)
            .And.Contain(Caption("ButtonUnblockPortal", scope))
            .And.Contain(nameof(ConfirmType.PortalUnblock));
    }

    protected override void AssertDefaultCultureText(RenderedLetter letter, LetterScope scope)
    {
        letter.Subject.Should().Be($"Last chance to keep your {LetterEnvironment.LogoText} space (deleted on {ShortDay(DeleteOn, scope)})");

        letter.Body.Should().Contain("is still blocked because its subscription has ended")
            .And.Contain("This is a friendly last reminder")
            .And.Contain("then renew your subscription to get back to work", "a portal that comes back unpaid has to be renewed to be used")
            .And.Contain("unblock your space and back it up", "a portal that comes back unpaid can only be backed up");
    }
}

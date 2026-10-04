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
/// The block, as a portal that has paid before hears of it (<c>saas_owner_retention_blocked</c>): the day
/// it is deleted, and the button that unblocks it. <see cref="SaasOwnerRetentionBlockedFreeLetterTests"/>
/// renders the same letter for a free portal, which is sent to support instead.
/// </summary>
public class SaasOwnerRetentionBlockedLetterTests : PortalRetentionLetterTestBase<SaasOwnerRetentionBlockedNotifyAction>
{
    protected override PortalRetentionCategory Category => PortalRetentionCategory.FormerPaying;

    protected override PortalRetentionLetter? Letter => PortalRetentionLetter.Blocked;

    protected override void AssertContent(RenderedLetter letter, LetterScope scope)
    {
        letter.Body.Should().Contain(Day(DeleteOn, scope))
            .And.Contain(Caption("ButtonUnblockPortal", scope))
            .And.Contain(nameof(ConfirmType.PortalUnblock), "the button is the confirmation link that unblocks the portal");
    }

    protected override void AssertDefaultCultureText(RenderedLetter letter, LetterScope scope)
    {
        letter.Subject.Should().Be($"Your {LetterEnvironment.LogoText} has been blocked");

        letter.Body.Should().Contain("has been blocked because it was not used")
            .And.Contain("unblock it before that date")
            .And.NotContain(", contact our", "a portal that can be unblocked from the letter is not sent to support");
    }
}

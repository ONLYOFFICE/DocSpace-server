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
/// The block of a lapsed portal, as its payer reads it (<c>saas_owner_retention_blocked</c>): the payer is
/// not the owner, so the letter carries no unblocking link - that link signs its holder in as the owner -
/// and sends the payer to support instead.
/// </summary>
public class SaasOwnerRetentionBlockedPayerLetterTests : PortalRetentionLetterTestBase<SaasOwnerRetentionBlockedNotifyAction>
{
    protected override PortalRetentionCategory Category => PortalRetentionCategory.FormerPaying;

    protected override PortalRetentionLetter? Letter => PortalRetentionLetter.Blocked;

    protected override string PreviewName(SaasOwnerRetentionBlockedNotifyAction action) => action.ID + "_payer";

    protected override UserInfo Reader(LetterScope scope)
    {
        var payer = (UserInfo)scope.Recipient.Clone();
        payer.Id = Guid.NewGuid();

        return payer;
    }

    protected override void AssertContent(RenderedLetter letter, LetterScope scope)
    {
        letter.Body.Should().Contain(Caption("ButtonContactSupport", scope), "the payer is sent to support")
            .And.NotContain(Caption("ButtonUnblockPortal", scope))
            .And.NotContain(nameof(ConfirmType.PortalUnblock), "the owner's link never goes to anyone but the owner");
    }

    protected override void AssertDefaultCultureText(RenderedLetter letter, LetterScope scope)
    {
        letter.Body.Should().Contain("has ended, we")
            .And.Contain("Just get in touch with our support team before")
            .And.NotContain("Just unblock your space");
    }
}

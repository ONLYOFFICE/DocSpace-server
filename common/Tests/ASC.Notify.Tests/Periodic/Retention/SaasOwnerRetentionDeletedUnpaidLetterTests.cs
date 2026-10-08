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
/// The deletion, as a portal whose paid tariff lapsed hears of it (<c>saas_owner_retention_deleted</c>):
/// it is deleted for the subscription that was not renewed, and thanked as a customer rather than for
/// trying the product.
/// </summary>
public class SaasOwnerRetentionDeletedUnpaidLetterTests : PortalRetentionLetterTestBase<SaasOwnerRetentionDeletedNotifyAction>
{
    private const string Domain = "retention-deleted-unpaid.example.com";

    protected override PortalRetentionCategory Category => PortalRetentionCategory.FormerPaying;

    protected override PortalRetentionLetter? Letter => null;

    protected override string? PortalDomain => Domain;

    protected override string PreviewName(SaasOwnerRetentionDeletedNotifyAction action) => action.ID + "_unpaid";

    protected override void AssertContent(RenderedLetter letter, LetterScope scope)
    {
        letter.Body.Should().Contain(Domain, "the letter names the portal by the address it had")
            .And.Contain(Caption("ButtonShareFeedback", scope));
    }

    protected override void AssertDefaultCultureText(RenderedLetter letter, LetterScope scope)
    {
        letter.Body.Should().Contain("ended and was not renewed")
            .And.Contain("Thank you for working with us")
            .And.NotContain("a try", "a customer who paid is not thanked for trying the product");
    }
}

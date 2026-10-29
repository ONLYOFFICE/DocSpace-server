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
/// The portal has been deleted by the retention policy (<c>saas_owner_retention_deleted</c>). The removal
/// renames the alias before the letter is rendered, so the letter names the address the job captured
/// before it, as text rather than a link.
/// </summary>
public class SaasOwnerRetentionDeletedLetterTests : PortalRetentionLetterTestBase<SaasOwnerRetentionDeletedNotifyAction>
{
    private const string Domain = "retention-deleted.example.com";

    protected override PortalRetentionCategory Category => PortalRetentionCategory.Free;

    protected override PortalRetentionLetter? Letter => null;

    protected override string? PortalDomain => Domain;

    protected override void AssertContent(RenderedLetter letter, LetterScope scope)
    {
        letter.Body.Should().Contain(Domain, "the letter names the portal by the address it had")
            .And.Contain(Caption("ButtonShareFeedback", scope))
            .And.Contain(LetterEnvironment.NotificationImageUrl(scope.PortalUrl, "docspace_deleted.gif"));
    }

    protected override void AssertDefaultCultureText(RenderedLetter letter, LetterScope scope)
    {
        letter.Subject.Should().Be($"Your {LetterEnvironment.LogoText} space has been deleted");

        letter.Body.Should().Contain("Your space has been deleted")
            .And.Contain("used for a long time")
            .And.Contain("a try", "a free portal is thanked for trying the product")
            .And.Contain("Privacy Policy")
            .And.Contain("create a new space");
    }
}

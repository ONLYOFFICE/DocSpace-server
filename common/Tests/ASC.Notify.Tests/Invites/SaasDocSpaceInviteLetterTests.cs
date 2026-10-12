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

namespace ASC.Notify.Tests.Invites;

/// <summary>
/// The invitation to the portal itself (<c>saas_docspace_invite</c>). Unlike its room and agent siblings
/// this one is still HTML table markup rather than textile. The confirmation link is an input: the
/// sending code builds it before calling <c>Init</c>.
/// </summary>
public class SaasDocSpaceInviteLetterTests : LetterTestBase<SaasDocSpaceInviteNotifyAction>
{
    private static string ConfirmUrl(LetterScope scope)
    {
        return $"{scope.PortalUrl}/confirm/LinkInvite";
    }

    protected override Task InitAsync(SaasDocSpaceInviteNotifyAction action, LetterScope scope)
    {
        action.Init(ConfirmUrl(scope), scope.Culture.Name);

        return Task.CompletedTask;
    }

    protected override void AssertContent(RenderedLetter letter, LetterScope scope)
    {
        letter.Body.Should().Contain(Resource("ButtonAccept", scope.Culture))
            .And.Contain(ConfirmUrl(scope))
            .And.Contain(scope.PortalUrl);
    }

    protected override void AssertDefaultCultureText(RenderedLetter letter, LetterScope scope)
    {
        var logoText = LetterEnvironment.LogoText;

        letter.Subject.Should().Be($"Join {logoText}");

        letter.Body.Should().Contain("Hello!")
            .And.Contain($"You are invited to join {logoText} at")
            .And.Contain("Accept the invitation by clicking the link:");
    }
}

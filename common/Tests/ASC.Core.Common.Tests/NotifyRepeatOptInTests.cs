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

namespace ASC.Core.Common.Tests;

/// <summary>
/// Which letters drop repeats. The check is off unless a letter turns it on, and it may be turned on only for letters
/// a caller can make the server send again and again through the API. These tests pin that list, so a letter joins it
/// only through a visible change here, and keep out the letters whose loss hurts: security alerts, letters whose link
/// is keyed on the latest request, and letters whose key cannot tell two events apart.
/// </summary>
/// <remarks>
/// The actions are read from the Web.Core and Files.Core assemblies without running their constructors: the members
/// read here are constant expressions. The backup and LDAP letters live in assemblies this project does not reference;
/// none of them opts in.
/// </remarks>
public class NotifyRepeatOptInTests
{
    private static readonly string[] _optedIn =
    [
        // POST portal/suspend, POST portal/delete
        "portal_deactivate",
        "portal_delete",
        // PUT people/self/delete
        "profile_delete",
        // PUT people/invite
        "activate_email",
        "saas_docspace_invite",
        // PUT people/type/{type}
        "user_type_changed",
        // PUT files/rooms/{id}/share, POST files/rooms/{id}/resend
        "saas_room_invite",
        "saas_agent_invite",
        "saas_room_invite_existing_user",
        "saas_agent_invite_existing_user",
        "user_role_changed",
        "user_agent_role_changed",
        "InvitedToRoom",
        "InvitedToAgent",
        "RoomUpdateAccessForUser",
        "AgentUpdateAccessForUser",
        // PUT files/file/{id}/share, PUT files/folder/{id}/share
        "ShareDocument",
        "ShareFolder",
        "ShareEncryptedDocument"
    ];

    [Fact]
    public void OnlyListedLetters_DropRepeats()
    {
        var optedIn = Actions().Where(a => a.SuppressRepeats).Select(a => a.ID);

        optedIn.Should().BeEquivalentTo(_optedIn);
    }

    [Theory]
    // a security alert: the second one may come from someone else
    [InlineData("password_changed")]
    [InlineData("change_tfa")]
    [InlineData("suspicious_login")]
    // the link is keyed on the latest request, so only the newest letter carries a link that still works
    [InlineData("change_password_v115")]
    [InlineData("set_password")]
    [InlineData("change_email_v115")]
    [InlineData("saas_admin_activation_v1")]
    // the key cannot tell two events apart: who joined, where the portal moved, what the comment says
    [InlineData("user_has_join")]
    [InlineData("portal_rename")]
    [InlineData("EditorMentions")]
    public void LetterThatMustAlwaysGoOut_DoesNotDropRepeats(string id)
    {
        var action = Actions().Single(a => a.ID == id);

        action.SuppressRepeats.Should().BeFalse();
    }

    private static IEnumerable<INotifyAction> Actions()
    {
        Assembly[] assemblies =
        [
            typeof(ASC.Web.Studio.Core.Notify.UserTypeChangedNotifyAction).Assembly,
            typeof(ASC.Files.Core.Services.NotifyService.ShareDocumentNotifyAction).Assembly
        ];

        return assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false } && typeof(INotifyAction).IsAssignableFrom(t))
            .Select(t => (INotifyAction)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(t));
    }
}

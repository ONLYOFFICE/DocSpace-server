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

namespace ASC.Notify.Tests.Account;

/// <summary>
/// What a member gets when their role in an AI agent changes (<c>user_agent_role_changed</c>). It reuses
/// the room letter's tags, which is why the agent arrives under <c>RoomTitle</c> and <c>RoomUrl</c>.
/// </summary>
public class UserAgentRoleChangedLetterTests : LetterTestBase<UserAgentRoleChangedNotifyAction>
{
    private const string AgentTitle = "Agent title";
    private const string UserRole = "Editor";

    /// <summary>The access rights article the letter points at.</summary>
    private static string HelpCenterUrl(CultureInfo culture)
    {
        return LetterEnvironment.ExternalEntry(LetterEnvironment.ExternalResources.Helpcenter, "accessrights", culture, "https://helpcenter.onlyoffice.com");
    }

    private static string AgentUrl(LetterScope scope)
    {
        return $"{scope.PortalUrl}/ai/agents/1";
    }

    protected override Task InitAsync(UserAgentRoleChangedNotifyAction action, LetterScope scope)
    {
        action.Init(scope.Recipient, AgentTitle, AgentUrl(scope), UserRole);

        return Task.CompletedTask;
    }

    protected override void AssertContent(RenderedLetter letter, LetterScope scope)
    {
        letter.Body.Should()
            .Contain(AgentTitle)
            .And.Contain(AgentUrl(scope))
            .And.Contain(UserRole)
            .And.Contain(HelpCenterUrl(scope.Culture));
    }

    protected override void AssertDefaultCultureText(RenderedLetter letter, LetterScope scope)
    {
        var logoText = LetterEnvironment.LogoText;

        letter.Subject.Should().Be($"{logoText}: Your role in the AI agent has changed");

        letter.Body.Should().Contain("Hello!")
            .And.Contain($"You are assigned a new role in the {logoText} AI agent")
            .And.Contain("Learn more about room roles and permissions in");

        // The brand no longer carries the DocSpace suffix.
        letter.Body.Should().NotContain("DocSpace");
    }
}

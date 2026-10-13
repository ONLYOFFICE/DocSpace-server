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

namespace ASC.AI.Tests.Tests.AgentsTests;

[Trait("Category", "Permissions")]
[Trait("Feature", "AI/Agents")]
public class AgentListPermissionsTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    private const string AgentsPath = "/internal/ai/agents";

    /// <summary>
    /// The root of the AI agents section used to report the stored counters of the whole portal, so a member who
    /// could see none of the agents still learned how many there were. The section root now reports 0, like the
    /// root of the rooms section.
    /// </summary>
    [Fact]
    [Trait("Bug", "81482")]
    [Trait("Bug", "80658")]
    public async Task GetAgents_UserWithoutAccess_CurrentDoesNotCountHiddenAgents()
    {
        // Arrange
        using (var created = await _ai.PostAsync(AgentsPath, new { title = "Autotest Hidden Agent" }, TestContext.Current.CancellationToken))
        {
            created.EnsureSuccessStatusCode();
        }

        var user = await InviteContact(EmployeeType.User, TestContext.Current.CancellationToken);
        await _aiClient.Authenticate(user);

        // Act
        using var response = await _ai.GetAsync(AgentsPath, TestContext.Current.CancellationToken);
        var content = await _ai.ReadAsync<JsonElement>(response, TestContext.Current.CancellationToken);

        // Assert
        content.GetProperty("total").GetInt32().Should().Be(0);
        content.GetProperty("folders").GetArrayLength().Should().Be(0);

        var current = content.GetProperty("current");
        current.GetProperty("foldersCount").GetInt32().Should().Be(0);
        current.GetProperty("filesCount").GetInt32().Should().Be(0);
    }
}

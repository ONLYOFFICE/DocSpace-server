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

/// <summary>
/// An AI agent is used by people who hold no encryption key, so it cannot be private. The refusal comes before the
/// agent is saved.
/// </summary>
[Trait("Category", "Rooms")]
[Trait("Feature", "AI/Agents")]
public class PrivateAgentTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    private const string AgentsPath = "/internal/ai/agents";

    [Fact]
    public async Task CreateAgent_Private_IsABadRequestThatLeavesNothing()
    {
        var title = $"Autotest Private Agent {Guid.NewGuid():N}";
        var grantedTitle = $"Autotest Agent {Guid.NewGuid():N}";

        using (var refused = await _ai.PostAsync(AgentsPath, new { title, @private = true }, TestContext.Current.CancellationToken))
        {
            ((int)refused.StatusCode).Should().Be(400);
            (await refused.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).Should().Contain("cannot be private");
        }

        // An agent created after the refusal is listed, so the listing shows created agents and the absence is real.
        using (var granted = await _ai.PostAsync(AgentsPath, new { title = grantedTitle }, TestContext.Current.CancellationToken))
        {
            granted.EnsureSuccessStatusCode();
        }

        using var listed = await _ai.GetAsync(AgentsPath, TestContext.Current.CancellationToken);
        var content = await _ai.ReadAsync<JsonElement>(listed, TestContext.Current.CancellationToken);
        var titles = content.GetProperty("folders").EnumerateArray().Select(f => f.GetProperty("title").GetString()).ToList();

        titles.Should().Contain(grantedTitle);
        titles.Should().NotContain(title);
    }

    [Fact]
    public async Task CreateAgent_NotPrivate_IsCreated()
    {
        // The same body without the flag is accepted, so the refusal above is about the flag alone.
        var title = $"Autotest Agent {Guid.NewGuid():N}";

        using var created = await _ai.PostAsync(AgentsPath, new { title, @private = false }, TestContext.Current.CancellationToken);

        created.EnsureSuccessStatusCode();
    }
}

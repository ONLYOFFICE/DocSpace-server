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


namespace ASC.AI.Tests.Tests.PreferenceTests;

/// <summary>
/// A room, a folder, an AI room or an id that exists nowhere is not a scope of its own: the chat service validates the
/// <c>entityId</c> and drops it, so a read, write or clear aimed at any of them is a read, write or clear of the
/// user's one value (Bug 82900, closed by that design). The TypeScript suite expected a separate value per location
/// and per agent (Bugs 82900, 84306, 84418, 84419, 84420 as open defects); the single value is what the service
/// documents, so that is what is asserted. The seed is always a value chosen on purpose, so a no-op cannot pass.
/// </summary>
[Trait("Category", "Scope")]
[Trait("Feature", "AI/Preferences")]
public class PreferenceScopeTests(AspireAppFixture fixture) : PreferenceTestBase(fixture)
{
    [Theory]
    [Trait("Bug", "82900")]
    [InlineData("room")]
    [InlineData("folder")]
    public async Task SetDeepMode_WithARoomOrFolderId_WritesTheUsersValueWhereEveryReadLooks(string kind)
    {
        var entityId = await CreateEntityAsync(kind);
        await SetDeepModeAsync(false);

        await SetDeepModeAsync(true, entityId);

        var expected = new PreferenceState(true, true, AiReasoningLevel.Medium);
        (await ReadStateAsync()).Should().Be(expected);
        (await ReadStateAsync(entityId)).Should().Be(expected, $"the {kind} keeps what was written to it");
    }

    [Fact]
    [Trait("Bug", "84418")]
    public async Task SetDeepMode_WithANonexistentId_WritesTheUsersValueWhereEveryReadLooks()
    {
        var entityId = await CreateEntityAsync("nonexistent");
        await SetDeepModeAsync(false);

        await SetDeepModeAsync(true, entityId);

        var expected = new PreferenceState(true, true, AiReasoningLevel.Medium);
        (await ReadStateAsync()).Should().Be(expected);
        (await ReadStateAsync(entityId)).Should().Be(expected);
    }

    [Theory]
    [Trait("Bug", "84306")]
    [InlineData("room")]
    [InlineData("folder")]
    [InlineData("nonexistent")]
    public async Task SetReasoningLevel_WithAnyEntityId_WritesTheUsersValueWhereEveryReadLooks(string kind)
    {
        var entityId = await CreateEntityAsync(kind);
        await SetLevelAsync(AiReasoningLevel.Low);

        await SetLevelAsync(AiReasoningLevel.Max, entityId);

        var expected = new PreferenceState(true, true, AiReasoningLevel.Max);
        (await ReadStateAsync()).Should().Be(expected);
        (await ReadStateAsync(entityId)).Should().Be(expected, $"a {kind} reads the one value");
    }

    [Theory]
    [Trait("Bug", "84419")]
    [InlineData("room")]
    [InlineData("folder")]
    [InlineData("nonexistent")]
    public async Task ClearDeepMode_WithAnyEntityId_ClearsTheUsersValueAndAnswersSuccess(string kind)
    {
        var entityId = await CreateEntityAsync(kind);
        await SetLevelAsync(AiReasoningLevel.Low);

        await ClearAsync(entityId);

        (await ReadStateAsync()).Should().Be(_unset);
        (await ReadStateAsync(entityId)).Should().Be(_unset);
    }

    [Theory]
    [Trait("Bug", "84420")]
    [InlineData("room")]
    [InlineData("folder")]
    [InlineData("nonexistent")]
    public async Task ReadRoutes_WithAnyEntityId_ServeTheUsersOneValue(string kind)
    {
        var entityId = await CreateEntityAsync(kind);
        await SetLevelAsync(AiReasoningLevel.Medium);

        // All three readers: a location with nothing of its own has no separate value to report.
        (await ReadStateAsync(entityId)).Should().Be(new PreferenceState(true, true, AiReasoningLevel.Medium));
    }

    [Fact]
    public async Task SetDeepMode_ForOneAiRoom_IsTheValueOfEveryOtherAiRoom()
    {
        // Replaces "two agents keep separate values" and "clearing one agent leaves the other": an AI room is the
        // successor of the agent, and it is no scope either.
        var first = await CreateRoomAsync("Autotest Preference AI Room A");
        var second = await CreateRoomAsync("Autotest Preference AI Room B");

        await SetDeepModeAsync(true, first.ToString());

        var expected = new PreferenceState(true, true, AiReasoningLevel.Medium);
        (await ReadStateAsync(second.ToString())).Should().Be(expected);

        await ClearAsync(first.ToString());

        (await ReadStateAsync(second.ToString())).Should().Be(_unset);
    }
}

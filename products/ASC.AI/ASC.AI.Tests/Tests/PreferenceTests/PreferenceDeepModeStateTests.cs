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
/// Deep mode as a switch: unset by default, on and off both remembered as a choice, cleared back to unset, and how it
/// relates to a stored reasoning depth (off replaces it, on keeps it, on after off lands on <c>medium</c>).
/// </summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "AI/Preferences")]
public class PreferenceDeepModeStateTests(AspireAppFixture fixture) : PreferenceTestBase(fixture)
{
    [Fact]
    public async Task DeepMode_Fresh_IsOffAndUnset()
    {
        (await ReadStateAsync()).Should().Be(_unset);
    }

    [Fact]
    public async Task SetDeepMode_On_IsStoredAndLandsOnMedium()
    {
        await SetDeepModeAsync(true);

        // is-deep-mode-set is the "has the user ever chosen" flag; a depth turns the switch on.
        (await ReadStateAsync()).Should().Be(new PreferenceState(true, true, AiReasoningLevel.Medium));
    }

    [Fact]
    public async Task SetDeepMode_ExplicitOff_IsRememberedAsAChoice()
    {
        await SetDeepModeAsync(true);
        await SetDeepModeAsync(false);

        // Off, but chosen: the pair (false, set) is what lets a client leave the switch alone.
        (await ReadStateAsync()).Should().Be(new PreferenceState(false, true, AiReasoningLevel.Off));
    }

    [Fact]
    public async Task ClearDeepMode_ReturnsTheSettingToUnset_AndClearingAgainIsAccepted()
    {
        await SetDeepModeAsync(true);

        await ClearAsync();

        (await ReadStateAsync()).Should().Be(_unset);

        // Clearing an already-clear setting is accepted rather than refused.
        await ClearAsync();
        (await ReadStateAsync()).Should().Be(_unset);
    }

    [Fact]
    public async Task ClearDeepMode_AfterADepthWasChosen_ReadsOffOffAndUnset()
    {
        await SetLevelAsync(AiReasoningLevel.High);
        (await ReadStateAsync()).Level.Should().Be(AiReasoningLevel.High, "control: the depth was stored");

        await ClearAsync();

        (await ReadStateAsync()).Should().Be(_unset);
    }

    [Theory]
    [InlineData(AiReasoningLevel.Low)]
    [InlineData(AiReasoningLevel.Medium)]
    [InlineData(AiReasoningLevel.High)]
    [InlineData(AiReasoningLevel.Max)]
    public async Task SetDeepMode_On_KeepsAStoredDepth(AiReasoningLevel level)
    {
        await SetLevelAsync(level);

        await SetDeepModeAsync(true);

        (await ReadStateAsync()).Should().Be(new PreferenceState(true, true, level), "an on that names no depth must not reset a chosen one");
    }

    [Fact]
    public async Task SetDeepMode_Off_ReplacesAStoredDepthWithOff()
    {
        await SetLevelAsync(AiReasoningLevel.High);

        await SetDeepModeAsync(false);

        (await ReadStateAsync()).Should().Be(new PreferenceState(false, true, AiReasoningLevel.Off));
    }

    [Fact]
    public async Task SetDeepMode_OnAfterOff_LandsOnMediumNotOnTheEarlierDepth()
    {
        await SetLevelAsync(AiReasoningLevel.High);
        await SetDeepModeAsync(false);
        // Control: the off really replaced the depth, so nothing hidden survives to be found below.
        (await ReadStateAsync()).Level.Should().Be(AiReasoningLevel.Off);

        await SetDeepModeAsync(true);

        (await ReadStateAsync()).Should().Be(new PreferenceState(true, true, AiReasoningLevel.Medium));
    }
}

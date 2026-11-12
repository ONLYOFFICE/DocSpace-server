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
/// The reasoning level (<c>off | low | medium | high | max</c>, lower-case) on its own: unset reads <c>off</c>, every
/// level is stored and read back, a write twice is idempotent, and anything outside the five levels is refused
/// without touching the stored one.
/// </summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "AI/Preferences")]
public class PreferenceReasoningLevelTests(AspireAppFixture fixture) : PreferenceTestBase(fixture)
{
    [Fact]
    public async Task GetReasoningLevel_Unset_ReadsOffAndUnset()
    {
        (await _aiPreferences.AiPreferencesGetReasoningLevelAsync(cancellationToken: TestContext.Current.CancellationToken))
            .Should().Be(AiReasoningLevel.Off);
        (await ReadStateAsync()).Should().Be(_unset);
    }

    [Fact]
    public async Task SetReasoningLevel_EveryLevel_IsStoredAndReadBack()
    {
        // Walked off -> max -> off so that each step is a change from the previous value, never a repeat of it.
        AiReasoningLevel[] walk = [AiReasoningLevel.Off, AiReasoningLevel.Low, AiReasoningLevel.Medium, AiReasoningLevel.High, AiReasoningLevel.Max, AiReasoningLevel.Off];

        foreach (var level in walk)
        {
            await SetLevelAsync(level);

            // A depth turns deep mode on, off turns it off, and an explicit off is a stored choice too.
            (await ReadStateAsync()).Should().Be(new PreferenceState(level != AiReasoningLevel.Off, true, level), $"after storing {level}");
        }
    }

    [Fact]
    public async Task SetReasoningLevel_SameLevelTwice_IsIdempotent()
    {
        for (var attempt = 1; attempt <= 2; attempt++)
        {
            await SetLevelAsync(AiReasoningLevel.High);

            (await ReadStateAsync()).Should().Be(new PreferenceState(true, true, AiReasoningLevel.High), $"write {attempt}");
        }
    }

    [Theory]
    [InlineData("""{"value":"ultra"}""")]
    [InlineData("""{"value":"High"}""")]
    [InlineData("""{"value":"LOW"}""")]
    [InlineData("""{"value":""}""")]
    [InlineData("""{"value":3}""")]
    [InlineData("""{"value":true}""")]
    [InlineData("""{"value":null}""")]
    [InlineData("{}")]
    public async Task SetReasoningLevel_ValueOutsideTheFiveLevels_Returns400AndChangesNothing(string body)
    {
        // Positive control: a stored `high` is what an accepted bad write would overwrite. On an unset scope a refusal
        // and a silent no-op both read `off`. Raw: the SDK enum cannot carry a value outside its members.
        await SetLevelAsync(AiReasoningLevel.High);

        var (status, error) = await RawAsync(HttpMethod.Put, "set-reasoning-level", body);

        (await ReadStateAsync()).Should().Be(new PreferenceState(true, true, AiReasoningLevel.High));
        status.Should().Be(HttpStatusCode.BadRequest, error);
    }
}

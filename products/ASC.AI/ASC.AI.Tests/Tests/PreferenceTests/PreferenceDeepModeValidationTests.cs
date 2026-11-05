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
/// <c>PUT set-deep-mode</c> takes a real JSON boolean and nothing else: a string (Bug 82813), a missing value (Bug
/// 82814) or any other type is a 400 that leaves the stored value alone — no coercion, no write of the default.
/// All bodies are raw: the SDK model carries a bool and always sends it.
/// </summary>
[Trait("Category", "Validation")]
[Trait("Feature", "AI/Preferences")]
public class PreferenceDeepModeValidationTests(AspireAppFixture fixture) : PreferenceTestBase(fixture)
{
    private static readonly PreferenceState _seeded = new(true, true, AiReasoningLevel.High);

    [Fact]
    [Trait("Bug", "82813")]
    public async Task SetDeepMode_ANonBooleanString_Returns400AndKeepsTheStoredValue()
    {
        // Start from an explicitly chosen "off", so a coercion to true would be visible as a change.
        await SetDeepModeAsync(false);

        var (status, error) = await RawAsync(HttpMethod.Put, "set-deep-mode", """{"value":"yes"}""");

        (await ReadStateAsync()).Should().Be(new PreferenceState(false, true, AiReasoningLevel.Off));
        status.Should().Be(HttpStatusCode.BadRequest, error);
    }

    [Fact]
    [Trait("Bug", "82814")]
    public async Task SetDeepMode_AnEmptyBody_Returns400AndKeepsTheStoredValue()
    {
        await SetDeepModeAsync(true);

        // A body with no `value` used to be taken as "set it to false".
        var (status, error) = await RawAsync(HttpMethod.Put, "set-deep-mode", "{}");

        (await ReadStateAsync()).Should().Be(new PreferenceState(true, true, AiReasoningLevel.Medium));
        status.Should().Be(HttpStatusCode.BadRequest, error);
    }

    [Theory]
    [InlineData("""{"value":null}""")]
    [InlineData("""{"value":0}""")]
    [InlineData("""{"value":1}""")]
    [InlineData("""{"value":""}""")]
    [InlineData("""{"value":"false"}""")]
    [InlineData("""{"value":"true"}""")]
    [InlineData("""{"value":"TRUE"}""")]
    [InlineData("""{"value":[]}""")]
    [InlineData("""{"value":[true]}""")]
    [InlineData("""{"value":{}}""")]
    public async Task SetDeepMode_EveryOtherNonBooleanValue_Returns400AndKeepsTheStoredValue(string body)
    {
        // The seed is a depth: an accepted `false` would have replaced it with off, an accepted truthy value would
        // have kept it, so deep mode alone could not tell a no-op from a coercion.
        await SetLevelAsync(AiReasoningLevel.High);

        var (status, error) = await RawAsync(HttpMethod.Put, "set-deep-mode", body);

        (await ReadStateAsync()).Should().Be(_seeded);
        status.Should().Be(HttpStatusCode.BadRequest, error);
    }
}

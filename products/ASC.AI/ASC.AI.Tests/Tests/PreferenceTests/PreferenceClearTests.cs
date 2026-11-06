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
/// <c>DELETE clear-deep-mode</c> and its body: the body carries an optional <c>entityId</c>, as an object or bare
/// (Bug 82815 — a bare id answered success and cleared nothing). No body, an empty or null one, and a null, empty or
/// zero <c>entityId</c> all name the user's own scope; a malformed id is a 400 and clears nothing.
/// </summary>
/// <remarks>
/// As the TypeScript suite measured: no body and an empty object are a 400 (the target has to be named, even as null),
/// and the id in the query is not read at all. The query form is not ported.
/// </remarks>
[Trait("Category", "Validation")]
[Trait("Feature", "AI/Preferences")]
public class PreferenceClearTests(AspireAppFixture fixture) : PreferenceTestBase(fixture)
{
    [Theory]
    [InlineData(null)]
    [InlineData("{}")]
    public async Task Clear_WithNoBodyOrAnEmptyObject_Returns400AndClearsNothing(string? body)
    {
        await SetLevelAsync(AiReasoningLevel.Low);

        // Raw: the SDK always sends a string body.
        var (status, answer) = await RawAsync(HttpMethod.Delete, "clear-deep-mode", body);

        (await ReadStateAsync()).Should().Be(new PreferenceState(true, true, AiReasoningLevel.Low));
        status.Should().Be(HttpStatusCode.BadRequest, answer);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("""{"entityId":null}""")]
    [InlineData("""{"entityId":""}""")]
    [InlineData("""{"entityId":"0"}""")]
    public async Task Clear_WithNoScopeInTheBody_ClearsTheUsersValue(string body)
    {
        await SetLevelAsync(AiReasoningLevel.Low);

        // Raw: the SDK always sends a string body, never none, a JSON null or an object.
        var (status, answer) = await RawAsync(HttpMethod.Delete, "clear-deep-mode", body);

        (await ReadStateAsync()).Should().Be(_unset);
        status.Should().Be(HttpStatusCode.OK, answer);
        answer.Should().Contain("\"success\":true");
    }

    [Fact]
    [Trait("Bug", "82815")]
    public async Task Clear_WithABareEntityIdString_ClearsAndReportsSuccess()
    {
        var roomId = await CreateRoomAsync("Autotest Preference AI Room");
        await SetLevelAsync(AiReasoningLevel.Low);

        // The SDK sends the string as the whole body: the bare form.
        await ClearAsync(roomId.ToString());

        (await ReadStateAsync()).Should().Be(_unset, "a clear that reports success must actually clear");
    }

    [Fact]
    [Trait("Bug", "82815")]
    public async Task Clear_WithABareNumericEntityId_ClearsAndReportsSuccess()
    {
        var roomId = await CreateRoomAsync("Autotest Preference AI Room");
        await SetLevelAsync(AiReasoningLevel.Low);

        // Raw: a JSON number as the whole body, which a string parameter cannot send.
        var (status, answer) = await RawAsync(HttpMethod.Delete, "clear-deep-mode", roomId.ToString());

        (await ReadStateAsync()).Should().Be(_unset, "a clear that reports success must actually clear");
        status.Should().Be(HttpStatusCode.OK, answer);
    }

    [Theory]
    [InlineData("\"abc\"")]
    [InlineData("-5")]
    public async Task Clear_WithABareMalformedEntityId_Returns400AndClearsNothing(string body)
    {
        await SetLevelAsync(AiReasoningLevel.Low);

        // Raw: a bare negative number is not a string, so the typed call cannot send it.
        var (status, answer) = await RawAsync(HttpMethod.Delete, "clear-deep-mode", body);

        (await ReadStateAsync()).Should().Be(new PreferenceState(true, true, AiReasoningLevel.Low));
        status.Should().Be(HttpStatusCode.BadRequest, answer);
    }

    [Fact]
    public async Task Clear_WithTheObjectForm_ClearsTheUsersValue()
    {
        var roomId = await CreateRoomAsync("Autotest Preference AI Room");
        await SetLevelAsync(AiReasoningLevel.High);

        var (status, answer) = await RawAsync(HttpMethod.Delete, "clear-deep-mode", $$"""{"entityId":"{{roomId}}"}""");

        (await ReadStateAsync()).Should().Be(_unset);
        status.Should().Be(HttpStatusCode.OK, answer);
    }

    [Fact]
    public async Task Clear_NothingEverStored_IsAcceptedAndChangesNothing()
    {
        await ClearAsync();

        (await ReadStateAsync()).Should().Be(_unset);
    }
}

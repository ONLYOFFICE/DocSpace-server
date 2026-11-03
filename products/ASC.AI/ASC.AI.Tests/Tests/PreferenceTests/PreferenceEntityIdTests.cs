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
/// The <c>entityId</c> of every preferences route has to be a room id: a positive integer, with <c>""</c> and
/// <c>"0"</c> naming the user's own scope. A thread id or any malformed value is a 400 that moves nothing, on the write
/// routes (Bug 84026) and on the read routes alike (Bug 84417).
/// </summary>
[Trait("Category", "Validation")]
[Trait("Feature", "AI/Preferences")]
public class PreferenceEntityIdTests(AspireAppFixture fixture) : PreferenceTestBase(fixture)
{
    private const string ScopeError = "entityId must be a room id (a positive integer)";

    private static readonly PreferenceState _seeded = new(true, true, AiReasoningLevel.High);

    // A depth chosen on purpose: an accepted bad write would move it, a refused one cannot.
    private Task SeedAsync() => SetLevelAsync(AiReasoningLevel.High);

    private static string Body(string route, string entityId)
    {
        var entityJson = JsonSerializer.Serialize(entityId);

        return route switch
        {
            "set-deep-mode" => $$"""{"value":false,"entityId":{{entityJson}}}""",
            "set-reasoning-level" => $$"""{"value":"max","entityId":{{entityJson}}}""",
            _ => $$"""{"entityId":{{entityJson}}}"""
        };
    }

    // Raw: the typed calls cannot be pointed at a route with an id the SDK model would never carry in these forms.
    private Task<(HttpStatusCode Status, string Body)> WriteAsync(string route, string entityId) =>
        RawAsync(route == "clear-deep-mode" ? HttpMethod.Delete : HttpMethod.Put, route, Body(route, entityId));

    [Theory]
    [Trait("Bug", "84026")]
    [InlineData("set-deep-mode")]
    [InlineData("set-reasoning-level")]
    [InlineData("clear-deep-mode")]
    public async Task WriteRoute_WithAThreadId_Returns400AndMovesNoScope(string route)
    {
        await SeedAsync();
        var thread = await CreateOwnThreadAsync("Autotest preference thread");

        var (status, error) = await WriteAsync(route, thread.ThreadId);

        (await ReadStateAsync()).Should().Be(_seeded, "no scope moves");
        status.Should().Be(HttpStatusCode.BadRequest, error);
        error.Should().Contain(ScopeError);
    }

    [Theory]
    [Trait("Bug", "84026")]
    [InlineData("get-deep-mode")]
    [InlineData("is-deep-mode-set")]
    [InlineData("get-reasoning-level")]
    public async Task ReadRoute_WithAThreadId_Returns400(string route)
    {
        await SeedAsync();
        var thread = await CreateOwnThreadAsync("Autotest preference thread");

        var (status, error) = await RawReadAsync(route, thread.ThreadId);

        status.Should().Be(HttpStatusCode.BadRequest, error);
        error.Should().Contain(ScopeError);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("-5")]
    [InlineData("1.5")]
    [InlineData(" ")]
    public async Task WriteRoutes_WithAMalformedEntityId_Return400AndWriteNowhere(string entityId)
    {
        await SeedAsync();

        var setDeepMode = await WriteAsync("set-deep-mode", entityId);
        var setLevel = await WriteAsync("set-reasoning-level", entityId);
        var clear = await WriteAsync("clear-deep-mode", entityId);

        // State first: a refusal that still wrote or cleared is the worse defect.
        (await ReadStateAsync()).Should().Be(_seeded);
        setDeepMode.Status.Should().Be(HttpStatusCode.BadRequest, setDeepMode.Body);
        setLevel.Status.Should().Be(HttpStatusCode.BadRequest, setLevel.Body);
        clear.Status.Should().Be(HttpStatusCode.BadRequest, clear.Body);
    }

    [Theory]
    [Trait("Bug", "84417")]
    [InlineData("get-deep-mode")]
    [InlineData("is-deep-mode-set")]
    [InlineData("get-reasoning-level")]
    public async Task ReadRoute_WithAMalformedEntityId_Returns400LikeTheWriteRoutes(string route)
    {
        await SeedAsync();

        // Control: the user-wide read works, so a 400 below is the route judging the id and not a dead route.
        var control = await RawReadAsync(route, "0");

        var statuses = new Dictionary<string, HttpStatusCode>();
        foreach (var entityId in new[] { "abc", "-5", "1.5", " " })
        {
            statuses[entityId] = (await RawReadAsync(route, entityId)).Status;
        }

        control.Status.Should().Be(HttpStatusCode.OK, control.Body);
        statuses.Values.Should().OnlyContain(s => s == HttpStatusCode.BadRequest, $"statuses were {string.Join(", ", statuses)}");
    }

    [Theory]
    [InlineData("")]
    [InlineData("0")]
    public async Task EveryRoute_WithAnEmptyOrZeroEntityId_IsTheUsersOwnScope(string token)
    {
        await SetLevelAsync(AiReasoningLevel.High);

        // set-reasoning-level with the token writes the user's value ...
        await SetLevelAsync(AiReasoningLevel.Low, token);
        (await ReadStateAsync()).Should().Be(new PreferenceState(true, true, AiReasoningLevel.Low));

        // ... set-deep-mode too: off is stored, as a choice, in the same place ...
        await SetDeepModeAsync(false, token);
        (await ReadStateAsync()).Should().Be(new PreferenceState(false, true, AiReasoningLevel.Off));

        // ... all three readers resolve the token to it ...
        await SetLevelAsync(AiReasoningLevel.Max);
        (await ReadStateAsync(token)).Should().Be(new PreferenceState(true, true, AiReasoningLevel.Max));

        // ... and clear-deep-mode clears it.
        await ClearAsync(token);
        (await ReadStateAsync()).Should().Be(_unset);
    }
}

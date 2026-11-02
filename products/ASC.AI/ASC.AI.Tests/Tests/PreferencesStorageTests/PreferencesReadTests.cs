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

namespace ASC.AI.Tests.Tests.PreferencesStorageTests;

[Trait("Category", "CRUD")]
[Trait("Feature", "AI/Preferences")]
public class PreferencesReadTests(AspireAppFixture fixture) : PreferencesTestBase(fixture)
{
    [Fact]
    public async Task Read_Global_NoneStored_ReadsAsOff()
    {
        // Nothing stored: the level reads as the default "off", and is-deep-mode-set reports no row.
        (await IsDepthStoredAsync()).Should().BeFalse();
        (await GetReasoningLevelAsync()).Should().Be(AiReasoningLevel.Off);
    }

    [Fact]
    public async Task Read_Global_Existing_ReturnsValue()
    {
        await SetReasoningLevelAsync(AiReasoningLevel.High);

        (await IsDepthStoredAsync()).Should().BeTrue();
        (await GetReasoningLevelAsync()).Should().Be(AiReasoningLevel.High);
    }

    [Fact]
    public async Task Read_WithRoomEntityId_ServesUserWideValue()
    {
        // Publicly the depth is one value per user: a write from a room is what a read from anywhere sees.
        var roomId = await CreateRoomAsync();
        await SetReasoningLevelAsync(AiReasoningLevel.High, roomId.ToString());

        (await GetReasoningLevelAsync()).Should().Be(AiReasoningLevel.High);
        (await GetReasoningLevelAsync(roomId.ToString())).Should().Be(AiReasoningLevel.High);
    }

    [Fact]
    public async Task Read_NonNumericEntityId_Returns400()
    {
        // Raw on purpose — an SDK defect: the operation returns an enum, and on any non-2xx the generated
        // ApiClient.ToApiResponse unboxes the null result into it, so the typed call throws NullReferenceException
        // instead of ApiException. Same for every operation returning an enum or a bool.
        using var response = await new RawApiClient(_aiChatClient).GetAsync(
            "/api/2.0/ai/preferences/get-reasoning-level?entityId=not-a-room",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // Internal contract: entity-scoped preferences have no public route (the Node preferences routes validate entityId and drop it).
    [Fact]
    public async Task Read_WithEntityId_NoneStored_ReturnsNull()
    {
        var roomId = await CreateRoomAsync();

        var preferences = await ReadPreferencesInternalAsync(roomId.ToString());

        preferences.Should().BeNull();
    }

    // Internal contract: entity-scoped preferences have no public route (the Node preferences routes validate entityId and drop it).
    [Fact]
    public async Task Read_WithEntityId_Existing_ReturnsScopedValue()
    {
        var roomId = await CreateRoomAsync();
        await UpsertPreferencesInternalAsync(depth: ReasoningDepth.High, entityId: roomId.ToString());

        var preferences = await ReadPreferencesInternalAsync(roomId.ToString());

        preferences.Should().NotBeNull();
        preferences!.Depth.Should().Be(ReasoningDepth.High);
    }

    // Internal contract: entity-scoped preferences have no public route (the Node preferences routes validate entityId and drop it).
    [Fact]
    public async Task Read_Global_NotAffectedByScoped()
    {
        var roomId = await CreateRoomAsync();
        await UpsertPreferencesInternalAsync(depth: ReasoningDepth.High, entityId: roomId.ToString());

        var global = await ReadPreferencesInternalAsync();

        global.Should().BeNull();
    }

    // Internal contract: entity-scoped preferences have no public route (the Node preferences routes validate entityId and drop it).
    [Fact]
    public async Task Read_WithEntityId_NotAffectedByGlobal()
    {
        var roomId = await CreateRoomAsync();
        await UpsertPreferencesInternalAsync(depth: ReasoningDepth.High);

        var scoped = await ReadPreferencesInternalAsync(roomId.ToString());

        scoped.Should().BeNull();
    }

    // Internal contract: entity-scoped preferences have no public route (the Node preferences routes validate entityId and drop it).
    [Fact]
    public async Task Read_GlobalAndScoped_StoreDifferentValues()
    {
        var roomId = await CreateRoomAsync();
        await UpsertPreferencesInternalAsync(depth: ReasoningDepth.High);
        await UpsertPreferencesInternalAsync(depth: ReasoningDepth.None, entityId: roomId.ToString());

        var global = await ReadPreferencesInternalAsync();
        var scoped = await ReadPreferencesInternalAsync(roomId.ToString());

        global.Should().NotBeNull();
        global!.Depth.Should().Be(ReasoningDepth.High);

        scoped.Should().NotBeNull();
        scoped!.Depth.Should().Be(ReasoningDepth.None);
    }

    // Internal contract: entity-scoped preferences have no public route (the Node preferences routes never forward entityId, so a nonexistent room is not looked up).
    [Fact]
    public async Task Read_NonExistentEntityId_Returns404()
    {
        using var response = await _ai.GetAsync(
            $"{PreferencesPath}?entityId=999999999",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

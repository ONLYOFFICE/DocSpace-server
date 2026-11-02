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
public class PreferencesDeleteTests(AspireAppFixture fixture) : PreferencesTestBase(fixture)
{
    [Fact]
    public async Task Delete_Global_RemovesValue()
    {
        await SetReasoningLevelAsync(AiReasoningLevel.High);

        await ClearAsync();

        (await IsDepthStoredAsync()).Should().BeFalse();
        (await GetReasoningLevelAsync()).Should().Be(AiReasoningLevel.Off);
    }

    [Fact]
    public async Task Delete_Global_NoneStored_Succeeds()
    {
        await ClearAsync();

        (await IsDepthStoredAsync()).Should().BeFalse();
    }

    // Internal contract: entity-scoped preferences have no public route (the Node preferences routes validate entityId and drop it).
    [Fact]
    public async Task Delete_WithEntityId_RemovesScopedValue()
    {
        var roomId = await CreateRoomAsync();
        await UpsertPreferencesInternalAsync(depth: ReasoningDepth.High, entityId: roomId.ToString());

        using var response = await _ai.DeleteAsync(
            $"{PreferencesPath}?entityId={roomId}",
            TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var stored = await ReadPreferencesInternalAsync(roomId.ToString());
        stored.Should().BeNull();
    }

    // Internal contract: entity-scoped preferences have no public route (the Node preferences routes validate entityId and drop it).
    [Fact]
    public async Task Delete_WithEntityId_NoneStored_NoContent()
    {
        var roomId = await CreateRoomAsync();

        using var response = await _ai.DeleteAsync(
            $"{PreferencesPath}?entityId={roomId}",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // Internal contract: entity-scoped preferences have no public route (the Node preferences routes validate entityId and drop it).
    [Fact]
    public async Task Delete_WithEntityId_DoesNotAffectGlobal()
    {
        var roomId = await CreateRoomAsync();
        await UpsertPreferencesInternalAsync(depth: ReasoningDepth.High);
        await UpsertPreferencesInternalAsync(depth: ReasoningDepth.None, entityId: roomId.ToString());

        using var response = await _ai.DeleteAsync(
            $"{PreferencesPath}?entityId={roomId}",
            TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var global = await ReadPreferencesInternalAsync();
        global.Should().NotBeNull();
        global!.Depth.Should().Be(ReasoningDepth.High);

        var scoped = await ReadPreferencesInternalAsync(roomId.ToString());
        scoped.Should().BeNull();
    }

    // Internal contract: entity-scoped preferences have no public route (the Node preferences routes validate entityId and drop it).
    [Fact]
    public async Task Delete_Global_DoesNotAffectScoped()
    {
        var roomId = await CreateRoomAsync();
        await UpsertPreferencesInternalAsync(depth: ReasoningDepth.High);
        await UpsertPreferencesInternalAsync(depth: ReasoningDepth.None, entityId: roomId.ToString());

        using var response = await _ai.DeleteAsync(
            PreferencesPath,
            TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var global = await ReadPreferencesInternalAsync();
        global.Should().BeNull();

        var scoped = await ReadPreferencesInternalAsync(roomId.ToString());
        scoped.Should().NotBeNull();
        scoped!.Depth.Should().Be(ReasoningDepth.None);
    }

    // Internal contract: entity-scoped preferences have no public route (the Node preferences routes validate entityId and drop it).
    [Fact]
    public async Task Delete_TwoEntities_OnlyTargetedRemoved()
    {
        var firstRoomId = await CreateRoomAsync();
        var secondRoomId = await CreateRoomAsync();
        await UpsertPreferencesInternalAsync(depth: ReasoningDepth.High, entityId: firstRoomId.ToString());
        await UpsertPreferencesInternalAsync(depth: ReasoningDepth.None, entityId: secondRoomId.ToString());

        using var response = await _ai.DeleteAsync(
            $"{PreferencesPath}?entityId={firstRoomId}",
            TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await ReadPreferencesInternalAsync(firstRoomId.ToString())).Should().BeNull();

        var second = await ReadPreferencesInternalAsync(secondRoomId.ToString());
        second.Should().NotBeNull();
        second!.Depth.Should().Be(ReasoningDepth.None);
    }

    // Internal contract: entity-scoped preferences have no public route (the Node preferences routes never forward entityId, so a nonexistent room is not looked up).
    [Fact]
    public async Task Delete_NonExistentEntityId_Returns404()
    {
        using var response = await _ai.DeleteAsync(
            $"{PreferencesPath}?entityId=999999999",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

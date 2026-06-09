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

[Collection("Test Collection")]
[Trait("Category", "CRUD")]
[Trait("Feature", "AI/Preferences")]
public class PreferencesUpsertTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task Upsert_Global_InsertsAndIsReadable()
    {
        using var response = await Ai.PutAsync(
            PreferencesPath,
            new { deepMode = true },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var stored = await ReadPreferencesAsync();
        stored.Should().NotBeNull();
        stored!.DeepMode.Should().BeTrue();
    }

    [Fact]
    public async Task Upsert_WithEntityId_InsertsAndIsReadable()
    {
        var roomId = await CreateRoomAsync();

        using var response = await Ai.PutAsync(
            PreferencesPath,
            new { deepMode = true, entityId = roomId.ToString() },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var stored = await ReadPreferencesAsync(roomId.ToString());
        stored.Should().NotBeNull();
        stored!.DeepMode.Should().BeTrue();
    }

    [Fact]
    public async Task Upsert_Global_TwiceUpdatesValue()
    {
        await UpsertPreferencesAsync(deepMode: true);
        await UpsertPreferencesAsync(deepMode: false);

        var stored = await ReadPreferencesAsync();
        stored.Should().NotBeNull();
        stored!.DeepMode.Should().BeFalse();
    }

    [Fact]
    public async Task Upsert_WithEntityId_TwiceUpdatesValue()
    {
        var roomId = await CreateRoomAsync();

        await UpsertPreferencesAsync(deepMode: true, entityId: roomId.ToString());
        await UpsertPreferencesAsync(deepMode: false, entityId: roomId.ToString());

        var stored = await ReadPreferencesAsync(roomId.ToString());
        stored.Should().NotBeNull();
        stored!.DeepMode.Should().BeFalse();
    }

    [Fact]
    public async Task Upsert_WithEntityId_DoesNotAffectGlobal()
    {
        var roomId = await CreateRoomAsync();
        await UpsertPreferencesAsync(deepMode: true);

        await UpsertPreferencesAsync(deepMode: false, entityId: roomId.ToString());

        var global = await ReadPreferencesAsync();
        global.Should().NotBeNull();
        global!.DeepMode.Should().BeTrue();
    }

    [Fact]
    public async Task Upsert_Global_DoesNotAffectScoped()
    {
        var roomId = await CreateRoomAsync();
        await UpsertPreferencesAsync(deepMode: true, entityId: roomId.ToString());

        await UpsertPreferencesAsync(deepMode: false);

        var scoped = await ReadPreferencesAsync(roomId.ToString());
        scoped.Should().NotBeNull();
        scoped!.DeepMode.Should().BeTrue();
    }

    [Fact]
    public async Task Upsert_TwoDifferentEntities_AreIsolated()
    {
        var firstRoomId = await CreateRoomAsync();
        var secondRoomId = await CreateRoomAsync();

        await UpsertPreferencesAsync(deepMode: true, entityId: firstRoomId.ToString());
        await UpsertPreferencesAsync(deepMode: false, entityId: secondRoomId.ToString());

        var first = await ReadPreferencesAsync(firstRoomId.ToString());
        var second = await ReadPreferencesAsync(secondRoomId.ToString());

        first.Should().NotBeNull();
        first!.DeepMode.Should().BeTrue();

        second.Should().NotBeNull();
        second!.DeepMode.Should().BeFalse();
    }

    [Fact]
    public async Task Upsert_NullDeepMode_Persisted()
    {
        await UpsertPreferencesAsync(deepMode: true);

        using var response = await Ai.PutAsync(
            PreferencesPath,
            new { deepMode = (bool?)null },
            TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var stored = await ReadPreferencesAsync();
        stored.Should().NotBeNull();
        stored!.DeepMode.Should().BeNull();
    }

    [Fact]
    public async Task Upsert_NonExistentEntityId_Returns404()
    {
        using var response = await Ai.PutAsync(
            PreferencesPath,
            new { deepMode = true, entityId = "999999999" },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

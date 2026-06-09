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
public class PreferencesReadTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task Read_Global_NoneStored_ReturnsNull()
    {
        var preferences = await ReadPreferencesAsync();

        preferences.Should().BeNull();
    }

    [Fact]
    public async Task Read_Global_Existing_ReturnsValue()
    {
        await UpsertPreferencesAsync(deepMode: true);

        var preferences = await ReadPreferencesAsync();

        preferences.Should().NotBeNull();
        preferences!.DeepMode.Should().BeTrue();
    }

    [Fact]
    public async Task Read_WithEntityId_NoneStored_ReturnsNull()
    {
        var roomId = await CreateRoomAsync();

        var preferences = await ReadPreferencesAsync(roomId.ToString());

        preferences.Should().BeNull();
    }

    [Fact]
    public async Task Read_WithEntityId_Existing_ReturnsScopedValue()
    {
        var roomId = await CreateRoomAsync();
        await UpsertPreferencesAsync(deepMode: true, entityId: roomId.ToString());

        var preferences = await ReadPreferencesAsync(roomId.ToString());

        preferences.Should().NotBeNull();
        preferences!.DeepMode.Should().BeTrue();
    }

    [Fact]
    public async Task Read_Global_NotAffectedByScoped()
    {
        var roomId = await CreateRoomAsync();
        await UpsertPreferencesAsync(deepMode: true, entityId: roomId.ToString());

        var global = await ReadPreferencesAsync();

        global.Should().BeNull();
    }

    [Fact]
    public async Task Read_WithEntityId_NotAffectedByGlobal()
    {
        var roomId = await CreateRoomAsync();
        await UpsertPreferencesAsync(deepMode: true);

        var scoped = await ReadPreferencesAsync(roomId.ToString());

        scoped.Should().BeNull();
    }

    [Fact]
    public async Task Read_GlobalAndScoped_StoreDifferentValues()
    {
        var roomId = await CreateRoomAsync();
        await UpsertPreferencesAsync(deepMode: true);
        await UpsertPreferencesAsync(deepMode: false, entityId: roomId.ToString());

        var global = await ReadPreferencesAsync();
        var scoped = await ReadPreferencesAsync(roomId.ToString());

        global.Should().NotBeNull();
        global!.DeepMode.Should().BeTrue();

        scoped.Should().NotBeNull();
        scoped!.DeepMode.Should().BeFalse();
    }

    [Fact]
    public async Task Read_NonExistentEntityId_Returns404()
    {
        using var response = await Ai.GetAsync(
            $"{PreferencesPath}?entityId=999999999",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

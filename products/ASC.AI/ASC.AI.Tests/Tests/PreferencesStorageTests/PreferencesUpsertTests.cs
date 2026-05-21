// (c) Copyright Ascensio System SIA 2009-2026
//
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
//
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
//
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
//
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

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

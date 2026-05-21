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

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

namespace ASC.Files.Tests.Tests._08_Privacy;

/// <summary>
/// <c>GET /api/2.0/privacyroom/{roomId}/access</c> — getUserKeysForRoom: the access-key set of a
/// private room, as seen by its creator.
/// </summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "PrivacyRoom")]
public class RoomAccessKeysTests(AspireAppFixture fixture) : PrivacyRoomTestBase(fixture)
{
    [Fact]
    public async Task GetUserKeysForRoom_PrivateRoom_ReturnsAccessKeys()
    {
        await _filesClient.Authenticate(Owner);
        var ownerKey = await SetFakeKeys(publicKeyPrefix: "owner");

        var room = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto("Autotest Privacy Room", roomType: RoomType.CustomRoom, @private: true),
            TestContext.Current.CancellationToken)).Response;
        room.Private.Should().BeTrue();

        var keys = (await _privacyRoomApi.GetUserKeysForRoomAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        keys.Select(k => k.PublicKey).Should().Contain(ownerKey.PublicKey);
    }

    [Fact]
    public async Task GetUserKeysForRoom_EveryKeyTheCallerHolds_IsReturned()
    {
        // A user may hold several keys and the room does not pin one of them: the response is a
        // live view of the caller's whole key set.
        await _filesClient.Authenticate(Owner);

        var keys = new[]
        {
            await SetFakeKeys(Guid.Empty, "zero"),
            await SetFakeKeys(Guid.NewGuid(), "a"),
            await SetFakeKeys(Guid.NewGuid(), "b")
        };

        var room = await CreatePrivateRoom("Autotest Privacy Room", RoomType.CustomRoom);

        var roomKeys = (await _privacyRoomApi.GetUserKeysForRoomAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        roomKeys.Should().HaveCount(3);
        roomKeys.Select(k => k.PublicKey).Should().BeEquivalentTo(keys.Select(k => k.PublicKey));
    }

    [Fact]
    public async Task GetUserKeysForRoom_RotatingOneOfSeveralKeys_OthersUntouched()
    {
        await _filesClient.Authenticate(Owner);

        var zeroKey = await SetFakeKeys(publicKeyPrefix: "zero");
        var idA = Guid.NewGuid();
        await SetFakeKeys(idA, "a");
        var room = await CreatePrivateRoom("Autotest Privacy Room", RoomType.CustomRoom);

        var pkARotated = $"pk-a-rotated-{Guid.NewGuid():N}";
        await _privacyRoomApi.ReplaceKeyAsync(new EncryptionKeyRequestDto(idA, pkARotated, "ap2"), TestContext.Current.CancellationToken);

        var roomKeys = (await _privacyRoomApi.GetUserKeysForRoomAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        roomKeys.Select(k => k.PublicKey).Should().BeEquivalentTo([pkARotated, zeroKey.PublicKey]);
    }

    [Fact]
    public async Task GetUserKeysForRoom_DeletingOneOfSeveralKeys_DropsOnlyThatKey()
    {
        await _filesClient.Authenticate(Owner);

        var zeroKey = await SetFakeKeys(publicKeyPrefix: "zero");
        var idA = Guid.NewGuid();
        await SetFakeKeys(idA, "a");
        var room = await CreatePrivateRoom("Autotest Privacy Room", RoomType.CustomRoom);

        var before = (await _privacyRoomApi.GetUserKeysForRoomAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        before.Should().HaveCount(2);

        await _privacyRoomApi.DeleteKeysAsync(idA, TestContext.Current.CancellationToken);

        var after = (await _privacyRoomApi.GetUserKeysForRoomAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        after.Select(k => k.PublicKey).Should().BeEquivalentTo([zeroKey.PublicKey]);
    }

    [Fact]
    public async Task GetUserKeysForRoom_CreatorDeniedAfterDeletingAllKeys()
    {
        // Access is gated on the caller actually holding a key: once the creator deletes their
        // last key they are denied their OWN private room with 403, even though the room itself is
        // untouched and still listed as private.
        await _filesClient.Authenticate(Owner);
        await SetFakeKeys();
        var room = await CreatePrivateRoom("Autotest Privacy Room", RoomType.CustomRoom);

        await _privacyRoomApi.DeleteKeysAsync(Guid.Empty, TestContext.Current.CancellationToken);

        var remaining = await PollUntil(
            async () => (await _privacyRoomApi.GetUserKeysAsync(TestContext.Current.CancellationToken)).Response?.Count ?? 0,
            count => count == 0);
        remaining.Should().Be(0);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _privacyRoomApi.GetUserKeysForRoomAsync(room.Id, TestContext.Current.CancellationToken));
        exception.ErrorCode.Should().Be(403);

        // The room survives the key loss — only the key material is gone.
        var info = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        info.Private.Should().BeTrue();
    }

    [Fact]
    [Trait("Bug", "82804")]
    public async Task GetUserKeysForRoom_WipedKey_MustNotBeReportedAsRoomAccess()
    {
        // BUG 82804: after an empty-body replaceKey erases the key material, the row still exists,
        // so the endpoint answers 200 with an entry that carries NO publicKey — the caller is told
        // it has access to a room it can no longer decrypt.
        await _filesClient.Authenticate(Owner);
        await SetFakeKeys();
        var room = await CreatePrivateRoom("Autotest Privacy Room", RoomType.CustomRoom);

        await _privacyRoomApi.ReplaceKeyAsync(cancellationToken: TestContext.Current.CancellationToken);

        var keys = (await _privacyRoomApi.GetUserKeysForRoomAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        keys.Should().OnlyContain(k => !string.IsNullOrEmpty(k.PublicKey));
    }

    [Fact]
    public async Task GetUserKeysForRoom_ArchivedPrivateRoom_StillReturnsAccessKeys()
    {
        await _filesClient.Authenticate(Owner);
        var ownerKey = await SetFakeKeys(publicKeyPrefix: "owner");
        var room = await CreatePrivateRoom("Autotest Privacy Room", RoomType.CustomRoom);

        await _roomsApi.ArchiveRoomAsync(room.Id, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        var keys = (await _privacyRoomApi.GetUserKeysForRoomAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        keys.Select(k => k.PublicKey).Should().BeEquivalentTo([ownerKey.PublicKey]);
    }

    [Fact]
    public async Task GetUserKeysForRoom_DeletedPrivateRoom_ReturnsNotFound()
    {
        // Once the room is in Trash its keys are gone from the endpoint's point of view, so it
        // answers like a room that never existed.
        await _filesClient.Authenticate(Owner);
        await SetFakeKeys();
        var room = await CreatePrivateRoom("Autotest Privacy Room", RoomType.CustomRoom);

        await _roomsApi.DeleteRoomAsync(room.Id, new DeleteRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _privacyRoomApi.GetUserKeysForRoomAsync(room.Id, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task GetUserKeysForRoom_NonPrivateRoom_RejectedWithCleanClientError()
    {
        // A non-encrypted room has no access keys, so the endpoint rejects the call with 400.
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Plain Room");

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _privacyRoomApi.GetUserKeysForRoomAsync(room.Id, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
    }

    [Theory]
    [InlineData(999999999)]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetUserKeysForRoom_NonExistentOrOutOfRangeRoomId_ReturnsNotFound(int roomId)
    {
        await _filesClient.Authenticate(Owner);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _privacyRoomApi.GetUserKeysForRoomAsync(roomId, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }
}

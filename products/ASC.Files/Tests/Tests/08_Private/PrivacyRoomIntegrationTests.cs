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
/// End-to-end key lifecycle and how key management interacts with private-room membership.
/// The full encrypt-upload-share flow with real cryptography is already covered by
/// <c>PrivacyRoomTest.SetFileAccess_PrivateRoom_WithUserKeys_ReturnsOk</c>; these tests stay at the
/// membership/key-set level, which the API does not expose re-encryption material for anyway.
/// </summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "PrivacyRoom")]
public class PrivacyRoomIntegrationTests(AspireAppFixture fixture) : PrivacyRoomTestBase(fixture)
{
    [Fact]
    public async Task KeyLifecycle_SetGetReplaceDelete()
    {
        await _filesClient.Authenticate(Owner);

        var pk = $"pk-{Guid.NewGuid():N}";
        var newPk = $"pk-new-{Guid.NewGuid():N}";

        var created = (await _privacyRoomApi.SetKeysAsync(
            new EncryptionKeyRequestDto(publicKey: pk, privateKeyEnc: "prv"),
            TestContext.Current.CancellationToken)).Response;
        created.Should().ContainSingle();
        created[0].PublicKey.Should().Be(pk);

        var afterSet = (await _privacyRoomApi.GetUserKeysAsync(TestContext.Current.CancellationToken)).Response;
        afterSet.Should().ContainSingle();
        afterSet[0].PublicKey.Should().Be(pk);

        await _privacyRoomApi.ReplaceKeyAsync(
            new EncryptionKeyRequestDto(publicKey: newPk, privateKeyEnc: "prv2"),
            TestContext.Current.CancellationToken);
        var afterReplace = (await _privacyRoomApi.GetUserKeysAsync(TestContext.Current.CancellationToken)).Response;
        afterReplace.Should().ContainSingle();
        afterReplace[0].PublicKey.Should().Be(newPk);

        await _privacyRoomApi.DeleteKeysAsync(Guid.Empty, TestContext.Current.CancellationToken);
        var afterDelete = (await _privacyRoomApi.GetUserKeysAsync(TestContext.Current.CancellationToken)).Response;
        afterDelete.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task AddingThenRemovingAMember_UpdatesTheRoomsAccessKeySet()
    {
        // Verifies the room's access-key MEMBERSHIP LIST (who is allowed and whose public key is
        // part of the room's key set), not the cryptographic re-encryption itself: the API does not
        // expose the encrypted room key material.
        await _filesClient.Authenticate(Owner);
        var ownerKey = await SetFakeKeys(publicKeyPrefix: "owner");
        var room = await CreatePrivateRoom("Autotest Privacy Room", RoomType.CustomRoom);

        var member = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(member);
        var memberKey = await SetFakeKeys(publicKeyPrefix: "member");

        var beforeInvite = await Assert.ThrowsAsync<ApiException>(
            async () => await _privacyRoomApi.GetUserKeysForRoomAsync(room.Id, TestContext.Current.CancellationToken));
        beforeInvite.ErrorCode.Should().Be(403);

        await _filesClient.Authenticate(Owner);
        await InviteToRoom(room.Id, member, FileShare.ContentCreator);

        await _filesClient.Authenticate(member);
        var afterInvite = (await _privacyRoomApi.GetUserKeysForRoomAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        afterInvite.Select(k => k.PublicKey).Should().Contain([memberKey.PublicKey, ownerKey.PublicKey]);

        await _filesClient.Authenticate(Owner);
        await InviteToRoom(room.Id, member, FileShare.None);

        await _filesClient.Authenticate(member);
        var afterRemoval = await Assert.ThrowsAsync<ApiException>(
            async () => await _privacyRoomApi.GetUserKeysForRoomAsync(room.Id, TestContext.Current.CancellationToken));
        afterRemoval.ErrorCode.Should().Be(403);

        await _filesClient.Authenticate(Owner);
        var ownerView = (await _privacyRoomApi.GetUserKeysForRoomAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        ownerView.Select(k => k.PublicKey).Should().Contain(ownerKey.PublicKey);
        ownerView.Select(k => k.PublicKey).Should().NotContain(memberKey.PublicKey);
    }

    [Fact]
    public async Task ReplacingTheActiveKey_UpdatesTheRoomsAccessKeys()
    {
        await _filesClient.Authenticate(Owner);
        var oldKey = await SetFakeKeys(publicKeyPrefix: "old");
        var room = await CreatePrivateRoom("Autotest Privacy Room", RoomType.CustomRoom);

        var before = (await _privacyRoomApi.GetUserKeysForRoomAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        before.Select(k => k.PublicKey).Should().Contain(oldKey.PublicKey);

        var newPk = $"new-{Guid.NewGuid():N}";
        await _privacyRoomApi.ReplaceKeyAsync(
            new EncryptionKeyRequestDto(publicKey: newPk, privateKeyEnc: "np"),
            TestContext.Current.CancellationToken);

        var after = (await _privacyRoomApi.GetUserKeysForRoomAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        var publicKeys = after.Select(k => k.PublicKey).ToList();
        publicKeys.Should().Contain(newPk);
        publicKeys.Should().NotContain(oldKey.PublicKey);
    }

    [Fact]
    public async Task UserWithoutEncryptionKeys_CannotBeInvitedToPrivateRoom()
    {
        // The room key is wrapped for each member's public key, so a keyless user cannot be added
        // at all: the invite itself is refused. The positive control matters here — the SAME
        // invite of the SAME user succeeds as soon as they hold a key, which proves the 403 is
        // about the missing key and not about the user, the access level or the room.
        await _filesClient.Authenticate(Owner);
        var ownerKey = await SetFakeKeys(publicKeyPrefix: "owner");
        var room = await CreatePrivateRoom("Autotest Privacy Room", RoomType.CustomRoom);

        var user = await InviteContact(EmployeeType.User);

        var denied = await Assert.ThrowsAsync<ApiException>(
            async () => await InviteToRoom(room.Id, user, FileShare.ContentCreator));
        denied.ErrorCode.Should().Be(403);
        denied.ErrorContent?.ToString().Should().Contain("does not have an encryption key");

        var ownerView = (await _privacyRoomApi.GetUserKeysForRoomAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        ownerView.Select(k => k.PublicKey).Should().BeEquivalentTo([ownerKey.PublicKey]);

        await _filesClient.Authenticate(user);
        var userKey = await SetFakeKeys(publicKeyPrefix: "user");

        await _filesClient.Authenticate(Owner);
        await InviteToRoom(room.Id, user, FileShare.ContentCreator);

        await _filesClient.Authenticate(user);
        var memberView = (await _privacyRoomApi.GetUserKeysForRoomAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        memberView.Select(k => k.PublicKey).Should().BeEquivalentTo([ownerKey.PublicKey, userKey.PublicKey]);
    }
}

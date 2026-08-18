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
/// Access control for <c>GET /api/2.0/privacyroom/{roomId}/access</c>. The room's access-key set is
/// MEMBERSHIP-scoped, not role-scoped: a DocSpaceAdmin who is not a member is denied, while any
/// member from Read upwards reads the full set.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "PrivacyRoom")]
public class RoomAccessKeysPermissionsTests(AspireAppFixture fixture) : PrivacyRoomTestBase(fixture)
{
    private async Task<(FolderDtoInteger Room, EncryptionKeyDto OwnerKey)> CreatePrivateRoomAsOwner()
    {
        await _filesClient.Authenticate(Owner);
        var ownerKey = await SetFakeKeys(publicKeyPrefix: "owner");
        var room = await CreatePrivateRoom("Autotest Privacy Room", RoomType.CustomRoom);

        return (room, ownerKey);
    }

    [Fact]
    public async Task GetUserKeysForRoom_NonMember_CannotReadKeys()
    {
        var (room, _) = await CreatePrivateRoomAsOwner();

        var user = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(user);
        await SetFakeKeys(publicKeyPrefix: "user");

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _privacyRoomApi.GetUserKeysForRoomAsync(room.Id, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetUserKeysForRoom_DocSpaceAdminNotAMember_CannotReadTheRoomsE2EKeys()
    {
        // End-to-end encryption means even an admin who is not a member of the room is denied
        // (403), exactly like a regular non-member.
        var (room, ownerKey) = await CreatePrivateRoomAsOwner();

        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);
        await SetFakeKeys(publicKeyPrefix: "admin");

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _privacyRoomApi.GetUserKeysForRoomAsync(room.Id, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().NotContain(ownerKey.PublicKey);
    }

    [Fact]
    public async Task GetUserKeysForRoom_Anonymous_Unauthorized()
    {
        await _filesClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _privacyRoomApi.GetUserKeysForRoomAsync(1, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    // Every access level a User-type member can hold in a private room grants the room's key set —
    // a Viewer sees exactly what a ContentCreator sees. RoomManager cannot be granted to a
    // User-type member at all (the invite is 403), so it is covered by the RoomAdmin test below.
    [Theory]
    [MemberData(nameof(RoomAccessData.NonManagerAccesses), MemberType = typeof(RoomAccessData))]
    public async Task GetUserKeysForRoom_MemberWithAccess_ReadsTheRoomsKeySet(FileShare access)
    {
        var (room, ownerKey) = await CreatePrivateRoomAsOwner();

        var member = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(member);
        var memberKey = await SetFakeKeys(publicKeyPrefix: "member");

        await _filesClient.Authenticate(Owner);
        await InviteToRoom(room.Id, member, access);

        await _filesClient.Authenticate(member);
        var keys = (await _privacyRoomApi.GetUserKeysForRoomAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        keys.Select(k => k.PublicKey).Should().BeEquivalentTo([memberKey.PublicKey, ownerKey.PublicKey]);
    }

    [Fact]
    public async Task GetUserKeysForRoom_RoomAdminInvitedAsRoomManager_ReadsTheRoomsKeySet()
    {
        // RoomManager is the one level a User-type member cannot be given; a RoomAdmin-type member
        // can, and it grants the key set like any other level.
        var (room, ownerKey) = await CreatePrivateRoomAsOwner();

        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        await _filesClient.Authenticate(roomAdmin);
        var roomAdminKey = await SetFakeKeys(publicKeyPrefix: "roomadmin");

        await _filesClient.Authenticate(Owner);
        await InviteToRoom(room.Id, roomAdmin, FileShare.RoomManager);

        await _filesClient.Authenticate(roomAdmin);
        var keys = (await _privacyRoomApi.GetUserKeysForRoomAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        keys.Select(k => k.PublicKey).Should().BeEquivalentTo([ownerKey.PublicKey, roomAdminKey.PublicKey]);
    }

    [Fact]
    public async Task GetUserKeysForRoom_DocSpaceAdminWhoIsAMember_ReadsTheRoomsKeySet()
    {
        // Positive counterpart to the non-member DocSpaceAdmin test above: the admin role grants
        // nothing by itself, but a normal invitation does.
        var (room, ownerKey) = await CreatePrivateRoomAsOwner();

        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);
        var adminKey = await SetFakeKeys(publicKeyPrefix: "admin");

        var denied = await Assert.ThrowsAsync<ApiException>(
            async () => await _privacyRoomApi.GetUserKeysForRoomAsync(room.Id, TestContext.Current.CancellationToken));
        denied.ErrorCode.Should().Be(403);

        await _filesClient.Authenticate(Owner);
        await InviteToRoom(room.Id, admin, FileShare.ContentCreator);

        await _filesClient.Authenticate(admin);
        var keys = (await _privacyRoomApi.GetUserKeysForRoomAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        keys.Select(k => k.PublicKey).Should().BeEquivalentTo([adminKey.PublicKey, ownerKey.PublicKey]);
    }

    [Fact]
    public async Task Invite_GuestToPrivateRoom_AlwaysRefused()
    {
        // Membership requires an encryption key and a guest cannot hold one by design, so every
        // invitation of a guest to a private room is refused. The plain room is the positive
        // control: the same guest, the same access level and the same call succeed there, so the
        // 403 is about the private room's key requirement and not about guests being uninvitable.
        var (room, _) = await CreatePrivateRoomAsOwner();
        var plainRoom = await CreateCustomRoom("Autotest Plain Room");

        var guest = await InviteGuest();

        var toPrivate = await Assert.ThrowsAsync<ApiException>(
            async () => await InviteToRoom(room.Id, guest, FileShare.Read));
        toPrivate.ErrorCode.Should().Be(403);
        toPrivate.ErrorContent?.ToString().Should().Contain("does not have an encryption key");

        await InviteToRoom(plainRoom.Id, guest, FileShare.Read);
    }

    [Fact]
    [Trait("Bug", "82803")]
    public async Task GetUserKeysForRoom_EveryMemberReceivesOtherMembersPrivateKeyEnc()
    {
        // BUG 82803: the response carries one entry per member, and each entry includes that
        // member's privateKeyEnc — so any member of a private room, at any access level, is handed
        // every other member's encrypted private key, the room owner's included. In an end-to-end
        // scheme the private half must never leave its owner. The caller's OWN entry is asserted
        // first as the positive control, so an empty or broken read cannot pass this test.
        var (room, _) = await CreatePrivateRoomAsOwner();

        var member = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(member);
        var memberPrv = $"member-prv-{Guid.NewGuid():N}";
        await _privacyRoomApi.SetKeysAsync(
            new EncryptionKeyRequestDto(publicKey: $"member-{Guid.NewGuid():N}", privateKeyEnc: memberPrv),
            TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(Owner);
        await InviteToRoom(room.Id, member, FileShare.Read);

        await _filesClient.Authenticate(member);
        var keys = (await _privacyRoomApi.GetUserKeysForRoomAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        var own = keys.SingleOrDefault(k => k.UserId == member.Id);
        own.Should().NotBeNull();
        own!.PrivateKeyEnc.Should().Be(memberPrv);

        var others = keys.Where(k => k.UserId != member.Id).ToList();
        others.Should().NotBeEmpty();
        others.Should().OnlyContain(k => !string.IsNullOrEmpty(k.PublicKey) && string.IsNullOrEmpty(k.PrivateKeyEnc));
    }
}

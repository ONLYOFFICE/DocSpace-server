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
/// <c>GET /api/2.0/privacyroom/{roomId}/access/candidates</c>: the public keys of the people a caller
/// is about to invite.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "PrivacyRoom")]
public class RoomCandidateKeysTests(AspireAppFixture fixture) : PrivacyRoomTestBase(fixture)
{
    private async Task<FolderDto> CreatePrivateRoomAsOwner()
    {
        await _filesClient.Authenticate(Owner);
        await SetFakeKeys(publicKeyPrefix: "owner");

        return await CreatePrivateRoom("Autotest Privacy Room", RoomType.CustomRoom);
    }

    private async Task<User> InviteContactWithKeys(EmployeeType type, string prefix)
    {
        var user = await InviteContact(type);
        await _filesClient.Authenticate(user);
        await SetFakeKeys(publicKeyPrefix: prefix);

        return user;
    }

    private async Task<List<EncryptionKeyDto>> GetCandidates(int roomId, params Guid[] userIds)
    {
        return (await _privacyRoomApi.GetRoomCandidateKeysAsync(roomId, [.. userIds], TestContext.Current.CancellationToken)).Response;
    }

    private async Task AssertStatus(int roomId, HttpStatusCode expected, params Guid[] userIds)
    {
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await GetCandidates(roomId, userIds));

        exception.ErrorCode.Should().Be((int)expected);
    }

    [Fact]
    public async Task Owner_GetsThePublicKeyOfANonMember_AndCanInviteThem()
    {
        var candidate = await InviteContactWithKeys(EmployeeType.User, "candidate");
        var room = await CreatePrivateRoomAsOwner();

        var key = (await GetCandidates(room.Id, candidate.Id)).Should().ContainSingle().Subject;

        key.UserId.Should().Be(candidate.Id);
        key.PublicKey.Should().StartWith("candidate-");
        key.PrivateKeyEnc.Should().BeNull();

        await InviteToRoom(room.Id, candidate, FileShare.Read);
    }

    [Fact]
    public async Task CallerAmongTheCandidates_DoesNotGetTheirOwnPrivateHalf()
    {
        var room = await CreatePrivateRoomAsOwner();

        var key = (await GetCandidates(room.Id, Owner.Id)).Should().ContainSingle().Subject;

        key.PrivateKeyEnc.Should().BeNull();
    }

    [Fact]
    public async Task UserWithoutKeys_IsAbsentFromTheAnswer()
    {
        var withKeys = await InviteContactWithKeys(EmployeeType.User, "candidate");
        var withoutKeys = await InviteContact(EmployeeType.User);
        var room = await CreatePrivateRoomAsOwner();

        var keys = await GetCandidates(room.Id, withKeys.Id, withoutKeys.Id);

        keys.Select(k => k.UserId).Should().BeEquivalentTo([withKeys.Id]);
    }

    [Fact]
    public async Task SameUserTwice_IsReportedOnce()
    {
        var candidate = await InviteContactWithKeys(EmployeeType.User, "candidate");
        var room = await CreatePrivateRoomAsOwner();

        (await GetCandidates(room.Id, candidate.Id, candidate.Id)).Should().ContainSingle();
    }

    [Fact]
    public async Task NoUsers_AnswersAnEmptyList()
    {
        var room = await CreatePrivateRoomAsOwner();

        (await GetCandidates(room.Id)).Should().BeEmpty();
    }

    [Fact]
    public async Task KeysOfSomebodyOutsideThePortal_AreNotReturned()
    {
        // account_links is not partitioned by tenant, so a key row exists for every portal's users.
        var outsider = Guid.NewGuid();
        await PlantLegacyKeys(outsider, "pk-outsider", "prv-outsider");
        var room = await CreatePrivateRoomAsOwner();

        (await GetCandidates(room.Id, outsider)).Should().BeEmpty();
    }

    [Fact]
    public async Task Guest_IsAbsentFromTheAnswer()
    {
        // A guest cannot store keys through the API and cannot become a member of a private room.
        var guest = await InviteGuest();
        await PlantLegacyKeys(guest.Id, "pk-guest", "prv-guest");
        var room = await CreatePrivateRoomAsOwner();

        (await GetCandidates(room.Id, guest.Id)).Should().BeEmpty();
    }

    [Fact]
    public async Task TerminatedUser_IsAbsentFromTheAnswer()
    {
        var candidate = await InviteContactWithKeys(EmployeeType.User, "candidate");
        await TerminateUser(candidate);
        var room = await CreatePrivateRoomAsOwner();

        (await GetCandidates(room.Id, candidate.Id)).Should().BeEmpty();
    }

    [Fact]
    public async Task RoomManager_MayAsk()
    {
        var manager = await InviteContactWithKeys(EmployeeType.RoomAdmin, "manager");
        var candidate = await InviteContactWithKeys(EmployeeType.User, "candidate");
        var room = await CreatePrivateRoomAsOwner();
        await InviteToRoom(room.Id, manager, FileShare.RoomManager);

        await _filesClient.Authenticate(manager);

        (await GetCandidates(room.Id, candidate.Id)).Should().ContainSingle();
    }

    [Fact]
    public async Task MemberWhoCannotManageAccess_IsRefused()
    {
        var member = await InviteContactWithKeys(EmployeeType.User, "member");
        var room = await CreatePrivateRoomAsOwner();
        await InviteToRoom(room.Id, member, FileShare.Read);

        await _filesClient.Authenticate(member);

        await AssertStatus(room.Id, HttpStatusCode.Forbidden, Owner.Id);
    }

    [Theory]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    public async Task NonMember_IsRefused(EmployeeType type)
    {
        var outsider = await InviteContactWithKeys(type, "outsider");
        var room = await CreatePrivateRoomAsOwner();

        await _filesClient.Authenticate(outsider);

        await AssertStatus(room.Id, HttpStatusCode.Forbidden, Owner.Id);
    }

    [Fact]
    public async Task Anonymous_IsUnauthorized()
    {
        var room = await CreatePrivateRoomAsOwner();

        await _filesClient.Authenticate(null);

        await AssertStatus(room.Id, HttpStatusCode.Unauthorized, Owner.Id);
    }

    [Fact]
    public async Task ArchivedRoom_IsRefused()
    {
        var room = await CreatePrivateRoomAsOwner();
        await ArchiveRoom(room.Id);

        await AssertStatus(room.Id, HttpStatusCode.Forbidden, Owner.Id);
    }

    [Fact]
    public async Task OrdinaryRoom_IsABadRequest()
    {
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest room");

        await AssertStatus(room.Id, HttpStatusCode.BadRequest, Owner.Id);
    }

    [Fact]
    public async Task SubfolderOfAPrivateRoom_IsABadRequest()
    {
        var room = await CreatePrivateRoomAsOwner();
        var subfolder = (await _foldersApi.CreateFolderAsync(room.Id, new CreateFolder("Subfolder"), TestContext.Current.CancellationToken)).Response;

        await AssertStatus(subfolder.Id, HttpStatusCode.BadRequest, Owner.Id);
    }

    [Fact]
    public async Task UnknownRoom_IsNotFound()
    {
        await _filesClient.Authenticate(Owner);

        await AssertStatus(int.MaxValue, HttpStatusCode.NotFound, Owner.Id);
    }

    [Fact]
    public async Task MalformedUserId_IsABadRequest()
    {
        var room = await CreatePrivateRoomAsOwner();

        // raw: the typed signature takes Guid values and cannot produce a malformed one
        using var response = await SendRawPrivacyRoomRequest(HttpMethod.Get, $"api/2.0/privacyroom/{room.Id}/access/candidates?userIds=not-a-guid");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

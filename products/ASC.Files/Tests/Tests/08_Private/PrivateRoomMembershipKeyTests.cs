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
/// The key check of a private room asks for a key only when somebody gets access to the room for the first time, every
/// key check of a request runs before its first write, a missing key has a refusal of its own, and a group is never a
/// member of a private room.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "PrivacyRoom")]
public class PrivateRoomMembershipKeyTests(AspireAppFixture fixture) : PrivacyRoomTestBase(fixture)
{
    private const string KeyRefusalType = "ASC.Web.Studio.Core.EncryptionKeyNotSetException";

    private async Task<FolderDto> CreateRoomAsOwner()
    {
        await _filesClient.Authenticate(Owner);
        await SetFakeKeys(publicKeyPrefix: "owner");

        return await CreatePrivateRoom("Autotest Privacy Room", RoomType.CustomRoom);
    }

    private async Task DeleteOwnKey(User user)
    {
        await _filesClient.Authenticate(user);
        await _privacyRoomApi.DeleteKeysAsync(Guid.Empty, TestContext.Current.CancellationToken);
    }

    private async Task<List<FileShareDto>> Members(int roomId)
    {
        await _filesClient.Authenticate(Owner);

        return (await _roomsApi.GetRoomSecurityInfoAsync(roomId, cancellationToken: TestContext.Current.CancellationToken)).Response;
    }

    private async Task<ApiException> InviteRefused(int roomId, params RoomInvitation[] invitations)
    {
        return await Assert.ThrowsAsync<ApiException>(async () => await _roomsApi.SetRoomSecurityAsync(
            roomId,
            new RoomInvitationRequest { Invitations = [.. invitations], Notify = false },
            cancellationToken: TestContext.Current.CancellationToken));
    }

    private static string ErrorType(ApiException exception)
    {
        using var body = JsonDocument.Parse(exception.ErrorContent!.ToString()!);

        return body.RootElement.TryGetProperty("error", out var error) && error.TryGetProperty("type", out var type)
            ? type.GetString() ?? ""
            : "";
    }

    [Fact]
    public async Task KeylessMember_GetsAnotherRoleAndIsRemoved()
    {
        var member = await InviteWithKeys(EmployeeType.RoomAdmin);
        var room = await CreateRoomAsOwner();
        await InviteToRoom(room.Id, member, FileShare.Read);
        await DeleteOwnKey(member);

        await _filesClient.Authenticate(Owner);
        await InviteToRoom(room.Id, member, FileShare.ContentCreator);
        (await Members(room.Id)).Should().Contain(s => s.SharedToUser != null && s.SharedToUser.Id == member.Id && s.Access == FileShare.ContentCreator);

        await InviteToRoom(room.Id, member, FileShare.None);
        (await Members(room.Id)).Should().NotContain(s => s.SharedToUser != null && s.SharedToUser.Id == member.Id);
    }

    [Fact]
    public async Task KeylessPreviousOwner_HandsTheRoomOver()
    {
        var newOwner = await InviteWithKeys(EmployeeType.RoomAdmin);
        var room = await CreateRoomAsOwner();
        await InviteToRoom(room.Id, newOwner, FileShare.RoomManager);
        await DeleteOwnKey(Owner);

        await _filesClient.Authenticate(Owner);
        await _sharingApi.ChangeFileOwnerAsync(
            new ChangeOwnerRequestDto(folderIds: [new(room.Id)], userId: newOwner.Id),
            TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(newOwner);
        (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response.CreatedBy.Id.Should().Be(newOwner.Id);
    }

    [Fact]
    public async Task KeylessInvitee_IsRefusedWithATypeOfItsOwn()
    {
        var keyless = await InviteContact(EmployeeType.User);
        var keyed = await InviteWithKeys(EmployeeType.User);
        var viewer = await InviteWithKeys(EmployeeType.User);
        var room = await CreateRoomAsOwner();
        await InviteToRoom(room.Id, viewer, FileShare.Read);

        await _filesClient.Authenticate(Owner);
        var keyRefusal = await InviteRefused(room.Id, new RoomInvitation { Id = keyless.Id, Access = FileShare.Read });

        await _filesClient.Authenticate(viewer);
        var rightsRefusal = await InviteRefused(room.Id, new RoomInvitation { Id = keyed.Id, Access = FileShare.Read });

        keyRefusal.ErrorCode.Should().Be(403);
        rightsRefusal.ErrorCode.Should().Be(403);
        ErrorType(keyRefusal).Should().Be(KeyRefusalType);
        ErrorType(rightsRefusal).Should().NotBe(KeyRefusalType);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MixedList_StoresNothing(bool keylessFirst)
    {
        var keyless = await InviteContact(EmployeeType.User);
        var keyed = await InviteWithKeys(EmployeeType.User);
        var room = await CreateRoomAsOwner();

        RoomInvitation[] invitations = keylessFirst
            ? [new() { Id = keyless.Id, Access = FileShare.Read }, new() { Id = keyed.Id, Access = FileShare.Read }]
            : [new() { Id = keyed.Id, Access = FileShare.Read }, new() { Id = keyless.Id, Access = FileShare.Read }];

        await _filesClient.Authenticate(Owner);
        var exception = await InviteRefused(room.Id, invitations);

        exception.ErrorCode.Should().Be(403);
        (await Members(room.Id)).Should().NotContain(s => s.SharedToUser != null && (s.SharedToUser.Id == keyed.Id || s.SharedToUser.Id == keyless.Id));
    }

    [Fact]
    public async Task Group_IsRefusedByARuleOfItsOwn()
    {
        var keyed = await InviteWithKeys(EmployeeType.User);
        var room = await CreateRoomAsOwner();

        await _peopleClient.Authenticate(Owner);
        var group = (await _groupApi.AddGroupAsync(
            new GroupRequestDto(groupManager: Owner.Id, groupName: Guid.NewGuid().ToString("N")[..10], members: [keyed.Id]),
            TestContext.Current.CancellationToken)).Response;

        await _filesClient.Authenticate(Owner);
        var exception = await InviteRefused(room.Id, new RoomInvitation { Id = group.Id, Access = FileShare.Read });

        exception.ErrorCode.Should().Be(403);
        ErrorType(exception).Should().NotBe(KeyRefusalType);
        (await Members(room.Id)).Should().NotContain(s => s.SharedToGroup != null && s.SharedToGroup.Id == group.Id);
    }
}

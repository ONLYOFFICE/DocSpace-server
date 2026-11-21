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
/// A user made a guest leaves the private rooms altogether: their own rooms go to the recipient of the type change, their
/// memberships, file keys and key pair go, and nothing of it comes back with a later type change. The type change is
/// refused before it starts when the recipient could not open a room they would get.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "PrivacyRoom")]
public class PrivateRoomGuestDemotionTests(AspireAppFixture fixture) : PrivacyRoomTestBase(fixture)
{
    private const string OwnershipRefusalType = "ASC.Files.Core.Helpers.PrivateRoomOwnershipException";

    /// <summary>Makes the user a guest through the type change job, as the portal owner, and waits for the job.</summary>
    private async Task<TaskProgressResponseDto> MakeGuest(User user, User recipient)
    {
        await _peopleClient.Authenticate(Owner);
        await _userTypeApi.StartUserTypeUpdateAsync(new StartUpdateUserTypeDto(EmployeeType.Guest, user.Id, recipient.Id), TestContext.Current.CancellationToken);

        return await PollUntil(
            async () => (await _userTypeApi.GetUserTypeUpdateProgressAsync(user.Id, TestContext.Current.CancellationToken)).Response,
            progress => progress is { IsCompleted: true },
            TimeSpan.FromSeconds(60));
    }

    private async Task<long> CountRows(string sql, Guid userId, int roomId = 0)
    {
        await using var connection = await Fixture.OpenDatabaseAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();

        command.CommandText = sql;
        command.Parameters.AddWithValue("@user", userId.ToString());
        command.Parameters.AddWithValue("@room", roomId);

        return Convert.ToInt64(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken));
    }

    private Task<long> FileKeyRows(Guid userId) => CountRows("SELECT COUNT(*) FROM files_file_keys WHERE user_id = @user;", userId);

    private Task<long> KeyPairRows(Guid userId) => CountRows("SELECT COUNT(*) FROM account_links WHERE id = @user AND provider = 'e2e';", userId);

    private Task<long> RoomOwnedBy(Guid userId, int roomId) => CountRows("SELECT COUNT(*) FROM files_folder WHERE id = @room AND create_by = @user;", userId, roomId);

    private async Task<List<Guid>> MemberIds(int roomId)
    {
        await _filesClient.Authenticate(Owner);

        return [.. (await _roomsApi.GetRoomSecurityInfoAsync(roomId, cancellationToken: TestContext.Current.CancellationToken)).Response
            .Where(s => s.SharedToUser != null)
            .Select(s => s.SharedToUser.Id)];
    }

    private static string ErrorType(ApiException exception)
    {
        using var body = JsonDocument.Parse(exception.ErrorContent!.ToString()!);

        return body.RootElement.TryGetProperty("error", out var error) && error.TryGetProperty("type", out var type)
            ? type.GetString() ?? ""
            : "";
    }

    [Fact]
    public async Task MemberMadeGuest_LeavesThePrivateRoom_AndGetsNothingBackLater()
    {
        var member = await InviteWithKeys(EmployeeType.User);
        await _filesClient.Authenticate(Owner);
        await SetFakeKeys(publicKeyPrefix: "owner");
        var room = await CreatePrivateRoom("Autotest Privacy Room", RoomType.CustomRoom);
        var fileId = await UploadEncryptedFile(room.Id, "Encrypted.docx");
        await InviteToRoom(room.Id, member, FileShare.Read);
        (await WriteKeys(fileId, member.Id)).Should().BeEmpty();

        (await MemberIds(room.Id)).Should().Contain(member.Id);
        (await FileKeyRows(member.Id)).Should().Be(1);
        (await KeyPairRows(member.Id)).Should().Be(1);

        var progress = await MakeGuest(member, Owner);

        progress.Error.Should().BeNullOrEmpty();
        (await MemberIds(room.Id)).Should().NotContain(member.Id);
        (await FileKeyRows(member.Id)).Should().Be(0);
        (await KeyPairRows(member.Id)).Should().Be(0);

        // A guest holds no key, so the ordinary key check refuses inviting them again.
        await _filesClient.Authenticate(Owner);
        var exception = await Assert.ThrowsAsync<ApiException>(() => _roomsApi.SetRoomSecurityAsync(
            room.Id,
            new RoomInvitationRequest { Invitations = [new RoomInvitation { Id = member.Id, Access = FileShare.Read }], Notify = false },
            cancellationToken: TestContext.Current.CancellationToken));
        exception.ErrorCode.Should().Be(403);

        // Made a user again: no key pair and no room come back.
        await _peopleClient.Authenticate(Owner);
        await _userTypeApi.UpdateUserTypeAsync(EmployeeType.User, new UpdateMembersRequestDto([member.Id]), TestContext.Current.CancellationToken);
        (await KeyPairRows(member.Id)).Should().Be(0);
        (await MemberIds(room.Id)).Should().NotContain(member.Id);
    }

    [Fact]
    public async Task OwnerMadeGuest_HandsThePrivateRoomToTheRecipient()
    {
        var roomOwner = await InviteWithKeys(EmployeeType.RoomAdmin);
        var room = await CreatePrivateRoom("Autotest Privacy Room", RoomType.CustomRoom);
        var fileId = await UploadEncryptedFile(room.Id, "Encrypted.docx");

        // The recipient holds a key and the key of the file, so they can take the room.
        await _filesClient.Authenticate(Owner);
        await SetFakeKeys(publicKeyPrefix: "owner");
        await _filesClient.Authenticate(roomOwner);
        await InviteToRoom(room.Id, Owner, FileShare.RoomManager);
        (await WriteKeys(fileId, Owner.Id)).Should().BeEmpty();
        (await RoomOwnedBy(roomOwner.Id, room.Id)).Should().Be(1);

        var progress = await MakeGuest(roomOwner, Owner);

        progress.Error.Should().BeNullOrEmpty();
        (await RoomOwnedBy(Owner.Id, room.Id)).Should().Be(1);
        (await MemberIds(room.Id)).Should().NotContain(roomOwner.Id);
        (await KeyPairRows(roomOwner.Id)).Should().Be(0);
    }

    [Fact]
    public async Task RecipientWithoutTheFileKey_IsRefusedBeforeAnythingChanges()
    {
        var roomOwner = await InviteWithKeys(EmployeeType.RoomAdmin);
        var recipient = await InviteWithKeys(EmployeeType.RoomAdmin);
        await _filesClient.Authenticate(roomOwner);
        var room = await CreatePrivateRoom("Autotest Privacy Room", RoomType.CustomRoom);
        await UploadEncryptedFile(room.Id, "Encrypted.docx");

        await _peopleClient.Authenticate(Owner);
        var exception = await Assert.ThrowsAsync<ApiException>(() => _userTypeApi.StartUserTypeUpdateAsync(
            new StartUpdateUserTypeDto(EmployeeType.Guest, roomOwner.Id, recipient.Id), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        ErrorType(exception).Should().Be(OwnershipRefusalType);
        (await RoomOwnedBy(roomOwner.Id, room.Id)).Should().Be(1);
        (await KeyPairRows(roomOwner.Id)).Should().Be(1);
        (await _profilesApi.GetProfileByUserIdAsync(roomOwner.Id.ToString(), TestContext.Current.CancellationToken)).Response.IsRoomAdmin.Should().BeTrue();
    }
}

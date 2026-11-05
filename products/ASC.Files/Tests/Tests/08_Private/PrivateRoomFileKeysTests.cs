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
/// Who holds the key of which encrypted file: the check that stops a new owner from receiving a room
/// they cannot open, the route that says what is missing, and a key write that no longer fails as a
/// whole because of one recipient who has left.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "PrivacyRoom")]
public class PrivateRoomFileKeysTests(AspireAppFixture fixture) : PrivacyRoomTestBase(fixture)
{
    private async Task<User> InviteWithKeys(EmployeeType type)
    {
        var user = await InviteContact(type);
        await _filesClient.Authenticate(user);
        await SetFakeKeys(publicKeyPrefix: "user");

        return user;
    }

    private async Task<(FolderDto Room, int FileId)> CreateRoomWithEncryptedFile()
    {
        await _filesClient.Authenticate(Owner);
        await SetFakeKeys(publicKeyPrefix: "owner");
        var room = await CreatePrivateRoom("Autotest Privacy Room", RoomType.CustomRoom);

        return (room, await UploadEncryptedFile(room.Id, "Encrypted.docx"));
    }

    private async Task<List<Guid>> WriteKeys(int fileId, params Guid[] userIds)
    {
        List<AccessRequestKeyDto> keys = [.. userIds.Select(id => new AccessRequestKeyDto(id, Guid.Empty, "wrapped"))];

        return (await _filesApi.SetEncryptionInfoAsync(fileId, keys, TestContext.Current.CancellationToken)).Response.SkippedUserIds;
    }

    private async Task<List<RoomMissingFileKeysDto>> GetMissing(int roomId, params Guid[] userIds)
    {
        return (await _privacyRoomApi.GetRoomMissingFileKeysAsync(roomId, [.. userIds], TestContext.Current.CancellationToken)).Response;
    }

    private async Task AssertMissingRefused(int roomId, int status)
    {
        var exception = await Assert.ThrowsAsync<ApiException>(() => GetMissing(roomId, Owner.Id));

        exception.ErrorCode.Should().Be(status);
    }

    private async Task ChangeOwner(int roomId, Guid userId)
    {
        await _sharingApi.ChangeFileOwnerAsync(
            new ChangeOwnerRequestDto(folderIds: [new(roomId)], userId: userId),
            TestContext.Current.CancellationToken);
    }

    private async Task<Guid> RoomOwner(int roomId)
    {
        return (await _roomsApi.GetRoomInfoAsync(roomId, TestContext.Current.CancellationToken)).Response.CreatedBy.Id;
    }

    [Fact]
    public async Task ChangeOwner_NewOwnerWithoutAFileKey_IsAConflict()
    {
        var newOwner = await InviteWithKeys(EmployeeType.RoomAdmin);
        var (room, _) = await CreateRoomWithEncryptedFile();

        var exception = await Assert.ThrowsAsync<ApiException>(() => ChangeOwner(room.Id, newOwner.Id));

        exception.ErrorCode.Should().Be(409);
        (await RoomOwner(room.Id)).Should().Be(Owner.Id);
    }

    [Fact]
    public async Task ChangeOwner_FileInASubfolderWithoutAKey_IsAConflict()
    {
        var newOwner = await InviteWithKeys(EmployeeType.RoomAdmin);
        await _filesClient.Authenticate(Owner);
        await SetFakeKeys(publicKeyPrefix: "owner");
        var room = await CreatePrivateRoom("Autotest Privacy Room", RoomType.CustomRoom);
        var subfolder = (await _foldersApi.CreateFolderAsync(room.Id, new CreateFolder("Subfolder"), TestContext.Current.CancellationToken)).Response;
        await UploadEncryptedFile(subfolder.Id, "Encrypted.docx");

        var exception = await Assert.ThrowsAsync<ApiException>(() => ChangeOwner(room.Id, newOwner.Id));

        exception.ErrorCode.Should().Be(409);
    }

    [Fact]
    public async Task ChangeOwner_AfterTheMissingKeyIsWritten_Succeeds()
    {
        var newOwner = await InviteWithKeys(EmployeeType.RoomAdmin);
        var (room, fileId) = await CreateRoomWithEncryptedFile();
        await InviteToRoom(room.Id, newOwner, FileShare.RoomManager);

        (await WriteKeys(fileId, newOwner.Id)).Should().BeEmpty();

        await ChangeOwner(room.Id, newOwner.Id);

        (await RoomOwner(room.Id)).Should().Be(newOwner.Id);
    }

    [Fact]
    public async Task ChangeOwner_RoomWithNoEncryptedFile_Succeeds()
    {
        var newOwner = await InviteWithKeys(EmployeeType.RoomAdmin);
        await _filesClient.Authenticate(Owner);
        var room = await CreatePrivateRoom("Autotest Privacy Room", RoomType.CustomRoom);

        await ChangeOwner(room.Id, newOwner.Id);

        (await RoomOwner(room.Id)).Should().Be(newOwner.Id);
    }

    [Fact]
    public async Task ReassignOfADeletedUser_IsNotStoppedByAMissingFileKey()
    {
        // The user is leaving and cannot write the keys any more, so the check of the API call must not reach here.
        var leaving = await InviteWithKeys(EmployeeType.RoomAdmin);
        var room = await CreatePrivateRoom("Autotest Privacy Room", RoomType.CustomRoom);
        await UploadEncryptedFile(room.Id, "Encrypted.docx");

        await _filesClient.Authenticate(Owner);
        await SetFakeKeys(publicKeyPrefix: "owner");
        await TerminateUser(leaving);

        await _userDataApi.StartReassignAsync(new StartReassignRequestDto(leaving.Id, Owner.Id, deleteProfile: false), TestContext.Current.CancellationToken);

        var progress = await PollUntil(
            async () =>
            {
                await _peopleClient.Authenticate(Owner);

                return (await _userDataApi.GetReassignProgressAsync(leaving.Id, TestContext.Current.CancellationToken)).Response;
            },
            state => state.IsCompleted,
            TimeSpan.FromSeconds(60));

        progress.Error.Should().BeNullOrEmpty();

        await _filesClient.Authenticate(Owner);
        (await RoomOwner(room.Id)).Should().Be(Owner.Id);
    }

    [Fact]
    public async Task ChangeOwner_KeepsTheKeysOfTheNewOwner()
    {
        // Changing the owner removes the new owner's share first, which must not count as them leaving the room.
        var newOwner = await InviteWithKeys(EmployeeType.RoomAdmin);
        var (room, fileId) = await CreateRoomWithEncryptedFile();
        await InviteToRoom(room.Id, newOwner, FileShare.RoomManager);
        await WriteKeys(fileId, newOwner.Id);

        await ChangeOwner(room.Id, newOwner.Id);

        await _filesClient.Authenticate(newOwner);
        (await GetMissing(room.Id, newOwner.Id)).Should().BeEmpty();
    }

    [Fact]
    public async Task RemovingAMember_DeletesTheirKeysAndKeepsTheOthers()
    {
        var leaving = await InviteWithKeys(EmployeeType.User);
        var staying = await InviteWithKeys(EmployeeType.User);
        var (room, fileId) = await CreateRoomWithEncryptedFile();
        await InviteToRoom(room.Id, leaving, FileShare.Read);
        await InviteToRoom(room.Id, staying, FileShare.Read);
        await WriteKeys(fileId, leaving.Id, staying.Id);

        await InviteToRoom(room.Id, leaving, FileShare.None);

        (await GetMissing(room.Id, leaving.Id, staying.Id)).Select(m => m.UserId).Should().BeEquivalentTo([leaving.Id]);

        var recorded = await PollUntil(() => AuditEvents(5165, leaving.Id), count => count > 0);
        recorded.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task RemovingAMember_ReachesTheFilesInSubfolders()
    {
        var leaving = await InviteWithKeys(EmployeeType.User);
        await _filesClient.Authenticate(Owner);
        await SetFakeKeys(publicKeyPrefix: "owner");
        var room = await CreatePrivateRoom("Autotest Privacy Room", RoomType.CustomRoom);
        var subfolder = (await _foldersApi.CreateFolderAsync(room.Id, new CreateFolder("Subfolder"), TestContext.Current.CancellationToken)).Response;
        var fileId = await UploadEncryptedFile(subfolder.Id, "Encrypted.docx");
        await InviteToRoom(room.Id, leaving, FileShare.Read);
        await WriteKeys(fileId, leaving.Id);

        await InviteToRoom(room.Id, leaving, FileShare.None);

        (await GetMissing(room.Id, leaving.Id)).Should().ContainSingle();
    }

    [Fact]
    public async Task ChangingTheAccessOfAMember_KeepsTheirKeys()
    {
        var member = await InviteWithKeys(EmployeeType.RoomAdmin);
        var (room, fileId) = await CreateRoomWithEncryptedFile();
        await InviteToRoom(room.Id, member, FileShare.ContentCreator);
        await WriteKeys(fileId, member.Id);

        await InviteToRoom(room.Id, member, FileShare.Read);

        (await GetMissing(room.Id, member.Id)).Should().BeEmpty();
    }

    [Fact]
    public async Task DeletingAUser_DeletesTheirKeys()
    {
        var leaving = await InviteWithKeys(EmployeeType.User);
        var (room, fileId) = await CreateRoomWithEncryptedFile();
        await InviteToRoom(room.Id, leaving, FileShare.Read);
        await WriteKeys(fileId, leaving.Id);
        (await FileKeyRows(leaving.Id)).Should().Be(1);

        await TerminateUser(leaving);
        await _peopleClient.Authenticate(Owner);
        await _userDataApi.StartRemoveAsync(new TerminateRequestDto(leaving.Id), TestContext.Current.CancellationToken);

        var rows = await PollUntil(() => FileKeyRows(leaving.Id), count => count == 0, TimeSpan.FromSeconds(60));

        rows.Should().Be(0);
    }

    private async Task<long> FileKeyRows(Guid userId)
    {
        await using var connection = await Fixture.OpenDatabaseAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();

        command.CommandText = "SELECT COUNT(*) FROM files_file_keys WHERE user_id = @user;";
        command.Parameters.AddWithValue("@user", userId.ToString());

        return Convert.ToInt64(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken));
    }

    private async Task<long> AuditEvents(int action, Guid mentionedUser)
    {
        await using var connection = await Fixture.OpenDatabaseAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();

        command.CommandText = "SELECT COUNT(*) FROM audit_events WHERE action = @action AND description LIKE @user;";
        command.Parameters.AddWithValue("@action", action);
        command.Parameters.AddWithValue("@user", $"%{mentionedUser}%");

        return Convert.ToInt64(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Missing_NamesTheFileOfTheListedUser_UntilTheKeyIsWritten()
    {
        var member = await InviteWithKeys(EmployeeType.User);
        var (room, fileId) = await CreateRoomWithEncryptedFile();
        await InviteToRoom(room.Id, member, FileShare.Read);

        var missing = (await GetMissing(room.Id, member.Id)).Should().ContainSingle().Subject;
        missing.UserId.Should().Be(member.Id);
        missing.FileIds.Should().BeEquivalentTo([fileId]);

        await WriteKeys(fileId, member.Id);

        (await GetMissing(room.Id, member.Id)).Should().BeEmpty();
    }

    [Fact]
    public async Task Missing_WithoutUsers_CoversTheMembersOfTheRoom()
    {
        var member = await InviteWithKeys(EmployeeType.User);
        var outsider = await InviteWithKeys(EmployeeType.User);
        var (room, _) = await CreateRoomWithEncryptedFile();
        await InviteToRoom(room.Id, member, FileShare.Read);

        var userIds = (await GetMissing(room.Id)).Select(m => m.UserId).ToList();

        userIds.Should().Contain(member.Id).And.NotContain(outsider.Id);
    }

    [Fact]
    public async Task Missing_PlainFile_IsNotReported()
    {
        var member = await InviteWithKeys(EmployeeType.User);
        await _filesClient.Authenticate(Owner);
        var room = await CreatePrivateRoom("Autotest Privacy Room", RoomType.CustomRoom);
        await MakeFileOpen(await UploadEncryptedFile(room.Id, "Plain.docx"));

        (await GetMissing(room.Id, member.Id)).Should().BeEmpty();
    }

    [Fact]
    public async Task Missing_SomebodyOutsideThePortal_IsNotReported()
    {
        var (room, _) = await CreateRoomWithEncryptedFile();

        (await GetMissing(room.Id, Guid.NewGuid())).Should().BeEmpty();
    }

    [Fact]
    public async Task Missing_MemberWhoCannotManageAccess_IsRefused()
    {
        var member = await InviteWithKeys(EmployeeType.User);
        var (room, _) = await CreateRoomWithEncryptedFile();
        await InviteToRoom(room.Id, member, FileShare.Read);

        await _filesClient.Authenticate(member);

        await AssertMissingRefused(room.Id, 403);
    }

    [Fact]
    public async Task Missing_AdminWhoIsNotAMember_IsRefused()
    {
        var admin = await InviteWithKeys(EmployeeType.DocSpaceAdmin);
        var (room, _) = await CreateRoomWithEncryptedFile();

        await _filesClient.Authenticate(admin);

        await AssertMissingRefused(room.Id, 403);
    }

    [Fact]
    public async Task Missing_ArchivedRoom_IsRefused()
    {
        var (room, _) = await CreateRoomWithEncryptedFile();
        await ArchiveRoom(room.Id);

        await AssertMissingRefused(room.Id, 403);
    }

    [Fact]
    public async Task Missing_OrdinaryRoom_IsABadRequest()
    {
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest room");

        await AssertMissingRefused(room.Id, 400);
    }

    [Fact]
    public async Task Missing_UnknownRoom_IsNotFound()
    {
        await _filesClient.Authenticate(Owner);

        await AssertMissingRefused(int.MaxValue, 404);
    }

    [Fact]
    public async Task Missing_Anonymous_IsUnauthorized()
    {
        var (room, _) = await CreateRoomWithEncryptedFile();

        await _filesClient.Authenticate(null);

        await AssertMissingRefused(room.Id, 401);
    }

    [Fact]
    public async Task Missing_MalformedUserId_IsABadRequest()
    {
        var (room, _) = await CreateRoomWithEncryptedFile();

        // raw: the typed signature takes Guid values and cannot produce a malformed one
        using var response = await SendRawPrivacyRoomRequest(HttpMethod.Get, $"api/2.0/privacyroom/{room.Id}/access/missing?userIds=not-a-guid");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task WriteKeys_ARecipientWhoCannotRead_IsSkippedAndTheOthersAreStored(bool memberFirst)
    {
        // A security check keeps what it found on the entry, so the order is what used to let the second one through.
        var member = await InviteWithKeys(EmployeeType.User);
        var gone = await InviteWithKeys(EmployeeType.User);
        var (room, fileId) = await CreateRoomWithEncryptedFile();
        await InviteToRoom(room.Id, member, FileShare.Read);

        var skipped = memberFirst ? await WriteKeys(fileId, member.Id, gone.Id) : await WriteKeys(fileId, gone.Id, member.Id);

        skipped.Should().BeEquivalentTo([gone.Id]);
        (await GetMissing(room.Id, member.Id)).Should().BeEmpty();
        (await GetMissing(room.Id, gone.Id)).Should().ContainSingle();
    }

    [Fact]
    public async Task WriteKeys_EveryRecipientGone_StoresNothingAndRecordsNothing()
    {
        var gone = await InviteWithKeys(EmployeeType.User);
        var (_, fileId) = await CreateRoomWithEncryptedFile();

        (await WriteKeys(fileId, gone.Id)).Should().BeEquivalentTo([gone.Id]);

        var history = (await _filesApi.GetFileHistoryAsync(fileId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        history.Should().NotContain(e => e.Action!.Id == MessageAction.FileKeysSet);
    }

    [Fact]
    public async Task WriteKeys_IsRecordedInTheFileHistory()
    {
        var member = await InviteWithKeys(EmployeeType.User);
        var (room, fileId) = await CreateRoomWithEncryptedFile();
        await InviteToRoom(room.Id, member, FileShare.Read);

        await WriteKeys(fileId, member.Id);

        var history = await PollUntil(
            async () => (await _filesApi.GetFileHistoryAsync(fileId, cancellationToken: TestContext.Current.CancellationToken)).Response,
            entries => entries.Exists(e => e.Action?.Id == MessageAction.FileKeysSet));

        history.Should().Contain(e => e.Action!.Id == MessageAction.FileKeysSet);
    }
}

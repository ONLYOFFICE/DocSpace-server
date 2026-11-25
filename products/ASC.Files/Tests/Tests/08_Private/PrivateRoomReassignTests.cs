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
/// What becomes of the private rooms of a user whose data is removed or transferred: a room goes to someone holding a
/// key, else it stays with its owner and the progress names it, and a DocSpace admin who is a member hands it over
/// afterwards.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "PrivacyRoom")]
public class PrivateRoomReassignTests(AspireAppFixture fixture) : PrivacyRoomTestBase(fixture)
{
    private async Task ChangeOwner(int roomId, Guid userId)
    {
        await _sharingApi.ChangeFileOwnerAsync(
            new ChangeOwnerRequestDto(folderIds: [new(roomId)], userId: userId),
            TestContext.Current.CancellationToken);
    }

    private async Task<(FolderDto Room, int FileId)> CreateRoomWithEncryptedFile()
    {
        await _filesClient.Authenticate(Owner);
        await SetFakeKeys(publicKeyPrefix: "owner");
        var room = await CreatePrivateRoom("Autotest Privacy Room", RoomType.CustomRoom);

        return (room, await UploadEncryptedFile(room.Id, "Encrypted.docx"));
    }

    private async Task<long> FileKeyRows(Guid userId)
    {
        await using var connection = await Fixture.OpenDatabaseAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();

        command.CommandText = "SELECT COUNT(*) FROM files_file_keys WHERE user_id = @user;";
        command.Parameters.AddWithValue("@user", userId.ToString());

        return Convert.ToInt64(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken));
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

    /// <summary>
    /// A private room owned by a disabled user, as left from before the owner of a private room could no longer be
    /// disabled: the room is created by the portal owner, the user is disabled while owning nothing, and only then is
    /// the room's owner set to them in the database, which nothing in the API can do any more.
    /// </summary>
    private async Task<(User Leaving, FolderDto Room, User Member)> PrivateRoomOfADisabledOwner(User? manager = null)
    {
        var leaving = await InviteWithKeys(EmployeeType.RoomAdmin);
        var member = await InviteWithKeys(EmployeeType.User);
        await _filesClient.Authenticate(Owner);
        await SetFakeKeys(publicKeyPrefix: "owner");
        var room = await CreatePrivateRoom("Autotest Privacy Room", RoomType.CustomRoom);
        var fileId = await UploadEncryptedFile(room.Id, "Encrypted.docx");
        await InviteToRoom(room.Id, leaving, FileShare.RoomManager);
        await InviteToRoom(room.Id, member, FileShare.Read);

        if (manager != null)
        {
            // a manager who also holds the key of the file, added while the portal owner still owns the room
            await InviteToRoom(room.Id, manager, FileShare.RoomManager);
            (await WriteKeys(fileId, manager.Id)).Should().BeEmpty();
        }

        await TerminateUser(leaving);

        await using var connection = await Fixture.OpenDatabaseAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE files_folder SET create_by = @user WHERE id = @id;";
        command.Parameters.AddWithValue("@user", leaving.Id.ToString());
        command.Parameters.AddWithValue("@id", room.Id);
        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);

        return (leaving, room, member);
    }

    private async Task<JsonElement> RemoveProgress(Guid userId, string route)
    {
        return await PollUntil(
            async () =>
            {
                // Read raw: the SDK's TaskProgressResponseDto has no `keptPrivateRoomIds` yet.
                await _peopleClient.Authenticate(Owner);
                using var response = await _peopleClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, $"api/2.0/people/{route}/progress/{userId}"), TestContext.Current.CancellationToken);
                response.EnsureSuccessStatusCode();
                using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

                return body.RootElement.GetProperty("response").Clone();
            },
            state => state.ValueKind == JsonValueKind.Object && state.GetProperty("isCompleted").GetBoolean(),
            TimeSpan.FromSeconds(60));
    }

    /// <summary>
    /// The owner as stored: the reassignment writes it straight into the database, so the cached room the Files service
    /// keeps from before the setup's own database change would show the old one.
    /// </summary>
    private async Task<Guid> StoredRoomOwner(int roomId)
    {
        await using var connection = await Fixture.OpenDatabaseAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();

        command.CommandText = "SELECT create_by FROM files_folder WHERE id = @id;";
        command.Parameters.AddWithValue("@id", roomId);

        return Guid.Parse(Convert.ToString(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken))!);
    }

    private static List<int> KeptPrivateRoomIds(JsonElement progress)
    {
        return progress.TryGetProperty("keptPrivateRoomIds", out var ids) && ids.ValueKind == JsonValueKind.Array
            ? [.. ids.EnumerateArray().Select(i => i.GetInt32())]
            : [];
    }

    [Fact]
    public async Task ReassignOfADeletedUser_IsNotStoppedByAMissingFileKey()
    {
        // The portal owner holds a key but no file key; nobody is left to issue one, so the reassignment goes on.
        var (leaving, room, member) = await PrivateRoomOfADisabledOwner();

        await _peopleClient.Authenticate(Owner);
        await _userDataApi.StartReassignAsync(new StartReassignRequestDto(leaving.Id, Owner.Id, deleteProfile: false), TestContext.Current.CancellationToken);

        var progress = await RemoveProgress(leaving.Id, "reassign");

        (progress.TryGetProperty("error", out var error) ? error.GetString() : null).Should().BeNullOrEmpty();
        KeptPrivateRoomIds(progress).Should().BeEmpty();

        (await StoredRoomOwner(room.Id)).Should().Be(Owner.Id);
    }

    [Fact]
    public async Task RemovalStartedByAKeyHolder_GivesThemThePrivateRoom()
    {
        var (leaving, room, member) = await PrivateRoomOfADisabledOwner();

        await _peopleClient.Authenticate(Owner);
        await _userDataApi.StartRemoveAsync(new TerminateRequestDto(leaving.Id), TestContext.Current.CancellationToken);

        var progress = await RemoveProgress(leaving.Id, "remove");

        KeptPrivateRoomIds(progress).Should().BeEmpty();
        (await StoredRoomOwner(room.Id)).Should().Be(Owner.Id);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RemovalWhenNobodyHoldsAKey_KeepsThePrivateRoomWithItsOwner(bool deleteProfile)
    {
        var (leaving, room, member) = await PrivateRoomOfADisabledOwner();
        await _filesClient.Authenticate(Owner);
        await _privacyRoomApi.DeleteKeysAsync(Guid.Empty, TestContext.Current.CancellationToken);

        await _peopleClient.Authenticate(Owner);
        if (deleteProfile)
        {
            await _profilesApi.DeleteMemberAsync(leaving.Id.ToString(), TestContext.Current.CancellationToken);
        }
        else
        {
            await _userDataApi.StartRemoveAsync(new TerminateRequestDto(leaving.Id), TestContext.Current.CancellationToken);
        }

        var progress = await RemoveProgress(leaving.Id, "remove");

        KeptPrivateRoomIds(progress).Should().BeEquivalentTo([room.Id]);

        // The room keeps the removed account as its owner, and its members still work in it.
        (await StoredRoomOwner(room.Id)).Should().Be(leaving.Id);
        await _filesClient.Authenticate(member);
        (await _foldersApi.GetFolderByFolderIdAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response.Files.Should().ContainSingle();
    }

    [Fact]
    public async Task KeptPrivateRoom_IsHandedOverByAMemberAdministrator()
    {
        // The heir, a DocSpace admin who manages the room, holds a key of the file, but the removal does not look for
        // one: it offers the room to the caller and the portal owner only, and the owner's keys are gone, so the room is
        // kept. Its owner is the removed account, so only a member DocSpace admin can hand it over.
        var heir = await InviteWithKeys(EmployeeType.DocSpaceAdmin);
        var (leaving, room, _) = await PrivateRoomOfADisabledOwner(heir);
        await _filesClient.Authenticate(Owner);
        await _privacyRoomApi.DeleteKeysAsync(Guid.Empty, TestContext.Current.CancellationToken);

        await _peopleClient.Authenticate(Owner);
        await _userDataApi.StartRemoveAsync(new TerminateRequestDto(leaving.Id), TestContext.Current.CancellationToken);
        KeptPrivateRoomIds(await RemoveProgress(leaving.Id, "remove")).Should().BeEquivalentTo([room.Id]);
        (await StoredRoomOwner(room.Id)).Should().Be(leaving.Id);

        await _filesClient.Authenticate(heir);
        await ChangeOwner(room.Id, heir.Id);

        (await StoredRoomOwner(room.Id)).Should().Be(heir.Id);
    }
}

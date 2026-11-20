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
/// What a refused invitation to a private room leaves behind - nothing that a granted one sends - and the access record
/// of a group from before groups were refused, which can still be revoked.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "PrivacyRoom")]
public class PrivateRoomRefusalEffectsTests(AspireAppFixture fixture) : PrivacyRoomTestBase(fixture)
{
    private const string ModifyFolder = "s:modify-folder";

    /// <summary><c>MessageAction.RoomCreateUser</c>: an account added to a room.</summary>
    private const int RoomCreateUserAction = 5085;

    /// <summary>How much later a second recipient could get what the first one already has.</summary>
    private static readonly TimeSpan _noDeliveryWindow = TimeSpan.FromSeconds(2);

    private async Task<FolderDto> CreateRoomAsOwner()
    {
        await _filesClient.Authenticate(Owner);
        await SetFakeKeys(publicKeyPrefix: "owner");

        return await CreatePrivateRoom("Autotest Privacy Room", RoomType.CustomRoom);
    }

    private async Task Invite(int roomId, params User[] users)
    {
        await _roomsApi.SetRoomSecurityAsync(
            roomId,
            new RoomInvitationRequest { Invitations = [.. users.Select(u => new RoomInvitation { Id = u.Id, Access = FileShare.Read })], Notify = true },
            cancellationToken: TestContext.Current.CancellationToken);
    }

    /// <summary>Connects as the user and joins the folder room of the Rooms section, its own part too, as the client does.</summary>
    private async Task<PortalSocket> WatchRooms(User user)
    {
        var roomsId = await GetFolderIdAsync(FolderType.VirtualRooms, user);
        var socket = await ConnectSocket(user);

        await socket.SubscribeAsync(true, TestContext.Current.CancellationToken, $"DIR-{roomsId}");

        return socket;
    }

    /// <summary>The event that shows a room to a new member: <c>create</c> of the room as a folder.</summary>
    private static Func<JsonElement, bool> RoomShown(int roomId)
    {
        return payload => payload.GetProperty("cmd").GetString() == "create"
                          && payload.GetProperty("type").GetString() == "folder"
                          && payload.GetProperty("id").GetRawText() == roomId.ToString(CultureInfo.InvariantCulture);
    }

    private async Task<long> CountRows(string sql, params (string Name, object Value)[] parameters)
    {
        await using var connection = await Fixture.OpenDatabaseAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();

        command.CommandText = sql;

        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }

        return Convert.ToInt64(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RefusedInvite_LeavesNoMemberSocketEventLetterOrAuditRecord()
    {
        // The refused list holds a member with a key and one without; a granted invitation of a third account follows,
        // and every channel is read only after it has carried that one, so an absence cannot come from a slow channel.
        // setRoomSecurity raises no webhook, granted or refused, so there is none to look for.
        var keyless = await InviteContact(EmployeeType.User);
        var refused = await InviteWithKeys(EmployeeType.User);
        var granted = await InviteWithKeys(EmployeeType.User);
        var room = await CreateRoomAsOwner();
        await using var refusedSocket = await WatchRooms(refused);
        await using var grantedSocket = await WatchRooms(granted);
        await using var bus = await Fixture.ListenToEventBusAsync(nameof(NotifyItemIntegrationEvent), TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(Owner);
        var exception = await Assert.ThrowsAsync<ApiException>(() => Invite(room.Id, refused, keyless));
        await Invite(room.Id, granted);

        exception.ErrorCode.Should().Be(403);

        await grantedSocket.WaitForAsync(ModifyFolder, RoomShown(room.Id), cancellationToken: TestContext.Current.CancellationToken);
        (await refusedSocket.ArrivesWithinAsync(ModifyFolder, RoomShown(room.Id), _noDeliveryWindow, TestContext.Current.CancellationToken))
            .Should().BeFalse("the refused request added nobody, so it showed the room to nobody");

        var letters = await bus.ReadAsync<NotifyItemIntegrationEvent>(
            l => l.TenantId == TenantId && l.Recipients.Any(r => r.Id == granted.Id.ToString() || r.Id == refused.Id.ToString()),
            TestContext.Current.CancellationToken);
        letters.Should().NotBeEmpty("the granted invitation notifies its member");
        letters.SelectMany(l => l.Recipients).Should().NotContain(r => r.Id == refused.Id.ToString());

        var records = await PollUntil(
            () => CountRows("SELECT COUNT(*) FROM audit_events WHERE tenant_id = @tenant AND action = @action;", ("@tenant", TenantId), ("@action", RoomCreateUserAction)),
            count => count > 0);
        records.Should().Be(1, "only the granted invitation is recorded");

        await _filesClient.Authenticate(Owner);
        (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response
            .Should().NotContain(s => s.SharedToUser != null && (s.SharedToUser.Id == refused.Id || s.SharedToUser.Id == keyless.Id));
    }

    [Fact]
    public async Task RefusedRole_RevokesNobodysKeys()
    {
        // One request takes a member out and gives another a role their user type cannot have. The refusal comes after
        // the leaver is known, so the key rows of the leaver show whether it also came before their keys went.
        var leaving = await InviteWithKeys(EmployeeType.User);
        var promoted = await InviteWithKeys(EmployeeType.User);
        var room = await CreateRoomAsOwner();
        var fileId = await UploadEncryptedFile(room.Id, "Encrypted.docx");
        await InviteToRoom(room.Id, leaving, FileShare.Read);
        await InviteToRoom(room.Id, promoted, FileShare.Read);
        (await WriteKeys(fileId, leaving.Id)).Should().BeEmpty();

        const string keyRows = "SELECT COUNT(*) FROM files_file_keys WHERE user_id = @user;";
        (await CountRows(keyRows, ("@user", leaving.Id.ToString()))).Should().Be(1);

        await _filesClient.Authenticate(Owner);
        var exception = await Assert.ThrowsAsync<ApiException>(() => _roomsApi.SetRoomSecurityAsync(
            room.Id,
            new RoomInvitationRequest
            {
                Invitations =
                [
                    new RoomInvitation { Id = leaving.Id, Access = FileShare.None },
                    new RoomInvitation { Id = promoted.Id, Access = FileShare.RoomManager }
                ],
                Notify = false
            },
            cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        (await CountRows(keyRows, ("@user", leaving.Id.ToString()))).Should().Be(1, "a refused request revokes nobody's keys");
        (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response
            .Should().Contain(s => s.SharedToUser != null && s.SharedToUser.Id == leaving.Id);
    }

    [Fact]
    public async Task GroupRecordFromBeforeTheRule_IsRevoked()
    {
        // A group can no longer be invited, so its record is planted the way an older release stored it; the request
        // that takes it away has to pass the rule that refuses groups.
        var member = await InviteWithKeys(EmployeeType.User);
        var room = await CreateRoomAsOwner();

        await _peopleClient.Authenticate(Owner);
        var group = (await _groupApi.AddGroupAsync(
            new GroupRequestDto(groupManager: Owner.Id, groupName: Guid.NewGuid().ToString("N")[..10], members: [member.Id]),
            TestContext.Current.CancellationToken)).Response;

        const string groupRows = "SELECT COUNT(*) FROM files_security WHERE tenant_id = @tenant AND entry_id = @room AND subject = @group;";
        (string, object)[] keys = [("@tenant", TenantId), ("@room", room.Id.ToString(CultureInfo.InvariantCulture)), ("@group", group.Id.ToString())];

        await using (var connection = await Fixture.OpenDatabaseAsync(TestContext.Current.CancellationToken))
        await using (var command = connection.CreateCommand())
        {
            command.CommandText =
                "INSERT INTO files_security (tenant_id, entry_id, internal_entry_id, entry_type, subject, subject_type, owner, security, timestamp) " +
                "VALUES (@tenant, @room, @internal, 1, @group, 2, @owner, 2, UTC_TIMESTAMP());";
            command.Parameters.AddWithValue("@tenant", TenantId);
            command.Parameters.AddWithValue("@room", room.Id.ToString(CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("@internal", room.Id);
            command.Parameters.AddWithValue("@group", group.Id.ToString());
            command.Parameters.AddWithValue("@owner", Owner.Id.ToString());
            await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }

        (await CountRows(groupRows, keys)).Should().Be(1);

        await _filesClient.Authenticate(Owner);
        await _roomsApi.SetRoomSecurityAsync(
            room.Id,
            new RoomInvitationRequest { Invitations = [new RoomInvitation { Id = group.Id, Access = FileShare.None }], Notify = false },
            cancellationToken: TestContext.Current.CancellationToken);

        (await CountRows(groupRows, keys)).Should().Be(0);
    }
}

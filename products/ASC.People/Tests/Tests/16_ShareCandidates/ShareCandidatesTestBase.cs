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

namespace ASC.People.Tests.Tests._16_ShareCandidates;

/// <summary>
/// Shared setup for the share candidates: who a caller is offered when it shares an entry of its own
/// through <c>GET /people/{file|folder}/{id}</c>, <c>GET /group/{file|folder}/{id}</c> and
/// <c>GET /accounts/{file|folder}/{id}/search</c>. A user or a guest has no access to the contacts
/// of the portal, so it is offered only the accounts it already works with and the Everyone group;
/// administrators and room admins are offered the portal as before. The lists are read with plain
/// SQL, so a read right after the write that should change them is safe.
/// </summary>
public abstract class ShareCandidatesTestBase(AspireAppFixture fixture) : SharingTestBase(fixture)
{
    /// <summary>
    /// A custom room created by the owner with the given subjects - users or groups - invited at
    /// <see cref="FileShare.Editing"/>, the access every member type accepts in a custom room.
    /// </summary>
    protected async Task<int> CreateRoomWithMembersAsync(params Guid[] subjects)
    {
        var roomId = await CreateCustomRoomAsync();

        foreach (var subject in subjects)
        {
            await InviteToRoom(roomId, subject, FileShare.Editing);
        }

        return roomId;
    }

    /// <summary>
    /// A room template made by the owner from an empty custom room and given to the accounts with the
    /// given emails. Template creation runs in the background, so its status is polled on a deadline
    /// and the last observed one is asserted.
    /// </summary>
    protected async Task<int> CreateTemplateSharedWithAsync(params string[] emails)
    {
        var sourceRoomId = await CreateCustomRoomAsync();

        await _roomsApi.CreateRoomTemplateAsync(
            new RoomTemplateDto(sourceRoomId, "Autotest Template " + Guid.NewGuid().ToString()[..8], share: [.. emails]),
            TestContext.Current.CancellationToken);

        var deadline = DateTime.UtcNow.AddSeconds(30);

        while (true)
        {
            var status = (await _roomsApi.GetRoomTemplateCreatingStatusAsync(TestContext.Current.CancellationToken)).Response;

            if (status is { IsCompleted: true } || DateTime.UtcNow >= deadline)
            {
                status.Should().NotBeNull("the room template creation status must be reported within 30 seconds");
                status.IsCompleted.Should().BeTrue("the room template must be created within 30 seconds (error '{0}')", status.Error);

                return status.TemplateId;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }

    protected const string ProfileLocation = "Autotest Location";
    protected const string ProfileNotes = "Autotest Notes";
    protected const string ProfileContactType = "skype";
    protected const string ProfileContactValue = "autotest.contact";

    /// <summary>
    /// Location, notes and contacts can only be set by the account itself, so it signs in to fill them.
    /// </summary>
    protected async Task FillContactDetailsAsync(User user)
    {
        await _peopleClient.Authenticate(user);
        await _profilesApi.UpdateMemberAsync(
            user.Id.ToString(),
            new UpdateMemberRequestDto(
                location: ProfileLocation,
                comment: ProfileNotes,
                contacts: [new Contact(ProfileContactType, ProfileContactValue)]),
            TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Gives the account a Restrict record on the room, or turns its record into one. The room API
    /// accepts no Restrict for a room member, yet such records are in the data (left by older releases)
    /// and the room listing hides the room from a user restricted personally, so the record is written
    /// directly. It is copied from the room's record of <paramref name="templateSubject"/> to keep the
    /// tenant, the entry id and the owner as the product writes them.
    /// </summary>
    protected async Task RestrictInRoomAsync(int roomId, Guid subject, Guid templateSubject)
    {
        await using var connection = await Fixture.OpenDatabaseAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();

        command.CommandText =
            """
            INSERT INTO files_security (tenant_id, entry_id, internal_entry_id, entry_type, subject, owner, security, timestamp, subject_type, options)
            SELECT tenant_id, entry_id, internal_entry_id, entry_type, @subject, owner, @restrict, UTC_TIMESTAMP(), 0, NULL
            FROM files_security
            WHERE internal_entry_id = @roomId AND entry_type = 1 AND subject = @templateSubject
            ON DUPLICATE KEY UPDATE security = @restrict;
            """;
        command.Parameters.AddWithValue("@subject", subject.ToString());
        command.Parameters.AddWithValue("@restrict", (int)FileShare.Restrict);
        command.Parameters.AddWithValue("@roomId", roomId);
        command.Parameters.AddWithValue("@templateSubject", templateSubject.ToString());

        var written = await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        written.Should().BePositive("the room must have a record of the template subject to copy");
    }

    /// <summary>
    /// Makes the account the manager of the group without being its member. The group API always adds
    /// the manager as a member as well and removes both together, but <c>core_usergroup</c> keeps the
    /// manager as a row of its own, so the state exists in the data (directory sync, older releases)
    /// and is written directly: the manager row of the group is handed over to the account.
    /// </summary>
    protected async Task MakeManagerOnlyAsync(Guid groupId, Guid userId)
    {
        await using var connection = await Fixture.OpenDatabaseAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();

        command.CommandText = "UPDATE core_usergroup SET userid = @userId WHERE groupid = @groupId AND ref_type = 1 AND removed = 0;";
        command.Parameters.AddWithValue("@userId", userId.ToString());
        command.Parameters.AddWithValue("@groupId", groupId.ToString());

        var written = await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        written.Should().Be(1, "the group must have exactly one manager row to hand over");
    }

    /// <summary>
    /// The users the caller is offered for its file, read as the share dialog reads them: every
    /// active account of the area, without a search term.
    /// </summary>
    protected async Task<List<EmployeeFullDto>> UsersOfferedForFileAsync(User caller, int fileId, AccountSearchArea area = AccountSearchArea.People)
    {
        await _peopleClient.Authenticate(caller);

        return (await _peopleSearchApi.GetUsersWithFilesSharedAsync(
            fileId,
            employeeStatus: EmployeeStatus.Active,
            area: area,
            count: 100,
            cancellationToken: TestContext.Current.CancellationToken)).Response;
    }
}

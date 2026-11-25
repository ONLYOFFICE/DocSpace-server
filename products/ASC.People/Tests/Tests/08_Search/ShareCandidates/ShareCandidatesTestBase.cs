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

namespace ASC.People.Tests.Tests._08_Search.ShareCandidates;

/// <summary>
/// Shared setup for the share candidates: who a caller is offered when it shares an entry of its own
/// through <c>GET /people/{file|folder|room}/{id}</c> and <c>GET /accounts/{file|folder}/{id}/search</c>;
/// the groups offered through <c>GET /group/{file|folder}/{id}</c> are covered with the group search.
/// A user or a guest has no access to the contacts of the portal, so it is offered only the accounts
/// it already works with and the Everyone group; administrators and room admins are offered the portal
/// as before. The lists are read with plain
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

    /// <summary>
    /// Moves the room to the archive as the owner. Archiving runs in the background, so the archive is polled on a
    /// deadline and the last observed listing is asserted.
    /// </summary>
    protected async Task ArchiveRoomAsync(int roomId)
    {
        await _filesClient.Authenticate(Owner);
        await _roomsApi.ArchiveRoomAsync(roomId, new ArchiveRoomRequest(deleteAfter: false), TestContext.Current.CancellationToken);

        var deadline = DateTime.UtcNow.AddSeconds(30);

        while (true)
        {
            var archive = (await _roomsApi.GetRoomsFolderAsync(searchArea: SearchArea.Archive, count: 100, cancellationToken: TestContext.Current.CancellationToken)).Response;
            var archived = archive.Folders.OfType<FolderDto>().Select(f => f.Id).ToList();

            if (archived.Contains(roomId) || DateTime.UtcNow >= deadline)
            {
                archived.Should().Contain(roomId, "the room must be in the archive within 30 seconds");

                return;
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
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var db = await Fixture.CreateDbContextAsync<FilesDbContext>(cancellationToken);

        // Read without tracking, so each template row, changed below, is added as a new row.
        var templates = await db.Security
            .Where(row => row.InternalEntryId == roomId && row.EntryType == ASC.Files.Core.FileEntryType.Folder && row.Subject == templateSubject)
            .ToListAsync(cancellationToken);
        templates.Should().NotBeEmpty("the room must have a record of the template subject to copy");

        foreach (var row in templates)
        {
            // An existing record of the subject is restricted in place, as the former ON DUPLICATE KEY UPDATE did.
            var restricted = await db.Security
                .Where(existing => existing.TenantId == row.TenantId && existing.EntryId == row.EntryId && existing.EntryType == row.EntryType && existing.Subject == subject)
                .ExecuteUpdateAsync(setters => setters.SetProperty(existing => existing.Share, CoreFileShare.Restrict), cancellationToken);

            if (restricted == 0)
            {
                row.Subject = subject;
                row.Share = CoreFileShare.Restrict;
                row.TimeStamp = DateTime.UtcNow;
                row.SubjectType = CoreSubjectType.User;
                row.Options = null;
                db.Security.Add(row);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Makes the account the manager of the group without being its member. The group API always adds
    /// the manager as a member as well and removes both together, but <c>core_usergroup</c> keeps the
    /// manager as a row of its own, so the state exists in the data (directory sync, older releases)
    /// and is written directly: the manager row of the group is handed over to the account.
    /// </summary>
    protected async Task MakeManagerOnlyAsync(Guid groupId, Guid userId)
    {
        await using var db = await Fixture.CreateDbContextAsync<UserDbContext>(TestContext.Current.CancellationToken);

        var written = await db.UserGroups
            .Where(row => row.UserGroupId == groupId && row.RefType == CoreUserGroupRefType.Manager && !row.Removed)
            .ExecuteUpdateAsync(setters => setters.SetProperty(row => row.Userid, userId), TestContext.Current.CancellationToken);
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

    /// <summary>The same list for a folder of the caller.</summary>
    protected async Task<List<EmployeeFullDto>> UsersOfferedForFolderAsync(User caller, int folderId)
    {
        await _peopleClient.Authenticate(caller);

        return (await _peopleSearchApi.GetUsersWithFoldersSharedAsync(
            folderId,
            employeeStatus: EmployeeStatus.Active,
            area: AccountSearchArea.People,
            count: 100,
            cancellationToken: TestContext.Current.CancellationToken)).Response;
    }

    /// <summary>
    /// The list narrowed by the request's own filters on who has access, with its total: the filters and the total
    /// are answers of their own, apart from the <c>shared</c> flag of each account.
    /// </summary>
    protected async Task<EmployeeFullArrayWrapper> UsersOfFileByAccessAsync(User caller, int fileId, bool? includeShared = null, bool? excludeShared = null)
    {
        await _peopleClient.Authenticate(caller);

        return await _peopleSearchApi.GetUsersWithFilesSharedAsync(
            fileId,
            employeeStatus: EmployeeStatus.Active,
            excludeShared: excludeShared,
            includeShared: includeShared,
            area: AccountSearchArea.People,
            count: 100,
            cancellationToken: TestContext.Current.CancellationToken);
    }

    /// <summary>A document created by the owner in the room.</summary>
    protected async Task<int> CreateFileInRoomAsync(int roomId)
    {
        await _filesClient.Authenticate(Owner);

        var file = await _filesApi.CreateFileAsync(
            roomId,
            new CreateFileRequest("Autotest File " + Guid.NewGuid().ToString()[..8]),
            TestContext.Current.CancellationToken);

        return file.Response.Id;
    }

    /// <summary>The first and the last name of the account, as an administrator reads them.</summary>
    protected async Task<string> FullNameOfAsync(User user)
    {
        await _peopleClient.Authenticate(Owner);
        var profile = (await _profilesApi.GetProfileByUserIdAsync(user.Id.ToString(), TestContext.Current.CancellationToken)).Response;

        return $"{profile.FirstName} {profile.LastName}";
    }
}

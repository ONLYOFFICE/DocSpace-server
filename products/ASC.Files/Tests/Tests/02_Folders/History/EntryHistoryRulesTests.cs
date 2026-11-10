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

namespace ASC.Files.Tests.Tests._02_Folders.History;

/// <summary>
/// The rules shared by <c>GET /files/file/{fileId}/log</c> and <c>GET /files/folder/{folderId}/log</c>:
/// the shape of a file entry, newest first, the 1..100 page length, the date filter, the grouping of
/// the entries of one action and the read right that decides who may see the history.
/// </summary>
[Trait("Category", "Folders")]
public class EntryHistoryRulesTests(
    AspireAppFixture fixture)
    : FolderHistoryTestBase(fixture)
{
    [Fact]
    public async Task GetFileHistory_FileInRoom_ContainsFileCreatedWithInitiatorAndDate()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest File Log Shape");
        var file = await CreateFile("File Log Shape", room.Id);

        // Act
        var entry = await PollFileHistoryEntryAsync(file.Id, MessageAction.FileCreated);

        // Assert
        entry.Should().NotBeNull();
        entry!.Id.Should().NotBe(0);
        entry.Action.Id.Should().Be(MessageAction.FileCreated);
        entry.Initiator.DisplayName.Should().Be(await GetDisplayNameAsync());
        entry.Date.UtcTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(5));
    }

    [Fact]
    public async Task GetFolderHistory_EntriesAreNewestFirst()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Folder Log Order");
        await CreateFile("Folder Log Order", room.Id);
        await AssertHistoryContainsAsync(room.Id, MessageAction.FileCreated);

        // Act
        var history = (await _foldersApi.GetFolderHistoryAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        history.Count.Should().BeGreaterThanOrEqualTo(2);
        history.Select(e => e.Date.UtcTime).Should().BeInDescendingOrder();
        history[0].Action.Id.Should().Be(MessageAction.FileCreated);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task GetFolderHistory_CountOutsideRange_Returns400(int count)
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Folder Log Count Range");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.GetFolderHistoryAsync(room.Id, count: count, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task GetFolderHistory_FromDateInTheFuture_ReturnsNoEntries()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Folder Log Future");
        await PollHistoryCountAsync(room.Id, 1);

        // Act
        var history = (await _foldersApi.GetFolderHistoryAsync(
            room.Id,
            fromDate: DateTime.UtcNow.AddDays(2),
            toDate: DateTime.UtcNow.AddDays(3),
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        history.Should().BeEmpty();
    }

    [Fact]
    public async Task GetFolderHistory_TwoUsersInvitedInOneCall_AreOneEntryWithRelated()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Folder Log Grouping");
        var first = await InviteContact(EmployeeType.User);
        var second = await InviteContact(EmployeeType.User);

        // Act
        await _roomsApi.SetRoomSecurityAsync(
            room.Id,
            new RoomInvitationRequest
            {
                Invitations = [new RoomInvitation { Id = first.Id, Access = FileShare.Read }, new RoomInvitation { Id = second.Id, Access = FileShare.Read }],
                Notify = false
            },
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        var entry = await AssertHistoryContainsAsync(room.Id, MessageAction.RoomCreateUser);
        var history = (await _foldersApi.GetFolderHistoryAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        history.Count(e => e.Action.Id == MessageAction.RoomCreateUser).Should().Be(1);
        entry.Related.Should().ContainSingle().Which.Action.Id.Should().Be(MessageAction.RoomCreateUser);
    }

    [Fact]
    public async Task GetFileHistory_RoomMemberWithRead_Allowed()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest File Log Read");
        var file = await CreateFile("File Log Read", room.Id);
        var user = await InviteContact(EmployeeType.User);
        await InviteToRoom(room.Id, user, FileShare.Read);
        await PollFileHistoryEntryAsync(file.Id, MessageAction.FileCreated);
        await _filesClient.Authenticate(user);

        // Act
        var history = (await _filesApi.GetFileHistoryAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        history.Should().Contain(e => e.Action.Id == MessageAction.FileCreated);
    }

    [Fact]
    public async Task GetFileHistory_UserWithoutRoomAccess_Returns403()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest File Log No Access");
        var file = await CreateFile("File Log No Access", room.Id);
        var user = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.GetFileHistoryAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetFileHistory_UntrackedAction_IsNotShown()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest File Log Untracked");
        var file = await CreateFile("File Log Untracked", room.Id);

        // Act: marking as favourite is audited but not in the tracked set; the rename after it is tracked
        await _filesApi.ToggleFileFavoriteAsync(file.Id, true, TestContext.Current.CancellationToken);
        await _filesApi.UpdateFileAsync(file.Id, new UpdateFileRequest(title: "File Log Untracked Renamed"), TestContext.Current.CancellationToken);
        await PollFileHistoryEntryAsync(file.Id, MessageAction.FileRenamed);
        await Task.Delay(2_000, TestContext.Current.CancellationToken);

        // Assert
        var history = (await _filesApi.GetFileHistoryAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        history.Should().Contain(e => e.Action.Id == MessageAction.FileRenamed);
        history.Should().NotContain(e => e.Action.Id == MessageAction.FileMarkedAsFavorite);
    }

    [Fact]
    public async Task GetFileHistory_VersionRestored_IsShownAsUserFileUpdated()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest File Log Restore Version");
        var file = await CreateFile("File Log Restore Version", room.Id);
        await PollFileHistoryEntryAsync(file.Id, MessageAction.FileCreated);

        // Act: FileRestoreVersion is tracked and presented as UserFileUpdated
        await _filesApi.UpdateFileAsync(file.Id, new UpdateFileRequest { LastVersion = 2 }, TestContext.Current.CancellationToken);

        // Assert
        var entry = await PollFileHistoryEntryAsync(file.Id, MessageAction.UserFileUpdated);
        entry.Should().NotBeNull();
        var history = (await _filesApi.GetFileHistoryAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        history.Should().NotContain(e => e.Action.Id == MessageAction.FileRestoreVersion);
    }

    private async Task<HistoryDto?> PollFileHistoryEntryAsync(int fileId, MessageAction action)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);

        while (true)
        {
            var history = (await _filesApi.GetFileHistoryAsync(fileId, cancellationToken: TestContext.Current.CancellationToken)).Response;
            var entry = history.Find(e => e.Action?.Id == action);

            if (entry is not null || DateTime.UtcNow >= deadline)
            {
                return entry;
            }

            await Task.Delay(1_000, TestContext.Current.CancellationToken);
        }
    }
}

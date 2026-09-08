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
/// <c>GET /api/2.0/files/folder/{folderId}/log</c> - the endpoint's own shape: response structure,
/// pagination, date filtering, error routing and the couple of "no entry for that" cases. Which
/// individual actions get recorded is covered by <see cref="FolderHistoryFileActionsTests"/>,
/// <see cref="FolderHistoryMembershipTests"/> and <see cref="FolderHistoryRoomSettingsTests"/>.
/// </summary>
[Trait("Category", "Folders")]
[Trait("Feature", "History")]
public class FolderHistoryQueryTests(
    AspireAppFixture fixture)
    : FolderHistoryTestBase(fixture)
{
    [Fact]
    public async Task GetFolderHistory_Room_ReturnsCorrectStructure()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Folder History Structure");

        // Act
        var history = (await _foldersApi.GetFolderHistoryAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        history.Should().NotBeEmpty();
        var entry = history[0];
        entry.Action.Should().NotBeNull();
        entry.Action.Id.Should().NotBeNull();
        entry.Initiator.Should().NotBeNull();
        entry.Initiator.DisplayName.Should().NotBeNullOrEmpty();
        entry.Date.Should().NotBeNull();
    }

    [Fact]
    public async Task GetFolderHistory_NewRoom_ContainsRoomCreated()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Folder History RoomCreated");

        // Act
        var history = (await _foldersApi.GetFolderHistoryAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        history.ConvertAll(e => e.Action?.Id).Should().Contain(MessageAction.RoomCreated);
    }

    [Fact]
    public async Task GetFolderHistory_AfterRename_ContainsRoomRenamed()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Folder History Rename");

        var beforeIds = await GetHistoryActionIdsAsync(room.Id);
        beforeIds.Should().NotContain(MessageAction.RoomRenamed);

        // Act
        await _roomsApi.UpdateRoomAsync(room.Id, new UpdateRoomRequest(title: "Autotest Folder History Renamed"), TestContext.Current.CancellationToken);

        // Assert
        var afterIds = await PollHistoryActionIdsContainingAsync(room.Id, MessageAction.RoomRenamed, TimeSpan.FromSeconds(30));
        afterIds.Should().Contain(MessageAction.RoomCreated);
        afterIds.Should().Contain(MessageAction.RoomRenamed);
    }

    [Fact]
    public async Task GetFolderHistory_CountParameter_LimitsReturnedEntries()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Folder History Count");
        await _roomsApi.UpdateRoomAsync(room.Id, new UpdateRoomRequest(title: "Autotest Folder History Count Renamed 1"), TestContext.Current.CancellationToken);
        await _roomsApi.UpdateRoomAsync(room.Id, new UpdateRoomRequest(title: "Autotest Folder History Count Renamed 2"), TestContext.Current.CancellationToken);

        // Act
        var history = (await _foldersApi.GetFolderHistoryAsync(room.Id, count: 1, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        history.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetFolderHistory_StartIndex_ShiftsResultSet()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Folder History StartIndex");
        await _roomsApi.UpdateRoomAsync(room.Id, new UpdateRoomRequest(title: "Autotest Folder History StartIndex Renamed"), TestContext.Current.CancellationToken);

        // Act
        var page0 = (await _foldersApi.GetFolderHistoryAsync(room.Id, startIndex: 0, count: 1, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var page1 = (await _foldersApi.GetFolderHistoryAsync(room.Id, startIndex: 1, count: 1, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        page0[0].Id.Should().NotBe(page1[0].Id);
    }

    [Fact]
    public async Task GetFolderHistory_StartIndexBeyondTotal_ReturnsEmptyArray()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Folder History Beyond");

        // Act
        var history = (await _foldersApi.GetFolderHistoryAsync(room.Id, startIndex: 99999, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        history.Should().BeEmpty();
    }

    [Fact]
    public async Task GetFolderHistory_CountField_MatchesArrayLength()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Folder History CountMatch");
        await _roomsApi.UpdateRoomAsync(room.Id, new UpdateRoomRequest(title: "Autotest Folder History CountMatch Renamed"), TestContext.Current.CancellationToken);

        // Act
        var wrapper = await _foldersApi.GetFolderHistoryAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        wrapper.Count.Should().Be(wrapper.Response.Count);
    }

    [Fact]
    public async Task GetFolderHistory_DateRangeFilter_ContainsEntriesWithinRange()
    {
        // Arrange
        var fromDate = DateTime.UtcNow.AddMinutes(-1);
        var room = await CreateCustomRoom("Autotest Folder History DateFilter");
        var toDate = DateTime.UtcNow.AddMinutes(1);

        // Act
        var history = (await _foldersApi.GetFolderHistoryAsync(room.Id, fromDate: fromDate, toDate: toDate, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        history.Should().NotBeEmpty();
        history.ConvertAll(e => e.Action?.Id).Should().Contain(MessageAction.RoomCreated);
    }

    [Fact]
    public async Task GetFolderHistory_SubfolderInMyDocuments_ContainsFolderCreated()
    {
        // Arrange
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);

        var beforeIds = await GetHistoryActionIdsAsync(myDocsFolderId);
        beforeIds.Should().NotContain(MessageAction.FolderCreated);

        var folder = await CreateFolder("Autotest Folder History MyDocs", myDocsFolderId);

        // Act
        var history = (await _foldersApi.GetFolderHistoryAsync(folder.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        history.Should().NotBeEmpty();
        history.ConvertAll(e => e.Action?.Id).Should().Contain(MessageAction.FolderCreated);
    }

    [Fact]
    public async Task GetFolderHistory_NonExistentFolderId_Returns404()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.GetFolderHistoryAsync(999999999, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task GetFolderHistory_FolderIdZero_Returns404()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.GetFolderHistoryAsync(0, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task GetFolderHistory_ArchivedRoom_ContainsRoomArchived()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Folder History Archived");

        var beforeIds = await GetHistoryActionIdsAsync(room.Id);
        beforeIds.Should().NotContain(MessageAction.RoomArchived);

        // Act
        await ArchiveRoom(room.Id);

        // Assert
        var history = (await _foldersApi.GetFolderHistoryAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        history.Should().NotBeEmpty();
        history.ConvertAll(e => e.Action?.Id).Should().Contain(MessageAction.RoomArchived);
    }

    [Fact]
    public async Task GetFolderHistory_Sequence_AfterInviteRoleChangeAndRemoval()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Folder History Sequence");
        var user = await InviteContact(EmployeeType.User);

        var beforeIds = await GetHistoryActionIdsAsync(room.Id);
        beforeIds.Should().NotContain(MessageAction.RoomCreateUser);
        beforeIds.Should().NotContain(MessageAction.RoomUpdateAccessForUser);
        beforeIds.Should().NotContain(MessageAction.RoomRemoveUser);

        // Act
        await InviteToRoom(room.Id, user, FileShare.Read);
        await InviteToRoom(room.Id, user, FileShare.ContentCreator);
        await InviteToRoom(room.Id, user, FileShare.None);

        // Assert
        await AssertHistoryContainsAsync(room.Id, MessageAction.RoomCreateUser, timeoutSeconds: 30);
        await AssertHistoryContainsAsync(room.Id, MessageAction.RoomUpdateAccessForUser, timeoutSeconds: 30);
        await AssertHistoryContainsAsync(room.Id, MessageAction.RoomRemoveUser, timeoutSeconds: 30);
    }

    [Fact]
    public async Task GetFolderHistory_UserTypeChange_DoesNotCreateEntry()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Folder History UsersUpdatedType");
        var user = await InviteContact(EmployeeType.User);
        await InviteToRoom(room.Id, user, FileShare.Read);

        // Act
        await UpdateUserTypeAsync(user.Id, EmployeeType.DocSpaceAdmin);

        // Assert
        var history = (await _foldersApi.GetFolderHistoryAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        history.ConvertAll(e => e.Action?.Id).Should().NotContain(MessageAction.UsersUpdatedType);
    }

    /// <summary>Polls until <paramref name="action"/> shows up, then returns the full set of action ids observed.</summary>
    private async Task<List<MessageAction?>> PollHistoryActionIdsContainingAsync(int roomId, MessageAction action, TimeSpan timeout)
    {
        await PollHistoryEntryAsync(roomId, action, timeout);

        return await GetHistoryActionIdsAsync(roomId);
    }
}

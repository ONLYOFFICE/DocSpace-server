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

namespace ASC.Files.Tests.Tests._03_Rooms.NewItems;

/// <summary>
/// GET /files/rooms/news — the same "new items" notion as the per-room endpoint, but aggregated
/// across every room the caller can see and grouped by room.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomsNewItemsTests(
    AspireAppFixture fixture)
    : RoomNewItemsTestBase(fixture)
{
    #region Contract

    [Fact]
    public async Task RoomsNews_OnlyOwnContent_ReturnsNothing()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await CreateCustomRoom("Autotest Rooms News Empty");

        // Act
        var news = (await _roomsApi.GetRoomsNewItemsAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        FlattenRoomItems(news).Should().BeEmpty();
    }

    [Fact]
    public async Task RoomsNews_ReturnedGroups_CarryRoomAndEntryFields()
    {
        // Arrange
        var (room, member) = await CreateRoomWithVisitor("Autotest Rooms News Shape", FileShare.Read);
        await CreateFile("Autotest Rooms News Shape File.docx", room.Id);

        // Act
        await _filesClient.Authenticate(member);
        await PollRoomsNewsTitles(t => t.Contains("Autotest Rooms News Shape File.docx"));
        var news = (await _roomsApi.GetRoomsNewItemsAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        var groups = RoomGroupsOf(news);
        groups.Should().NotBeEmpty();
        groups.Should().OnlyContain(g => !string.IsNullOrEmpty(g.Room.Title));

        var items = FlattenRoomItems(news);
        items.Should().OnlyContain(i => !string.IsNullOrEmpty(i.Title));
        items.Should().OnlyContain(i => i.CreatedBy != null);
        items.Should().OnlyContain(i => i.Updated != null);
    }

    #endregion

    #region Core semantics

    [Fact]
    public async Task RoomsNews_FileCreatedByAnotherUser_Appears()
    {
        // Arrange
        var (room, member) = await CreateRoomWithVisitor("Autotest Rooms News File By Other", FileShare.Read);

        // Act
        await CreateFile("Autotest Rooms News Other File.docx", room.Id);

        // Assert
        await _filesClient.Authenticate(member);
        var titles = await PollRoomsNewsTitles(t => t.Contains("Autotest Rooms News Other File.docx"));
        titles.Should().Contain("Autotest Rooms News Other File.docx");
    }

    [Fact]
    public async Task RoomsNews_OwnFile_DoesNotAppear()
    {
        // Arrange
        var (room, member) = await CreateRoomWithVisitor("Autotest Rooms News Own File", FileShare.ContentCreator);

        // Act
        await _filesClient.Authenticate(member);
        await CreateFile("Autotest Rooms News My Own File.docx", room.Id);

        // Assert
        var news = (await _roomsApi.GetRoomsNewItemsAsync(TestContext.Current.CancellationToken)).Response;
        RoomItemTitlesOf(news).Should().NotContain("Autotest Rooms News My Own File.docx");
    }

    [Fact]
    public async Task RoomsNews_SubfolderCreatedByAnotherUser_DoesNotAppear()
    {
        // Arrange
        var (room, member) = await CreateRoomWithVisitor("Autotest Rooms News Subfolder", FileShare.Read);

        // Act
        await CreateFolder("Autotest Rooms News Subfolder By Owner", room.Id);

        // Assert
        await _filesClient.Authenticate(member);
        var news = (await _roomsApi.GetRoomsNewItemsAsync(TestContext.Current.CancellationToken)).Response;
        RoomItemTitlesOf(news).Should().NotContain("Autotest Rooms News Subfolder By Owner");
    }

    [Fact]
    public async Task RoomsNews_FileInsideSubfolder_AppearsRecursively()
    {
        // Arrange
        var (room, member) = await CreateRoomWithVisitor("Autotest Rooms News Recursive", FileShare.Read);

        // Act
        var subfolder = await CreateFolder("Autotest Rooms News Subfolder For File", room.Id);
        await CreateFile("Autotest Rooms News File In Subfolder.docx", subfolder.Id);

        // Assert
        await _filesClient.Authenticate(member);
        var titles = await PollRoomsNewsTitles(t => t.Contains("Autotest Rooms News File In Subfolder.docx"));
        titles.Should().Contain("Autotest Rooms News File In Subfolder.docx");
    }

    [Fact]
    public async Task RoomsNews_MultipleFilesFromOneRoom_AllAppear()
    {
        // Arrange
        var (room, member) = await CreateRoomWithVisitor("Autotest Rooms News Multiple Files", FileShare.Read);

        // Act
        await CreateFile("Autotest Rooms News Multi File 1.docx", room.Id);
        await CreateFile("Autotest Rooms News Multi File 2.docx", room.Id);
        await CreateFile("Autotest Rooms News Multi File 3.docx", room.Id);

        // Assert
        await _filesClient.Authenticate(member);
        var titles = await PollRoomsNewsTitles(t => t.Contains("Autotest Rooms News Multi File 3.docx"));
        titles.Should().Contain(["Autotest Rooms News Multi File 1.docx", "Autotest Rooms News Multi File 2.docx", "Autotest Rooms News Multi File 3.docx"]);
    }

    [Fact]
    public async Task RoomsNews_MixedOwnAndOthersFiles_OnlyOthersAppear()
    {
        // Arrange
        var (room, member) = await CreateRoomWithVisitor("Autotest Rooms News Mixed", FileShare.ContentCreator);

        // Act
        await _filesClient.Authenticate(member);
        await CreateFile("Autotest Rooms News Mixed Own File.docx", room.Id);

        await _filesClient.Authenticate(Owner);
        await CreateFile("Autotest Rooms News Mixed Other File.docx", room.Id);

        // Assert
        await _filesClient.Authenticate(member);
        var titles = await PollRoomsNewsTitles(t => t.Contains("Autotest Rooms News Mixed Other File.docx"));
        titles.Should().Contain("Autotest Rooms News Mixed Other File.docx");
        titles.Should().NotContain("Autotest Rooms News Mixed Own File.docx");
    }

    [Fact]
    public async Task RoomsNews_DeletedFile_IsNotReturned()
    {
        // Arrange
        var (room, member) = await CreateRoomWithVisitor("Autotest Rooms News Deleted File", FileShare.Read);
        var file = await CreateFile("Autotest Rooms News Will Be Deleted.docx", room.Id);

        // The badge is written asynchronously — wait for it before deleting, or the cleanup races
        // ahead of it and leaves an orphan badge behind.
        await _filesClient.Authenticate(member);
        var before = await PollRoomsNewsTitles(t => t.Contains("Autotest Rooms News Will Be Deleted.docx"));
        before.Should().Contain("Autotest Rooms News Will Be Deleted.docx");

        // Act
        await _filesClient.Authenticate(Owner);
        await _filesApi.DeleteFileAsync(
            file.Id,
            new Delete(false, true),
            false,
            TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Assert
        await _filesClient.Authenticate(member);
        var after = await PollRoomsNewsTitles(t => !t.Contains("Autotest Rooms News Will Be Deleted.docx"));
        after.Should().NotContain("Autotest Rooms News Will Be Deleted.docx");
    }

    #endregion

    #region Aggregation across rooms

    [Fact]
    public async Task RoomsNews_SeveralRooms_ReturnsItemsFromEach()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var roomA = await CreateCustomRoom("Autotest Rooms News Aggregate Room A");
        var roomB = await CreateCustomRoom("Autotest Rooms News Aggregate Room B");

        var member = await InviteMember(EmployeeType.User);

        foreach (var room in new[] { roomA, roomB })
        {
            await _filesClient.Authenticate(Owner);
            await InviteToRoom(room.Id, member, FileShare.Read);

            await _filesClient.Authenticate(member);
            await VisitRoom(room.Id);
        }

        // Act
        await _filesClient.Authenticate(Owner);
        await CreateFile("Autotest Rooms News Aggregate File A.docx", roomA.Id);
        await CreateFile("Autotest Rooms News Aggregate File B.docx", roomB.Id);

        // Assert
        await _filesClient.Authenticate(member);
        await PollRoomsNewsTitles(t => t.Contains("Autotest Rooms News Aggregate File B.docx"));
        var news = (await _roomsApi.GetRoomsNewItemsAsync(TestContext.Current.CancellationToken)).Response;

        RoomItemTitlesOf(news).Should().Contain(["Autotest Rooms News Aggregate File A.docx", "Autotest Rooms News Aggregate File B.docx"]);
        RoomTitlesOf(news).Should().Contain(["Autotest Rooms News Aggregate Room A", "Autotest Rooms News Aggregate Room B"]);
    }

    [Fact]
    public async Task RoomsNews_RoomsWithoutAccess_AreNotIncluded()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var visibleRoom = await CreateCustomRoom("Autotest Rooms News Visible Room A");
        var hiddenRoom = await CreateCustomRoom("Autotest Rooms News Hidden Room B");

        // The member is invited to the first room only
        var member = await InviteMember(EmployeeType.User);
        await InviteToRoom(visibleRoom.Id, member, FileShare.Read);

        await _filesClient.Authenticate(member);
        await VisitRoom(visibleRoom.Id);

        // Act
        await _filesClient.Authenticate(Owner);
        await CreateFile("Autotest Rooms News Visible File.docx", visibleRoom.Id);
        await CreateFile("Autotest Rooms News Hidden File.docx", hiddenRoom.Id);

        // Assert
        await _filesClient.Authenticate(member);
        await PollRoomsNewsTitles(t => t.Contains("Autotest Rooms News Visible File.docx"));
        var news = (await _roomsApi.GetRoomsNewItemsAsync(TestContext.Current.CancellationToken)).Response;

        RoomItemTitlesOf(news).Should().Contain("Autotest Rooms News Visible File.docx");
        RoomItemTitlesOf(news).Should().NotContain("Autotest Rooms News Hidden File.docx");
        RoomTitlesOf(news).Should().NotContain("Autotest Rooms News Hidden Room B");
    }

    #endregion

    #region Cross-check and access control

    /// <summary>
    /// The aggregated endpoint must report the same entry as the per-room one for the same room.
    /// </summary>
    [Fact]
    public async Task RoomsNews_MatchesSingleRoomEndpoint()
    {
        // Arrange
        var (room, member) = await CreateRoomWithVisitor("Autotest Rooms News Cross Parity", FileShare.Read);
        await CreateFile("Autotest Rooms News Cross File.docx", room.Id);

        // Act
        await _filesClient.Authenticate(member);
        await PollRoomsNewsTitles(t => t.Contains("Autotest Rooms News Cross File.docx"));

        var aggregated = (await _roomsApi.GetRoomsNewItemsAsync(TestContext.Current.CancellationToken)).Response;
        var single = (await _roomsApi.GetNewRoomItemsAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        TitlesOf(single).Should().Contain("Autotest Rooms News Cross File.docx");
        RoomItemTitlesOf(aggregated).Should().Contain("Autotest Rooms News Cross File.docx");
    }

    [Fact]
    public async Task RoomsNews_Anonymous_Unauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetRoomsNewItemsAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    #endregion
}

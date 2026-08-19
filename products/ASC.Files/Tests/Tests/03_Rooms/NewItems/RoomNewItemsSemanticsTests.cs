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
/// GET /files/rooms/{id}/news — what counts as "new". An entry is new for a member when somebody
/// else created it in a room the member has already opened; their own work never shows up.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomNewItemsSemanticsTests(
    AspireAppFixture fixture)
    : RoomNewItemsTestBase(fixture)
{
    [Fact]
    public async Task News_FileCreatedByAnotherUser_Appears()
    {
        // Arrange
        var (room, member) = await CreateRoomWithVisitor("Autotest News File By Other", FileShare.Read);

        // Act
        await CreateFile("Autotest News Other File.docx", room.Id);

        // Assert
        await _filesClient.Authenticate(member);
        var titles = await PollNewsTitles(room.Id, t => t.Contains("Autotest News Other File.docx"));
        titles.Should().Contain("Autotest News Other File.docx");
    }

    [Fact]
    public async Task News_OwnFile_DoesNotAppear()
    {
        // Arrange
        var (room, member) = await CreateRoomWithVisitor("Autotest News Own File", FileShare.ContentCreator);

        // Act - the member creates the file themselves
        await _filesClient.Authenticate(member);
        await CreateFile("Autotest News My Own File.docx", room.Id);

        // Assert
        var news = (await _roomsApi.GetNewRoomItemsAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        TitlesOf(news).Should().NotContain("Autotest News My Own File.docx");
    }

    /// <remarks>
    /// BUG 81712: a file created before the member ever opened the room is still reported as new
    /// to them. Marked <c>test.fail</c> in the TypeScript suite.
    /// </remarks>
    [Fact]
    [Trait("Bug", "81712")]
    public async Task News_FileCreatedBeforeFirstVisit_DoesNotAppear()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest News Pre-Visit File");

        var member = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, member, FileShare.Read);

        // The owner creates the file BEFORE the member ever opens the room
        await CreateFile("Autotest News Pre-Visit File.docx", room.Id);

        await _filesClient.Authenticate(member);
        await VisitRoom(room.Id);

        // Act
        var news = (await _roomsApi.GetNewRoomItemsAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        TitlesOf(news).Should().NotContain("Autotest News Pre-Visit File.docx");
    }

    /// <remarks>
    /// BUG 81712: re-opening the room does not clear the new items. Marked <c>test.fail</c> in the
    /// TypeScript suite.
    /// </remarks>
    [Fact]
    [Trait("Bug", "81712")]
    public async Task News_RevisitingRoom_ClearsNews()
    {
        // Arrange
        var (room, member) = await CreateRoomWithVisitor("Autotest News Re-Visit", FileShare.Read);
        await CreateFile("Autotest News Re-Visit File.docx", room.Id);

        // The badge is written asynchronously: re-visiting before it lands would clear nothing and
        // the assertion below would race the marker instead of testing the re-visit.
        await _filesClient.Authenticate(member);
        await PollNewsTitles(room.Id, t => t.Contains("Autotest News Re-Visit File.docx"));

        // Act - the member opens the room again, which should mark everything as read
        await VisitRoom(room.Id);

        // Assert
        var news = (await _roomsApi.GetNewRoomItemsAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        FlattenItems(news).Should().BeEmpty();
    }

    /// <remarks>
    /// BUG 81713: a file updated by somebody else is not reported as new. Marked <c>test.fail</c>
    /// in the TypeScript suite.
    /// </remarks>
    [Fact]
    [Trait("Bug", "81713")]
    public async Task News_FileUpdatedByAnotherUser_Appears()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest News Updated File");
        var file = await CreateFile("Autotest News File Before Update.docx", room.Id);

        var member = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, member, FileShare.Read);

        await _filesClient.Authenticate(member);
        await VisitRoom(room.Id);

        // Act
        await _filesClient.Authenticate(Owner);
        await _filesApi.UpdateFileAsync(
            file.Id,
            new UpdateFile { Title = "Autotest News File After Update.docx" },
            TestContext.Current.CancellationToken);

        // Assert
        await _filesClient.Authenticate(member);
        var titles = await PollNewsTitles(room.Id, t => t.Contains("Autotest News File After Update.docx"));
        titles.Should().Contain("Autotest News File After Update.docx");
    }

    /// <remarks>
    /// BUG 81713: a renamed file is not reported as new under its new title. Marked
    /// <c>test.fail</c> in the TypeScript suite.
    /// </remarks>
    [Fact]
    [Trait("Bug", "81713")]
    public async Task News_RenamedFile_AppearsWithNewTitle()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest News Renamed File");
        var file = await CreateFile("Autotest News Old Title.docx", room.Id);

        var member = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, member, FileShare.Read);

        await _filesClient.Authenticate(member);
        await VisitRoom(room.Id);

        // Act
        await _filesClient.Authenticate(Owner);
        await _filesApi.UpdateFileAsync(
            file.Id,
            new UpdateFile { Title = "Autotest News New Title.docx" },
            TestContext.Current.CancellationToken);

        // Assert
        await _filesClient.Authenticate(member);
        var titles = await PollNewsTitles(room.Id, t => t.Contains("Autotest News New Title.docx"));
        titles.Should().Contain("Autotest News New Title.docx");
        titles.Should().NotContain("Autotest News Old Title.docx");
    }

    [Fact]
    public async Task News_SubfolderCreatedByAnotherUser_DoesNotAppear()
    {
        // Arrange
        var (room, member) = await CreateRoomWithVisitor("Autotest News Subfolder Check", FileShare.Read);

        // Act
        await CreateFolder("Autotest News Subfolder By Owner", room.Id);

        // Assert
        await _filesClient.Authenticate(member);
        var news = (await _roomsApi.GetNewRoomItemsAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        TitlesOf(news).Should().NotContain("Autotest News Subfolder By Owner");
    }

    [Fact]
    public async Task News_FileInsideSubfolder_AppearsRecursively()
    {
        // Arrange
        var (room, member) = await CreateRoomWithVisitor("Autotest News Recursive File", FileShare.Read);

        // Act
        var subfolder = await CreateFolder("Autotest News Subfolder For File", room.Id);
        await CreateFile("Autotest News File In Subfolder.docx", subfolder.Id);

        // Assert
        await _filesClient.Authenticate(member);
        var titles = await PollNewsTitles(room.Id, t => t.Contains("Autotest News File In Subfolder.docx"));
        titles.Should().Contain("Autotest News File In Subfolder.docx");
    }

    [Fact]
    public async Task News_MultipleNewFiles_AllAppear()
    {
        // Arrange
        var (room, member) = await CreateRoomWithVisitor("Autotest News Multiple Files", FileShare.Read);

        // Act
        await CreateFile("Autotest News Multi File 1.docx", room.Id);
        await CreateFile("Autotest News Multi File 2.docx", room.Id);
        await CreateFile("Autotest News Multi File 3.docx", room.Id);

        // Assert
        await _filesClient.Authenticate(member);
        var titles = await PollNewsTitles(room.Id, t => t.Contains("Autotest News Multi File 3.docx"));
        titles.Should().Contain(["Autotest News Multi File 1.docx", "Autotest News Multi File 2.docx", "Autotest News Multi File 3.docx"]);
    }

    [Fact]
    public async Task News_MixedOldAndNewFiles_OnlyNewAppear()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest News Mixed Old New");

        // Created before the member ever opens the room
        await CreateFile("Autotest News Old File A.docx", room.Id);

        var member = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, member, FileShare.Read);

        await _filesClient.Authenticate(member);
        await VisitRoom(room.Id);

        // Act - created after the visit
        await _filesClient.Authenticate(Owner);
        await CreateFile("Autotest News New File B.docx", room.Id);

        // Assert
        await _filesClient.Authenticate(member);
        var titles = await PollNewsTitles(room.Id, t => t.Contains("Autotest News New File B.docx"));
        titles.Should().Contain("Autotest News New File B.docx");
        titles.Should().NotContain("Autotest News Old File A.docx");
    }

    [Fact]
    public async Task News_MixedOwnAndOthersFiles_OnlyOthersAppear()
    {
        // Arrange
        var (room, member) = await CreateRoomWithVisitor("Autotest News Mixed Own Others", FileShare.ContentCreator);

        // Act
        await _filesClient.Authenticate(member);
        await CreateFile("Autotest News Mixed Own File.docx", room.Id);

        await _filesClient.Authenticate(Owner);
        await CreateFile("Autotest News Mixed Other File.docx", room.Id);

        // Assert
        await _filesClient.Authenticate(member);
        var titles = await PollNewsTitles(room.Id, t => t.Contains("Autotest News Mixed Other File.docx"));
        titles.Should().Contain("Autotest News Mixed Other File.docx");
        titles.Should().NotContain("Autotest News Mixed Own File.docx");
    }

    [Fact]
    public async Task News_DeletedFile_IsNotReturned()
    {
        // Arrange
        var (room, member) = await CreateRoomWithVisitor("Autotest News Deleted File", FileShare.Read);
        var file = await CreateFile("Autotest News Will Be Deleted.docx", room.Id);

        // Wait until the badge is actually written for the member before deleting — otherwise the
        // delete cleanup can race ahead of the asynchronous badge creation and leave an orphan.
        await _filesClient.Authenticate(member);
        var before = await PollNewsTitles(room.Id, t => t.Contains("Autotest News Will Be Deleted.docx"));
        before.Should().Contain("Autotest News Will Be Deleted.docx");

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
        var after = await PollNewsTitles(room.Id, t => !t.Contains("Autotest News Will Be Deleted.docx"));
        after.Should().NotContain("Autotest News Will Be Deleted.docx");
    }
}

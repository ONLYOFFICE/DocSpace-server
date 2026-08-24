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

namespace ASC.Files.Tests.Tests._02_Folders.News;

/// <summary>
/// GET /api/2.0/files/{folderId}/news — the "new items" badge for a single folder (typically a
/// room). An item counts as new for a member once it was created or changed after that member's
/// last visit to the folder, and is cleared again the next time they visit.
/// </summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "Folders")]
public class FolderNewItemsTests(
    AspireAppFixture fixture)
    : FolderNewItemsTestBase(fixture)
{
    #region Contract

    [Fact]
    public async Task News_ForRoom_ReturnsArray()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Folder News For Room");

        // Act
        var news = (await _foldersApi.GetNewFolderItemsAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        news.Should().NotBeNull();
    }

    [Fact]
    public async Task News_EmptyRoom_ReturnsEmptyArray()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Folder News Empty Room");

        // Act
        var news = (await _foldersApi.GetNewFolderItemsAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        news.Should().BeEmpty();
    }

    [Fact]
    public async Task News_ForMyDocuments_ReturnsArray()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var myFolderId = await GetUserFolderIdAsync(Owner);

        // Act
        var news = (await _foldersApi.GetNewFolderItemsAsync(myFolderId, TestContext.Current.CancellationToken)).Response;

        // Assert
        news.Should().NotBeNull();
    }

    [Fact]
    public async Task News_Items_CarryRequiredFields()
    {
        // Arrange
        var (room, member) = await CreateRoomWithVisitor("Autotest Folder News Fields", FileShare.ContentCreator);

        await _filesClient.Authenticate(member);
        await CreateFile("Autotest Folder News Fields File.docx", room.Id);

        // Act
        await _filesClient.Authenticate(Owner);
        var titles = await PollFolderNewsTitles(room.Id, t => t.Contains("Autotest Folder News Fields File.docx"));
        titles.Should().Contain("Autotest Folder News Fields File.docx");

        var news = (await _foldersApi.GetNewFolderItemsAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        news.Should().NotBeEmpty();
        news.Should().OnlyContain(i => !string.IsNullOrEmpty(i.Title));
        news.Should().OnlyContain(i => i.FileEntryType != null);
        news.Should().OnlyContain(i => i.CreatedBy != null);
        news.Should().OnlyContain(i => i.Updated != null);
    }

    #endregion

    #region Core semantics

    [Fact]
    public async Task News_FileCreatedByAnotherUser_Appears()
    {
        // Arrange
        var (room, member) = await CreateRoomWithVisitor("Autotest Folder News File By Other", FileShare.ContentCreator);

        // Act
        await _filesClient.Authenticate(member);
        await CreateFile("Autotest News File By User.docx", room.Id);

        // Assert
        await _filesClient.Authenticate(Owner);
        var titles = await PollFolderNewsTitles(room.Id, t => t.Contains("Autotest News File By User.docx"));
        titles.Should().Contain("Autotest News File By User.docx");
    }

    [Fact]
    public async Task News_SubfolderCreatedByAnotherUser_DoesNotAppear()
    {
        // Arrange - folders are intentionally not marked as new, only files are.
        var (room, member) = await CreateRoomWithVisitor("Autotest Folder News Subfolder", FileShare.ContentCreator);

        // Act
        await _filesClient.Authenticate(member);
        await CreateFolder("Autotest News Subfolder By User", room.Id);

        // Assert
        await _filesClient.Authenticate(Owner);
        var news = (await _foldersApi.GetNewFolderItemsAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        news.ConvertAll(e => e.Title).Should().NotContain("Autotest News Subfolder By User");
    }

    [Fact]
    public async Task News_MultipleFilesCreatedAfterVisit_AllAppear()
    {
        // Arrange
        var (room, member) = await CreateRoomWithVisitor("Autotest Folder News Count Check", FileShare.ContentCreator);

        // Act
        await _filesClient.Authenticate(member);
        await CreateFile("Autotest News Count File 1.docx", room.Id);
        await CreateFile("Autotest News Count File 2.docx", room.Id);

        // Assert
        await _filesClient.Authenticate(Owner);
        var titles = await PollFolderNewsTitles(room.Id, t => t.Contains("Autotest News Count File 1.docx") && t.Contains("Autotest News Count File 2.docx"));
        titles.Should().Contain(["Autotest News Count File 1.docx", "Autotest News Count File 2.docx"]);
    }

    [Fact]
    public async Task News_ItemsFromMultipleUsers_AllAppear()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Folder News Multi User");

        var user1 = await InviteMember(EmployeeType.User);
        var user2 = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, user1, FileShare.ContentCreator);
        await InviteToRoom(room.Id, user2, FileShare.ContentCreator);

        await VisitRoom(room.Id);

        // Act
        await _filesClient.Authenticate(user1);
        await CreateFile("Autotest News File By User1.docx", room.Id);

        await _filesClient.Authenticate(user2);
        await CreateFile("Autotest News File By User2.docx", room.Id);

        // Assert
        await _filesClient.Authenticate(Owner);
        var titles = await PollFolderNewsTitles(room.Id, t => t.Contains("Autotest News File By User1.docx") && t.Contains("Autotest News File By User2.docx"));
        titles.Should().Contain(["Autotest News File By User1.docx", "Autotest News File By User2.docx"]);
    }

    /// <remarks>
    /// BUG 81712: the owner's last-read marker is set when the room is created and a later visit
    /// does not move it, so a file another member added in between is still reported as new.
    /// Asserts the behaviour the product should have.
    /// </remarks>
    [Fact]
    [Trait("Bug", "81712")]
    public async Task News_ItemsCreatedBeforeVisit_DoNotAppear()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Folder News Pre-Visit");

        var member = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, member, FileShare.ContentCreator);

        // The member creates a file BEFORE the owner ever visits the room.
        await _filesClient.Authenticate(member);
        await CreateFile("Autotest Pre-Visit File.docx", room.Id);

        // Act - owner's first visit establishes the baseline after the file already existed.
        await _filesClient.Authenticate(Owner);
        await VisitRoom(room.Id);

        // Assert
        var news = (await _foldersApi.GetNewFolderItemsAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        news.ConvertAll(e => e.Title).Should().NotContain("Autotest Pre-Visit File.docx");
    }

    /// <remarks>
    /// BUG 81712: re-opening a folder does not clear its new items. The same defect is already
    /// pinned at room level by <c>RoomNewItemsSemanticsTests.News_RevisitingRoom_ClearsNews</c>;
    /// this asserts the behaviour the product should have, so it turns green when the bug is fixed.
    /// </remarks>
    [Fact]
    [Trait("Bug", "81712")]
    public async Task News_AfterRevisit_ReturnsEmptyArray()
    {
        // Arrange
        var (room, member) = await CreateRoomWithVisitor("Autotest Folder News Re-Visit", FileShare.ContentCreator);

        await _filesClient.Authenticate(member);
        await CreateFile("Autotest Re-Visit File.docx", room.Id);

        await _filesClient.Authenticate(Owner);
        await PollFolderNewsTitles(room.Id, t => t.Contains("Autotest Re-Visit File.docx"));

        // Act - the owner re-visits, which marks everything as read.
        await VisitRoom(room.Id);

        // Assert
        var titles = await PollFolderNewsTitles(room.Id, t => t.Count == 0);
        titles.Should().BeEmpty();
    }

    [Fact]
    public async Task News_OwnNewlyCreatedItems_DoNotAppear()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Folder News Own Items");
        await VisitRoom(room.Id);

        // Act
        await CreateFile("Autotest Owner Own File.docx", room.Id);

        // Assert
        var news = (await _foldersApi.GetNewFolderItemsAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        news.ConvertAll(e => e.Title).Should().NotContain("Autotest Owner Own File.docx");
    }

    #endregion

    #region Known bugs

    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "81519")]
    public async Task News_NonExistentFolderId_Returns404()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.GetNewFolderItemsAsync(999999999, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "81519")]
    public async Task News_FolderIdZero_Returns404()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.GetNewFolderItemsAsync(0, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    #endregion
}

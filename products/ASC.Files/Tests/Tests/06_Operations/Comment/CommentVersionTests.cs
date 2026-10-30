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

namespace ASC.Files.Tests.Tests._06_Operations.Comment;

/// <summary>
/// <c>PUT /api/2.0/files/file/{fileId}/comment</c> — the comment as the version history stores it. The
/// comment replaces that of ONE existing version, an empty comment clears it, and the caller needs the
/// right to edit the file's history. These tests read the history back after every write; the tests in
/// <see cref="CommentTests"/> look at the answer of the call alone.
/// </summary>
[Trait("Category", "Operations")]
[Trait("Feature", "Files")]
public class CommentVersionTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    private async Task<int> CreateVersions(int folderId, string name, int count)
    {
        var contents = Enumerable.Range(0, count).Select(i => OperationFileBytes.Make(10 + i, 1 + i)).ToArray();

        return await OperationFileBytes.UploadVersionsAsync(
            _filesOperationsApi, folderId, name, contents, TestContext.Current.CancellationToken);
    }

    private async Task<string?> SetComment(int fileId, int version, string? comment)
    {
        return (await _filesOperationsApi.UpdateFileCommentAsync(
            fileId, new UpdateCommentRequest(version: version, comment: comment!), TestContext.Current.CancellationToken)).Response;
    }

    private async Task<Dictionary<int, string>> CommentsOf(int fileId)
    {
        var versions = (await _filesApi.GetFileVersionInfoAsync(fileId, TestContext.Current.CancellationToken)).Response;

        return versions.ToDictionary(v => v.Version, v => v.Comment ?? "");
    }

    [Fact]
    public async Task Comment_Set_AffectsOneVersionOnlyAndRepeatingTheSameTextLeavesItAsItIs()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Comment Version Room");
        var id = await CreateVersions(room.Id, "versions.bin", 3);
        var initial = await CommentsOf(id);
        initial.Keys.Order().Should().Equal(1, 2, 3);

        // Act
        var set = await SetComment(id, 2, "second version note");

        // Assert
        set.Should().Be("second version note");
        var expected = new Dictionary<int, string>(initial) { [2] = "second version note" };
        (await CommentsOf(id)).Should().BeEquivalentTo(expected);

        (await SetComment(id, 2, "replaced note")).Should().Be("replaced note");
        (await SetComment(id, 2, "replaced note")).Should().Be("replaced note");
        expected[2] = "replaced note";
        (await CommentsOf(id)).Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task Comment_EmptyOrNull_ClearsThatNoteAndOnlyThatNote()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Comment Clear Room");
        var id = await CreateVersions(room.Id, "clear.bin", 3);
        await SetComment(id, 1, "note one");
        await SetComment(id, 2, "note two");
        var before = await CommentsOf(id);

        // Act & Assert
        (await SetComment(id, 1, "")).Should().BeEmpty();
        (await CommentsOf(id)).Should().BeEquivalentTo(new Dictionary<int, string>(before) { [1] = "" });

        (await SetComment(id, 2, null)).Should().BeEmpty();
        (await CommentsOf(id)).Should().BeEquivalentTo(new Dictionary<int, string>(before) { [1] = "", [2] = "" });
    }

    [Theory]
    [InlineData("quotes \" ' backslash \\ percent %")]
    [InlineData("first line\nsecond line\ttab")]
    [InlineData("  padded  ")]
    public async Task Comment_QuotesLineBreaksAndPadding_AreStoredExactlyAsSent(string text)
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Comment Text Room");
        var id = await CreateVersions(room.Id, "text.bin", 2);

        // Act
        var answer = await SetComment(id, 2, text);

        // Assert
        answer.Should().Be(text);
        (await CommentsOf(id))[2].Should().Be(text);
    }

    /// <summary>
    /// The answer echoes an emoji but the history stores a question mark in its place (the column cannot
    /// hold a character outside the Basic Multilingual Plane). The TypeScript suite marks it
    /// <c>BUG-PENDING</c>: no bug number yet.
    /// </summary>
    [Fact]
    [Trait("Category", "Bug")]
    public async Task Comment_CharacterOutsideTheBasicMultilingualPlane_IsStoredAsSent()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Comment Emoji Room");
        var id = await CreateVersions(room.Id, "emoji.bin", 2);

        // Premise: ordinary non-Latin text round-trips.
        (await SetComment(id, 2, "Привет")).Should().Be("Привет");
        (await CommentsOf(id))[2].Should().Be("Привет");

        const string text = "note 😀 done";

        // Act
        var answer = await SetComment(id, 2, text);

        // Assert
        answer.Should().Be(text);
        (await CommentsOf(id))[2].Should().Be(text);
    }

    [Fact]
    public async Task Comment_Of255Characters_IsStoredInFull()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Comment Limit Room");
        var id = await CreateVersions(room.Id, "limit.bin", 2);
        var full = new string('a', 255);

        // Act
        var answer = await SetComment(id, 2, full);

        // Assert
        answer.Should().Be(full);
        (await CommentsOf(id))[2].Should().Be(full);
    }

    [Theory]
    [InlineData(256)]
    [InlineData(1000)]
    public async Task Comment_LongerThan255Characters_IsRejectedAndTheStoredTextStays(int length)
    {
        // Arrange
        var room = await CreateCustomRoom($"Autotest Comment Too Long Room {length}");
        var id = await CreateVersions(room.Id, "toolong.bin", 2);
        var full = new string('a', 255);
        await SetComment(id, 2, full);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await SetComment(id, 2, new string('b', length)));

        // Assert
        exception.ErrorCode.Should().BeInRange(400, 499);
        (await CommentsOf(id))[2].Should().Be(full);
    }

    [Fact]
    public async Task Comment_ContentCreatorAndRoomManager_MayCommentOnTheirOwnAndOnSomebodyElsesFiles()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Comment Rights Room");
        var creator = await InviteMember(EmployeeType.User);
        var manager = await InviteMember(EmployeeType.RoomAdmin);
        await InviteToRoom(room.Id, creator, FileShare.ContentCreator);
        await InviteToRoom(room.Id, manager, FileShare.RoomManager);
        var foreign = await CreateVersions(room.Id, "foreign.bin", 2);

        await _filesClient.Authenticate(creator);
        var own = await CreateVersions(room.Id, "creator.bin", 2);

        // Act
        await SetComment(own, 2, "creator note");
        await SetComment(foreign, 2, "creator on foreign");

        await _filesClient.Authenticate(manager);
        await SetComment(foreign, 1, "manager note");

        // Assert
        await _filesClient.Authenticate(Owner);
        (await CommentsOf(own))[2].Should().Be("creator note");
        var foreignComments = await CommentsOf(foreign);
        foreignComments[1].Should().Be("manager note");
        foreignComments[2].Should().Be("creator on foreign");
    }

    [Fact]
    public async Task Comment_FileInTrash_IsRefusedAndTheCommentIsTheSameAfterRestoringIt()
    {
        // Arrange
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var id = await CreateVersions(myDocsFolderId, "Autotest Comment Trash.bin", 2);
        await SetComment(id, 1, "before trash");

        var deleted = (await _filesOperationsApi.DeleteBatchItemsAsync(
            new DeleteBatchRequestDto { FileIds = [new(id)], Immediately = false, ReturnSingleOperation = true },
            TestContext.Current.CancellationToken)).Response;
        await WaitLongOperation(deleted.FirstOrDefault()?.Id);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await SetComment(id, 1, "while in trash"));

        // Assert
        exception.ErrorCode.Should().BeInRange(400, 499);

        var restored = (await _filesOperationsApi.MoveBatchItemsAsync(
            new BatchRequestDto
            {
                FileIds = [new BatchRequestDtoAllOfFileIds(id)],
                FolderIds = [],
                DestFolderId = new BatchRequestDtoAllOfDestFolderId(myDocsFolderId),
                DeleteAfter = false,
                ReturnSingleOperation = true
            },
            TestContext.Current.CancellationToken)).Response;
        await WaitLongOperation(restored.FirstOrDefault()?.Id);

        (await CommentsOf(id))[1].Should().Be("before trash");
    }

    [Fact]
    public async Task Comment_ConcurrentUpdates_NeverFailOrCorrupt_TheLastWriteWinsWholeAndOtherVersionsKeepTheirOwn()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Comment Concurrent Room");
        var id = await CreateVersions(room.Id, "concurrent.bin", 3);
        var texts = new[] { "alpha", "bravo", "charlie", "delta", "echo" }.Select(w => $"concurrent {w}").ToList();

        // Act
        var answers = await Task.WhenAll(texts.Select(t => SetComment(id, 1, t)));

        // Assert
        answers.Should().BeEquivalentTo(texts);
        texts.Should().Contain((await CommentsOf(id))[1]);

        await Task.WhenAll(SetComment(id, 1, "parallel one"), SetComment(id, 2, "parallel two"), SetComment(id, 3, "parallel three"));
        (await CommentsOf(id)).Should().BeEquivalentTo(new Dictionary<int, string>
        {
            [1] = "parallel one",
            [2] = "parallel two",
            [3] = "parallel three"
        });
    }
}

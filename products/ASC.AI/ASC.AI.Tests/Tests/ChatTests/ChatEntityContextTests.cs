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

namespace ASC.AI.Tests.Tests.ChatTests;

/// <summary>
/// Chatting from somewhere that is not an AI project: an ordinary room or a folder. The <c>entityId</c> the client
/// sends is checked for being accessible and then ignored — every location of a user lists the same threads
/// (Bug 82855, by design), and the per-thread routes take no entity at all.
/// </summary>
[Trait("Category", "Chat")]
[Trait("Feature", "AI/Chat")]
public class ChatEntityContextTests(AspireAppFixture fixture) : ChatPart2TestBase(fixture)
{
    [Fact]
    public async Task CreateAndSend_InOrdinaryRoom_IsListedForTheRoomAndAnswered()
    {
        // Arrange
        var profile = await GetDefaultProfileAsync();
        var roomId = (await CreateCustomRoomAsync("Autotest Entity Room")).ToString();

        // Act
        var thread = await CreateThreadAsync("Autotest entity thread", profile.Id, roomId);
        var round = await SendWithStreamAsync(thread.ThreadId, ReplyOk, profile.Id, roomId);

        // Assert
        (await GetThreadAsync(thread.ThreadId)).ThreadId.Should().Be(thread.ThreadId);
        (await ListThreadIdsAsync(roomId)).Should().Contain(thread.ThreadId);

        AssertHealthyReply(round);
        AssistantTexts(await ReadMessagesAsync(thread.ThreadId))
            .Should().ContainSingle("the room-context thread got a reply").Which.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Threads started in room A are listed while the user is in room B, in an unrelated folder, under a string that
    /// is not an id and with no entity at all: one list per user (Bug 82855, confirmed by design).
    /// </summary>
    [Fact]
    [Trait("Bug", "82855")]
    public async Task List_EveryNonProjectEntity_SharesOneThreadList()
    {
        // Arrange
        var profile = await GetDefaultProfileAsync();
        var roomAId = await CreateCustomRoomAsync("Autotest Entity Room A");
        var roomBId = await CreateCustomRoomAsync("Autotest Entity Room B");
        var folderId = await MyDocumentsIdAsync();

        var inRoomA = await CreateThreadAsync("Autotest thread in room A", profile.Id, roomAId.ToString());

        // Act
        var scopes = new (string Label, string? EntityId)[]
        {
            ("room B", roomBId.ToString()),
            ("My Documents", folderId.ToString()),
            ("a string that is not an id", "autotest-not-an-entity"),
            ("no entity at all", null)
        };
        var listed = new List<(string Label, List<string> Ids)>();
        foreach (var (label, entityId) in scopes)
        {
            listed.Add((label, await ListThreadIdsAsync(entityId)));
        }

        // Assert
        foreach (var (label, ids) in listed)
        {
            ids.Should().Contain(inRoomA.ThreadId, $"room A's thread is listed for {label}");
        }
    }

    /// <summary>
    /// The same shared list seen from a thread started in a folder: it is listed for the sibling folder, for an
    /// unrelated room and for a folder inside that room (Bug 82855, confirmed by design).
    /// </summary>
    [Fact]
    [Trait("Bug", "82855")]
    public async Task List_ThreadStartedInFolder_IsListedForEveryOtherFolderAndRoom()
    {
        // Arrange
        var profile = await GetDefaultProfileAsync();
        var myDocumentsId = await MyDocumentsIdAsync();
        var folderAId = await CreateFolderAsync(myDocumentsId, "Autotest Scope Folder A");
        var folderBId = await CreateFolderAsync(myDocumentsId, "Autotest Scope Folder B");
        var roomId = await CreateCustomRoomAsync("Autotest Folder Scope Room");
        var subFolderId = await CreateFolderAsync(roomId, "Autotest Scope Subfolder");

        var inFolderA = await CreateThreadAsync("Autotest thread in folder A", profile.Id, folderAId.ToString());

        // Act
        var own = await ListThreadIdsAsync(folderAId.ToString());
        var sibling = await ListThreadIdsAsync(folderBId.ToString());
        var room = await ListThreadIdsAsync(roomId.ToString());
        var subFolder = await ListThreadIdsAsync(subFolderId.ToString());

        // Assert
        own.Should().Contain(inFolderA.ThreadId, "the thread is listed for the folder it was started in");
        sibling.Should().Contain(inFolderA.ThreadId, "folder A's thread is listed for the sibling folder");
        room.Should().Contain(inFolderA.ThreadId, "folder A's thread is listed for an unrelated room");
        subFolder.Should().Contain(inFolderA.ThreadId, "folder A's thread is listed for a folder inside that room");
    }

    /// <summary>
    /// A room, its subfolders at any depth, a folder outside it and another room are not in an inheritance
    /// relationship: they all list the same shared bucket (Bug 82855, confirmed by design).
    /// </summary>
    [Fact]
    [Trait("Bug", "82855")]
    public async Task List_RoomSubfoldersOtherRoomAndOutsideFolder_ListTheSameBucket()
    {
        // Arrange
        var profile = await GetDefaultProfileAsync();
        var roomId = await CreateCustomRoomAsync("Autotest Nesting Room");
        var subFolderId = await CreateFolderAsync(roomId, "Autotest Nesting Subfolder");
        var deepFolderId = await CreateFolderAsync(subFolderId, "Autotest Nesting Deep Subfolder");
        var outsideFolderId = await CreateFolderAsync(await MyDocumentsIdAsync(), "Autotest Nesting Outside Folder");
        var otherRoomId = await CreateCustomRoomAsync("Autotest Nesting Other Room");

        var allThreads = new List<string>
        {
            (await CreateThreadAsync("Autotest thread in the room root", profile.Id, roomId.ToString())).ThreadId,
            (await CreateThreadAsync("Autotest thread in the subfolder", profile.Id, subFolderId.ToString())).ThreadId,
            (await CreateThreadAsync("Autotest thread two levels down", profile.Id, deepFolderId.ToString())).ThreadId,
            (await CreateThreadAsync("Autotest thread outside the room", profile.Id, outsideFolderId.ToString())).ThreadId
        };

        // Act
        var locations = new (string Label, int Id)[]
        {
            ("the room root", roomId),
            ("the subfolder", subFolderId),
            ("the deep subfolder", deepFolderId),
            ("the folder outside the room", outsideFolderId),
            ("an unrelated room", otherRoomId)
        };
        var listed = new List<(string Label, List<string> Ids)>();
        foreach (var (label, id) in locations)
        {
            listed.Add((label, await ListThreadIdsAsync(id.ToString())));
        }

        // Assert
        foreach (var (label, ids) in listed)
        {
            ids.Should().Contain(allThreads, $"{label} lists every thread in the shared bucket");
        }
    }

    [Fact]
    public async Task ReadAndSend_FromAnotherLocation_ThreadIsUsableAndCarriesNoEntity()
    {
        // get-by-id and read-messages take no entity, and send-with-stream does not check the one it carries against
        // the thread's own: a thread started in room A is fully usable while the client says it is in room B. Turns
        // red the moment one of these routes starts enforcing a scope.

        // Arrange
        var profile = await GetDefaultProfileAsync();
        var roomAId = (await CreateCustomRoomAsync("Autotest Mismatch Room A")).ToString();
        var roomBId = (await CreateCustomRoomAsync("Autotest Mismatch Room B")).ToString();

        var thread = await CreateThreadAsync("Autotest thread of room A", profile.Id, roomAId);
        AssertHealthyReply(await SendWithStreamAsync(thread.ThreadId, ReplyOk, profile.Id, roomAId));

        // Act
        var info = await GetThreadAsync(thread.ThreadId);
        var read = await ReadMessagesAsync(thread.ThreadId);

        // Raw on purpose: the assertion is that the record publishes no entity field, which the typed AiThread
        // cannot tell — it has no such property and would drop one silently.
        using var rawInfo = await new RawApiClient(_aiChatClient).GetAsync(
            $"/api/2.0/ai/threads/get-by-id?threadId={thread.ThreadId}",
            TestContext.Current.CancellationToken);
        var rawBody = await rawInfo.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        var fromRoomB = await SendWithStreamAsync(thread.ThreadId, "Reply with the single word FINE.", profile.Id, roomBId);
        var after = await ReadMessagesAsync(thread.ThreadId);

        // Assert
        info.ThreadId.Should().Be(thread.ThreadId);
        rawInfo.StatusCode.Should().Be(HttpStatusCode.OK, rawBody);
        JsonDocument.Parse(rawBody).RootElement.TryGetProperty("entityId", out _)
            .Should().BeFalse($"a thread record carries no entity field: {rawBody}");
        read.Should().HaveCount(2);

        AssertHealthyReply(fromRoomB);
        after.Should().HaveCount(4, "a send naming another location lands in the same thread");
    }
}

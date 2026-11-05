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

namespace ASC.AI.Tests.Tests.ThreadStorageTests;

/// <summary>
/// End-to-end flows over ThreadsApi. Every step is checked through a route other than the one that made the change,
/// so a handler that answers 200 and does nothing cannot pass. The model's part is the run-wide stub provider.
/// </summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "AI/Threads")]
public class ThreadsLifecycleTests(AspireAppFixture fixture) : ThreadsPortTestBase(fixture)
{
    [Fact]
    public async Task Thread_CreateWriteEditRenameTouchPruneClearDelete_EachStepConfirmedByAnotherRoute()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        // create
        var threadId = (await CreateOwnThreadAsync("Lifecycle")).ThreadId;
        (await GetThreadAsync(threadId)).Title.Should().Be("Lifecycle");
        (await ListIdsAsync()).Should().Equal(threadId);

        // append-user-message x3, confirmed by read-messages and get-message-by-id
        var ids = new List<string>();
        foreach (var text in new[] { "one", "two", "three" })
        {
            ids.Add(await AppendUserMessageAsync(threadId, text));
        }

        var read = await ReadAllAsync(threadId);
        read.Select(m => m.Id).Should().Equal(ids);
        for (var i = 0; i < ids.Count; i++)
        {
            (await _aiThreads.AiThreadsGetMessageByIdAsync(ids[i], cancellationToken)).ToJson().Should().Be(read[i].ToJson());
        }

        // update-message, confirmed by get-message-by-id and read-messages
        (await _aiThreads.AiThreadsUpdateMessageAsync(
            new AiThreadsUpdateMessageRequest(ids[1], BuildUserMessage("two, edited")),
            cancellationToken)).Success.Should().BeTrue();
        TextOf(await _aiThreads.AiThreadsGetMessageByIdAsync(ids[1], cancellationToken)).Should().Be("two, edited");
        (await ReadTextsAsync(threadId)).Should().Equal("one", "two, edited", "three");

        // rename, confirmed by get-by-id and list
        (await _aiThreads.AiThreadsRenameAsync(new AiThreadsRenameRequest(threadId, "Lifecycle renamed"), cancellationToken))
            .Success.Should().BeTrue();
        (await GetThreadAsync(threadId)).Title.Should().Be("Lifecycle renamed");
        (await ListThreadsAsync())[0].Title.Should().Be("Lifecycle renamed");

        // touch, confirmed by the date and by the history being untouched
        var beforeTouch = await GetThreadAsync(threadId);
        var messagesBeforeTouch = (await ReadAllAsync(threadId)).Select(m => m.ToJson()).ToList();

        // Part of the scenario, not a wait for an asynchronous write: last_edit_date is a whole-second datetime column, so
        // a touch within the second of the rename would store the same date. More than a second later the stored dates
        // cannot be equal, whatever the rounding.
        await Task.Delay(TimeSpan.FromMilliseconds(1100), cancellationToken);

        (await _aiThreads.AiThreadsTouchAsync(new AiThreadsTouchRequest(threadId), cancellationToken)).Success.Should().BeTrue();
        (await GetThreadAsync(threadId)).LastEditDate.Should().BeGreaterThan(beforeTouch.LastEditDate, "a touch moves the date forward");
        (await ReadAllAsync(threadId)).Select(m => m.ToJson()).Should().Equal(messagesBeforeTouch);

        // delete-message, confirmed by read-messages and open-or-create
        (await _aiThreads.AiThreadsDeleteMessageAsync(ids[0], cancellationToken)).Success.Should().BeTrue();
        (await ReadTextsAsync(threadId)).Should().Equal("two, edited", "three");
        (await OpenAsync(threadId)).PriorMessages.Select(m => m.Id).Should().Equal(ids[1], ids[2]);

        // clear-messages, confirmed by read-messages; the thread stays
        (await _aiThreads.AiThreadsClearMessagesAsync(threadId, cancellationToken)).Success.Should().BeTrue();
        (await ReadAllAsync(threadId)).Should().BeEmpty();
        (await GetThreadAsync(threadId)).Title.Should().Be("Lifecycle renamed");

        // delete, confirmed by list and by every route answering 404
        (await _aiThreads.AiThreadsDeleteAsync(threadId, cancellationToken)).Success.Should().BeTrue();
        (await ListThreadsAsync()).Should().BeEmpty();
        (await StatusOfAsync(() => GetThreadAsync(threadId))).Should().Be(404, "get-by-id");
        (await StatusOfAsync(() => ReadAllAsync(threadId))).Should().Be(404, "read-messages");
        (await StatusOfAsync(() => _aiThreads.AiThreadsRenameAsync(new AiThreadsRenameRequest(threadId, "x"), cancellationToken)))
            .Should().Be(404, "rename");
        (await StatusOfAsync(() => _aiThreads.AiThreadsClearMessagesAsync(threadId, cancellationToken))).Should().Be(404, "clear-messages");
        (await StatusOfAsync(() => _aiThreads.AiThreadsTouchAsync(new AiThreadsTouchRequest(threadId), cancellationToken)))
            .Should().Be(404, "touch");
    }

    [Fact]
    public async Task Thread_OpenedByItsFirstMessage_IsARealThreadToEveryRoute_AndASecondMessageContinuesIt()
    {
        var profile = await GetProfileAsync();
        const string question = "What is the capital of France? Answer in one word.";

        (await ListThreadsAsync()).Should().BeEmpty("the portal starts with no threads");

        // send-with-stream with no threadId opens a thread and answers.
        var first = await SendWithStreamAsync(null, question, profile.Id);

        first.StatusCode.Should().Be(HttpStatusCode.OK, first.ToString());
        first.StreamError.Should().BeNull(first.ToString());
        first.Frames.Should().NotBeEmpty(first.ToString());
        first.Frames[0].GetProperty("type").GetString().Should().Be("thread-title", first.ToString());
        var threadId = first.ThreadId!;
        IsUuid(threadId).Should().BeTrue($"the opening frame announces the new thread: {first}");
        first.AssistantText.Should().NotBeNullOrEmpty(first.ToString());

        // get-by-id and list know the thread.
        var thread = await GetThreadAsync(threadId);
        thread.ThreadId.Should().Be(threadId);
        thread.ProfileId.Should().Be(profile.Id);
        var listed = await ListThreadsAsync();
        listed.Select(t => t.ThreadId).Should().Equal(threadId);
        listed[0].Title.Should().Be(thread.Title);

        // read-messages, open-or-create and get-message-by-id agree on the two messages.
        var afterFirstTurn = await ReadAllAsync(threadId);
        afterFirstTurn.Select(m => m.Role).Should().Equal(AiThreadMessageLike.RoleEnum.User, AiThreadMessageLike.RoleEnum.Assistant);
        TextOf(afterFirstTurn[0]).Should().Be(question);
        (await OpenAsync(threadId)).PriorMessages.Select(m => m.Id).Should().Equal(afterFirstTurn.Select(m => m.Id));
        foreach (var message in afterFirstTurn)
        {
            (await _aiThreads.AiThreadsGetMessageByIdAsync(message.Id, TestContext.Current.CancellationToken)).ToJson()
                .Should().Be(message.ToJson());
        }

        // A second message in the same thread keeps the first turn.
        var second = await SendWithStreamAsync(threadId, "Say OK.", profile.Id);
        second.StreamError.Should().BeNull(second.ToString());
        var messages = await ReadAllAsync(threadId);
        messages.Take(2).Select(m => m.Id).Should().Equal(afterFirstTurn.Select(m => m.Id));
        messages.Select(m => m.Role).Should().Equal(
            AiThreadMessageLike.RoleEnum.User,
            AiThreadMessageLike.RoleEnum.Assistant,
            AiThreadMessageLike.RoleEnum.User,
            AiThreadMessageLike.RoleEnum.Assistant);
        (await ListIdsAsync()).Should().Equal(threadId);

        // An empty threadId is another new thread, not this one.
        var third = await SendWithStreamAsync(string.Empty, "Say OK.", profile.Id);
        third.StatusCode.Should().Be(HttpStatusCode.OK, third.ToString());
        third.ThreadId.Should().NotBeNullOrEmpty(third.ToString()).And.NotBe(threadId);
        (await ListIdsAsync()).Should().BeEquivalentTo(new[] { threadId, third.ThreadId! });
    }

    [Fact]
    public async Task Thread_TheModelsOwnReply_CanBeReadRewrittenAndDeleted_AndClearingRemovesBothTurns()
    {
        var thread = await CreateOwnThreadAsync("Reply handling");
        var round = await SendWithStreamAsync(thread.ThreadId, "Say OK.", thread.ProfileId);
        round.StreamError.Should().BeNull(round.ToString());

        var messages = await ReadAllAsync(thread.ThreadId);
        var reply = messages.Single(m => m.Role == AiThreadMessageLike.RoleEnum.Assistant);

        var asAssistant = await _aiThreads.AiThreadsGetMessageByIdAsync(reply.Id, TestContext.Current.CancellationToken);
        asAssistant.Role.Should().Be(AiThreadMessageLike.RoleEnum.Assistant);
        asAssistant.Id.Should().Be(reply.Id);
        TextOf(asAssistant).Should().Be(TextOf(reply));

        // An assistant message can be rewritten in place and keeps its role and position.
        var edited = new AiThreadMessageLike(
            role: AiThreadMessageLike.RoleEnum.Assistant,
            content: new AiMessageContent([new AiMessageContentBlock(type: "text", text: "Edited reply")]));
        (await _aiThreads.AiThreadsUpdateMessageAsync(new AiThreadsUpdateMessageRequest(reply.Id, edited), TestContext.Current.CancellationToken))
            .Success.Should().BeTrue();
        var after = await ReadAllAsync(thread.ThreadId);
        after.Select(m => m.Id).Should().Equal(messages.Select(m => m.Id));
        after[1].Role.Should().Be(AiThreadMessageLike.RoleEnum.Assistant);
        TextOf(after[1]).Should().Be("Edited reply");

        // Deleting the reply leaves the question.
        (await _aiThreads.AiThreadsDeleteMessageAsync(reply.Id, TestContext.Current.CancellationToken)).Success.Should().BeTrue();
        (await ReadAllAsync(thread.ThreadId)).Select(m => m.Role).Should().Equal(AiThreadMessageLike.RoleEnum.User);

        // clear-messages removes the rest.
        (await _aiThreads.AiThreadsClearMessagesAsync(thread.ThreadId, TestContext.Current.CancellationToken)).Success.Should().BeTrue();
        (await ReadAllAsync(thread.ThreadId)).Should().BeEmpty();
    }

    [Fact]
    public async Task Thread_TwoUsersInOneRoom_EachKeepAPrivateThread_AndNeitherReachesTheOther()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var profile = await GetProfileAsync();
        var roomId = await CreateRoomAsync("Autotest Shared AI Room");
        var ownerThread = await CreateOwnThreadAsync("Owner's");
        await AppendUserMessageAsync(ownerThread.ThreadId, "owner secret");
        var ownerBefore = await SnapshotAsync(ownerThread.ThreadId);

        var member = await InviteContact(EmployeeType.User, cancellationToken);
        await RoomsApi.SetRoomSecurityAsync(
            roomId,
            new RoomInvitationRequest([new RoomInvitation(member.Id, FileShare.ContentCreator)], notify: false),
            cancellationToken);

        await _aiChatClient.Authenticate(member);
        var memberThread = await CreateThreadAsync("Member's", profile.Id, roomId.ToString());
        await AppendUserMessageAsync(memberThread.ThreadId, "member secret");
        var memberBefore = await SnapshotAsync(memberThread.ThreadId);

        // The member cannot read, change or remove the owner's thread.
        (await StatusOfAsync(() => GetThreadAsync(ownerThread.ThreadId))).Should().Be(403, "get-by-id");
        (await StatusOfAsync(() => ReadAllAsync(ownerThread.ThreadId))).Should().Be(403, "read-messages");
        (await StatusOfAsync(() => _aiThreads.AiThreadsRenameAsync(new AiThreadsRenameRequest(ownerThread.ThreadId, "x"), cancellationToken)))
            .Should().Be(403, "rename");
        (await StatusOfAsync(() => _aiThreads.AiThreadsClearMessagesAsync(ownerThread.ThreadId, cancellationToken))).Should().Be(403, "clear-messages");
        (await StatusOfAsync(() => _aiThreads.AiThreadsDeleteAsync(ownerThread.ThreadId, cancellationToken))).Should().Be(403, "delete");

        // The member's own list holds only the member's thread.
        (await ListIdsAsync()).Should().Equal(memberThread.ThreadId);

        await _aiChatClient.Authenticate(Owner);

        // The owner's data is unchanged, and the owner's list holds only the owner's thread.
        (await SnapshotAsync(ownerThread.ThreadId)).Should().Be(ownerBefore);
        (await ListIdsAsync()).Should().Equal(ownerThread.ThreadId);

        // The owner of the room has no back door into the member's thread either.
        (await StatusOfAsync(() => GetThreadAsync(memberThread.ThreadId))).Should().Be(403, "the owner reading the member's thread");
        (await StatusOfAsync(() => ReadAllAsync(memberThread.ThreadId))).Should().Be(403, "the owner reading the member's messages");

        // And the member's thread is exactly what it was.
        await _aiChatClient.Authenticate(member);
        (await SnapshotAsync(memberThread.ThreadId)).Should().Be(memberBefore);
    }

    /// <summary>
    /// Two threads never mix: changing or deleting one leaves the other whole. The TypeScript case put the two in
    /// two agents and listed them per agent; threads are one list per user by design (Bug 82719), so what remains
    /// is the isolation of one thread's changes from another's.
    /// </summary>
    [Fact]
    public async Task Thread_ChangingOrDeletingOne_LeavesAnotherWhole()
    {
        var profile = await GetProfileAsync();
        var otherRoom = await CreateRoomAsync("Autotest Second AI Room");
        var first = await CreateOwnThreadAsync("First thread");
        var second = await CreateThreadAsync("Second thread", profile.Id, otherRoom.ToString());
        var firstMessage = await AppendUserMessageAsync(first.ThreadId, "in the first");
        await AppendUserMessageAsync(second.ThreadId, "in the second");
        var secondBefore = await SnapshotAsync(second.ThreadId);

        await _aiThreads.AiThreadsRenameAsync(new AiThreadsRenameRequest(first.ThreadId, "Renamed first"), TestContext.Current.CancellationToken);
        await _aiThreads.AiThreadsUpdateMessageAsync(
            new AiThreadsUpdateMessageRequest(firstMessage, BuildUserMessage("edited in the first")),
            TestContext.Current.CancellationToken);
        await _aiThreads.AiThreadsClearMessagesAsync(first.ThreadId, TestContext.Current.CancellationToken);

        (await SnapshotAsync(second.ThreadId)).Should().Be(secondBefore, "rename, edit and clear stayed in their thread");

        await _aiThreads.AiThreadsDeleteAsync(first.ThreadId, TestContext.Current.CancellationToken);

        (await ListIdsAsync()).Should().Equal(second.ThreadId);
        (await SnapshotAsync(second.ThreadId)).Should().Be(secondBefore, "deleting a thread stayed in its thread");
    }

    [Fact]
    public async Task Thread_FortyMessageHistory_IsReadWholeEditedPrunedPagedClearedAndStartsOverClean()
    {
        var thread = await CreateOwnThreadAsync("Forty");
        var markers = Enumerable.Range(0, 40).Select(i => $"marker-{i:00}").ToList();
        var ids = new List<string>();

        // One request at a time: the order of the history is what is tested, and concurrent writes promise none.
        foreach (var marker in markers)
        {
            ids.Add(await AppendUserMessageAsync(thread.ThreadId, marker));
        }

        (await ReadTextsAsync(thread.ThreadId)).Should().Equal(markers, "all 40, in the order they were sent");
        (await ReadAllAsync(thread.ThreadId)).Select(m => m.Id).Should().Equal(ids);

        // Edit one in the middle.
        await _aiThreads.AiThreadsUpdateMessageAsync(
            new AiThreadsUpdateMessageRequest(ids[20], BuildUserMessage("marker-20 edited")),
            TestContext.Current.CancellationToken);
        var expected = markers.Select((m, i) => i == 20 ? "marker-20 edited" : m).ToList();
        (await ReadTextsAsync(thread.ThreadId)).Should().Equal(expected);

        // Delete the first, a middle and the last.
        int[] pruned = [0, 19, 39];
        foreach (var index in pruned)
        {
            (await _aiThreads.AiThreadsDeleteMessageAsync(ids[index], TestContext.Current.CancellationToken)).Success.Should().BeTrue();
        }

        var remaining = expected.Where((_, i) => !pruned.Contains(i)).ToList();
        (await ReadTextsAsync(thread.ThreadId)).Should().Equal(remaining);
        (await StatusOfAsync(() => _aiThreads.AiThreadsGetMessageByIdAsync(ids[19], TestContext.Current.CancellationToken)))
            .Should().Be(404, "a pruned message is not served");

        // Page the remainder back to front in sevens: every message once.
        var seen = new List<string?>();
        string? cursor = null;
        for (var page = 0; page < 8; page++)
        {
            var items = await _aiThreads.AiThreadsReadMessagesAsync(
                thread.ThreadId,
                7,
                cursor,
                "desc",
                TestContext.Current.CancellationToken);
            if (items.Count == 0)
            {
                break;
            }

            seen.AddRange(items.Select(TextOf));
            var last = items[^1];
            cursor = JsonSerializer.Serialize(new { createdAt = last.CreatedAt, id = last.Id });
        }

        seen.Should().Equal(Enumerable.Reverse(remaining));

        // Clear, then a new message, and the old history stays gone.
        (await _aiThreads.AiThreadsClearMessagesAsync(thread.ThreadId, TestContext.Current.CancellationToken)).Success.Should().BeTrue();
        (await ReadAllAsync(thread.ThreadId)).Should().BeEmpty();
        await AppendUserMessageAsync(thread.ThreadId, "a fresh start");
        (await ReadTextsAsync(thread.ThreadId)).Should().Equal("a fresh start");
        (await OpenAsync(thread.ThreadId)).PriorMessages.Should().ContainSingle("open-or-create replays only the new message");
    }

    [Fact]
    public async Task Thread_ReadWhileBeingDeleted_GivesOnlyWholeAnswers_AndIsGoneAfterwards()
    {
        var thread = await CreateOwnThreadAsync("Vanishing");
        foreach (var text in new[] { "a", "b", "c" })
        {
            await AppendUserMessageAsync(thread.ThreadId, text);
        }

        async Task<(int Status, List<string?>? Texts)> ReadOnceAsync()
        {
            try
            {
                return (200, (await ReadAllAsync(thread.ThreadId)).Select(TextOf).ToList());
            }
            catch (ApiException exception)
            {
                return (exception.ErrorCode, null);
            }
        }

        var readsTask = Task.WhenAll(Enumerable.Range(0, 4).Select(_ => ReadOnceAsync()));
        var deleteTask = _aiThreads.AiThreadsDeleteAsync(thread.ThreadId, TestContext.Current.CancellationToken);
        var reads = await readsTask;
        var deleted = await deleteTask;

        deleted.Success.Should().BeTrue();

        // A read that races the delete may land on either side of it. Both are legitimate; a 500 or a half-empty
        // list is not.
        foreach (var read in reads)
        {
            if (read.Status == 200)
            {
                read.Texts.Should().Equal("a", "b", "c");
            }
            else
            {
                read.Status.Should().Be(404);
            }
        }

        (await StatusOfAsync(() => GetThreadAsync(thread.ThreadId))).Should().Be(404);
        (await ListThreadsAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task Thread_IdsNeverIssued_ReachNothingOnAnyRoute()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var profile = await GetProfileAsync();
        var thread = await CreateOwnThreadAsync("Bystander");
        await AppendUserMessageAsync(thread.ThreadId, "still here");
        var before = await SnapshotAsync(thread.ThreadId);

        var outcomes = new Dictionary<string, int>
        {
            ["get-by-id"] = await StatusOfAsync(() => GetThreadAsync(UnknownId)),
            ["read-messages"] = await StatusOfAsync(() => ReadAllAsync(UnknownId)),
            ["rename"] = await StatusOfAsync(() => _aiThreads.AiThreadsRenameAsync(new AiThreadsRenameRequest(UnknownId, "x"), cancellationToken)),
            ["touch"] = await StatusOfAsync(() => _aiThreads.AiThreadsTouchAsync(new AiThreadsTouchRequest(UnknownId), cancellationToken)),
            ["clear-messages"] = await StatusOfAsync(() => _aiThreads.AiThreadsClearMessagesAsync(UnknownId, cancellationToken)),
            ["delete"] = await StatusOfAsync(() => _aiThreads.AiThreadsDeleteAsync(UnknownId, cancellationToken)),
            // The SDK call is fine on a failure: its defect (the declared shape of the answer) only bites on a 200.
            ["append-user-message"] = await StatusOfAsync(() => _aiThreads.AiThreadsAppendUserMessageAsync(
                new AiThreadsAppendUserMessageRequest(threadId: UnknownId, message: BuildUserMessage("x"), profileId: profile.Id),
                cancellationToken)),
            ["open-or-create"] = await StatusOfAsync(() => OpenAsync(UnknownId)),
            ["regenerate-title"] = await StatusOfAsync(() => _aiThreads.AiThreadsRegenerateTitleAsync(
                new AiThreadsRegenerateTitleRequest(UnknownId, profile),
                cancellationToken)),
            ["update-message"] = await StatusOfAsync(() => _aiThreads.AiThreadsUpdateMessageAsync(
                new AiThreadsUpdateMessageRequest(UnknownId, BuildUserMessage("x")),
                cancellationToken))
        };

        (await SnapshotAsync(thread.ThreadId)).Should().Be(before);
        (await ListIdsAsync()).Should().Equal(thread.ThreadId);
        outcomes.Where(o => o.Value != 404).Should().BeEmpty("every route answers 404 for an id that was never issued");
    }
}

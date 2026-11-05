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
/// Picking the model for one conversation. The model is a property of the THREAD, not of the location: two threads
/// side by side keep different models, the profile a message is sent with becomes the thread's model, and a message
/// sent without one keeps it.
/// </summary>
[Trait("Category", "Chat")]
[Trait("Feature", "AI/Chat")]
public class ChatThreadModelTests(AspireAppFixture fixture) : ChatPart2TestBase(fixture)
{
    private const string FirstAnswer = "Noted the code word";
    private const string Taught = "Remember the code word ORANGE. Reply with just: OK.";
    private const string Asked = "What code word did I ask you to remember? Reply with just that word.";

    [Fact]
    public async Task Create_TwoThreadsOfOneRoom_KeepDifferentModels()
    {
        // Arrange
        var (first, second) = await TwoChatProfilesAsync();
        var roomId = (await CreateCustomRoomAsync("Autotest Model Room")).ToString();

        var threadA = await CreateThreadAsync("Autotest thread on the first model", first.Id, roomId);
        var threadB = await CreateThreadAsync("Autotest thread on the second model", second.Id, roomId);

        // Act
        var readA = await ThreadProfileIdAsync(threadA.ThreadId);
        var readB = await ThreadProfileIdAsync(threadB.ThreadId);
        var listed = (await ListThreadsAsync(roomId)).ToDictionary(t => t.ThreadId, t => t.ProfileId);

        // Assert
        readA.Should().Be(first.Id);
        readB.Should().Be(second.Id);
        listed.Should().Contain(threadA.ThreadId, first.Id).And.Contain(threadB.ThreadId, second.Id);

        // Talking in one does not touch the other's model.
        AssertHealthyReply(await SendWithStreamAsync(threadB.ThreadId, ReplyOk, second.Id, roomId));

        (await ThreadProfileIdAsync(threadB.ThreadId)).Should().Be(second.Id);
        (await ThreadProfileIdAsync(threadA.ThreadId)).Should().Be(first.Id);
    }

    [Fact]
    public async Task Create_TwoThreadsOfOneFolder_KeepDifferentModels()
    {
        // A folder is the third context the picker appears in; the model stays attached to the thread even though
        // the listing does not separate the locations (Bug 82855, by design).

        // Arrange
        var (first, second) = await TwoChatProfilesAsync();
        var folderId = (await CreateFolderAsync(await MyDocumentsIdAsync(), "Autotest Model Folder")).ToString();

        // Act
        var threadA = await CreateThreadAsync("Autotest folder thread on the first model", first.Id, folderId);
        var threadB = await CreateThreadAsync("Autotest folder thread on the second model", second.Id, folderId);

        // Assert
        (await ThreadProfileIdAsync(threadA.ThreadId)).Should().Be(first.Id);
        (await ThreadProfileIdAsync(threadB.ThreadId)).Should().Be(second.Id);

        var listed = (await ListThreadsAsync(folderId)).ToDictionary(t => t.ThreadId, t => t.ProfileId);
        listed.Should().Contain(threadA.ThreadId, first.Id).And.Contain(threadB.ThreadId, second.Id);
    }

    [Fact]
    public async Task SendWithStream_AnotherProfile_BecomesTheThreadsModelButNotTheBinding()
    {
        // The picker has no route of its own: the new choice rides along with the next message, and sticks.

        // Arrange
        var (first, second) = await TwoChatProfilesAsync();
        await AssignAsync(AiActionType.Chat, first.Id);

        var roomId = (await CreateCustomRoomAsync("Autotest Model Room")).ToString();
        var thread = await CreateThreadAsync("Autotest switching thread", first.Id, roomId);
        (await ThreadProfileIdAsync(thread.ThreadId)).Should().Be(first.Id);

        // Act
        var round = await SendWithStreamAsync(thread.ThreadId, ReplyOk, second.Id, roomId);

        // Assert
        AssertHealthyReply(round);

        // Reopening the conversation shows the model it was last answered with, both by id and in the list.
        (await ThreadProfileIdAsync(thread.ThreadId)).Should().Be(second.Id);
        (await ListedProfileIdAsync(thread.ThreadId, roomId)).Should().Be(second.Id);

        // The switch was per thread: the binding has not moved, so a new conversation still starts on the first.
        (await GetAllAssignmentsAsync()).Should().ContainKey(nameof(AiActionType.Chat)).WhoseValue.Should().Be(first.Id);

        var sibling = await CreateThreadAsync("Autotest sibling thread", first.Id, roomId);
        (await ThreadProfileIdAsync(sibling.ThreadId)).Should().Be(first.Id);
    }

    [Fact]
    public async Task SendWithStream_PickerSwitchesModel_TheNewModelGetsTheEarlierTurns()
    {
        // The half the user feels: the model just picked has to answer from what was said to the previous one. Each
        // profile is on a provider of its own, so what the second model was sent is observable — a backend that
        // started a fresh conversation whenever the model changed would pass the stored-profileId test and fail
        // this one.

        // Arrange
        using var firstProvider = new StubModelProvider(FirstAnswer);
        using var secondProvider = new StubModelProvider("ORANGE");
        var first = await CreateProfileOnAsync(firstProvider);
        var second = await CreateProfileOnAsync(secondProvider);

        var roomId = (await CreateCustomRoomAsync("Autotest Model Room")).ToString();
        var thread = await CreateThreadAsync("Autotest thread that changes its model", first.Id, roomId);

        AssertHealthyReply(await SendWithStreamAsync(thread.ThreadId, Taught, first.Id, roomId));

        // Act
        var round = await SendWithStreamAsync(thread.ThreadId, Asked, second.Id, roomId);

        // Assert
        AssertHealthyReply(round);
        round.AssistantText.Should().Be("ORANGE", "the model the picker switched to is the one that answered");

        secondProvider.CompletionRequestBodies.Should().Contain(
            body => body.Contains("Remember the code word ORANGE") && body.Contains(FirstAnswer) && body.Contains("What code word"),
            $"the second model is sent the turn the first one was given\n{secondProvider.Describe()}");

        var messages = await ReadMessagesAsync(thread.ThreadId);
        UserTexts(messages).Should().Equal(Taught, Asked);
        AssistantTexts(messages).Should().Equal(FirstAnswer, "ORANGE");
        (await ThreadProfileIdAsync(thread.ThreadId)).Should().Be(second.Id);
    }

    [Fact]
    public async Task SendWithStream_RoomMemberSwitchesTheModelOfOwnThread_SwitchSticks()
    {
        // A member of somebody else's room is who the picker is for: the choice belongs to the user, not to whoever
        // owns the room.

        // Arrange
        var (first, second) = await TwoChatProfilesAsync();
        var roomId = await CreateCustomRoomAsync("Autotest Shared Model Room");
        var entityId = roomId.ToString();

        var member = await InviteContact(EmployeeType.RoomAdmin, TestContext.Current.CancellationToken);
        await InviteToRoomAsync(roomId, member);
        await _aiChatClient.Authenticate(member);

        var thread = await CreateThreadAsync("Autotest member thread", first.Id, entityId);
        (await ThreadProfileIdAsync(thread.ThreadId)).Should().Be(first.Id, "the thread starts on the model the member picked");

        // Act
        var round = await SendWithStreamAsync(thread.ThreadId, ReplyOk, second.Id, entityId);

        // Assert
        AssertHealthyReply(round);
        (await ThreadProfileIdAsync(thread.ThreadId)).Should().Be(second.Id, "the member's own switch is what the thread now runs on");
        (await ListedProfileIdAsync(thread.ThreadId, entityId)).Should().Be(second.Id, "and the list the picker is drawn from agrees");
    }

    /// <summary>
    /// A message sent without a <c>profileId</c> used to lose the user's pick: the backend resolved the location's
    /// model and wrote THAT onto the thread. The chat service now pre-fills the thread's own model
    /// (aiController.ts sendWithStream) and the engine recovers it too, so the thread keeps its model.
    /// </summary>
    [Fact]
    [Trait("Bug", "82860")]
    public async Task SendWithStream_NoProfileId_KeepsTheThreadsChosenModel()
    {
        // Arrange
        var (first, second) = await TwoChatProfilesAsync();
        var roomId = (await CreateCustomRoomAsync("Autotest Model Room")).ToString();

        // The room resolves through the portal-wide binding, so it is pointed at a profile the thread is NOT on —
        // otherwise "the model did not change" and "it was replaced by the one it already had" look the same.
        await AssignAsync(AiActionType.Chat, first.Id);
        (await _aiAssignments.AiAssignmentsResolveForActionAsync(nameof(AiActionType.Chat), roomId, TestContext.Current.CancellationToken))
            .ProfileId.Should().Be(first.Id, "the room resolves the other profile");

        var thread = await CreateThreadAsync("Autotest thread in a room", second.Id, roomId);
        (await ThreadProfileIdAsync(thread.ThreadId)).Should().Be(second.Id, "the thread starts on the chosen model");

        // Act
        var round = await SendWithStreamAsync(thread.ThreadId, ReplyOk, profileId: null, entityId: roomId);

        // Assert
        AssertHealthyReply(round);
        (await ThreadProfileIdAsync(thread.ThreadId)).Should().Be(second.Id, "the thread keeps the model it was created with");
    }
}

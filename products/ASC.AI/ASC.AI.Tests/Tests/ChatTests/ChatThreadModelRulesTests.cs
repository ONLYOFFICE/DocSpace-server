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
/// The edges of a thread's model: no route but a send moves it, a thread created without one has none, a rebinding
/// of Chat leaves running conversations alone, and a profile choice the backend cannot use is refused (on create)
/// or reported (on send) without touching the thread.
/// </summary>
[Trait("Category", "Validation")]
[Trait("Feature", "AI/Chat")]
public class ChatThreadModelRulesTests(AspireAppFixture fixture) : ChatPart2TestBase(fixture)
{
    // A well-formed GUID that names no profile.
    private const string UnknownProfileId = "019ed118-0000-0000-0000-0000000000ff";

    [Fact]
    public async Task ThreadRoutes_NoneButASendChangesTheModel()
    {
        // Why the switch has to ride on a message: a client looking for a "set the model of this thread" call finds
        // candidates that either do not exist or accept the field and drop it.

        // Arrange
        var (first, second) = await TwoChatProfilesAsync();
        var roomId = (await CreateCustomRoomAsync("Autotest Model Room")).ToString();
        var thread = await CreateThreadAsync("Autotest thread", first.Id, roomId);
        var raw = new RawApiClient(_aiChatClient);

        // Act
        // Raw on purpose: the first two routes do not exist, and rename's contract (AiThreadsRenameRequest) has no
        // profileId — the point is what the route does with one a client sends anyway.
        using var update = await raw.PutAsync(
            "/api/2.0/ai/threads/update",
            new { threadId = thread.ThreadId, profileId = second.Id },
            TestContext.Current.CancellationToken);
        using var setProfile = await raw.PutAsync(
            "/api/2.0/ai/threads/set-profile",
            new { threadId = thread.ThreadId, profileId = second.Id },
            TestContext.Current.CancellationToken);
        using var renamed = await raw.PutAsync(
            "/api/2.0/ai/threads/rename",
            new { threadId = thread.ThreadId, title = "Autotest thread", profileId = second.Id },
            TestContext.Current.CancellationToken);

        // Assert
        update.StatusCode.Should().Be(HttpStatusCode.NotFound, "PUT threads/update");
        setProfile.StatusCode.Should().Be(HttpStatusCode.NotFound, "PUT threads/set-profile");
        renamed.StatusCode.Should().Be(HttpStatusCode.OK, "rename exists and takes the extra field without complaint");
        (await ThreadProfileIdAsync(thread.ThreadId)).Should().Be(first.Id, "and ignores it");
    }

    [Fact]
    public async Task Create_WithoutProfileId_ThreadHasNoModelUntilASendBringsOne()
    {
        // "A new conversation starts on the model of the place it was started in" has no create-time half: leaving
        // the field out does not make the backend resolve anything onto the thread.

        // Arrange
        var profile = await CreateAssignableProfileAsync();
        var roomId = (await CreateCustomRoomAsync("Autotest Model Room")).ToString();

        // The Chat binding is set, so "no model" below cannot be "there was nothing to resolve to".
        await AssignAsync(AiActionType.Chat, profile.Id);

        // Act
        // The generated constructor is not annotated for nullability; null leaves profileId out of the body.
        var thread = await _aiThreads.AiThreadsCreateAsync(
            new AiThreadsCreateRequest(title: "Autotest thread with no model", profileId: null!, entityId: roomId),
            TestContext.Current.CancellationToken);

        // Assert
        thread.ThreadId.Should().NotBeNullOrEmpty();
        (await ThreadProfileIdAsync(thread.ThreadId)).Should().BeNull("the thread carries no model of its own");
        (await ListedProfileIdAsync(thread.ThreadId, roomId)).Should().BeNull("and the list the picker is drawn from carries none either");

        // Control: the field is not simply missing from the DTO — a message with a profile fills it in.
        AssertHealthyReply(await SendWithStreamAsync(thread.ThreadId, ReplyOk, profile.Id, roomId));
        (await ThreadProfileIdAsync(thread.ThreadId)).Should().Be(profile.Id);
    }

    [Fact]
    public async Task Assign_RebindingChat_DoesNotMoveAnExistingThread()
    {
        // The portal-wide binding is the only model setting an ordinary location has; changing it must not reach
        // back into conversations already under way.

        // Arrange
        var (first, second) = await TwoChatProfilesAsync();
        var roomId = (await CreateCustomRoomAsync("Autotest Model Room")).ToString();
        await AssignAsync(AiActionType.Chat, first.Id);

        var thread = await CreateThreadAsync("Autotest thread on the first model", first.Id, roomId);
        (await ThreadProfileIdAsync(thread.ThreadId)).Should().Be(first.Id);

        // Act
        await AssignAsync(AiActionType.Chat, second.Id);

        // Assert
        (await GetAllAssignmentsAsync())
            .Should().ContainKey(nameof(AiActionType.Chat)).WhoseValue.Should().Be(second.Id, "the binding really changed");
        (await ThreadProfileIdAsync(thread.ThreadId)).Should().Be(first.Id, "the conversation keeps the model it was started on");
        (await ListedProfileIdAsync(thread.ThreadId, roomId)).Should().Be(first.Id);
    }

    /// <summary>
    /// A chosen profile that is not a profile id at all is refused and leaves no thread behind. A profile exists on
    /// the portal, so the refusal is about the id and not about there being no model to fall back to. (A
    /// well-formed id of no profile, 404, is <c>ThreadCreateTests.Create_NonExistentProfileId_Returns404</c>.)
    /// </summary>
    [Theory]
    [InlineData("autotest-not-a-profile")]
    [InlineData("")]
    public async Task Create_MalformedProfileId_Returns400AndCreatesNothing(string badProfileId)
    {
        // Arrange
        await GetDefaultProfileAsync();
        var roomId = (await CreateCustomRoomAsync("Autotest Model Room")).ToString();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _aiThreads.AiThreadsCreateAsync(
                new AiThreadsCreateRequest(title: "Autotest bad profile", profileId: badProfileId, entityId: roomId),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400, exception.ErrorContent?.ToString());
        (await ListThreadsAsync(roomId)).Should().BeEmpty("the refusal left no thread behind");
    }

    /// <summary>
    /// A send naming a profile that does not exist used to be answered in silence by whatever model the backend
    /// picked instead. The chat service now refuses it before the stream opens (400, unknownProfileIdError in
    /// aiController.ts), and the thread keeps the model it was on.
    /// </summary>
    [Fact]
    [Trait("Bug", "83160")]
    public async Task SendWithStream_ProfileIdThatNamesNoModel_IsRefusedAndTheThreadKeepsItsModel()
    {
        // Arrange
        var (roomId, threadId, _, second) = await RoomThreadOnSecondModelAsync();

        // Act
        var round = await SendWithStreamAsync(threadId, ReplyOk, UnknownProfileId, roomId);

        // Assert
        round.StatusCode.Should().Be(HttpStatusCode.BadRequest, $"a model choice the backend cannot resolve is reported, not dropped: {round}");
        (await ThreadProfileIdAsync(threadId)).Should().Be(second.Id, "the thread keeps the model it was on");
        (await ReadMessagesAsync(threadId)).Should().BeEmpty("a refused send stores nothing");
    }

    [Fact]
    public async Task SendWithStream_BlankProfileId_LeavesTheThreadsModelAlone()
    {
        // "" is what a client sends when its picker holds nothing. The send takes it, answers, and the thread stays on
        // its own model — not on the portal-wide binding, which points at the other profile.

        // Arrange
        var (roomId, threadId, _, second) = await RoomThreadOnSecondModelAsync();

        // Act
        var round = await SendWithStreamAsync(threadId, ReplyOk, profileId: string.Empty, entityId: roomId);

        // Assert
        AssertHealthyReply(round);
        (await ThreadProfileIdAsync(threadId)).Should().Be(second.Id, "a blank id is not a pick: the thread keeps its own model");
        (await ListedProfileIdAsync(threadId, roomId)).Should().Be(second.Id);
    }

    /// <summary>
    /// A room thread on the second profile while the portal-wide Chat binding points at the first, so "kept its own
    /// model", "took the room's" and "was wiped" are three distinguishable outcomes.
    /// </summary>
    private async Task<(string RoomId, string ThreadId, AiProfile First, AiProfile Second)> RoomThreadOnSecondModelAsync()
    {
        var (first, second) = await TwoChatProfilesAsync();
        var roomId = (await CreateCustomRoomAsync("Autotest Model Room")).ToString();

        await AssignAsync(AiActionType.Chat, first.Id);
        (await _aiAssignments.AiAssignmentsResolveForActionAsync(nameof(AiActionType.Chat), roomId, TestContext.Current.CancellationToken))
            .ProfileId.Should().Be(first.Id, "the room resolves the other profile");

        var thread = await CreateThreadAsync("Autotest thread in a room", second.Id, roomId);
        (await ThreadProfileIdAsync(thread.ThreadId)).Should().Be(second.Id, "the thread starts on the chosen model");

        return (roomId, thread.ThreadId, first, second);
    }
}

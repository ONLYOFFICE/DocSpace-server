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
/// "Chat is off for guests": a Guest is refused by user type, not by access level — in an AI room (where Read is
/// the most a Guest can hold), in an ordinary room where they hold ContentCreator, with no room in scope, on the
/// routes that need no thread, and on the owner's thread and messages. The premise of each case (the Guest really
/// can open the room) is asserted, so a lost invitation cannot pass for the gate.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "AI/Chat")]
public class ChatPermissionGuestTests(AspireAppFixture fixture) : ChatPermissionTestBase(fixture)
{
    private const string GuestRoleRefused = "The role is not available for this user type";

    /// <summary>
    /// The TypeScript block also had a User invited at Read refused on the thread surface. Threads are one list per
    /// user and any accessible entity is accepted (Bug 82719 design), so a Read-level User is not refused by
    /// construction; only the Guest half — refused by user type — is ported.
    /// </summary>
    [Fact]
    public async Task Create_GuestInvitedAtReadToAnAiRoom_Returns403()
    {
        var profile = await GetProfileAsync();
        var roomId = await CreateRoomAsync("Autotest Chat AI Room");
        var guest = await InviteMemberAsync(EmployeeType.Guest);
        await ShareRoomAsync(roomId, guest.Id, FileShare.Read);

        await _aiChatClient.Authenticate(guest);
        var created = await OutcomeOfAsync(() => CreateThreadAsync("Read-only member thread", profile.Id, roomId.ToString()));
        var listed = await OutcomeOfAsync(() => ListThreadsAsync());

        listed.Status.Should().Be(403, listed.Error);
        listed.Error.Should().Contain("Forbidden");
        created.Status.Should().Be(403, created.Error);
        created.Error.Should().Contain("Forbidden");
    }

    [Fact]
    public async Task CreateAndList_GuestWithContentAccessInAnOrdinaryRoom_Return403()
    {
        var profile = await GetProfileAsync();
        var roomId = await CreateCustomRoomAsync("Autotest Guest Room");
        var guest = await InviteMemberAsync(EmployeeType.Guest);
        // ContentCreator, not Read: the refusal has to be about the user type, not the access level.
        await ShareRoomAsync(roomId, guest.Id, FileShare.ContentCreator);

        // Premise: the invitation landed and the room is open to this Guest.
        await FilesHttpClient.Authenticate(guest);
        (await RoomsApi.GetRoomInfoAsync(roomId, TestContext.Current.CancellationToken)).Response.Id
            .Should().Be(roomId, "the Guest can open the room they were invited into");

        await _aiChatClient.Authenticate(guest);
        var created = await OutcomeOfAsync(() => CreateThreadAsync("Guest room thread", profile.Id, roomId.ToString()));
        var listed = await OutcomeOfAsync(() => ListThreadsAsync());

        new { create = created.Status, list = listed.Status }.Should().BeEquivalentTo(new { create = 403, list = 403 });
    }

    [Fact]
    public async Task Create_GuestWithNoRoomInScope_Returns403_WhileTheOwnerCan()
    {
        var profile = await GetProfileAsync();
        var guest = await InviteMemberAsync(EmployeeType.Guest);

        // A Guest has no Documents of their own to name, so the empty scope is the whole of that surface for them.
        await _aiChatClient.Authenticate(guest);
        var guestThread = await OutcomeOfAsync(() => CreateThreadAsync("Guest thread, no entity", profile.Id));

        // Positive control: the same call from the owner. Without it a refusal here could just as well be "an empty
        // entityId is not a scope at all".
        await _aiChatClient.Authenticate(Owner);
        var ownerThread = await CreateThreadAsync("Owner thread, no entity", profile.Id);

        IsUuid(ownerThread.ThreadId).Should().BeTrue("the empty scope is a real chat for a non-Guest");
        (await ListIdsAsync()).Should().Equal(new[] { ownerThread.ThreadId });
        guestThread.Status.Should().Be(403, guestThread.Error);
    }

    /// <summary>
    /// Closing threads/create to a Guest only closes the threaded path: <c>send</c> resolves the model from the
    /// assignments and <c>send-custom</c> carries its own prompt, so if the gate lived on the thread routes alone the
    /// model would still be one request away.
    /// </summary>
    [Fact]
    public async Task SendAndSendCustom_Guest_Return403()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await GetProfileAsync();
        var roomId = await CreateCustomRoomAsync("Autotest Guest Room");
        var guest = await InviteMemberAsync(EmployeeType.Guest);
        await ShareRoomAsync(roomId, guest.Id, FileShare.ContentCreator);
        var message = BuildUserMessage("Reply with the single word OK.");

        await _aiChatClient.Authenticate(guest);

        // The SDK calls are fine on this path: the streaming defect only bites when the request succeeds.
        var send = await OutcomeOfAsync(() => _aiChat.AiSendAsync(
            new AiSendRequest(actionType: AiActionType.Chat, userMessage: message, entityId: roomId.ToString()),
            cancellationToken));
        var sendCustom = await OutcomeOfAsync(() => _aiChat.AiSendCustomAsync(
            new AiSendCustomRequest(isStream: false, systemPrompt: "Answer with a single word.", userMessage: message),
            cancellationToken));
        var sendCustomStreamed = await OutcomeOfAsync(() => _aiChat.AiSendCustomAsync(
            new AiSendCustomRequest(isStream: true, systemPrompt: "Answer with a single word.", userMessage: message),
            cancellationToken));

        new { send = send.Status, sendCustom = sendCustom.Status, sendCustomStreamed = sendCustomStreamed.Status }
            .Should().BeEquivalentTo(new { send = 403, sendCustom = 403, sendCustomStreamed = 403 });
    }

    /// <summary>
    /// The per-message routes take a message id and no entity, and open-or-create takes a thread id, so neither
    /// passes through threads/create where the guest gate sits. An ordinary non-member's open-or-create is the
    /// control: it shows the Guest's refusal is the same one any outsider gets.
    /// </summary>
    [Fact]
    public async Task MessageRoutesAndOpenOrCreate_GuestOnTheOwnersThread_Return403()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var seed = await SeedOwnerThreadAsync();
        var guest = await InviteMemberAsync(EmployeeType.Guest);
        var outsider = await InviteMemberAsync(EmployeeType.User);

        await _aiChatClient.Authenticate(guest);
        var read = await OutcomeOfAsync(() => _aiThreads.AiThreadsGetMessageByIdAsync(seed.MessageId, cancellationToken));
        var rewritten = await OutcomeOfAsync(() => _aiThreads.AiThreadsUpdateMessageAsync(
            new AiThreadsUpdateMessageRequest(seed.MessageId, BuildUserMessage("Hijacked by a Guest")),
            cancellationToken));
        var deleted = await OutcomeOfAsync(() => _aiThreads.AiThreadsDeleteMessageAsync(seed.MessageId, cancellationToken));
        var opened = await OutcomeOfAsync(() => OpenOwnersThreadAsync(seed, "Hijacked by a Guest"));

        await _aiChatClient.Authenticate(outsider);
        var openedByOutsider = await OutcomeOfAsync(() => OpenOwnersThreadAsync(seed, "Hijacked by an outsider"));

        // The owner's chat first: the refusals must not have cost the message, its text or the thread's timestamp.
        await ExpectOwnerThreadIntactAsync(seed);
        TextOf(await _aiThreads.AiThreadsGetMessageByIdAsync(seed.MessageId, cancellationToken)).Should().Be(OwnerMessage);

        new { getMessageById = read.Status, updateMessage = rewritten.Status, deleteMessage = deleted.Status }
            .Should().BeEquivalentTo(new { getMessageById = 403, updateMessage = 403, deleteMessage = 403 });
        new { guest = opened.Status, outsider = openedByOutsider.Status }
            .Should().BeEquivalentTo(new { guest = 403, outsider = 403 });
    }

    /// <summary>
    /// Read is the ceiling for a Guest in an AI room: every level above it is refused for the user type, and Read
    /// still goes through, so the refusal is about the level and not about Guests being barred from AI rooms.
    /// </summary>
    [Theory]
    [InlineData(FileShare.Editing)]
    [InlineData(FileShare.ContentCreator)]
    [InlineData(FileShare.RoomManager)]
    public async Task ShareAiRoom_GuestAboveRead_Returns403(FileShare access)
    {
        var roomId = await CreateRoomAsync("Autotest Chat AI Room");
        var guest = await InviteMemberAsync(EmployeeType.Guest);

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await ShareRoomAsync(roomId, guest.Id, access));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain(GuestRoleRefused);

        await ShareRoomAsync(roomId, guest.Id, FileShare.Read);
    }

    private Task<AiOpenOrCreateResult> OpenOwnersThreadAsync(OwnerSeed seed, string text) =>
        _aiThreads.AiThreadsOpenOrCreateAsync(
            new AiThreadsOpenOrCreateRequest(
                threadId: seed.ThreadId,
                profile: seed.Profile,
                profileId: seed.Profile.Id,
                firstMessage: BuildUserMessage(text),
                entityId: seed.RoomId.ToString()),
            TestContext.Current.CancellationToken);
}

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
/// Helpers for the chat suites that stage a conversation in a location (an ordinary room, a folder, nothing at all)
/// and check which model a thread runs on. Derives from the assignments base for its profile and binding helpers:
/// the model of a new conversation comes from the portal-wide Chat/Default binding.
/// </summary>
/// <remarks>
/// What these suites rely on (common/ASC.AI.Chat/app): threads are one list per user whatever folder the chat is
/// opened in (threadsStorage.ts stores every thread under <c>entityId: null</c> and ignores the entityId of the
/// list, Bug 82719/82855), so a location is only checked for being accessible. The model is a property of the thread:
/// a send with a <c>profileId</c> rebinds it (appendUserMessage touches the thread), a send without one keeps it
/// (aiController.ts pre-fills the thread's own, Bug 82860).
/// </remarks>
public abstract class ChatPart2TestBase(AspireAppFixture fixture) : AssignmentsStorageTests.AssignmentsTestBase(fixture)
{
    protected const string ReplyOk = "Reply with the single word OK.";

    private int? _myDocumentsId;

    /// <summary>
    /// The owner's "My documents" id. Reading it also signs the Files client in as the owner, which every other
    /// Files call of these suites needs: none of them ever switches that client to a member.
    /// </summary>
    protected async Task<int> MyDocumentsIdAsync() => _myDocumentsId ??= await GetMyDocumentsFolderIdAsync();

    /// <summary>Creates an ordinary (custom) room as the owner — a location that is not an AI room.</summary>
    protected async Task<int> CreateCustomRoomAsync(string? title = null)
    {
        await MyDocumentsIdAsync();

        var room = (await RoomsApi.CreateRoomAsync(
            new CreateRoomRequestDto(title ?? $"room-{Guid.NewGuid():N}", roomType: RoomType.CustomRoom),
            TestContext.Current.CancellationToken)).Response;

        return room.Id;
    }

    /// <summary>Creates a folder as the owner inside a folder or a room.</summary>
    protected async Task<int> CreateFolderAsync(int parentId, string? title = null)
    {
        await MyDocumentsIdAsync();

        var folder = (await FoldersApi.CreateFolderAsync(
            parentId,
            new CreateFolderRequest(title ?? $"folder-{Guid.NewGuid():N}"),
            TestContext.Current.CancellationToken)).Response;

        return folder.Id;
    }

    /// <summary>Invites a portal member into a room as the owner (ContentCreator: legal for a custom room and any member type but Guest).</summary>
    protected async Task InviteToRoomAsync(int roomId, User member)
    {
        await MyDocumentsIdAsync();

        await RoomsApi.SetRoomSecurityAsync(
            roomId,
            new RoomInvitationRequest(
                invitations: [new RoomInvitation(id: member.Id, access: DocSpace.API.SDK.Model.FileShare.ContentCreator)],
                notify: false),
            TestContext.Current.CancellationToken);
    }

    /// <summary>Invites a member of the given type and signs the public AI client in as them.</summary>
    protected async Task<User> ActAsNewMemberAsync(EmployeeType employeeType)
    {
        var member = await InviteContact(employeeType, TestContext.Current.CancellationToken);
        await _aiChatClient.Authenticate(member);

        return member;
    }

    /// <summary>The ids of the threads the public list answers for a location (or for none).</summary>
    protected async Task<List<string>> ListThreadIdsAsync(string? entityId = null) =>
        (await ListThreadsAsync(entityId)).Select(t => t.ThreadId).ToList();

    protected Task<List<AiThread>> ListThreadsAsync(string? entityId = null) =>
        _aiThreads.AiThreadsListAsync(entityId, cancellationToken: TestContext.Current.CancellationToken);

    protected Task<AiThread> GetThreadAsync(string threadId) =>
        _aiThreads.AiThreadsGetByIdAsync(threadId, TestContext.Current.CancellationToken);

    /// <summary>The model a thread is stored with: <c>get-by-id</c>'s <c>profileId</c>, null when it has none.</summary>
    protected async Task<string?> ThreadProfileIdAsync(string threadId) => (await GetThreadAsync(threadId)).ProfileId;

    /// <summary>The model a thread carries in the list the picker is drawn from.</summary>
    protected async Task<string?> ListedProfileIdAsync(string threadId, string? entityId = null) =>
        (await ListThreadsAsync(entityId)).Single(t => t.ThreadId == threadId).ProfileId;

    protected Task<List<AiThreadMessageLike>> ReadMessagesAsync(string threadId) =>
        _aiThreads.AiThreadsReadMessagesAsync(threadId, cancellationToken: TestContext.Current.CancellationToken);

    protected static List<string?> UserTexts(IEnumerable<AiThreadMessageLike> messages) =>
        messages.Where(m => m.Role == AiThreadMessageLike.RoleEnum.User).Select(TextOf).ToList();

    protected static List<string?> AssistantTexts(IEnumerable<AiThreadMessageLike> messages) =>
        messages.Where(m => m.Role == AiThreadMessageLike.RoleEnum.Assistant).Select(TextOf).ToList();

    /// <summary>
    /// A chat round that really answered: HTTP 200, no error frame, no incomplete reply, and some text. A refused
    /// inference still ends in a stored assistant message (with an error status), so "an assistant message exists"
    /// is not enough.
    /// </summary>
    protected static void AssertHealthyReply(ChatRound round)
    {
        round.StatusCode.Should().Be(HttpStatusCode.OK, round.ToString());
        round.StreamError.Should().BeNull(round.ToString());
        round.IncompleteStatus.Should().BeNull(round.ToString());
        round.AssistantText.Should().NotBeNullOrEmpty(round.ToString());
    }

    /// <summary>
    /// Two profiles on the run-wide stub that a Chat binding accepts. The first one created on the portal also holds
    /// the Default slot.
    /// </summary>
    protected async Task<(AiProfile First, AiProfile Second)> TwoChatProfilesAsync()
    {
        var first = await CreateAssignableProfileAsync();
        var second = await CreateAssignableProfileAsync();

        return (first, second);
    }

    /// <summary>Polls a Files operation until it is finished (or gone from the queue); returns the last statuses seen.</summary>
    protected async Task<List<FileOperationDto>> WaitForOperationAsync(string operationId)
    {
        var deadline = DateTime.UtcNow.AddSeconds(60);

        while (true)
        {
            var statuses = (await OperationsApi.GetOperationStatusesAsync(
                id: operationId,
                cancellationToken: TestContext.Current.CancellationToken)).Response;

            if (statuses.Count == 0 || statuses.TrueForAll(s => s.Finished) || DateTime.UtcNow >= deadline)
            {
                return statuses;
            }

            await Task.Delay(200, TestContext.Current.CancellationToken);
        }
    }
}

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
/// Every route of the chat surface refuses an anonymous caller with 401. Each sits behind its own middleware, so a
/// 401 on create says nothing about append or clear — every one is checked, the per-message routes, the inference
/// routes and the two tool-call decisions included (for approve/deny anonymous is the only refusal there is). Each
/// call names the owner's real room, thread and message, and the owner's thread must be intact afterwards.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "AI/Chat")]
public class ChatPermissionAnonymousTests(AspireAppFixture fixture) : ChatPermissionTestBase(fixture)
{
    public static TheoryData<string> Routes => new()
    {
        "threads/create",
        "threads/list",
        "threads/get-by-id",
        "threads/read-messages",
        "threads/rename",
        "threads/delete",
        "threads/clear-messages",
        "threads/touch",
        "threads/append-user-message",
        "ai/send-with-stream",
        "threads/open-or-create",
        "threads/regenerate-title",
        "threads/get-message-by-id",
        "threads/update-message",
        "threads/delete-message",
        "ai/regenerate-stream",
        "ai/send",
        "ai/send-custom",
        "ai/send-with-stream-openai",
        "ai/approve-tool-call",
        "ai/deny-tool-call",
        "profiles/list"
    };

    [Theory]
    [MemberData(nameof(Routes))]
    public async Task Route_Anonymous_Returns401(string route)
    {
        var seed = await SeedOwnerThreadAsync();

        await _aiChatClient.Authenticate(null);
        var (status, error) = route == "ai/send-with-stream"
            ? await SendAnonymouslyAsync(seed)
            : await OutcomeOfAsync(Act(route, seed));

        await ExpectOwnerThreadIntactAsync(seed);
        status.Should().Be(401, error);
        error.Should().Contain("Unauthorized");
    }

    /// <summary>
    /// The SDK calls are fine on this path: the defects of the streaming and boolean-returning operations only bite
    /// when the request succeeds, and every call here is refused before any of that.
    /// </summary>
    private Func<Task> Act(string route, OwnerSeed seed)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var roomId = seed.RoomId.ToString();
        var message = BuildUserMessage("anonymous");

        return route switch
        {
            "threads/create" => () => CreateThreadAsync("Anonymous thread", seed.Profile.Id, roomId),
            "threads/list" => () => ListThreadsAsync(),
            "threads/get-by-id" => () => GetThreadAsync(seed.ThreadId),
            "threads/read-messages" => () => ReadAllAsync(seed.ThreadId),
            "threads/rename" => () => _aiThreads.AiThreadsRenameAsync(new AiThreadsRenameRequest(seed.ThreadId, "Hacked title"), cancellationToken),
            "threads/delete" => () => _aiThreads.AiThreadsDeleteAsync(seed.ThreadId, cancellationToken),
            "threads/clear-messages" => () => _aiThreads.AiThreadsClearMessagesAsync(seed.ThreadId, cancellationToken),
            "threads/touch" => () => _aiThreads.AiThreadsTouchAsync(new AiThreadsTouchRequest(seed.ThreadId), cancellationToken),
            "threads/append-user-message" => () => _aiThreads.AiThreadsAppendUserMessageAsync(
                new AiThreadsAppendUserMessageRequest(threadId: seed.ThreadId, message: message, profileId: seed.Profile.Id),
                cancellationToken),
            "threads/open-or-create" => () => _aiThreads.AiThreadsOpenOrCreateAsync(
                new AiThreadsOpenOrCreateRequest(
                    threadId: seed.ThreadId,
                    profile: seed.Profile,
                    profileId: seed.Profile.Id,
                    firstMessage: message,
                    entityId: roomId),
                cancellationToken),
            "threads/regenerate-title" => () => _aiThreads.AiThreadsRegenerateTitleAsync(
                new AiThreadsRegenerateTitleRequest(seed.ThreadId, seed.Profile),
                cancellationToken),
            "threads/get-message-by-id" => () => _aiThreads.AiThreadsGetMessageByIdAsync(seed.MessageId, cancellationToken),
            "threads/update-message" => () => _aiThreads.AiThreadsUpdateMessageAsync(
                new AiThreadsUpdateMessageRequest(seed.MessageId, BuildUserMessage("Hijacked by anonymous")),
                cancellationToken),
            "threads/delete-message" => () => _aiThreads.AiThreadsDeleteMessageAsync(seed.MessageId, cancellationToken),
            "ai/regenerate-stream" => () => _aiChat.AiRegenerateStreamAsync(
                new AiRegenerateStreamRequest(threadId: seed.ThreadId, entityId: roomId, profileId: seed.Profile.Id),
                cancellationToken),
            "ai/send" => () => _aiChat.AiSendAsync(
                new AiSendRequest(actionType: AiActionType.Chat, userMessage: message, entityId: roomId),
                cancellationToken),
            // The one route that needs neither a thread nor an entity: nothing about it is the portal's data, so the
            // auth check is all that stands in front of the model.
            "ai/send-custom" => () => _aiChat.AiSendCustomAsync(
                new AiSendCustomRequest(isStream: false, systemPrompt: "Answer with a single word.", userMessage: message),
                cancellationToken),
            "ai/send-with-stream-openai" => () => _aiChat.AiSendWithStreamOpenAIAsync(
                new AiSendStreamBody(threadId: seed.ThreadId, userMessage: message, entityId: roomId, profileId: seed.Profile.Id),
                cancellationToken),
            "ai/approve-tool-call" => () => _aiChat.AiApproveToolCallAsync(
                new AiApproveToolCallRequest(
                    result: "done",
                    threadId: seed.ThreadId,
                    messageId: seed.MessageId,
                    idx: 0,
                    entityId: roomId,
                    profileId: seed.Profile.Id),
                cancellationToken),
            "ai/deny-tool-call" => () => _aiChat.AiDenyToolCallAsync(
                new AiToolCallData(threadId: seed.ThreadId, messageId: seed.MessageId, idx: 0, entityId: roomId, profileId: seed.Profile.Id),
                cancellationToken),
            _ => () => _aiProfiles.AiProfilesListAsync(cancellationToken)
        };
    }

    private async Task<(int Status, string Error)> SendAnonymouslyAsync(OwnerSeed seed)
    {
        var round = await SendWithStreamAsync(seed.ThreadId, "Hello", seed.Profile.Id, seed.RoomId.ToString());

        return ((int)round.StatusCode, round.Raw);
    }
}

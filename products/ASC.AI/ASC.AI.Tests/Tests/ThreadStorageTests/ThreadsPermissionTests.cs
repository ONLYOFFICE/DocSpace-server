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
/// Nobody but the owner reaches the owner's thread: every ThreadsApi route that takes an existing thread or message,
/// attempted by someone who is not its owner, in every standing a person can have towards the AI room the thread was
/// started in — including the ones the product could be tempted to give a back door, a DocSpaceAdmin and a RoomAdmin
/// who manages that very room.
/// </summary>
/// <remarks>
/// Each case checks three things in this order: the owner's thread is exactly what it was (side effects first), the
/// actor's own space still works where they have one (positive control), and only then that every refusal is 403.
/// Anonymous 401 is in ChatPermissionAnonymousTests, AI-off 403 in ChatAiDisabledTests / ThreadsAiDisabledTests.
/// </remarks>
[Trait("Category", "Permissions")]
[Trait("Feature", "AI/Threads")]
public class ThreadsPermissionTests(AspireAppFixture fixture) : ThreadsPortTestBase(fixture)
{
    private const string IllegitimateText = "Written by someone who does not own this thread";

    /// <summary>The actor's type and the access they hold in the AI room (null: never invited).</summary>
    public static TheoryData<EmployeeType, FileShare?> Actors => new()
    {
        { EmployeeType.DocSpaceAdmin, null },
        { EmployeeType.RoomAdmin, null },
        { EmployeeType.User, null },
        { EmployeeType.Guest, null },
        { EmployeeType.DocSpaceAdmin, FileShare.ContentCreator },
        { EmployeeType.RoomAdmin, FileShare.RoomManager },
        { EmployeeType.User, FileShare.ContentCreator }
    };

    [Theory]
    [MemberData(nameof(Actors))]
    public async Task EveryRoute_OnTheOwnersThreadAndMessage_IsRefusedToAnyoneElse(EmployeeType type, FileShare? access)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var profile = await GetProfileAsync();
        var roomId = await CreateRoomAsync("Autotest Threads AI Room");
        var thread = await CreateThreadAsync("Owner thread", profile.Id, roomId.ToString());
        var threadId = thread.ThreadId;
        var messageId = await AppendUserMessageAsync(threadId, "A private note");
        await AppendUserMessageAsync(threadId, "A second private note");
        var before = await SnapshotAsync(threadId);

        var member = type == EmployeeType.Guest
            ? await InviteGuest(cancellationToken)
            : await InviteContact(type, cancellationToken);
        if (access is not null)
        {
            await RoomsApi.SetRoomSecurityAsync(
                roomId,
                new RoomInvitationRequest([new RoomInvitation(member.Id, access)], notify: false),
                cancellationToken);
        }

        await _aiChatClient.Authenticate(member);

        // Last: if delete ever succeeded the other refusals would be reported against a thread that no longer exists.
        var outcomes = new Dictionary<string, int>
        {
            ["get-by-id"] = await StatusOfAsync(() => GetThreadAsync(threadId)),
            ["read-messages"] = await StatusOfAsync(() => ReadAllAsync(threadId)),
            ["rename"] = await StatusOfAsync(() => _aiThreads.AiThreadsRenameAsync(new AiThreadsRenameRequest(threadId, "Hijacked"), cancellationToken)),
            ["touch"] = await StatusOfAsync(() => _aiThreads.AiThreadsTouchAsync(new AiThreadsTouchRequest(threadId), cancellationToken)),
            // The SDK call is fine here: its defect (the declared shape of the answer) only bites on a 200.
            ["append-user-message"] = await StatusOfAsync(() => _aiThreads.AiThreadsAppendUserMessageAsync(
                new AiThreadsAppendUserMessageRequest(threadId: threadId, message: BuildUserMessage(IllegitimateText), profileId: profile.Id),
                cancellationToken)),
            ["open-or-create"] = await StatusOfAsync(() => OpenAsync(threadId, IllegitimateText)),
            ["regenerate-title"] = await StatusOfAsync(() => _aiThreads.AiThreadsRegenerateTitleAsync(
                new AiThreadsRegenerateTitleRequest(threadId, profile),
                cancellationToken)),
            ["get-message-by-id"] = await StatusOfAsync(() => _aiThreads.AiThreadsGetMessageByIdAsync(messageId, cancellationToken)),
            ["update-message"] = await StatusOfAsync(() => _aiThreads.AiThreadsUpdateMessageAsync(
                new AiThreadsUpdateMessageRequest(messageId, BuildUserMessage(IllegitimateText)),
                cancellationToken)),
            ["delete-message"] = await StatusOfAsync(() => _aiThreads.AiThreadsDeleteMessageAsync(messageId, cancellationToken)),
            ["clear-messages"] = await StatusOfAsync(() => _aiThreads.AiThreadsClearMessagesAsync(threadId, cancellationToken)),
            ["delete"] = await StatusOfAsync(() => _aiThreads.AiThreadsDeleteAsync(threadId, cancellationToken))
        };

        string? ownThread = null;
        if (access is not null)
        {
            // Positive control: an invited member has a working space of their own, so the refusals above are about
            // ownership and not about a dead account.
            ownThread = (await CreateThreadAsync("Member's own thread", profile.Id, roomId.ToString())).ThreadId;
            await AppendUserMessageAsync(ownThread, "mine");
            (await ListIdsAsync()).Should().Equal(new[] { ownThread }, "the member's list holds only their own thread");
        }

        // Side effects before statuses: whatever the codes say, the owner's data is exactly what it was.
        await _aiChatClient.Authenticate(Owner);
        (await SnapshotAsync(threadId)).Should().Be(before);
        var ownerList = await ListIdsAsync();
        ownerList.Should().Contain(threadId);
        if (ownThread is not null)
        {
            ownerList.Should().NotContain(ownThread, "the member's thread is not the owner's");
        }

        outcomes.Where(o => o.Value != 403).Should().BeEmpty("every route on someone else's thread is refused with 403");
    }
}

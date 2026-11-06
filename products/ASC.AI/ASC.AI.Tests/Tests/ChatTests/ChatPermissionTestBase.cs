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
/// Shared plumbing of the chat access-control suites ported from chat.permission.spec.ts: the owner's AI room with a
/// thread and one message in it, members of any type invited at a chosen access, and the check that the owner's
/// thread is exactly what it was after someone else tried to reach it.
/// </summary>
/// <remarks>
/// The TypeScript suite seeded an "agent"; its routes are gone from the chat service, and the agent is a room of type
/// AiRoom — what <see cref="BaseTest.CreateRoomAsync"/> creates. The thread helpers (snapshots, raw requests, status
/// collection) come from the threads suite's base rather than being copied.
/// </remarks>
public abstract class ChatPermissionTestBase(AspireAppFixture fixture) : ThreadStorageTests.ThreadsPortTestBase(fixture)
{
    protected const string OwnerThreadTitle = "Owner thread";
    protected const string OwnerMessage = "A private note";

    /// <summary>The owner's room, thread and message as they stood before anyone else acted.</summary>
    protected sealed record OwnerSeed(int RoomId, AiProfile Profile, string ThreadId, string MessageId, decimal LastEditDate);

    /// <summary>
    /// The owner creates an AI room and, in it, a thread holding one message. All of it happens as the owner, before
    /// any member exists.
    /// </summary>
    protected async Task<OwnerSeed> SeedOwnerThreadAsync()
    {
        var profile = await GetProfileAsync();
        var roomId = await CreateRoomAsync("Autotest Chat AI Room");
        var thread = await CreateThreadAsync(OwnerThreadTitle, profile.Id, roomId.ToString());
        var messageId = await AppendUserMessageAsync(thread.ThreadId, OwnerMessage);
        var stored = await GetThreadAsync(thread.ThreadId);

        return new OwnerSeed(roomId, profile, thread.ThreadId, messageId, stored.LastEditDate);
    }

    /// <summary>A new member of the given type; a Guest goes through its own invitation route.</summary>
    protected async Task<User> InviteMemberAsync(EmployeeType type) =>
        type == EmployeeType.Guest
            ? await InviteGuest(TestContext.Current.CancellationToken)
            : await InviteContact(type, TestContext.Current.CancellationToken);

    /// <summary>Sets the member's access in the room as the owner; fails the test when the room refuses it.</summary>
    protected async Task ShareRoomAsync(int roomId, Guid userId, FileShare access)
    {
        await FilesHttpClient.Authenticate(Owner);
        await RoomsApi.SetRoomSecurityAsync(
            roomId,
            new RoomInvitationRequest([new RoomInvitation(userId, access)], notify: false),
            TestContext.Current.CancellationToken);
    }

    /// <summary>An ordinary (custom) room of the owner's, for the cases about a room that is not an AI room.</summary>
    protected async Task<int> CreateCustomRoomAsync(string title)
    {
        await FilesHttpClient.Authenticate(Owner);
        var room = await RoomsApi.CreateRoomAsync(
            new CreateRoomRequestDto(title, roomType: RoomType.CustomRoom),
            TestContext.Current.CancellationToken);

        return room.Response.Id;
    }

    /// <summary>
    /// Everything the owner's thread must still look like: listed, same title and lastEditDate, the one message with
    /// its text. Signs the chat client back in as the owner.
    /// </summary>
    protected async Task ExpectOwnerThreadIntactAsync(OwnerSeed seed)
    {
        await _aiChatClient.Authenticate(Owner);

        (await ListIdsAsync()).Should().Contain(seed.ThreadId);

        var thread = await GetThreadAsync(seed.ThreadId);
        thread.Title.Should().Be(OwnerThreadTitle);
        thread.LastEditDate.Should().Be(seed.LastEditDate);

        var messages = await ReadAllAsync(seed.ThreadId);
        messages.Should().ContainSingle();
        TextOf(messages[0]).Should().Be(OwnerMessage);
    }

    /// <summary>The assistant replies stored on a thread, read as the current caller.</summary>
    protected async Task<List<AiThreadMessageLike>> AssistantMessagesAsync(string threadId) =>
        (await ReadAllAsync(threadId)).Where(m => m.Role == AiThreadMessageLike.RoleEnum.Assistant).ToList();
}

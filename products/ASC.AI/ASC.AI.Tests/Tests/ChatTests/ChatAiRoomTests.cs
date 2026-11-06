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
/// An AI room (<c>roomType</c> 9) made through <c>POST /files/rooms</c>. It carries no model of its own: assignments
/// are one portal-wide map (the reads take an <c>entityId</c> only for compatibility), so a chat in it runs on the
/// portal's bindings or on the model the thread was opened with — never on anything of the room's.
/// </summary>
[Trait("Category", "Chat")]
[Trait("Feature", "AI/Chat")]
public class ChatAiRoomTests(AspireAppFixture fixture) : ChatPart3TestBase(fixture)
{
    [Fact]
    public async Task CreateRoom_AiRoom_HasNoModelOfItsOwnAndTheThreadKeepsItsModel()
    {
        var portalProvider = NewProvider();
        var portalProfile = await CreateProfileOnAsync(portalProvider);
        var threadProvider = NewProvider();
        var threadProfile = await CreateProfileOnAsync(threadProvider);

        var room = await CreateRoomOfTypeAsync(RoomType.AiRoom);
        var roomId = room.Id.ToString();

        room.RoomType.Should().Be(RoomType.AiRoom);
        (room.ChatSettings?.Prompt).Should().BeNullOrEmpty("nothing was carried over: no instructions");

        // The room's scope is the portal-wide map, and Chat resolves to the portal's default model there.
        (await GetAllAssignmentsAsync(roomId)).Should().BeEquivalentTo(await GetAllAssignmentsAsync());
        var resolved = await _aiAssignments.AiAssignmentsResolveForActionAsync(
            nameof(AiActionType.Chat),
            roomId,
            TestContext.Current.CancellationToken);
        resolved.ProfileId.Should().Be(portalProfile.Id, "the first profile of the portal is its default");

        // A thread opened on another model keeps it: the room has none to impose.
        var thread = await CreateThreadAsync(profileId: threadProfile.Id, entityId: roomId);
        var question = $"ai-room-{Guid.NewGuid():N}";
        threadProvider.Enqueue(StubModelProvider.Text("OK"));

        var round = await SendWithStreamAsync(thread.ThreadId, question, entityId: roomId);

        round.StreamError.Should().BeNull(round.ToString());
        round.AssistantText.Should().Be("OK", $"{round}\n{threadProvider.Describe()}");
        StreamedRequestsCarrying(threadProvider, question).Should().NotBeEmpty("the thread's own model answered");
        StreamedRequestsCarrying(portalProvider, question).Should().BeEmpty("not the portal default");

        var stored = await _aiThreads.AiThreadsGetByIdAsync(thread.ThreadId, TestContext.Current.CancellationToken);
        stored.ProfileId.Should().Be(threadProfile.Id, "no model of the room's overrode the thread's own");
    }
}

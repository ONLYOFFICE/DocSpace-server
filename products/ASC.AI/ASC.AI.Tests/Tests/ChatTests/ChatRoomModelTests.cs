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
/// The model a NEW conversation in an ordinary room starts on. A room has no binding of its own: <c>assign</c> takes
/// only <c>{actionType, profileId}</c>, the read of a room scope answers the portal-wide map, so a room chat resolves
/// through the portal-wide Chat/Default binding.
/// </summary>
[Trait("Category", "Chat")]
[Trait("Feature", "AI/Chat")]
public class ChatRoomModelTests(AspireAppFixture fixture) : ChatPart2TestBase(fixture)
{
    [Fact]
    public async Task Assign_WithEntityId_IsIgnoredAndTheBindingLandsPortalWide()
    {
        // Arrange
        var (first, other) = await TwoChatProfilesAsync();
        var roomId = await CreateCustomRoomAsync("Autotest Model Room");
        var unrelatedRoomId = await CreateCustomRoomAsync("Autotest Unrelated Room");

        var portalWide = await GetAllAssignmentsAsync();
        portalWide.Should().ContainKey(DefaultSlot).WhoseValue.Should().Be(first.Id);
        portalWide.Should().NotContainKey(nameof(AiActionType.Chat), "whatever Chat reads back afterwards is written by this call");

        // Act
        // Raw on purpose: AiAssignmentsAssignRequest has no entityId (the contract takes none), and the point is what
        // the route does with one a client sends anyway.
        using var assigned = await new RawApiClient(_aiChatClient).PutAsync(
            PublicAssignPath,
            new { actionType = nameof(AiActionType.Chat), profileId = other.Id, entityId = roomId.ToString() },
            TestContext.Current.CancellationToken);
        var assignedBody = await assigned.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        assigned.StatusCode.Should().Be(HttpStatusCode.OK, assignedBody);
        using (var assignedJson = JsonDocument.Parse(assignedBody))
        {
            assignedJson.RootElement.GetProperty("success").GetBoolean().Should().BeTrue(assignedBody);
        }

        // The room appears to have its own binding...
        (await GetAllAssignmentsAsync(roomId.ToString()))
            .Should().ContainKey(nameof(AiActionType.Chat)).WhoseValue.Should().Be(other.Id);

        // ...but it is the same one every other scope now has, including an unrelated room and the portal itself.
        (await GetAllAssignmentsAsync(unrelatedRoomId.ToString()))
            .Should().ContainKey(nameof(AiActionType.Chat)).WhoseValue.Should().Be(other.Id);

        var afterPortal = await GetAllAssignmentsAsync();
        afterPortal.Should().ContainKey(nameof(AiActionType.Chat)).WhoseValue.Should().Be(other.Id);
        afterPortal.Should().ContainKey(DefaultSlot).WhoseValue.Should().Be(first.Id);
    }

    [Fact]
    public async Task SendWithStream_RoomChatWithoutProfileId_IsAnsweredThroughThePortalWideBinding()
    {
        // Arrange
        var profile = await GetDefaultProfileAsync();
        var roomId = (await CreateCustomRoomAsync("Autotest Model Room")).ToString();

        // The room has no binding of its own to fall back to: resolution can only come from the portal-wide Default.
        var resolved = await _aiAssignments.AiAssignmentsResolveForActionAsync(
            nameof(AiActionType.Chat),
            roomId,
            TestContext.Current.CancellationToken);
        resolved.ProfileId.Should().Be(profile.Id);

        // A thread that names no model either: a stored one would win over the binding (aiController sendWithStream).
        var thread = await _aiThreads.AiThreadsCreateAsync(
            new AiThreadsCreateRequest(title: "Autotest entity thread", entityId: roomId),
            TestContext.Current.CancellationToken);
        (await ThreadProfileIdAsync(thread.ThreadId)).Should().BeNullOrEmpty("the thread starts with no model of its own");

        // Act
        var round = await SendWithStreamAsync(thread.ThreadId, ReplyOk, profileId: null, entityId: roomId);

        // Assert
        AssertHealthyReply(round);
        (await ThreadProfileIdAsync(thread.ThreadId)).Should().Be(profile.Id, "the round ran on the model the binding resolved");
        AssistantTexts(await ReadMessagesAsync(thread.ThreadId))
            .Should().ContainSingle("a send without profileId was answered").Which.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Naming a room the caller has no access to as the scope of the assignments read used to crash the access
    /// check (500); it is refused with 403. The same caller is answered without the scope.
    /// </summary>
    [Fact]
    [Trait("Bug", "82895")]
    public async Task GetAllAssignments_ScopeOfARoomTheCallerCannotOpen_Returns403()
    {
        // Arrange
        var (_, other) = await TwoChatProfilesAsync();
        var roomId = await CreateCustomRoomAsync("Autotest Private Room");

        // A binding that is not the portal default, so a room-specific answer would be distinguishable.
        await AssignAsync(AiActionType.Chat, other.Id);

        await ActAsNewMemberAsync(EmployeeType.User);

        // Act
        var scoped = await Assert.ThrowsAsync<ApiException>(async () => await GetAllAssignmentsAsync(roomId.ToString()));
        var own = await GetAllAssignmentsAsync();

        // Assert
        own.Should().ContainKey(nameof(AiActionType.Chat), "the caller's own portal-wide read is answered");
        scoped.ErrorCode.Should().Be(403, "a room the caller cannot open is refused");
    }
}

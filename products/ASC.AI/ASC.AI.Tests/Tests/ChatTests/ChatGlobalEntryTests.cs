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
/// The chat the header button opens: no agent, no room, no folder. A thread is created with no entity, answered,
/// and listed in the global panel — per user, and closed to a Guest.
/// </summary>
[Trait("Category", "Chat")]
[Trait("Feature", "AI/Chat")]
public class ChatGlobalEntryTests(AspireAppFixture fixture) : ChatPart2TestBase(fixture)
{
    private const string GlobalChatTitle = "Autotest global chat";

    [Fact]
    public async Task CreateAndSend_OwnerWithNoEntity_ThreadAnswersAndIsListed()
    {
        // Arrange
        var profile = await GetDefaultProfileAsync();

        // Act
        var thread = await CreateThreadAsync(GlobalChatTitle, profile.Id);
        var round = await SendWithStreamAsync(thread.ThreadId, ReplyOk, profile.Id);

        // Assert
        var read = await GetThreadAsync(thread.ThreadId);
        read.ThreadId.Should().Be(thread.ThreadId);
        read.Title.Should().Be(GlobalChatTitle);
        read.ProfileId.Should().Be(profile.Id, "the model the composer picked is stored on a thread without a scope too");

        AssertHealthyReply(round);
        AssistantTexts(await ReadMessagesAsync(thread.ThreadId)).Should().ContainSingle().Which.Should().NotBeNullOrEmpty();

        (await ListThreadIdsAsync()).Should().Contain(thread.ThreadId, "the panel the button opens lists what it just wrote");
    }

    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    public async Task CreateAndSend_MemberWithNoEntity_ThreadAnswersAndIsListed(EmployeeType employeeType)
    {
        // Arrange
        var profile = await GetDefaultProfileAsync();
        await ActAsNewMemberAsync(employeeType);

        // Act
        var thread = await CreateThreadAsync(GlobalChatTitle, profile.Id);
        var round = await SendWithStreamAsync(thread.ThreadId, ReplyOk, profile.Id);

        // Assert
        thread.ThreadId.Should().NotBeNullOrEmpty($"{employeeType} starts a thread with no entity");
        AssertHealthyReply(round);
        (await ListThreadIdsAsync()).Should().Contain(thread.ThreadId);
    }

    [Fact]
    public async Task ListAndRead_AnotherUsersGlobalChat_IsInvisible()
    {
        // The entity-less list is one bucket across locations; the sharing stops at the user boundary.

        // Arrange
        var profile = await GetDefaultProfileAsync();
        var ownerThread = await CreateThreadAsync("Autotest owner global chat", profile.Id);

        await ActAsNewMemberAsync(EmployeeType.RoomAdmin);

        // Act
        var listed = await ListThreadIdsAsync();
        var getById = await Assert.ThrowsAsync<ApiException>(async () => await GetThreadAsync(ownerThread.ThreadId));
        var readMessages = await Assert.ThrowsAsync<ApiException>(async () => await ReadMessagesAsync(ownerThread.ThreadId));

        // Assert
        listed.Should().NotContain(ownerThread.ThreadId);
        getById.ErrorCode.Should().Be(403);
        readMessages.ErrorCode.Should().Be(403);

        // Positive control: the member's own global chat does land in that same list, so the absence above is
        // isolation and not a read that returns nothing.
        var memberThread = await CreateThreadAsync("Autotest member global chat", profile.Id);
        (await ListThreadIdsAsync()).Should().Equal(memberThread.ThreadId);
    }

    [Fact]
    public async Task CreateSendList_Guest_IsRefused()
    {
        // The one user type the global entry point is closed to: the thread storage admits DocSpaceAdmin, RoomAdmin
        // and User only (ThreadStorageService._allowedTypes).

        // Arrange
        var profile = await GetDefaultProfileAsync();
        var ownerThread = await CreateThreadAsync("Autotest owner global chat", profile.Id);

        var guest = await InviteGuest(TestContext.Current.CancellationToken);
        await _aiChatClient.Authenticate(guest);

        // Act
        var created = await Assert.ThrowsAsync<ApiException>(async () => await CreateThreadAsync(GlobalChatTitle, profile.Id));
        var listed = await Assert.ThrowsAsync<ApiException>(async () => await ListThreadsAsync());
        var sent = await SendWithStreamAsync(ownerThread.ThreadId, ReplyOk, profile.Id);

        // Assert
        created.ErrorCode.Should().Be(403, "a Guest starting a global chat");
        listed.ErrorCode.Should().Be(403, "a Guest listing global chats");
        sent.StatusCode.Should().Be(HttpStatusCode.Forbidden, $"a Guest sending into a global chat: {sent}");
    }

    /// <summary>
    /// A chat started from the portal interface, with no entity at all, is listed when the same user opens the chat
    /// panel in an unrelated room and in a folder — by design (Bug 82855: outside an AI project there is one shared
    /// list per user).
    /// </summary>
    [Fact]
    [Trait("Bug", "82855")]
    public async Task List_GlobalChat_IsListedInsideRoomsAndFolders()
    {
        // Arrange
        var profile = await GetDefaultProfileAsync();
        var roomId = await CreateCustomRoomAsync("Autotest Global Chat Room");
        var folderId = await MyDocumentsIdAsync();

        var globalThread = await CreateThreadAsync(GlobalChatTitle, profile.Id);

        // Act
        var inRoom = await ListThreadIdsAsync(roomId.ToString());
        var inMyDocuments = await ListThreadIdsAsync(folderId.ToString());

        // Assert
        inRoom.Should().Contain(globalThread.ThreadId, "the global chat's thread is listed for a room");
        inMyDocuments.Should().Contain(globalThread.ThreadId, "the global chat's thread is listed for My Documents");
    }
}

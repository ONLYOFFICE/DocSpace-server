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
/// Starting a thread in a place the caller cannot open: an AI room they were never invited into, an ordinary room,
/// another user's folder. The refusal is a 403 (it used to be a 500 for every role but DocSpaceAdmin, Bug 82715) and
/// leaves no thread behind. Read access does not close an ordinary room: a Read-level member chats there.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "AI/Chat")]
public class ChatPermissionNonMemberTests(AspireAppFixture fixture) : ChatPermissionTestBase(fixture)
{
    // Not ported: "a DocSpace admin not in the agent cannot start a thread". The agent and its membership check are
    // gone (ecf76402b3); an entityId now only has to be a folder the caller can read, and a DocSpace admin reads every
    // room that is not private (FileSecurity's administrator bypass), so the admin is let in by design. The same rule
    // for an AI project (canChat = inRoom) is what a replacement test would have to check.

    // No Guest row: a Guest is refused by user type wherever they are, invited or not, with or without a room in scope
    // (ChatPermissionGuestTests), so a Guest outsider is answered by that gate and never reaches the membership check
    // this test is about.
    [Theory]
    [Trait("Bug", "82715")]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    public async Task Create_NonMemberOfTheAiRoom_Returns403NotA500(EmployeeType type)
    {
        await CreateInAnUninvitedRoomAndExpect403Async(type);
    }

    [Fact]
    [Trait("Bug", "82715")]
    public async Task Create_UserNotInAnOrdinaryRoom_Returns403NotA500()
    {
        var profile = await GetProfileAsync();
        var roomId = await CreateCustomRoomAsync("Autotest Private Room");
        var member = await InviteMemberAsync(EmployeeType.User);

        await _aiChatClient.Authenticate(member);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await CreateThreadAsync("Outsider room thread", profile.Id, roomId.ToString()));

        await ExpectNoThreadWasCreatedAsync(member, roomId, profile);
        exception.ErrorCode.Should().Be(403, "refusing an outsider is a 403, not an Internal Server Error");
    }

    [Fact]
    public async Task Create_ReadLevelMemberOfAnOrdinaryRoom_StartsAThreadAndGetsAnAnswer()
    {
        var profile = await GetProfileAsync();
        var roomId = await CreateCustomRoomAsync("Autotest Read Only Room");
        var member = await InviteMemberAsync(EmployeeType.User);
        await ShareRoomAsync(roomId, member.Id, FileShare.Read);

        await _aiChatClient.Authenticate(member);

        // Read closes an AI room's thread surface to a Guest by user type (ChatPermissionGuestTests); an ordinary
        // room is not that, and the chat opened next to it belongs to the user, so Read is enough here.
        var thread = await CreateThreadAsync("Read-only member room thread", profile.Id, roomId.ToString());
        (await ListIdsAsync()).Should().Contain(thread.ThreadId);

        // And it is a usable chat, not just a record: the member gets an actual answer.
        var round = await SendWithStreamAsync(thread.ThreadId, "Reply with the single word OK.", profile.Id, roomId.ToString());

        round.StatusCode.Should().Be(HttpStatusCode.OK, round.ToString());
        round.StreamError.Should().BeNull(round.ToString());
        round.AssistantText.Should().NotBeNullOrEmpty(round.ToString());
        (await AssistantMessagesAsync(thread.ThreadId)).Should().ContainSingle(round.ToString());
    }

    [Fact]
    [Trait("Bug", "82715")]
    public async Task Create_AnotherUsersFolder_Returns403_WhileTheCallersOwnFolderWorks()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var profile = await GetProfileAsync();
        var ownerDocuments = await GetMyDocumentsFolderIdAsync();
        var folder = await FoldersApi.CreateFolderAsync(ownerDocuments, new CreateFolderRequest("Autotest Owner Private Folder"), cancellationToken);
        var member = await InviteMemberAsync(EmployeeType.User);

        // A folder in someone else's Documents is not a place this user can chat in, and the id is guessable.
        await _aiChatClient.Authenticate(member);
        var refused = await OutcomeOfAsync(() => CreateThreadAsync("Outsider folder thread", profile.Id, folder.Response.Id.ToString()));

        // Positive control: the very same caller can open a thread on a folder that IS theirs, so the refusal above
        // is about this folder's owner and not about the route being closed to them.
        await FilesHttpClient.Authenticate(member);
        var memberDocuments = (await FoldersApi.GetMyFolderAsync(cancellationToken: cancellationToken)).Response.Current.Id;
        var control = await CreateThreadAsync("Control thread", profile.Id, memberDocuments.ToString());

        IsUuid(control.ThreadId).Should().BeTrue("the caller's own Documents is a place to chat");
        (await ListIdsAsync()).Should().Equal(new[] { control.ThreadId }, "the refused call left no thread");
        refused.Status.Should().Be(403, $"a folder the caller cannot open is a 403, not a crash: {refused.Error}");
    }

    private async Task CreateInAnUninvitedRoomAndExpect403Async(EmployeeType type)
    {
        var profile = await GetProfileAsync();
        var roomId = await CreateRoomAsync("Autotest Chat AI Room");
        var member = await InviteMemberAsync(type);

        await _aiChatClient.Authenticate(member);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await CreateThreadAsync("Uninvited thread", profile.Id, roomId.ToString()));

        await ExpectNoThreadWasCreatedAsync(member, roomId, profile);
        exception.ErrorCode.Should().Be(403, "refusing an outsider is a 403, not an Internal Server Error");
    }

    /// <summary>
    /// Proves a refused create really created nothing: the caller is invited afterwards, a thread they then create
    /// legitimately is the positive control, and their list holds that one and nothing else.
    /// </summary>
    private async Task ExpectNoThreadWasCreatedAsync(User member, int roomId, AiProfile profile)
    {
        await ShareRoomAsync(roomId, member.Id, FileShare.ContentCreator);
        await _aiChatClient.Authenticate(member);

        (await ListThreadsAsync()).Should().BeEmpty("the refused call left no thread");

        var control = await CreateThreadAsync("Control thread", profile.Id, roomId.ToString());

        (await ListIdsAsync()).Should().Equal(new[] { control.ThreadId });
    }
}

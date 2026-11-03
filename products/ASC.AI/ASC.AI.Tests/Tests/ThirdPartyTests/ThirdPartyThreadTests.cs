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

namespace ASC.AI.Tests.Tests.ThirdPartyTests;

/// <summary>
/// AI threads scoped to a third-party room (thirdparty.spec.ts, "AI thread scoping"). A third-party room's own id is its
/// provider folder id (<c>sbox-N</c>), an opaque string to every AI surface. Needs a Nextcloud account; see
/// <see cref="ThirdPartyTestBase"/>.
/// </summary>
[Trait("Category", "ThirdParty")]
[Trait("Requires", "Nextcloud")]
[Trait("Feature", "AI/ThirdParty")]
public class ThirdPartyThreadTests(AspireAppFixture fixture) : ThirdPartyTestBase(fixture)
{
    [Fact]
    public async Task Thread_ScopedToAThirdPartyRoom_IsStoredAnswersAndIsListed()
    {
        var roomId = await CreateNextcloudRoomAsync("TP Thread Room");
        var profile = await GetDefaultProfileAsync();
        var thread = await CreateThreadAsync("Autotest TP thread", profile.Id, roomId);

        var round = await SendWithStreamAsync(thread.ThreadId, "Reply with the single word: acknowledged", profile.Id, roomId);

        round.StatusCode.Should().Be(HttpStatusCode.OK, round.ToString());
        round.StreamError.Should().BeNull(round.ToString());
        round.AssistantText.Should().NotBeNullOrEmpty("the round was answered");
        (await ListThreadsForAsync(roomId)).Select(t => t.ThreadId).Should().Contain(thread.ThreadId);
    }

    /// <summary>
    /// A thread started in a third-party room is listed for an unrelated internal room and for no entity at all: outside
    /// an AI project there is one thread list per user (Bug 82855, confirmed by design), whether or not the id resolves
    /// to internal storage.
    /// </summary>
    [Fact]
    [Trait("Bug", "82855")]
    public async Task Thread_ScopedToAThirdPartyRoom_FallsIntoTheSameSharedBucketAsAnInternalRoom()
    {
        var roomId = await CreateNextcloudRoomAsync("TP Bucket Room");
        var internalRoomId = await CreateCustomRoomAsync("Internal Sibling Room");
        var profile = await GetDefaultProfileAsync();
        var thread = await CreateThreadAsync("Autotest bucket thread", profile.Id, roomId);

        var fromInternalRoom = await ListThreadsForAsync(internalRoomId.ToString());
        var fromNoEntity = await ListThreadsForAsync(null);

        fromInternalRoom.Select(t => t.ThreadId).Should().Contain(thread.ThreadId, "an unrelated internal room's list carries the third-party room's thread");
        fromNoEntity.Select(t => t.ThreadId).Should().Contain(thread.ThreadId, "so does the no-entity list");
    }

    /// <summary>
    /// The TypeScript original expects the list to refuse a non-member with 403. Threads are one list per user whatever
    /// <c>entityId</c> names (the test above: an unrelated room lists the same threads), so the list cannot refuse by room;
    /// what a non-member must not get is the owner's thread.
    /// </summary>
    [Fact]
    public async Task ListThreads_NonMemberOfAThirdPartyRoom_SeesNoneOfItsThreads()
    {
        var roomId = await CreateNextcloudRoomAsync("TP NonMember Room");
        var profile = await GetDefaultProfileAsync();
        var ownerThread = await CreateThreadAsync("Autotest non-member thread", profile.Id, roomId);

        var user = await InviteContact(EmployeeType.User, TestContext.Current.CancellationToken);

        // The room read itself is refused for a non-member...
        await FilesHttpClient.Authenticate(user);
        var roomRead = await Assert.ThrowsAsync<ApiException>(
            async () => await RoomsApi.GetRoomInfoAsync(roomId, TestContext.Current.CancellationToken));
        roomRead.ErrorCode.Should().Be(403, "reading the room as a non-member");

        // ...and the threads listed for it are the caller's own, none of the owner's.
        await _aiChatClient.Authenticate(user);
        var listed = await ListThreadsForAsync(roomId);
        listed.Select(t => t.ThreadId).Should().NotContain(ownerThread.ThreadId, "a non-member is never handed the owner's thread");
    }
}

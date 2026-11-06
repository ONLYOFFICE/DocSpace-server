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
/// Thread management beyond the single-route cases of <c>ThreadStorageTests</c>: touch reorders the list and moves only
/// the touched thread, and every member type can rename, clear and delete a thread of their own.
/// </summary>
[Trait("Category", "Chat")]
[Trait("Feature", "AI/Threads")]
public class ChatThreadManagementTests(AspireAppFixture fixture) : ChatPart1TestBase(fixture)
{
    [Fact]
    public async Task Touch_OlderThread_MovesToTheTopAndOnlyItMoves()
    {
        // Arrange — lastEditDate has second granularity, hence the waits between calls that must land apart.
        var profile = await GetDefaultProfileAsync();
        var older = await CreateThreadAsync("Older thread", profile.Id);
        await Task.Delay(1100, TestContext.Current.CancellationToken);
        var newer = await CreateThreadAsync("Newer thread", profile.Id);

        var before = await ListThreadsAsync();
        before.Select(t => t.ThreadId).Should().Equal(newer.ThreadId, older.ThreadId);
        var olderDateBefore = before.Single(t => t.ThreadId == older.ThreadId).LastEditDate;
        var newerDateBefore = before.Single(t => t.ThreadId == newer.ThreadId).LastEditDate;

        await Task.Delay(1100, TestContext.Current.CancellationToken);

        // Act
        var result = await _aiThreads.AiThreadsTouchAsync(new AiThreadsTouchRequest(older.ThreadId), TestContext.Current.CancellationToken);

        // Assert
        result.Success.Should().BeTrue();

        var after = await ListThreadsAsync();
        after.Select(t => t.ThreadId).Should().Equal(older.ThreadId, newer.ThreadId);
        after.Single(t => t.ThreadId == older.ThreadId).LastEditDate.Should().BeGreaterThan(olderDateBefore);
        after.Single(t => t.ThreadId == newer.ThreadId).LastEditDate.Should().Be(newerDateBefore, "only the touched thread moves");
    }

    /// <summary>
    /// A backend that treated threads as the portal owner's property would pass the isolation matrix (a member is
    /// refused on someone else's thread) while leaving every other member's chat menu dead, so the positive half is
    /// pinned here per member type.
    /// </summary>
    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    public async Task RenameClearDelete_Member_ManagesTheirOwnThread(EmployeeType employeeType)
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var profile = await GetDefaultProfileAsync();
        var member = await InviteContact(employeeType, cancellationToken);
        await _aiChatClient.Authenticate(member);

        var thread = await CreateThreadAsync("Member thread", profile.Id);
        // The control for every read below: whatever happens to the thread under test, this one stays.
        var sibling = await CreateThreadAsync("Member sibling thread", profile.Id);

        // Act + Assert: renames it
        var renamed = await _aiThreads.AiThreadsRenameAsync(new AiThreadsRenameRequest(thread.ThreadId, "Renamed by the member"), cancellationToken);

        renamed.Success.Should().BeTrue();
        (await GetThreadAsync(thread.ThreadId)).Title.Should().Be("Renamed by the member");
        (await GetThreadAsync(sibling.ThreadId)).Title.Should().Be("Member sibling thread", "the rename hit one thread, not the member's whole list");

        // Act + Assert: clears its history and keeps the thread
        await AppendUserMessageAsync(thread.ThreadId, "A message of my own");
        (await ReadMessagesAsync(thread.ThreadId)).Should().ContainSingle();

        var cleared = await _aiThreads.AiThreadsClearMessagesAsync(thread.ThreadId, cancellationToken);

        cleared.Success.Should().BeTrue();
        (await ReadMessagesAsync(thread.ThreadId)).Should().BeEmpty();
        (await GetThreadAsync(thread.ThreadId)).Title.Should().Be("Renamed by the member", "the thread survives, under the member's name");

        // Act + Assert: deletes it
        var deleted = await _aiThreads.AiThreadsDeleteAsync(thread.ThreadId, cancellationToken);

        deleted.Success.Should().BeTrue();
        (await ListThreadIdsAsync()).Should().Equal(new[] { sibling.ThreadId }, "the deleted thread left the list and the sibling is still read");
    }
}

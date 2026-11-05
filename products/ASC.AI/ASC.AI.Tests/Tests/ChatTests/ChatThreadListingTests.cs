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
/// The listing contract the chat sidebar is drawn from (<c>GET threads/list</c>): newest activity first, with a title,
/// the selected profile and a timestamp per thread; a deleted thread leaves it; count, cursor and query are honoured.
/// </summary>
[Trait("Category", "Chat")]
[Trait("Feature", "AI/Threads")]
public class ChatThreadListingTests(AspireAppFixture fixture) : ChatPart1TestBase(fixture)
{
    [Fact]
    public async Task List_Threads_ComeBackNewestActivityFirstWithWhatTheSidebarNeeds()
    {
        // Arrange — lastEditDate has second granularity, so the creations are spaced to give the order a meaning.
        var profile = await GetDefaultProfileAsync();
        var alpha = await CreateThreadAsync("Alpha thread", profile.Id);
        await Task.Delay(1100, TestContext.Current.CancellationToken);
        var beta = await CreateThreadAsync("Beta thread", profile.Id);
        await Task.Delay(1100, TestContext.Current.CancellationToken);
        var gamma = await CreateThreadAsync("Gamma thread", profile.Id);

        // Act
        var threads = await ListThreadsAsync();

        // Assert
        threads.Select(t => t.ThreadId).Should().Equal(gamma.ThreadId, beta.ThreadId, alpha.ThreadId);

        foreach (var thread in threads)
        {
            thread.Title.Should().NotBeNullOrWhiteSpace();
            thread.ProfileId.Should().Be(profile.Id);
            thread.LastEditDate.Should().BePositive();
        }

        threads.Select(t => t.LastEditDate).Should().BeInDescendingOrder("the order is built from lastEditDate");
    }

    [Fact]
    public async Task List_DeletedThread_LeavesTheList()
    {
        // Arrange
        var profile = await GetDefaultProfileAsync();
        var doomed = await CreateThreadAsync("Doomed thread", profile.Id);
        var keeper = await CreateThreadAsync("Keeper thread", profile.Id);

        // Act
        var result = await _aiThreads.AiThreadsDeleteAsync(doomed.ThreadId, TestContext.Current.CancellationToken);

        // Assert
        result.Success.Should().BeTrue();
        (await ListThreadIdsAsync()).Should().Equal(keeper.ThreadId);
    }

    [Fact]
    [Trait("Bug", "82825")]
    public async Task List_CountCursorAndQuery_AreHonored()
    {
        // Arrange
        var profile = await GetDefaultProfileAsync();
        foreach (var title in new[] { "Alpha thread", "Beta thread", "Gamma thread" })
        {
            await CreateThreadAsync(title, profile.Id);
        }

        // Act + Assert
        (await ListThreadsAsync()).Should().HaveCount(3);
        (await ListThreadsAsync(count: 1)).Should().ContainSingle("count=1 returns a single thread");

        // "1" is not a cursor the list understands; it is ignored rather than refused, and count still applies.
        (await ListThreadsAsync(count: 2, cursor: "1")).Should().HaveCount(2, "count=2 returns two threads");

        (await ListThreadsAsync(query: "Alpha")).Select(t => t.Title).Should().Equal(new[] { "Alpha thread" }, "a query matching one title returns only that thread");
        (await ListThreadsAsync(query: "nothing-matches-this")).Should().BeEmpty("a query matching no title returns no threads");
    }
}

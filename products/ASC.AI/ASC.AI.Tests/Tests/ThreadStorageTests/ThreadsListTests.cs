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
/// <c>GET /api/2.0/ai/threads/list</c> beyond <see cref="ThreadReadTests"/> and <see cref="ThreadSearchTests"/>:
/// the list mirrors get-by-id and follows rename, clear and delete; count slices exactly and refuses 0 or below;
/// the cursor keyed by <c>threadId</c>; the search cases those classes leave out.
/// </summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "AI/Threads")]
public class ThreadsListTests(AspireAppFixture fixture) : ThreadsPortTestBase(fixture)
{
    [Fact]
    public async Task List_MirrorsGetById_AndFollowsRenameClearAndDelete()
    {
        (await ListThreadsAsync()).Should().BeEmpty("a portal with no threads");

        var a = await CreateOwnThreadAsync("Thread A");
        var b = await CreateOwnThreadAsync("Thread B");
        await AppendUserMessageAsync(b.ThreadId, "history");

        var listed = await ListThreadsAsync();
        listed.Should().HaveCount(2);
        foreach (var item in listed)
        {
            item.ToJson().Should().Be((await GetThreadAsync(item.ThreadId)).ToJson(), "every item equals what get-by-id says");
        }

        await _aiThreads.AiThreadsRenameAsync(new AiThreadsRenameRequest(a.ThreadId, "Thread A renamed"), TestContext.Current.CancellationToken);
        (await ListThreadsAsync()).ToDictionary(t => t.ThreadId, t => t.Title).Should().BeEquivalentTo(
            new Dictionary<string, string> { [a.ThreadId] = "Thread A renamed", [b.ThreadId] = "Thread B" });

        await _aiThreads.AiThreadsClearMessagesAsync(b.ThreadId, TestContext.Current.CancellationToken);
        await ExpectOnlyThreadsAsync([a.ThreadId, b.ThreadId], "a cleared thread stays listed");

        await _aiThreads.AiThreadsDeleteAsync(a.ThreadId, TestContext.Current.CancellationToken);
        await ExpectOnlyThreadsAsync([b.ThreadId], "only the deleted thread went");
    }

    [Fact]
    public async Task List_Count_IsExactlyTheNewestSlice()
    {
        foreach (var title in new[] { "One", "Two", "Three", "Four" })
        {
            await CreateOwnThreadAsync(title);
        }

        var everything = (await ListThreadsAsync()).Select(t => t.ToJson()).ToList();
        everything.Should().HaveCount(4);

        (await ListThreadsAsync(1)).Select(t => t.ToJson()).Should().Equal(everything.Take(1), "count=1 is the newest one");
        (await ListThreadsAsync(3)).Select(t => t.ToJson()).Should().Equal(everything.Take(3), "count=3 is the three newest");
        (await ListThreadsAsync(4)).Select(t => t.ToJson()).Should().Equal(everything, "count equal to the total");
        (await ListThreadsAsync(100)).Select(t => t.ToJson()).Should().Equal(everything, "count above the total");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task List_CountZeroOrBelow_Returns400(int count)
    {
        await CreateOwnThreadAsync("One");

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await ListThreadsAsync(count));

        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task List_CursorKeyedByThreadId_VisitsEveryThreadOnce()
    {
        // Created back to back on purpose: several land in the same second, which is exactly where a keyset cursor
        // without a tie-breaker would repeat or skip an item. The cursor names the thread as `threadId` here — the
        // other spelling parseThreadsCursor accepts (ThreadReadTests walks with `id`).
        for (var i = 0; i < 5; i++)
        {
            await CreateOwnThreadAsync($"Paged {i}");
        }

        var everything = await ListIdsAsync();
        everything.Should().HaveCount(5);

        var seen = new List<string>();
        string? cursor = null;
        for (var page = 0; page < 6; page++)
        {
            var items = await _aiThreads.AiThreadsListAsync(
                count: 2,
                cursor: cursor,
                cancellationToken: TestContext.Current.CancellationToken);
            if (items.Count == 0)
            {
                break;
            }

            seen.AddRange(items.Select(t => t.ThreadId));
            var last = items[^1];
            cursor = JsonSerializer.Serialize(new { lastEditDate = (long)last.LastEditDate, threadId = last.ThreadId });
        }

        seen.Should().Equal(everything, "the pages concatenate to the unpaged list");
    }

    [Fact]
    public async Task List_Query_IsACaseInsensitiveLiteralTitleMatch()
    {
        var report = await CreateOwnThreadAsync("Quarterly Report");
        var reportRu = await CreateOwnThreadAsync("Отчёт за квартал");
        var percent = await CreateOwnThreadAsync("100% done");
        var underscore = await CreateOwnThreadAsync("snake_case");
        var plain = await CreateOwnThreadAsync("Something else");

        async Task<List<string>> IdsAsync(string query) => (await ListThreadsAsync(query: query)).Select(t => t.ThreadId).ToList();

        (await IdsAsync("Quarterly Report")).Should().Equal(new[] { report.ThreadId }, "the exact title");
        (await IdsAsync("ОТЧЁТ")).Should().Equal(new[] { reportRu.ThreadId }, "Cyrillic in another case");
        (await IdsAsync("_")).Should().Equal(new[] { underscore.ThreadId }, "_ is not a wildcard");
        (await IdsAsync("e")).Should().BeEquivalentTo(
            new[] { report.ThreadId, percent.ThreadId, underscore.ThreadId, plain.ThreadId },
            "a letter many titles share");
        (await ListThreadsAsync(1, query: "e")).Should().ContainSingle("query and count together");
    }
}

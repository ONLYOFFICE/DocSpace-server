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

[Trait("Category", "CRUD")]
[Trait("Feature", "AI/Threads")]
public class ThreadReadTests(AspireAppFixture fixture) : ThreadTestBase(fixture)
{
    [Fact]
    public async Task ReadById_Existing_ReturnsThread()
    {
        var created = await CreateOwnThreadAsync("my-thread");

        var stored = await GetThreadAsync(created.ThreadId);

        stored.ThreadId.Should().Be(created.ThreadId);
        stored.Title.Should().Be("my-thread");
    }

    [Fact]
    public async Task ReadById_NonExisting_Returns404()
    {
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await GetThreadAsync(Guid.NewGuid().ToString()));

        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task ReadAll_Owner_ReturnsAllCreated()
    {
        var first = await CreateOwnThreadAsync("first");
        var second = await CreateOwnThreadAsync("second");

        var all = await ListThreadsAsync();

        all.Should().Contain(t => t.ThreadId == first.ThreadId);
        all.Should().Contain(t => t.ThreadId == second.ThreadId);
    }

    [Fact]
    public async Task ReadAll_Empty_ReturnsEmpty()
    {
        var all = await ListThreadsAsync();

        all.Should().BeEmpty();
    }

    // Internal contract: entityId scoping of the thread list has no public route (the chat service stores every
    // thread under entityId null and ignores the entityId of the list, by design - Bug 82719).
    [Fact]
    public async Task ReadAll_WithEntityId_ReturnsOnlyScopedThreads()
    {
        var roomId = await CreateRoomAsync();
        var globalThread = await CreateThreadInternalAsync("global");
        var scopedThread = await CreateThreadInternalAsync("scoped", entityId: roomId.ToString());

        var scoped = await ReadAllThreadsInternalAsync(roomId.ToString());

        scoped.Should().ContainSingle(t => t.Id == scopedThread.Id);
        scoped.Should().NotContain(t => t.Id == globalThread.Id);
    }

    // Internal contract: the required count of the storage page has no public route (the chat service always sends
    // one; a public list without count walks every page instead - see ReadAll_WithoutCount_ReturnsEveryThread).
    [Fact]
    public async Task ReadAll_WithoutCount_Returns400()
    {
        using var response = await _ai.GetAsync(ThreadsPath, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReadAll_WithoutCount_ReturnsEveryThread()
    {
        var created = new List<string>();
        for (var i = 0; i < 3; i++)
        {
            created.Add((await CreateOwnThreadAsync($"thread-{i}")).ThreadId);
        }

        var all = await ListThreadsAsync();

        all.Select(t => t.ThreadId).Should().BeEquivalentTo(created);
    }

    [Fact]
    public async Task ReadAll_WithCount_ReturnsBoundedNewestFirstPage()
    {
        var created = new List<string>();
        foreach (var title in new[] { "first", "second", "third" })
        {
            created.Add((await CreateOwnThreadAsync(title)).ThreadId);
        }

        var page = await ListThreadsAsync(2);

        page.Should().HaveCount(2);
        page.Select(t => t.LastEditDate).Should().BeInDescendingOrder();

        var rest = await ListThreadsAsync(2, after: page[^1]);

        rest.Should().ContainSingle();
        page.Concat(rest).Select(t => t.ThreadId).Should().BeEquivalentTo(created);
    }

    [Fact]
    public async Task ReadAll_AfterLastPage_ReturnsEmpty()
    {
        await CreateOwnThreadAsync("first");
        await CreateOwnThreadAsync("second");

        var page = await ListThreadsAsync(2);

        page.Should().HaveCount(2);
        (await ListThreadsAsync(2, after: page[^1])).Should().BeEmpty();
    }

    [Fact]
    public async Task ReadAll_PagingByOne_WalksEveryThreadWithoutGaps()
    {
        var created = new List<string>();
        for (var i = 0; i < 5; i++)
        {
            var thread = await CreateOwnThreadAsync($"thread-{i}");
            created.Add(thread.ThreadId);
        }

        var expected = await ListThreadsAsync();
        expected.Select(t => t.ThreadId).Should().BeEquivalentTo(created);

        var walked = new List<string>();
        AiThread? last = null;

        for (var guard = 0; guard <= created.Count; guard++)
        {
            var page = await ListThreadsAsync(1, after: last);
            if (page.Count == 0)
            {
                break;
            }

            page.Should().ContainSingle();
            walked.Add(page[0].ThreadId);
            last = page[0];
        }

        walked.Should().Equal(expected.Select(t => t.ThreadId));
    }

    [Fact]
    public async Task ReadAll_MalformedCursor_StartsFromTheFirstPage()
    {
        await CreateOwnThreadAsync("first");
        await CreateOwnThreadAsync("second");

        var firstPage = await ListThreadsAsync(1);

        // A cursor that is not JSON is ignored rather than rejected (parseThreadsCursor in threadsController.ts).
        var page = await _aiThreads.AiThreadsListAsync(
            count: 1,
            cursor: "not-json",
            cancellationToken: TestContext.Current.CancellationToken);

        page.Select(t => t.ThreadId).Should().Equal(firstPage.Select(t => t.ThreadId));
    }

    // Internal contract: entityId scoping of the thread list has no public route (the chat service stores every
    // thread under entityId null and ignores the entityId of the list, by design - Bug 82719).
    [Fact]
    public async Task ReadAll_WithCursor_ScopedByEntity()
    {
        var roomId = await CreateRoomAsync();
        await CreateThreadInternalAsync("scoped-first", entityId: roomId.ToString());
        await CreateThreadInternalAsync("scoped-second", entityId: roomId.ToString());
        await CreateThreadInternalAsync("global");

        var firstPage = await ReadThreadsPageInternalAsync(1, roomId.ToString());
        firstPage.Items.Should().ContainSingle();
        firstPage.Cursor.Should().NotBeNull();

        var secondPage = await ReadThreadsPageInternalAsync(
            1,
            roomId.ToString(),
            firstPage.Cursor!.LastEditDate,
            firstPage.Cursor.Id);

        secondPage.Items.Should().ContainSingle();
        secondPage.Items[0].Id.Should().NotBe(firstPage.Items[0].Id);
        secondPage.Cursor.Should().BeNull();
    }
}

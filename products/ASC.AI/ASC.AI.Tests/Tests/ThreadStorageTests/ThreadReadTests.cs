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

[Collection("Test Collection")]
[Trait("Category", "CRUD")]
[Trait("Feature", "AI/Threads")]
public class ThreadReadTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task ReadById_Existing_ReturnsThread()
    {
        var created = await CreateThreadAsync("my-thread");

        var stored = await ReadThreadAsync(created.Id);

        stored.Id.Should().Be(created.Id);
        stored.Title.Should().Be("my-thread");
    }

    [Fact]
    public async Task ReadById_NonExisting_Returns404()
    {
        using var response = await Ai.GetAsync(
            $"{ThreadsPath}/{Guid.NewGuid()}",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReadAll_Owner_ReturnsAllCreated()
    {
        var first = await CreateThreadAsync("first");
        var second = await CreateThreadAsync("second");

        var all = await ReadAllThreadsAsync();

        all.Should().Contain(t => t.Id == first.Id);
        all.Should().Contain(t => t.Id == second.Id);
    }

    [Fact]
    public async Task ReadAll_Empty_ReturnsEmpty()
    {
        var all = await ReadAllThreadsAsync();

        all.Should().BeEmpty();
    }

    [Fact]
    public async Task ReadAll_WithEntityId_ReturnsOnlyScopedThreads()
    {
        var roomId = await CreateRoomAsync();
        var globalThread = await CreateThreadAsync("global");
        var scopedThread = await CreateThreadAsync("scoped", entityId: roomId.ToString());

        var scoped = await ReadAllThreadsAsync(roomId.ToString());

        scoped.Should().ContainSingle(t => t.Id == scopedThread.Id);
        scoped.Should().NotContain(t => t.Id == globalThread.Id);
    }

    [Fact]
    public async Task ReadAll_WithoutCount_Returns400()
    {
        using var response = await Ai.GetAsync(ThreadsPath, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReadAll_WithCount_ReturnsBoundedResultsAndCursor()
    {
        await CreateThreadAsync("first");
        await CreateThreadAsync("second");
        await CreateThreadAsync("third");

        var page = await ReadThreadsPageAsync(2);

        page.Items.Should().HaveCount(2);
        page.Cursor.Should().NotBeNull();
        page.Cursor!.Id.Should().Be(page.Items[^1].Id);
        page.Cursor.LastEditDate.Should().Be(page.Items[^1].LastEditDate);
    }

    [Fact]
    public async Task ReadAll_LastPage_HasNoCursor()
    {
        await CreateThreadAsync("first");
        await CreateThreadAsync("second");

        var page = await ReadThreadsPageAsync(2);

        page.Items.Should().HaveCount(2);
        page.Cursor.Should().BeNull();
    }

    [Fact]
    public async Task ReadAll_PagingByOne_WalksEveryThreadWithoutGaps()
    {
        var created = new List<Guid>();
        for (var i = 0; i < 5; i++)
        {
            var thread = await CreateThreadAsync($"thread-{i}");
            created.Add(thread.Id);
        }

        var expected = await ReadAllThreadsAsync();
        expected.Select(t => t.Id).Should().BeEquivalentTo(created);

        var walked = new List<Guid>();
        long? cursorLastEditDate = null;
        Guid? cursorId = null;

        while (true)
        {
            var page = await ReadThreadsPageAsync(1, cursorLastEditDate: cursorLastEditDate, cursorId: cursorId);
            walked.AddRange(page.Items.Select(t => t.Id));

            if (page.Cursor is null)
            {
                break;
            }

            cursorLastEditDate = page.Cursor.LastEditDate;
            cursorId = page.Cursor.Id;
        }

        walked.Should().Equal(expected.Select(t => t.Id));
    }

    [Fact]
    public async Task ReadAll_WithCursor_ScopedByEntity()
    {
        var roomId = await CreateRoomAsync();
        await CreateThreadAsync("scoped-first", entityId: roomId.ToString());
        await CreateThreadAsync("scoped-second", entityId: roomId.ToString());
        await CreateThreadAsync("global");

        var firstPage = await ReadThreadsPageAsync(1, roomId.ToString());
        firstPage.Items.Should().ContainSingle();
        firstPage.Cursor.Should().NotBeNull();

        var secondPage = await ReadThreadsPageAsync(
            1,
            roomId.ToString(),
            firstPage.Cursor!.LastEditDate,
            firstPage.Cursor.Id);

        secondPage.Items.Should().ContainSingle();
        secondPage.Items[0].Id.Should().NotBe(firstPage.Items[0].Id);
        secondPage.Cursor.Should().BeNull();
    }
}

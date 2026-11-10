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

namespace ASC.AI.Tests.Tests.MessageStorageTests;

[Trait("Category", "CRUD")]
[Trait("Feature", "AI/Messages")]
public class MessageReadTests(AspireAppFixture fixture) : MessageTestBase(fixture)
{
    [Fact]
    public async Task ReadById_Existing_ReturnsMessage()
    {
        var threadId = await CreateMessageThreadAsync();
        var messageId = await AppendAsync(threadId, "hello");

        var stored = await GetMessageAsync(messageId);

        stored.Id.Should().Be(messageId);
        stored.Role.Should().Be(AiThreadMessageLike.RoleEnum.User);
        TextOf(stored).Should().Be("hello");
    }

    [Fact]
    public async Task ReadById_NonExisting_Returns404()
    {
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await GetMessageAsync(Guid.NewGuid().ToString()));

        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task ReadByThread_ReturnsAllMessagesOrderedByTimestamp()
    {
        var threadId = await CreateMessageThreadAsync();
        var first = await AppendAsync(threadId, "first");
        var second = await AppendAsync(threadId, "second");
        var third = await AppendAsync(threadId, "third");

        var messages = await ReadMessagesAsync(threadId, DefaultPageCount);

        messages.Should().HaveCount(3);
        messages.Select(m => m.Id).Should().ContainInOrder(first, second, third);
    }

    [Fact]
    public async Task ReadByThread_Empty_ReturnsEmpty()
    {
        var threadId = await CreateMessageThreadAsync();

        var messages = await ReadMessagesAsync(threadId, DefaultPageCount);

        messages.Should().BeEmpty();
    }

    [Fact]
    public async Task ReadByThread_NonExistentThread_Returns404()
    {
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await ReadMessagesAsync(Guid.NewGuid().ToString(), DefaultPageCount));

        exception.ErrorCode.Should().Be(404);
    }

    // The internal route requires `count`; the public one makes it optional and reads the whole thread without it.
    [Fact]
    public async Task ReadByThread_WithoutCount_ReturnsWholeThread()
    {
        var threadId = await CreateMessageThreadAsync();
        var first = await AppendAsync(threadId, "first");
        var second = await AppendAsync(threadId, "second");

        var messages = await ReadMessagesAsync(threadId);

        messages.Select(m => m.Id).Should().Equal(first, second);
    }

    // The public read answers a bare list: the next cursor is built by the caller from the last message.
    [Fact]
    public async Task ReadByThread_WithCount_ReturnsBoundedResults()
    {
        var threadId = await CreateMessageThreadAsync();
        var first = await AppendAsync(threadId, "first");
        var second = await AppendAsync(threadId, "second");
        await AppendAsync(threadId, "third");

        var page = await ReadMessagesAsync(threadId, 2);

        page.Select(m => m.Id).Should().Equal(first, second);
    }

    [Fact]
    public async Task ReadByThread_CursorAfterLastPage_ReturnsEmpty()
    {
        var threadId = await CreateMessageThreadAsync();
        await AppendAsync(threadId, "first");
        await AppendAsync(threadId, "second");

        var page = await ReadMessagesAsync(threadId, 2);
        page.Should().HaveCount(2);

        var after = await ReadMessagesAsync(threadId, 2, CursorAfter(page[^1]));
        after.Should().BeEmpty();
    }

    [Fact]
    public async Task ReadByThread_WithCursor_ReturnsNextPage()
    {
        var threadId = await CreateMessageThreadAsync();
        var first = await AppendAsync(threadId, "first");
        var second = await AppendAsync(threadId, "second");
        var third = await AppendAsync(threadId, "third");
        var fourth = await AppendAsync(threadId, "fourth");

        var firstPage = await ReadMessagesAsync(threadId, 2);
        firstPage.Select(m => m.Id).Should().Equal(first, second);

        var secondPage = await ReadMessagesAsync(threadId, 2, CursorAfter(firstPage[^1]));

        secondPage.Select(m => m.Id).Should().Equal(third, fourth);
    }

    [Fact]
    public async Task ReadByThread_PagingByOne_WalksEveryMessageWithoutGaps()
    {
        const int maxPages = 10;

        var threadId = await CreateMessageThreadAsync();
        var created = new List<string>();
        for (var i = 0; i < 5; i++)
        {
            created.Add(await AppendAsync(threadId, $"message-{i}"));
        }

        var walked = new List<string>();
        string? cursor = null;

        for (var i = 0; i < maxPages; i++)
        {
            var page = await ReadMessagesAsync(threadId, 1, cursor);
            if (page.Count == 0)
            {
                break;
            }

            walked.AddRange(page.Select(m => m.Id));
            cursor = CursorAfter(page[^1]);
        }

        walked.Should().Equal(created);
    }

    /// <summary>
    /// A cursor built from the last message of a page — its millisecond <c>createdAt</c> and <c>id</c>, the way the
    /// chat widget builds it — used to return that message again on a forward read, because the stored timestamp
    /// kept microseconds; the last message of the thread came back on every request and the walk never ended.
    /// Messages are now stored with millisecond precision and the cursor is compared at that precision.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("desc")]
    [Trait("Bug", "84407")]
    public async Task ReadByThread_CursorFromLastMessage_ReturnsEveryMessageOnceAndEndsWithEmptyPage(string? direction)
    {
        const int pageSize = 3;
        const int maxPages = 10;

        var threadId = await CreateMessageThreadAsync();
        var created = new List<string>();
        for (var i = 0; i < 7; i++)
        {
            created.Add(await AppendAsync(threadId, $"m{i}"));
        }
        if (direction == "desc")
        {
            created.Reverse();
        }

        var pages = new List<List<string>>();
        string? cursor = null;

        for (var i = 0; i < maxPages; i++)
        {
            var page = await ReadMessagesAsync(threadId, pageSize, cursor, direction);
            pages.Add([.. page.Select(m => m.Id)]);

            if (page.Count == 0)
            {
                break;
            }

            cursor = CursorAfter(page[^1]);
        }

        List<List<string>> expected = [.. created.Chunk(pageSize).Select(chunk => chunk.ToList()), []];
        pages.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task ReadByThread_DoesNotReturnMessagesFromOtherThread()
    {
        var first = await CreateMessageThreadAsync("first");
        var second = await CreateMessageThreadAsync("second");

        var firstMessage = await AppendAsync(first, "first-msg");
        var secondMessage = await AppendAsync(second, "second-msg");

        var firstMessages = await ReadMessagesAsync(first, DefaultPageCount);
        firstMessages.Should().ContainSingle(m => m.Id == firstMessage);
        firstMessages.Should().NotContain(m => m.Id == secondMessage);
    }
}

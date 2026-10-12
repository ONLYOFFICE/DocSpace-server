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
public class MessageReadTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task ReadById_Existing_ReturnsMessage()
    {
        var thread = await CreateThreadAsync();
        var contents = BuildMessageContents("hello");
        var created = await CreateMessageAsync(thread.Id, contents);

        var stored = await ReadMessageAsync(created.Id);

        stored.Id.Should().Be(created.Id);
        stored.ThreadId.Should().Be(thread.Id);
        JsonEquals(stored.Contents, contents).Should().BeTrue();
    }

    [Fact]
    public async Task ReadById_NonExisting_Returns404()
    {
        using var response = await _ai.GetAsync(
            $"{MessagesPath}/{Guid.NewGuid()}",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReadByThread_ReturnsAllMessagesOrderedByTimestamp()
    {
        var thread = await CreateThreadAsync();
        var first = await CreateMessageAsync(thread.Id, BuildMessageContents("first"));
        var second = await CreateMessageAsync(thread.Id, BuildMessageContents("second"));
        var third = await CreateMessageAsync(thread.Id, BuildMessageContents("third"));

        var messages = await ReadMessagesByThreadAsync(thread.Id);

        messages.Should().HaveCount(3);
        messages.Select(m => m.Id).Should().ContainInOrder(first.Id, second.Id, third.Id);
    }

    [Fact]
    public async Task ReadByThread_Empty_ReturnsEmpty()
    {
        var thread = await CreateThreadAsync();

        var messages = await ReadMessagesByThreadAsync(thread.Id);

        messages.Should().BeEmpty();
    }

    [Fact]
    public async Task ReadByThread_NonExistentThread_Returns404()
    {
        using var response = await _ai.GetAsync(
            $"{ThreadsPath}/{Guid.NewGuid()}/messages?count={DefaultPageCount}",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReadByThread_WithoutCount_Returns400()
    {
        var thread = await CreateThreadAsync();

        using var response = await _ai.GetAsync(
            $"{ThreadsPath}/{thread.Id}/messages",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReadByThread_WithCount_ReturnsBoundedResultsAndCursor()
    {
        var thread = await CreateThreadAsync();
        var first = await CreateMessageAsync(thread.Id, BuildMessageContents("first"));
        var second = await CreateMessageAsync(thread.Id, BuildMessageContents("second"));
        await CreateMessageAsync(thread.Id, BuildMessageContents("third"));

        var page = await ReadMessagesPageAsync(thread.Id, 2);

        page.Items.Should().HaveCount(2);
        page.Items.Select(m => m.Id).Should().ContainInOrder(first.Id, second.Id);
        page.Cursor.Should().NotBeNull();
        page.Cursor!.Id.Should().Be(second.Id);
    }

    [Fact]
    public async Task ReadByThread_LastPage_HasNoCursor()
    {
        var thread = await CreateThreadAsync();
        await CreateMessageAsync(thread.Id, BuildMessageContents("first"));
        await CreateMessageAsync(thread.Id, BuildMessageContents("second"));

        var page = await ReadMessagesPageAsync(thread.Id, 2);

        page.Items.Should().HaveCount(2);
        page.Cursor.Should().BeNull();
    }

    [Fact]
    public async Task ReadByThread_WithCursor_ReturnsNextPage()
    {
        var thread = await CreateThreadAsync();
        var first = await CreateMessageAsync(thread.Id, BuildMessageContents("first"));
        var second = await CreateMessageAsync(thread.Id, BuildMessageContents("second"));
        var third = await CreateMessageAsync(thread.Id, BuildMessageContents("third"));
        var fourth = await CreateMessageAsync(thread.Id, BuildMessageContents("fourth"));

        var firstPage = await ReadMessagesPageAsync(thread.Id, 2);
        firstPage.Items.Select(m => m.Id).Should().ContainInOrder(first.Id, second.Id);
        firstPage.Cursor.Should().NotBeNull();

        var secondPage = await ReadMessagesPageAsync(
            thread.Id,
            2,
            firstPage.Cursor!.CreatedAt,
            firstPage.Cursor.Id);

        secondPage.Items.Select(m => m.Id).Should().ContainInOrder(third.Id, fourth.Id);
        secondPage.Cursor.Should().BeNull();
    }

    [Fact]
    public async Task ReadByThread_PagingByOne_WalksEveryMessageWithoutGaps()
    {
        var thread = await CreateThreadAsync();
        var created = new List<Guid>();
        for (var i = 0; i < 5; i++)
        {
            var message = await CreateMessageAsync(thread.Id, BuildMessageContents($"message-{i}"));
            created.Add(message.Id);
        }

        var walked = new List<Guid>();
        DateTimeOffset? cursorCreatedAt = null;
        Guid? cursorId = null;

        while (true)
        {
            var page = await ReadMessagesPageAsync(thread.Id, 1, cursorCreatedAt, cursorId);
            walked.AddRange(page.Items.Select(m => m.Id));

            if (page.Cursor is null)
            {
                break;
            }

            cursorCreatedAt = page.Cursor.CreatedAt;
            cursorId = page.Cursor.Id;
        }

        walked.Should().Equal(created);
    }

    [Fact]
    public async Task ReadByThread_DoesNotReturnMessagesFromOtherThread()
    {
        var first = await CreateThreadAsync("first");
        var second = await CreateThreadAsync("second");

        var firstMessage = await CreateMessageAsync(first.Id, BuildMessageContents("first-msg"));
        var secondMessage = await CreateMessageAsync(second.Id, BuildMessageContents("second-msg"));

        var firstMessages = await ReadMessagesByThreadAsync(first.Id);
        firstMessages.Should().ContainSingle(m => m.Id == firstMessage.Id);
        firstMessages.Should().NotContain(m => m.Id == secondMessage.Id);
    }
}

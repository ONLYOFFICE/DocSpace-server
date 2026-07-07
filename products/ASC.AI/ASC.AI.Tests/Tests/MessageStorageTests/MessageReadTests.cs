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

[Collection("Test Collection")]
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
        using var response = await Ai.GetAsync(
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
        using var response = await Ai.GetAsync(
            $"{ThreadsPath}/{Guid.NewGuid()}/messages",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReadByThread_WithLimit_ReturnsBoundedResults()
    {
        var thread = await CreateThreadAsync();
        var first = await CreateMessageAsync(thread.Id, BuildMessageContents("first"));
        var second = await CreateMessageAsync(thread.Id, BuildMessageContents("second"));
        await CreateMessageAsync(thread.Id, BuildMessageContents("third"));

        var messages = await ReadMessagesByThreadAsync(thread.Id, limit: 2);

        messages.Should().HaveCount(2);
        messages.Select(m => m.Id).Should().ContainInOrder(first.Id, second.Id);
    }

    [Fact]
    public async Task ReadByThread_WithStartIndex_SkipsLeadingMessages()
    {
        var thread = await CreateThreadAsync();
        await CreateMessageAsync(thread.Id, BuildMessageContents("first"));
        await CreateMessageAsync(thread.Id, BuildMessageContents("second"));
        var third = await CreateMessageAsync(thread.Id, BuildMessageContents("third"));

        var messages = await ReadMessagesByThreadAsync(thread.Id, startIndex: 2);

        messages.Should().ContainSingle();
        messages[0].Id.Should().Be(third.Id);
    }

    [Fact]
    public async Task ReadByThread_WithLimitAndStartIndex_ReturnsWindow()
    {
        var thread = await CreateThreadAsync();
        await CreateMessageAsync(thread.Id, BuildMessageContents("first"));
        var second = await CreateMessageAsync(thread.Id, BuildMessageContents("second"));
        var third = await CreateMessageAsync(thread.Id, BuildMessageContents("third"));
        await CreateMessageAsync(thread.Id, BuildMessageContents("fourth"));

        var messages = await ReadMessagesByThreadAsync(thread.Id, limit: 2, startIndex: 1);

        messages.Should().HaveCount(2);
        messages.Select(m => m.Id).Should().ContainInOrder(second.Id, third.Id);
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

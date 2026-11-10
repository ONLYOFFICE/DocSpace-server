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
public class MessageDeleteTests(AspireAppFixture fixture) : MessageTestBase(fixture)
{
    [Fact]
    public async Task Delete_Existing_Removes()
    {
        var threadId = await CreateMessageThreadAsync();
        var messageId = await AppendAsync(threadId);

        var result = await _aiThreads.AiThreadsDeleteMessageAsync(messageId, TestContext.Current.CancellationToken);

        result.Success.Should().BeTrue();

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await GetMessageAsync(messageId));
        exception.ErrorCode.Should().Be(404);
    }

    // The internal route answers 404; the public one is idempotent by design and reports success for an unknown id
    // (the storage swallows the 404 so the engine's cascades never fail on a message already gone).
    [Fact]
    public async Task Delete_NonExisting_ReportsSuccess()
    {
        var result = await _aiThreads.AiThreadsDeleteMessageAsync(Guid.NewGuid().ToString(), TestContext.Current.CancellationToken);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_OneMessage_DoesNotAffectOthers()
    {
        var threadId = await CreateMessageThreadAsync();
        var first = await AppendAsync(threadId, "first");
        var second = await AppendAsync(threadId, "second");

        var result = await _aiThreads.AiThreadsDeleteMessageAsync(first, TestContext.Current.CancellationToken);
        result.Success.Should().BeTrue();

        var remaining = await ReadMessagesAsync(threadId);
        remaining.Should().ContainSingle(m => m.Id == second);
    }

    [Fact]
    public async Task DeleteByThread_Existing_RemovesAllMessages()
    {
        var threadId = await CreateMessageThreadAsync();
        await AppendAsync(threadId, "first");
        await AppendAsync(threadId, "second");
        await AppendAsync(threadId, "third");

        var result = await _aiThreads.AiThreadsClearMessagesAsync(threadId, TestContext.Current.CancellationToken);

        result.Success.Should().BeTrue();

        var remaining = await ReadMessagesAsync(threadId);
        remaining.Should().BeEmpty();

        // The thread itself outlives its messages.
        var thread = await _aiThreads.AiThreadsGetByIdAsync(threadId, TestContext.Current.CancellationToken);
        thread.ThreadId.Should().Be(threadId);
    }

    [Fact]
    public async Task DeleteByThread_DoesNotAffectOtherThreads()
    {
        var first = await CreateMessageThreadAsync("first");
        var second = await CreateMessageThreadAsync("second");

        await AppendAsync(first, "first-msg");
        var secondMessage = await AppendAsync(second, "second-msg");

        var result = await _aiThreads.AiThreadsClearMessagesAsync(first, TestContext.Current.CancellationToken);
        result.Success.Should().BeTrue();

        var secondMessages = await ReadMessagesAsync(second);
        secondMessages.Should().ContainSingle(m => m.Id == secondMessage);
    }

    // The clear itself swallows a missing thread, but the touch that follows it does not, so an unknown thread is
    // a 404 — as rename answers for the same id.
    [Fact]
    public async Task DeleteByThread_NonExistentThread_Returns404()
    {
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _aiThreads.AiThreadsClearMessagesAsync(Guid.NewGuid().ToString(), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task DeleteByThread_EmptyThread_ReportsSuccess()
    {
        var threadId = await CreateMessageThreadAsync();

        var result = await _aiThreads.AiThreadsClearMessagesAsync(threadId, TestContext.Current.CancellationToken);

        result.Success.Should().BeTrue();
    }
}

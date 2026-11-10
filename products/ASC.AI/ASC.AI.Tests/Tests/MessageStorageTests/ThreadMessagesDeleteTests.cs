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

/// <summary>
/// <c>DELETE /api/2.0/ai/threads/delete-message</c> beyond <see cref="MessageDeleteTests"/>: deletes at every
/// position as the chat widget sees them, the last message of a thread, idempotence and the refused ids.
/// </summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "AI/Messages")]
public class ThreadMessagesDeleteTests(AspireAppFixture fixture) : MessagesPart3TestBase(fixture)
{
    [Fact]
    public async Task Delete_FirstMiddleAndLast_GoOneAtATimeAndTheRestKeepTheirOrder()
    {
        var threadId = await NewThreadAsync("Pruning");
        var bystander = await NewThreadAsync("Bystander");
        var ids = new List<string>();
        foreach (var text in new[] { "a", "b", "c", "d", "e" })
        {
            ids.Add(await AppendAsync(threadId, text));
        }
        await AppendAsync(bystander, "other");
        var bystanderBefore = (await ReadMessagesAsync(bystander)).Select(Snapshot).ToList();

        var first = await DeleteMessageAsync(ids[0]);
        first.Success.Should().BeTrue();
        await ExpectStateAsync("without the first", "b", "c", "d", "e");

        await DeleteMessageAsync(ids[2]);
        await ExpectStateAsync("without the middle", "b", "d", "e");

        await DeleteMessageAsync(ids[4]);
        await ExpectStateAsync("without the last", "b", "d");

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await GetMessageAsync(ids[0]));
        exception.ErrorCode.Should().Be(404, "a deleted message is no longer served");
        TextOf(await GetMessageAsync(ids[1])).Should().Be("b", "a kept one still is");
        (await ReadMessagesAsync(bystander)).Select(Snapshot).Should().Equal(bystanderBefore, "another thread is untouched");

        async Task ExpectStateAsync(string label, params string[] texts)
        {
            (await ReadTextsAsync(threadId)).Should().Equal(texts, label);
            (await OpenPriorMessagesAsync(threadId)).Select(TextOf).Should().Equal(texts, $"{label} (open-or-create)");
        }
    }

    [Fact]
    public async Task Delete_TheOnlyMessage_LeavesAnEmptyThreadThatStillExists()
    {
        var threadId = await NewThreadAsync("Down to none");
        var messageId = await AppendAsync(threadId, "the only one");

        var result = await DeleteMessageAsync(messageId);

        result.Success.Should().BeTrue();
        (await ReadMessagesAsync(threadId)).Should().BeEmpty();
        (await GetThreadAsync(threadId)).Title.Should().Be("Down to none");
        (await ListThreadIdsAsync()).Should().Equal(threadId);

        await AppendAsync(threadId, "and it takes new ones");
        (await ReadTextsAsync(threadId)).Should().Equal("and it takes new ones");
    }

    // A never-issued id is Delete_NonExisting_ReportsSuccess in MessageDeleteTests.
    [Fact]
    public async Task Delete_SameIdTwice_IsIdempotentAndDeletesOnlyTheTarget()
    {
        var threadId = await NewThreadAsync("Idempotent");
        var keep = await AppendAsync(threadId, "keep");
        var gone = await AppendAsync(threadId, "gone");

        var once = await DeleteMessageAsync(gone);
        var twice = await DeleteMessageAsync(gone);

        (await ReadTextsAsync(threadId)).Should().Equal(["keep"], "only the target went");
        TextOf(await GetMessageAsync(keep)).Should().Be("keep");
        once.Success.Should().BeTrue();
        twice.Success.Should().BeTrue("the same id again");
    }

    [Theory]
    [InlineData("a malformed id")]
    [InlineData("an empty id")]
    [InlineData("no body")]
    public async Task Delete_RefusedId_Returns400AndDeletesNothing(string kind)
    {
        var threadId = await NewThreadAsync("Idempotent");
        var keep = await AppendAsync(threadId, "keep");

        HttpStatusCode status;
        if (kind == "no body")
        {
            // Raw: the typed call always sends the id as its body.
            using var response = await new RawApiClient(_aiChatClient).DeleteAsync(DeleteMessagePath, TestContext.Current.CancellationToken);
            status = response.StatusCode;
        }
        else
        {
            var exception = await Assert.ThrowsAsync<ApiException>(async () =>
                await DeleteMessageAsync(kind == "a malformed id" ? MalformedId : string.Empty));
            status = (HttpStatusCode)exception.ErrorCode;
        }

        (await ReadTextsAsync(threadId)).Should().Equal(["keep"], "nothing was deleted");
        TextOf(await GetMessageAsync(keep)).Should().Be("keep");
        status.Should().Be(HttpStatusCode.BadRequest, kind);
    }
}

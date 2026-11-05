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
/// <c>GET /api/2.0/ai/threads/read-messages</c> beyond what <see cref="MessageReadTests"/> covers: whole messages
/// through the read path, a history that follows edits, the count bounds and the refused thread ids. The forward and
/// backward cursor walks (BUG 84407) are in <see cref="MessageReadTests"/>.
/// </summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "AI/Messages")]
public class ThreadMessagesReadTests(AspireAppFixture fixture) : MessagesPart3TestBase(fixture)
{
    [Fact]
    public async Task ReadMessages_StoredMessages_ComeBackWholeInOrderWithUniqueIds()
    {
        var threadId = await NewThreadAsync("Reading");

        (await ReadMessagesAsync(threadId)).Should().BeEmpty("an empty history");

        var ids = new List<string>();
        foreach (var key in AwkwardKeys)
        {
            ids.Add(await AppendAsync(threadId, AwkwardTexts[key]));
        }

        var messages = await ReadMessagesAsync(threadId);

        messages.Select(m => m.Id).Should().Equal(ids);
        messages.Select(TextOf).Should().Equal(AwkwardKeys.Select(key => AwkwardTexts[key]));
        messages.Select(m => m.Id).Should().OnlyHaveUniqueItems();

        foreach (var message in messages)
        {
            Guid.TryParse(message.Id, out _).Should().BeTrue($"message id {message.Id} is a UUID");
            message.Role.Should().Be(AiThreadMessageLike.RoleEnum.User);
            DateTimeOffset.TryParse(message.CreatedAt, out _).Should().BeTrue($"createdAt {message.CreatedAt} is a timestamp");
        }

        var stamps = messages.Select(m => DateTimeOffset.Parse(m.CreatedAt)).ToList();
        stamps.Should().BeInAscendingOrder("createdAt never goes backwards");

        foreach (var message in messages)
        {
            Snapshot(await GetMessageAsync(message.Id)).Should().Be(Snapshot(message), "get-message-by-id agrees with the history");
        }
    }

    [Fact]
    public async Task ReadMessages_History_FollowsUpdateDeleteAndClear()
    {
        var threadId = await NewThreadAsync("Following");
        var first = await AppendAsync(threadId, "first");
        var second = await AppendAsync(threadId, "second");
        await AppendAsync(threadId, "third");

        await UpdateTextAsync(second, "second, edited");
        (await ReadTextsAsync(threadId)).Should().Equal(["first", "second, edited", "third"], "after an update");

        await DeleteMessageAsync(first);
        (await ReadTextsAsync(threadId)).Should().Equal(["second, edited", "third"], "after a delete");

        await ClearMessagesAsync(threadId);
        (await ReadMessagesAsync(threadId)).Should().BeEmpty("after a clear");
    }

    [Fact]
    public async Task ReadMessages_Count_CapsThePage()
    {
        var threadId = await NewThreadAsync("Paged");
        string[] texts = ["m0", "m1", "m2", "m3", "m4"];
        foreach (var text in texts)
        {
            await AppendAsync(threadId, text);
        }

        (await ReadMessagesAsync(threadId, 1)).Select(TextOf).Should().Equal(["m0"], "count=1 is the oldest");
        (await ReadMessagesAsync(threadId, 3)).Select(TextOf).Should().Equal("m0", "m1", "m2");
        (await ReadMessagesAsync(threadId, 5)).Select(TextOf).Should().Equal(texts);
        (await ReadMessagesAsync(threadId, DefaultPageCount)).Select(TextOf).Should().Equal(texts, "count above the total");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ReadMessages_NonPositiveCount_Returns400(int count)
    {
        var threadId = await NewThreadAsync("Paged");
        await AppendAsync(threadId, "m0");

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await ReadMessagesAsync(threadId, count));

        exception.ErrorCode.Should().Be(400);
    }

    // An unknown id is ReadByThread_NonExistentThread_Returns404 in MessageReadTests.
    [Theory]
    [InlineData("deleted", 404)]
    [InlineData("malformed", 400)]
    public async Task ReadMessages_RefusedThreadId_IsRefusedAndTheRouteStillReads(string kind, int expected)
    {
        var keeper = await NewThreadAsync("Keeper");
        await AppendAsync(keeper, "control");

        var threadId = MalformedId;
        if (kind == "deleted")
        {
            threadId = await NewThreadAsync("Doomed");
            (await DeleteThreadAsync(threadId)).Success.Should().BeTrue();
        }

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await ReadMessagesAsync(threadId));

        (await ReadTextsAsync(keeper)).Should().Equal(["control"], "the route still reads");
        exception.ErrorCode.Should().Be(expected);
    }

    /// <summary>
    /// A request with no thread id used to answer 200 with an empty list, which a client cannot tell from a real
    /// empty thread; get-by-id answered 400 for the same omission. It is now a 400 "threadId required" too.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Bug", "84408")]
    public async Task ReadMessages_NoThreadId_IsRefusedLikeGetById(bool emptyValue)
    {
        var threadId = await NewThreadAsync("Real");
        await AppendAsync(threadId, "something");

        // Raw on purpose: threadId is a required parameter of the typed call, so it cannot be left out.
        (await GetRawStatusAsync(GetThreadByIdPath)).Should().Be(HttpStatusCode.BadRequest, "the neighbouring route refuses the omission");

        if (emptyValue)
        {
            var exception = await Assert.ThrowsAsync<ApiException>(async () => await ReadMessagesAsync(string.Empty));
            exception.ErrorCode.Should().Be(400, "an empty threadId");
        }
        else
        {
            (await GetRawStatusAsync(ReadMessagesPath)).Should().Be(HttpStatusCode.BadRequest, "no threadId");
        }
    }
}

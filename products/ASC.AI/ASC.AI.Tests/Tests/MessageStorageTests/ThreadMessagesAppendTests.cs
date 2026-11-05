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

/// <summary><c>POST /api/2.0/ai/threads/append-user-message</c>: what a successful append stores.</summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "AI/Messages")]
public class ThreadMessagesAppendTests(AspireAppFixture fixture) : MessagesPart3TestBase(fixture)
{
    [Fact]
    public async Task Append_StoredMessage_IsTheSameThroughEveryReadPath()
    {
        var threadId = await NewThreadAsync("Writing");
        var bystander = await NewThreadAsync("Bystander");

        var messageId = await AppendAsync(threadId, "Hello");

        Guid.TryParse(messageId, out _).Should().BeTrue($"message id {messageId} is a UUID");

        var read = await ReadMessagesAsync(threadId);
        read.Select(m => m.Id).Should().Equal(messageId);
        read[0].Role.Should().Be(AiThreadMessageLike.RoleEnum.User);
        TextOf(read[0]).Should().Be("Hello");

        Snapshot(await GetMessageAsync(messageId)).Should().Be(Snapshot(read[0]));
        (await OpenPriorMessagesAsync(threadId)).Select(m => m.Id).Should().Equal(messageId);

        (await ReadMessagesAsync(bystander)).Should().BeEmpty("another thread got nothing");
        (await ListThreadIdsAsync()).Should().BeEquivalentTo(new[] { threadId, bystander });
    }

    [Theory]
    [InlineData("cyrillic")]
    [InlineData("emoji")]
    [InlineData("whitespace")]
    [InlineData("markup")]
    [InlineData("long")]
    public async Task Append_AwkwardText_IsStoredExactlyAsSent(string key)
    {
        var text = AwkwardTexts[key];
        var threadId = await NewThreadAsync("Verbatim");

        var messageId = await AppendAsync(threadId, text);

        (await ReadTextsAsync(threadId)).Should().Equal(text);
        TextOf(await GetMessageAsync(messageId)).Should().Be(text);
    }

    [Fact]
    public async Task Append_ManyAppends_KeepOrderWorkAfterClearAndNeverReachTheModel()
    {
        // A provider of the test's own, so "the model was never asked" is something the test can observe instead of
        // only waiting for a reply that does not come.
        using var provider = new StubModelProvider();
        var profile = await CreateProfileOnAsync(provider);
        var threadId = (await CreateThreadAsync("Sequence", profile.Id)).ThreadId;
        var requestsBefore = provider.CompletionRequestBodies.Count();

        var texts = Enumerable.Range(0, 12).Select(i => $"step {i}").ToList();
        foreach (var text in texts)
        {
            await AppendAsync(threadId, text);
        }

        (await ReadTextsAsync(threadId)).Should().Equal(texts);

        await ClearMessagesAsync(threadId);
        await AppendAsync(threadId, "after the clear");
        (await ReadTextsAsync(threadId)).Should().Equal("after the clear");

        // "No inference" is an absence, so it gets an observation window: a reply would land within seconds.
        var deadline = DateTime.UtcNow.AddSeconds(6);
        List<AiThreadMessageLike> messages;
        while (true)
        {
            messages = await ReadMessagesAsync(threadId);
            if (messages.Count != 1 || DateTime.UtcNow >= deadline)
            {
                break;
            }

            await Task.Delay(1000, TestContext.Current.CancellationToken);
        }

        messages.Should().ContainSingle().Which.Role.Should().Be(AiThreadMessageLike.RoleEnum.User);
        provider.CompletionRequestBodies.Count().Should().Be(requestsBefore, "an append never sends a request to the model");
    }

    [Fact]
    public async Task Append_ParallelAppends_AreAllStored()
    {
        var threadId = await NewThreadAsync("Parallel");
        var other = await NewThreadAsync("Parallel other");

        // Ten requests in flight is what the route takes before the rate limiter starts answering 429.
        var texts = Enumerable.Range(0, 6).Select(i => $"p{i}").ToList();
        var otherTexts = Enumerable.Range(0, 4).Select(i => $"o{i}").ToList();

        await Task.WhenAll(
            texts.Select(text => AppendAsync(threadId, text))
                .Concat(otherTexts.Select(text => AppendAsync(other, text))));

        // The order of concurrent writes is not promised; their presence is.
        (await ReadTextsAsync(threadId)).Should().BeEquivalentTo(texts);
        (await ReadTextsAsync(other)).Should().BeEquivalentTo(otherTexts);
    }
}

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
public class MessageCreateTests(AspireAppFixture fixture) : MessageTestBase(fixture)
{
    [Fact]
    public async Task Create_Owner_PersistsAndIsReadable()
    {
        var threadId = await CreateMessageThreadAsync();

        var messageId = await AppendAsync(threadId, "hello world");

        var stored = await GetMessageAsync(messageId);
        stored.Id.Should().Be(messageId);
        stored.Role.Should().Be(AiThreadMessageLike.RoleEnum.User);
        TextOf(stored).Should().Be("hello world");
        stored.CreatedAt.Should().NotBeNullOrEmpty();

        // The public message carries no thread id: it belongs to the thread whose history holds it.
        var messages = await ReadMessagesAsync(threadId);
        messages.Should().ContainSingle(m => m.Id == messageId);
    }

    [Fact]
    public async Task Create_MultipleMessages_AllPersisted()
    {
        var threadId = await CreateMessageThreadAsync();

        var first = await AppendAsync(threadId, "first");
        var second = await AppendAsync(threadId, "second");

        first.Should().NotBe(second);

        var messages = await ReadMessagesAsync(threadId);
        messages.Should().HaveCount(2);
        messages.Should().Contain(m => m.Id == first && TextOf(m) == "first");
        messages.Should().Contain(m => m.Id == second && TextOf(m) == "second");
    }

    [Fact]
    public async Task Create_NonExistentThread_Returns404()
    {
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _aiThreads.AiThreadsAppendUserMessageAsync(
            new AiThreadsAppendUserMessageRequest(threadId: Guid.NewGuid().ToString(), message: BuildUserMessage()),
            TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task Create_MissingMessage_Returns400()
    {
        var threadId = await CreateMessageThreadAsync();

        // Raw on purpose: `message` is a required constructor parameter of the SDK request, so a typed call cannot
        // leave it out.
        using var response = await new RawApiClient(_aiChatClient).PostRawAsync(
            AppendUserMessagePath,
            JsonSerializer.Serialize(new { threadId }),
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ReadMessagesAsync(threadId)).Should().BeEmpty("a rejected append stores nothing");
    }
}

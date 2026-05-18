// (c) Copyright Ascensio System SIA 2009-2026
//
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
//
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
//
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
//
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

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

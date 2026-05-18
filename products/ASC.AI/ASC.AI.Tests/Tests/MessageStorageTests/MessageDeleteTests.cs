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
public class MessageDeleteTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task Delete_Existing_Removes()
    {
        var thread = await CreateThreadAsync();
        var created = await CreateMessageAsync(thread.Id);

        using var response = await Ai.DeleteAsync(
            $"{MessagesPath}/{created.Id}",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var readResponse = await Ai.GetAsync(
            $"{MessagesPath}/{created.Id}",
            TestContext.Current.CancellationToken);
        readResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_NonExisting_Returns404()
    {
        using var response = await Ai.DeleteAsync(
            $"{MessagesPath}/{Guid.NewGuid()}",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_OneMessage_DoesNotAffectOthers()
    {
        var thread = await CreateThreadAsync();
        var first = await CreateMessageAsync(thread.Id, BuildMessageContents("first"));
        var second = await CreateMessageAsync(thread.Id, BuildMessageContents("second"));

        using var response = await Ai.DeleteAsync(
            $"{MessagesPath}/{first.Id}",
            TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var remaining = await ReadMessagesByThreadAsync(thread.Id);
        remaining.Should().ContainSingle(m => m.Id == second.Id);
    }

    [Fact]
    public async Task DeleteByThread_Existing_RemovesAllMessages()
    {
        var thread = await CreateThreadAsync();
        await CreateMessageAsync(thread.Id, BuildMessageContents("first"));
        await CreateMessageAsync(thread.Id, BuildMessageContents("second"));
        await CreateMessageAsync(thread.Id, BuildMessageContents("third"));

        using var response = await Ai.DeleteAsync(
            $"{ThreadsPath}/{thread.Id}/messages",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var remaining = await ReadMessagesByThreadAsync(thread.Id);
        remaining.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteByThread_DoesNotAffectOtherThreads()
    {
        var first = await CreateThreadAsync("first");
        var second = await CreateThreadAsync("second");

        await CreateMessageAsync(first.Id, BuildMessageContents("first-msg"));
        var secondMessage = await CreateMessageAsync(second.Id, BuildMessageContents("second-msg"));

        using var response = await Ai.DeleteAsync(
            $"{ThreadsPath}/{first.Id}/messages",
            TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var secondMessages = await ReadMessagesByThreadAsync(second.Id);
        secondMessages.Should().ContainSingle(m => m.Id == secondMessage.Id);
    }

    [Fact]
    public async Task DeleteByThread_NonExistentThread_Returns404()
    {
        using var response = await Ai.DeleteAsync(
            $"{ThreadsPath}/{Guid.NewGuid()}/messages",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteByThread_EmptyThread_Returns204()
    {
        var thread = await CreateThreadAsync();

        using var response = await Ai.DeleteAsync(
            $"{ThreadsPath}/{thread.Id}/messages",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}

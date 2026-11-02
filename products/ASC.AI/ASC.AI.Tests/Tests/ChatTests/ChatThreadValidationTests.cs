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

namespace ASC.AI.Tests.Tests.ChatTests;

/// <summary>
/// Renaming, deleting and clearing a thread: what the three menu routes refuse. A route that answers
/// <c>{success: true}</c> for a thread that does not exist tells a client its rename or delete landed.
/// </summary>
/// <remarks>
/// Already covered in <c>ThreadStorageTests</c> / <c>MessageStorageTests</c> and not repeated here: a blank or
/// whitespace rename (400), rename / delete / clear of an id that never existed (404), clearing an empty thread.
/// </remarks>
[Trait("Category", "Chat")]
[Trait("Feature", "AI/Threads")]
public class ChatThreadValidationTests(AspireAppFixture fixture) : ChatPart3TestBase(fixture)
{
    private const string RenamePath = "/api/2.0/ai/threads/rename";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Bug", "83094")]
    public async Task Rename_NoTitle_Returns400AndKeepsTheName(bool explicitNull)
    {
        var thread = await CreateThreadAsync("original");

        // Positive control: a real title through this route does land.
        (await RenameAsync(thread.ThreadId, "renamed")).Success.Should().BeTrue();

        // Raw on purpose: the title is a required non-null parameter of AiThreadsRenameRequest, so neither a missing
        // title nor a null one can be sent through the typed client.
        var body = explicitNull
            ? JsonSerializer.Serialize(new { threadId = thread.ThreadId, title = (string?)null })
            : JsonSerializer.Serialize(new { threadId = thread.ThreadId });
        using var response = await new RawApiClient(_aiChatClient).PutRawAsync(RenamePath, body, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        (await GetThreadAsync(thread.ThreadId)).Title.Should().Be("renamed", "a refused rename does not wipe the name");
    }

    [Theory]
    [InlineData("rename")]
    [InlineData("delete")]
    [InlineData("clear-messages")]
    public async Task MutatingRoute_MalformedThreadId_Returns400(string route)
    {
        // get-by-id draws the line at 400 for an id that is not a thread id; the three mutating routes must too.
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await ActAsync(route, "abc"));

        exception.ErrorCode.Should().Be(400, exception.ErrorContent?.ToString());

        // Positive control: the route works on a thread that does exist.
        var control = await CreateThreadAsync("control");
        (await ActAsync(route, control.ThreadId)).Success.Should().BeTrue();
    }

    [Fact]
    [Trait("Bug", "83095")]
    public async Task Delete_AlreadyDeletedThread_Returns404()
    {
        // A client must be able to tell a delete that removed a chat from one that removed nothing; rename and
        // clear-messages answer 404 on the same ids, which makes delete the outlier rather than an idempotent contract.
        var keeper = await CreateThreadAsync("keeper");
        var doomed = await CreateThreadAsync("doomed");
        (await _aiThreads.AiThreadsDeleteAsync(doomed.ThreadId, TestContext.Current.CancellationToken)).Success.Should().BeTrue();

        var deletedTwice = await Record.ExceptionAsync(
            async () => await _aiThreads.AiThreadsDeleteAsync(doomed.ThreadId, TestContext.Current.CancellationToken));

        // Everything below holds whichever status delete settles on, so a fix cannot pass for the wrong reason.
        (await Assert.ThrowsAsync<ApiException>(async () => await ActAsync("clear-messages", doomed.ThreadId)))
            .ErrorCode.Should().Be(404, "clear-messages says 404 on the same deleted id");
        (await ListThreadIdsAsync()).Should().BeEquivalentTo(new[] { keeper.ThreadId }, "nothing was created or destroyed by any of it");

        deletedTwice.Should().BeOfType<ApiException>("deleting an already deleted thread is refused")
            .Which.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task Delete_ThenEveryRoute_TreatsTheThreadAsGone()
    {
        // Deleting from the menu has to be final: the id of a deleted thread behaves like one that never existed. A
        // second delete is left out — that is the bug test above.
        var doomed = await CreateThreadAsync("doomed");
        var keeper = await CreateThreadAsync("keeper");
        await AppendUserMessageAsync(doomed.ThreadId, "Something to lose with the thread");

        (await _aiThreads.AiThreadsDeleteAsync(doomed.ThreadId, TestContext.Current.CancellationToken)).Success.Should().BeTrue();

        var read = await Assert.ThrowsAsync<ApiException>(async () => await GetThreadAsync(doomed.ThreadId));
        read.ErrorCode.Should().Be(404);
        read.ErrorContent?.ToString().Should().Contain("thread not found");

        var (status, messages) = await TryReadMessagesRawAsync(doomed.ThreadId);
        status.Should().Be(HttpStatusCode.NotFound);
        messages.Should().BeEmpty("its messages went with it");

        foreach (var route in new[] { "rename", "clear-messages" })
        {
            (await Assert.ThrowsAsync<ApiException>(async () => await ActAsync(route, doomed.ThreadId)))
                .ErrorCode.Should().Be(404, $"{route} on a deleted thread");
        }

        (await GetThreadAsync(keeper.ThreadId)).Title.Should().Be("keeper", "nothing of this reached the thread next to it");
        (await ListThreadIdsAsync()).Should().BeEquivalentTo(new[] { keeper.ThreadId }, "and the deleted one did not reappear");
    }

    [Fact]
    public async Task ClearMessages_EmptyAndTwice_KeepsTheThreadWhole()
    {
        // The menu entry fires without knowing whether the thread holds anything, so an empty thread and a repeat are
        // ordinary successes — and neither may take the thread, its title or its model with it.
        var profile = await GetDefaultProfileAsync();
        var thread = await CreateThreadAsync("cleared twice", profile.Id);

        (await ClearAsync(thread.ThreadId)).Success.Should().BeTrue();
        await AssertIntactAsync("after clearing a thread that was already empty");

        await AppendUserMessageAsync(thread.ThreadId, "Something to clear");
        (await ReadMessagesRawAsync(thread.ThreadId)).Should().HaveCount(1);

        (await ClearAsync(thread.ThreadId)).Success.Should().BeTrue();
        await AssertIntactAsync("after clearing a thread that held a message");

        (await ClearAsync(thread.ThreadId)).Success.Should().BeTrue();
        await AssertIntactAsync("after clearing the same thread again");

        (await ListThreadIdsAsync()).Should().BeEquivalentTo(new[] { thread.ThreadId }, "it is still listed, not an orphan");

        async Task AssertIntactAsync(string when)
        {
            (await ReadMessagesRawAsync(thread.ThreadId)).Should().BeEmpty($"{when}: no messages");

            var stored = await GetThreadAsync(thread.ThreadId);
            stored.ThreadId.Should().Be(thread.ThreadId, $"{when}: the same thread");
            stored.Title.Should().Be("cleared twice", $"{when}: its title");
            stored.ProfileId.Should().Be(profile.Id, $"{when}: its model");
        }
    }

    private Task<AiThread> GetThreadAsync(string threadId) =>
        _aiThreads.AiThreadsGetByIdAsync(threadId, TestContext.Current.CancellationToken);

    private Task<AiSuccessResponse> RenameAsync(string threadId, string title) =>
        _aiThreads.AiThreadsRenameAsync(new AiThreadsRenameRequest(threadId, title), TestContext.Current.CancellationToken);

    private Task<AiSuccessResponse> ClearAsync(string threadId) =>
        _aiThreads.AiThreadsClearMessagesAsync(threadId, TestContext.Current.CancellationToken);

    private async Task<List<string>> ListThreadIdsAsync() =>
        [.. (await _aiThreads.AiThreadsListAsync(cancellationToken: TestContext.Current.CancellationToken)).Select(t => t.ThreadId)];

    private Task<AiSuccessResponse> ActAsync(string route, string threadId) => route switch
    {
        "rename" => RenameAsync(threadId, "renamed"),
        "delete" => _aiThreads.AiThreadsDeleteAsync(threadId, TestContext.Current.CancellationToken),
        "clear-messages" => ClearAsync(threadId),
        _ => throw new ArgumentOutOfRangeException(nameof(route), route, null)
    };
}

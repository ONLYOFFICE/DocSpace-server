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

namespace ASC.AI.Tests.Tests.ThreadStorageTests;

/// <summary>
/// <c>DELETE /api/2.0/ai/threads/delete</c> and <c>DELETE /api/2.0/ai/threads/clear-messages</c> beyond
/// <see cref="ThreadDeleteTests"/>: what each takes and what it leaves, and the bodies they refuse.
/// </summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "AI/Threads")]
public class ThreadsDeleteClearTests(AspireAppFixture fixture) : ThreadsPortTestBase(fixture)
{
    private const string DeletePath = PublicThreadsPath + "/delete";
    private const string ClearMessagesPath = PublicThreadsPath + "/clear-messages";

    [Fact]
    public async Task Delete_ThreadWithHistory_TakesItAndItsMessagesAndNoOtherThread()
    {
        var doomed = await CreateOwnThreadAsync("Doomed");
        var survivor = await CreateOwnThreadAsync("Survivor");
        var doomedMessage = await AppendUserMessageAsync(doomed.ThreadId, "gone with the thread");
        await AppendUserMessageAsync(survivor.ThreadId, "stays");
        var survivorBefore = await SnapshotAsync(survivor.ThreadId);
        var emptyDoomed = await CreateOwnThreadAsync("Empty doomed");

        var result = await _aiThreads.AiThreadsDeleteAsync(doomed.ThreadId, TestContext.Current.CancellationToken);
        var emptyResult = await _aiThreads.AiThreadsDeleteAsync(emptyDoomed.ThreadId, TestContext.Current.CancellationToken);

        result.Success.Should().BeTrue();
        emptyResult.Success.Should().BeTrue("an empty thread deletes too");
        await ExpectOnlyThreadsAsync([survivor.ThreadId], "only the survivor is listed");
        (await SnapshotAsync(survivor.ThreadId)).Should().Be(survivorBefore, "the survivor is untouched");

        (await StatusOfAsync(() => GetThreadAsync(doomed.ThreadId))).Should().Be(404);
        (await StatusOfAsync(() => ReadAllAsync(doomed.ThreadId))).Should().Be(404);
        (await StatusOfAsync(() => _aiThreads.AiThreadsGetMessageByIdAsync(doomedMessage, TestContext.Current.CancellationToken)))
            .Should().Be(404, "its message is no longer served");
    }

    // An unknown thread id is in ThreadDeleteTests.
    [Theory]
    [InlineData(null)]
    [InlineData("{}")]
    public async Task Delete_NoThreadId_Returns400AndDeletesNothing(string? json)
    {
        var keeper = await CreateOwnThreadAsync("Keeper");

        // Raw on purpose: the SDK always sends the id as the body, so neither no body nor one without an id can be
        // produced through it.
        var (status, body) = await SendRawAsync(HttpMethod.Delete, DeletePath, json);

        await ExpectOnlyThreadsAsync([keeper.ThreadId], "nothing was deleted");
        status.Should().Be(HttpStatusCode.BadRequest, body);
    }

    [Fact]
    public async Task Delete_MalformedThreadId_Returns400AndDeletesNothing()
    {
        var keeper = await CreateOwnThreadAsync("Keeper");

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _aiThreads.AiThreadsDeleteAsync(MalformedId, TestContext.Current.CancellationToken));

        await ExpectOnlyThreadsAsync([keeper.ThreadId], "nothing was deleted");
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task ClearMessages_EmptiesTheThreadEverywhere_KeepsItsIdentity_AndStartsANewHistory()
    {
        var thread = await CreateOwnThreadAsync("To clear");
        var other = await CreateOwnThreadAsync("Not cleared");
        var oldIds = new List<string>();
        foreach (var text in new[] { "old one", "old two", "old three" })
        {
            oldIds.Add(await AppendUserMessageAsync(thread.ThreadId, text));
        }

        await AppendUserMessageAsync(other.ThreadId, "bystander message");
        var otherBefore = await SnapshotAsync(other.ThreadId);
        var identityBefore = await GetThreadAsync(thread.ThreadId);

        var result = await _aiThreads.AiThreadsClearMessagesAsync(thread.ThreadId, TestContext.Current.CancellationToken);

        result.Success.Should().BeTrue();

        // Every read path is empty.
        (await ReadAllAsync(thread.ThreadId)).Should().BeEmpty();
        (await OpenAsync(thread.ThreadId)).PriorMessages.Should().BeEmpty();
        foreach (var messageId in oldIds)
        {
            (await StatusOfAsync(() => _aiThreads.AiThreadsGetMessageByIdAsync(messageId, TestContext.Current.CancellationToken)))
                .Should().Be(404, $"message {messageId} is gone");
        }

        // The thread itself survives with the same identity.
        var identityAfter = await GetThreadAsync(thread.ThreadId);
        identityAfter.Title.Should().Be(identityBefore.Title);
        identityAfter.ProfileId.Should().Be(identityBefore.ProfileId);
        await ExpectOnlyThreadsAsync([thread.ThreadId, other.ThreadId], "still listed");

        // A bystander thread is untouched.
        (await SnapshotAsync(other.ThreadId)).Should().Be(otherBefore);

        // New messages build a fresh history without the old one.
        await AppendUserMessageAsync(thread.ThreadId, "brand new");
        (await ReadTextsAsync(thread.ThreadId)).Should().Equal("brand new");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("{}")]
    public async Task ClearMessages_NoThreadId_Returns400AndClearsNothing(string? json)
    {
        var thread = await CreateOwnThreadAsync("Full thread");
        await AppendUserMessageAsync(thread.ThreadId, "must survive");
        var before = await SnapshotAsync(thread.ThreadId);

        // Raw on purpose: the SDK always sends the id as the body, so neither no body nor one without an id can be
        // produced through it.
        var (status, body) = await SendRawAsync(HttpMethod.Delete, ClearMessagesPath, json);

        (await SnapshotAsync(thread.ThreadId)).Should().Be(before);
        status.Should().Be(HttpStatusCode.BadRequest, body);
    }
}

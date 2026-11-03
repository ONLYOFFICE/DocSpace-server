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
/// <c>PUT /api/2.0/ai/threads/rename</c> beyond <see cref="ThreadUpdateTests"/> and
/// <see cref="ThreadTitleLengthTests"/>: a rename changes the title and nothing else, repeats and duplicate titles
/// work, titles are stored verbatim, and a refused body or thread id leaves the name alone.
/// </summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "AI/Threads")]
public class ThreadsRenameTests(AspireAppFixture fixture) : ThreadsPortTestBase(fixture)
{
    private const string RenamePath = PublicThreadsPath + "/rename";

    public static TheoryData<string> AwkwardTitles => new()
    {
        "Я",
        "Привет, мир — отчёт №1",
        "Plan 😀🚀 done",
        """He said "hi" <b>&amp;</b> 'x' % _ ; \ /""",
        new string('t', 255)
    };

    [Fact]
    public async Task Rename_Owner_ChangesTheTitleAndNothingElse()
    {
        var thread = await CreateOwnThreadAsync("Before");
        var sibling = await CreateOwnThreadAsync("Sibling");
        await AppendUserMessageAsync(thread.ThreadId, "kept through the rename");
        var before = await GetThreadAsync(thread.ThreadId);
        var messagesBefore = (await ReadAllAsync(thread.ThreadId)).Select(m => m.ToJson()).ToList();
        var siblingBefore = await SnapshotAsync(sibling.ThreadId);

        var result = await RenameAsync(thread.ThreadId, "After");

        result.Success.Should().BeTrue();
        var after = await GetThreadAsync(thread.ThreadId);
        after.Title.Should().Be("After");
        after.ThreadId.Should().Be(thread.ThreadId);
        after.ProfileId.Should().Be(before.ProfileId);
        (await ReadAllAsync(thread.ThreadId)).Select(m => m.ToJson()).Should().Equal(messagesBefore, "the history is untouched");
        (await SnapshotAsync(sibling.ThreadId)).Should().Be(siblingBefore, "a sibling is untouched");
        (await ListThreadsAsync()).Single(t => t.ThreadId == thread.ThreadId).Title.Should().Be("After");
    }

    [Fact]
    public async Task Rename_Twice_ToTheSameTitle_AndToASiblingsTitle_AllWork()
    {
        var a = await CreateOwnThreadAsync("A");
        var b = await CreateOwnThreadAsync("B");

        (await RenameAsync(a.ThreadId, "Second")).Success.Should().BeTrue();
        (await RenameAsync(a.ThreadId, "Third")).Success.Should().BeTrue();
        (await GetThreadAsync(a.ThreadId)).Title.Should().Be("Third");

        (await RenameAsync(a.ThreadId, "Third")).Success.Should().BeTrue("renaming to the title it has");
        (await GetThreadAsync(a.ThreadId)).Title.Should().Be("Third");

        (await RenameAsync(b.ThreadId, "Third")).Success.Should().BeTrue("renaming to a sibling's title");
        (await ListThreadsAsync()).Select(t => t.Title).Should().Equal("Third", "Third");
    }

    [Theory]
    [MemberData(nameof(AwkwardTitles))]
    public async Task Rename_AwkwardTitle_IsStoredVerbatim(string title)
    {
        var thread = await CreateOwnThreadAsync("Plain");

        (await RenameAsync(thread.ThreadId, title)).Success.Should().BeTrue();

        (await GetThreadAsync(thread.ThreadId)).Title.Should().Be(title);
    }

    /// <summary>
    /// Bodies the typed request cannot carry. <c>{threadId}</c> is replaced with the live thread; null sends no body.
    /// Blank and over-long titles are in <see cref="ThreadUpdateTests"/> / <see cref="ThreadTitleLengthTests"/>.
    /// The numeric and object titles stay: the type check is the route's own (<c>threadsController.rename</c>).
    /// </summary>
    [Theory]
    [InlineData("""{ "threadId": "{threadId}" }""")]
    [InlineData("""{ "threadId": "{threadId}", "title": null }""")]
    [InlineData("""{ "threadId": "{threadId}", "title": 123 }""")]
    [InlineData("""{ "threadId": "{threadId}", "title": { "a": 1 } }""")]
    [InlineData("""{ "title": "X" }""")]
    [InlineData("{}")]
    [InlineData(null)]
    public async Task Rename_RefusedBody_Returns400AndLeavesTheNameAlone(string? template)
    {
        var thread = await CreateOwnThreadAsync("Untouchable");

        // Raw on purpose: threadId and title are required constructor parameters of AiThreadsRenameRequest, so the
        // SDK can send neither a missing nor a null nor a wrong-typed one, nor no body at all.
        var (status, body) = await SendRawAsync(HttpMethod.Put, RenamePath, template?.Replace("{threadId}", thread.ThreadId));

        // Side effects first, the status after.
        (await GetThreadAsync(thread.ThreadId)).Title.Should().Be("Untouchable");
        await ExpectOnlyThreadsAsync([thread.ThreadId], "no thread appeared or vanished");
        status.Should().Be(HttpStatusCode.BadRequest, body);
    }

    // An unknown thread id is in ThreadUpdateTests.
    [Theory]
    [InlineData("malformed", 400)]
    [InlineData("deleted", 404)]
    public async Task Rename_BadThreadId_IsRefusedAndLeavesTheNameAlone(string kind, int expected)
    {
        var thread = await CreateOwnThreadAsync("Untouchable");
        var threadId = kind == "malformed" ? MalformedId : await CreateDeletedThreadAsync();

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await RenameAsync(threadId, "X"));

        (await GetThreadAsync(thread.ThreadId)).Title.Should().Be("Untouchable");
        await ExpectOnlyThreadsAsync([thread.ThreadId], "no thread appeared or vanished");
        exception.ErrorCode.Should().Be(expected);
    }

    private Task<AiSuccessResponse> RenameAsync(string threadId, string title) =>
        _aiThreads.AiThreadsRenameAsync(new AiThreadsRenameRequest(threadId, title), TestContext.Current.CancellationToken);
}

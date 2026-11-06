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
/// <c>POST /api/2.0/ai/threads/touch</c> beyond <see cref="ThreadTouchTests"/>: a touch moves only the date, never
/// back, and a bad or missing id is refused without changing anything.
/// </summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "AI/Threads")]
public class ThreadsTouchTests(AspireAppFixture fixture) : ThreadsPortTestBase(fixture)
{
    private const string TouchPath = PublicThreadsPath + "/touch";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Touch_AdvancesLastEditDate_AndChangesNothingElse(bool withHistory)
    {
        var thread = await CreateOwnThreadAsync(withHistory ? "With history" : "Empty");
        if (withHistory)
        {
            await AppendUserMessageAsync(thread.ThreadId, "one");
            await AppendUserMessageAsync(thread.ThreadId, "two");
        }

        var before = await GetThreadAsync(thread.ThreadId);
        var messagesBefore = (await ReadAllAsync(thread.ThreadId)).Select(m => m.ToJson()).ToList();

        // lastEditDate has one-second granularity, so a touch in the same second as the previous edit is invisible:
        // touch until the clock has moved on.
        var after = await TouchUntilAsync(thread.ThreadId, t => t.LastEditDate > before.LastEditDate);

        after.LastEditDate.Should().BeGreaterThan(before.LastEditDate, "lastEditDate moved forward");
        after.Title.Should().Be(before.Title);
        after.ProfileId.Should().Be(before.ProfileId);
        (await ReadAllAsync(thread.ThreadId)).Select(m => m.ToJson()).Should().Equal(messagesBefore, "the history is untouched");
    }

    [Fact]
    public async Task Touch_NeverMovesLastEditDateBack_AndARepeatIsHarmless()
    {
        var thread = await CreateOwnThreadAsync("Touched twice");

        var dates = new List<decimal> { (await GetThreadAsync(thread.ThreadId)).LastEditDate };
        for (var round = 0; round < 3; round++)
        {
            (await TouchAsync(thread.ThreadId)).Success.Should().BeTrue();
            dates.Add((await GetThreadAsync(thread.ThreadId)).LastEditDate);
        }

        dates.Should().BeInAscendingOrder();
        await ExpectOnlyThreadsAsync([thread.ThreadId], "no thread was added");
    }

    // An unknown thread id is in ThreadTouchTests.
    [Theory]
    [InlineData("malformed", 400)]
    [InlineData("deleted", 404)]
    public async Task Touch_BadThreadId_IsRefusedAndNothingChanges(string kind, int expected)
    {
        var thread = await CreateOwnThreadAsync("Bystander");
        var threadId = kind == "malformed" ? MalformedId : await CreateDeletedThreadAsync();
        var before = await SnapshotAsync(thread.ThreadId);

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await TouchAsync(threadId));

        (await SnapshotAsync(thread.ThreadId)).Should().Be(before);
        await ExpectOnlyThreadsAsync([thread.ThreadId], "nothing appeared or vanished");
        exception.ErrorCode.Should().Be(expected);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData(null)]
    public async Task Touch_MissingThreadId_Returns400AndNothingChanges(string? json)
    {
        var thread = await CreateOwnThreadAsync("Bystander");
        var before = await SnapshotAsync(thread.ThreadId);

        // Raw on purpose: threadId is a required constructor parameter of AiThreadsTouchRequest, so the SDK can send
        // neither a body without it nor no body at all.
        var (status, body) = await SendRawAsync(HttpMethod.Post, TouchPath, json);

        (await SnapshotAsync(thread.ThreadId)).Should().Be(before);
        await ExpectOnlyThreadsAsync([thread.ThreadId], "nothing appeared or vanished");
        status.Should().Be(HttpStatusCode.BadRequest, body);
    }

    private Task<AiSuccessResponse> TouchAsync(string threadId) =>
        _aiThreads.AiThreadsTouchAsync(new AiThreadsTouchRequest(threadId), TestContext.Current.CancellationToken);

    /// <summary>Touches until <paramref name="until"/> holds or the deadline passes; returns the last thread read.</summary>
    private async Task<AiThread> TouchUntilAsync(string threadId, Func<AiThread, bool> until)
    {
        var deadline = DateTime.UtcNow.AddSeconds(20);

        while (true)
        {
            (await TouchAsync(threadId)).Success.Should().BeTrue("every touch answers success");
            var thread = await GetThreadAsync(threadId);

            if (until(thread) || DateTime.UtcNow >= deadline)
            {
                return thread;
            }

            await Task.Delay(700, TestContext.Current.CancellationToken);
        }
    }
}

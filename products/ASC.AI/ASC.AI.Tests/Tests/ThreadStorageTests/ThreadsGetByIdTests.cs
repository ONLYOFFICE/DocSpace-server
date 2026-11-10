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
/// <c>GET /api/2.0/ai/threads/get-by-id</c>: the thread follows every change made to it, reading changes nothing,
/// and a deleted, malformed, empty or missing id is refused. An unknown id is in <see cref="ThreadReadTests"/>.
/// </summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "AI/Threads")]
public class ThreadsGetByIdTests(AspireAppFixture fixture) : ThreadsPortTestBase(fixture)
{
    [Fact]
    public async Task GetById_FollowsEveryChange_AndReadingChangesNothing()
    {
        var profile = await GetProfileAsync();
        var thread = await CreateOwnThreadAsync("Original");

        var fresh = await GetThreadAsync(thread.ThreadId);
        fresh.ThreadId.Should().Be(thread.ThreadId);
        fresh.Title.Should().Be("Original");
        fresh.ProfileId.Should().Be(profile.Id);
        fresh.LastEditDate.Should().BeGreaterThan(0);

        // Repeated reads are identical.
        (await GetThreadAsync(thread.ThreadId)).ToJson().Should().Be(fresh.ToJson());
        (await GetThreadAsync(thread.ThreadId)).ToJson().Should().Be(fresh.ToJson());

        // A thread with history answers the same fields.
        await AppendUserMessageAsync(thread.ThreadId, "hello");
        var withHistory = await GetThreadAsync(thread.ThreadId);
        withHistory.ThreadId.Should().Be(thread.ThreadId);
        withHistory.Title.Should().Be("Original");
        withHistory.ProfileId.Should().Be(profile.Id);

        // After a rename.
        (await _aiThreads.AiThreadsRenameAsync(new AiThreadsRenameRequest(thread.ThreadId, "Renamed"), TestContext.Current.CancellationToken))
            .Success.Should().BeTrue();
        var renamed = await GetThreadAsync(thread.ThreadId);
        renamed.Title.Should().Be("Renamed");
        renamed.ThreadId.Should().Be(thread.ThreadId);
        renamed.ProfileId.Should().Be(profile.Id);

        // After clear-messages the thread is still there.
        (await _aiThreads.AiThreadsClearMessagesAsync(thread.ThreadId, TestContext.Current.CancellationToken)).Success.Should().BeTrue();
        var cleared = await GetThreadAsync(thread.ThreadId);
        cleared.Title.Should().Be("Renamed");
        cleared.ProfileId.Should().Be(profile.Id);
    }

    [Theory]
    [InlineData("deleted", 404)]
    [InlineData("malformed", 400)]
    [InlineData("empty", 400)]
    public async Task GetById_BadId_IsRefused(string kind, int expected)
    {
        var keeper = await CreateOwnThreadAsync("Keeper");
        var threadId = kind switch
        {
            "deleted" => await CreateDeletedThreadAsync(),
            "malformed" => MalformedId,
            _ => string.Empty
        };

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await GetThreadAsync(threadId));

        // Positive control: the same route still serves a live thread.
        (await GetThreadAsync(keeper.ThreadId)).Title.Should().Be("Keeper");
        exception.ErrorCode.Should().Be(expected);
    }

    [Fact]
    public async Task GetById_MissingThreadId_Returns400()
    {
        var keeper = await CreateOwnThreadAsync("Keeper");

        // Raw on purpose: threadId is a required parameter of AiThreadsGetByIdAsync, so the SDK cannot leave it out.
        var (status, body) = await SendRawAsync(HttpMethod.Get, $"{PublicThreadsPath}/get-by-id");

        (await GetThreadAsync(keeper.ThreadId)).Title.Should().Be("Keeper");
        status.Should().Be(HttpStatusCode.BadRequest, body);
    }
}

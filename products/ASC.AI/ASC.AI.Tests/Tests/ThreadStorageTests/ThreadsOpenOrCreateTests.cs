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
/// <c>POST /api/2.0/ai/threads/open-or-create</c>: opening replays exactly what read-messages holds and writes
/// nothing; a bad thread id is refused without creating anything; with no thread id a thread is created.
/// </summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "AI/Threads")]
public class ThreadsOpenOrCreateTests(AspireAppFixture fixture) : ThreadsPortTestBase(fixture)
{
    private const string OpenOrCreatePath = PublicThreadsPath + "/open-or-create";

    [Fact]
    public async Task OpenOrCreate_ExistingThread_ReplaysTheHistoryAndWritesNothing()
    {
        var thread = await CreateOwnThreadAsync("Replay me");
        var bystander = await CreateOwnThreadAsync("Bystander");
        string[] texts = ["первый", "second\nwith a second line", "третий 😀"];
        foreach (var text in texts)
        {
            await AppendUserMessageAsync(thread.ThreadId, text);
        }

        var before = await SnapshotAsync(thread.ThreadId);
        var bystanderBefore = await SnapshotAsync(bystander.ThreadId);
        var stored = await ReadAllAsync(thread.ThreadId);

        var first = await OpenAsync(thread.ThreadId);
        var second = await OpenAsync(thread.ThreadId);

        first.ThreadId.Should().Be(thread.ThreadId);
        first.PriorMessages.Select(m => m.Id).Should().Equal(stored.Select(m => m.Id), "same ids, same order as read-messages");
        first.PriorMessages.Select(TextOf).Should().Equal(texts);
        second.PriorMessages.Select(m => m.ToJson()).Should().Equal(first.PriorMessages.Select(m => m.ToJson()), "a repeat open replays the same");

        // Read-only: nothing was stored (the first message included), nothing duplicated.
        (await SnapshotAsync(thread.ThreadId)).Should().Be(before);
        (await SnapshotAsync(bystander.ThreadId)).Should().Be(bystanderBefore);
        await ExpectOnlyThreadsAsync([thread.ThreadId, bystander.ThreadId], "no thread was added");
    }

    [Fact]
    public async Task OpenOrCreate_EmptyAndSingleMessageThreads_ReplayAsSuch()
    {
        var empty = await CreateOwnThreadAsync("Empty");
        var single = await CreateOwnThreadAsync("Single");
        var messageId = await AppendUserMessageAsync(single.ThreadId, "only one");

        (await OpenAsync(empty.ThreadId)).PriorMessages.Should().BeEmpty();
        (await OpenAsync(single.ThreadId)).PriorMessages.Select(m => m.Id).Should().Equal(messageId);
    }

    // A numeric threadId is not ported: rejecting a wrong-typed JSON value is the framework's model binding.
    [Theory]
    [InlineData("malformed", 400)]
    [InlineData("unknown", 404)]
    [InlineData("deleted", 404)]
    public async Task OpenOrCreate_BadThreadId_IsRefusedAndCreatesNothing(string kind, int expected)
    {
        var keeper = await CreateOwnThreadAsync("Keeper");
        var threadId = kind switch
        {
            "malformed" => MalformedId,
            "unknown" => UnknownId,
            _ => await CreateDeletedThreadAsync()
        };

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await OpenAsync(threadId));

        await ExpectOnlyThreadsAsync([keeper.ThreadId], "no refusal created a thread");
        exception.ErrorCode.Should().Be(expected);
    }

    [Fact]
    public async Task OpenOrCreate_EmptyBody_Returns404AndCreatesNothing()
    {
        var keeper = await CreateOwnThreadAsync("Keeper");

        // Raw on purpose: profile, profileId and firstMessage are required constructor parameters of
        // AiThreadsOpenOrCreateRequest, so the SDK cannot send an empty object.
        var (status, body) = await SendRawAsync(HttpMethod.Post, OpenOrCreatePath, "{}");

        await ExpectOnlyThreadsAsync([keeper.ThreadId], "nothing was created");
        // The route checks the profile object first: "an AI profile is required to open or create a thread".
        status.Should().Be(HttpStatusCode.NotFound, body);
    }

    /// <summary>
    /// With no thread id the route creates a thread. It used to crash on the create half instead (a TypeError on
    /// the missing profile, collapsed to a 500).
    /// </summary>
    /// <remarks>
    /// The TypeScript case also expected the first message to be stored. The engine documents the opposite
    /// (<c>ThreadsEngine.openOrCreate</c> "does not persist firstMessage" — the caller appends it), so only the
    /// thread itself is asserted.
    /// </remarks>
    [Fact]
    [Trait("Bug", "82826")]
    public async Task OpenOrCreate_NoThreadIdWithFirstMessage_CreatesAThread()
    {
        var profile = await GetProfileAsync();

        var result = await _aiThreads.AiThreadsOpenOrCreateAsync(
            new AiThreadsOpenOrCreateRequest(
                profile: profile,
                profileId: profile.Id,
                firstMessage: BuildUserMessage("First words")),
            TestContext.Current.CancellationToken);

        IsUuid(result.ThreadId).Should().BeTrue(result.ToJson());
        result.PriorMessages.Should().BeEmpty("a thread that was just created has no history");
        (await ListIdsAsync()).Should().Equal(result.ThreadId);
        (await GetThreadAsync(result.ThreadId)).ProfileId.Should().Be(profile.Id);
    }

    /// <summary>
    /// Without a <c>firstMessage</c> the create half still crashes: the engine generates the title from
    /// <c>[firstMessage]</c>, reads <c>role</c> of the missing message (a TypeError) and the route answers 500
    /// <c>{"error":"Internal server error"}</c>; nothing is created.
    /// </summary>
    [Fact]
    [Trait("Bug", "82826")]
    public async Task OpenOrCreate_NoThreadIdNoFirstMessage_CreatesAnEmptyThread()
    {
        var profile = await GetProfileAsync();

        // Raw on purpose: firstMessage is a required constructor parameter of AiThreadsOpenOrCreateRequest, so the
        // SDK cannot leave it out. The profile is the SDK model all the same.
        var (status, body) = await SendRawAsync(
            HttpMethod.Post,
            OpenOrCreatePath,
            $$"""{ "profile": {{profile.ToJson()}}, "profileId": "{{profile.Id}}" }""");

        // Whatever the route answers, it must not have half-created anything.
        var listed = await ListIdsAsync();

        status.Should().Be(HttpStatusCode.OK, body);
        using var document = JsonDocument.Parse(body);
        var threadId = document.RootElement.GetProperty("threadId").GetString()!;
        IsUuid(threadId).Should().BeTrue(body);
        listed.Should().Equal(threadId);
    }
}

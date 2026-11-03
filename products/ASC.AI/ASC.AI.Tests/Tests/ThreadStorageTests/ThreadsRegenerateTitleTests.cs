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
/// <c>POST /api/2.0/ai/threads/regenerate-title</c>: a bad thread id or an incomplete body is refused and the title
/// stays; a valid request returns the generated title, stores it and can be repeated. The title is what the stub
/// provider answers to a non-streamed request — its wording is not asserted, only that the server stores and lists
/// what it returned.
/// </summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "AI/Threads")]
public class ThreadsRegenerateTitleTests(AspireAppFixture fixture) : ThreadsPortTestBase(fixture)
{
    private const string RegenerateTitlePath = PublicThreadsPath + "/regenerate-title";

    [Theory]
    [InlineData("malformed", 400)]
    [InlineData("unknown", 404)]
    [InlineData("deleted", 404)]
    public async Task RegenerateTitle_BadThreadId_IsRefusedAndTheTitleStays(string kind, int expected)
    {
        var thread = await CreateOwnThreadAsync("Keep my name");
        await AppendUserMessageAsync(thread.ThreadId, "Something to title");
        var threadId = kind switch
        {
            "malformed" => MalformedId,
            "unknown" => UnknownId,
            _ => await CreateDeletedThreadAsync()
        };

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await RegenerateAsync(threadId));

        (await GetThreadAsync(thread.ThreadId)).Title.Should().Be("Keep my name");
        exception.ErrorCode.Should().Be(expected);
    }

    /// <summary>
    /// Incomplete bodies: <c>{threadId}</c> is replaced with the live thread, <c>{profile}</c> with the profile
    /// object, <c>{profileId}</c> with its id.
    /// </summary>
    [Theory]
    [InlineData("""{ "profile": {profile} }""")]
    [InlineData("""{ "threadId": "{threadId}" }""")]
    [InlineData("""{ "threadId": "{threadId}", "profileId": "{profileId}" }""")]
    [InlineData("{}")]
    public async Task RegenerateTitle_IncompleteBody_Returns400AndTheTitleStays(string template)
    {
        var profile = await GetProfileAsync();
        var thread = await CreateOwnThreadAsync("Keep my name");
        await AppendUserMessageAsync(thread.ThreadId, "Something to title");

        // Raw on purpose: threadId and profile are required constructor parameters of AiThreadsRegenerateTitleRequest,
        // so the SDK cannot leave either out. The profile object is the SDK model all the same.
        var json = template
            .Replace("{threadId}", thread.ThreadId)
            .Replace("{profileId}", profile.Id)
            .Replace("{profile}", profile.ToJson());
        var (status, body) = await SendRawAsync(HttpMethod.Post, RegenerateTitlePath, json);

        (await GetThreadAsync(thread.ThreadId)).Title.Should().Be("Keep my name");
        status.Should().Be(HttpStatusCode.BadRequest, body);
    }

    /// <summary>
    /// The route used to dereference the profile without checking it and answered 500. It now validates the body and
    /// returns the title the model generated, which is what the thread is then called.
    /// </summary>
    [Fact]
    [Trait("Bug", "82828")]
    public async Task RegenerateTitle_ReturnsStoresAndListsTheGeneratedTitle_AndCanRepeat()
    {
        var thread = await CreateOwnThreadAsync("Title me");
        var round = await SendWithStreamAsync(thread.ThreadId, "Explain gravity in one sentence.", thread.ProfileId);
        round.StatusCode.Should().Be(HttpStatusCode.OK, round.ToString());
        (await ReadAllAsync(thread.ThreadId)).Should().Contain(m => m.Role == AiThreadMessageLike.RoleEnum.Assistant, round.ToString());

        var first = await RegenerateAsync(thread.ThreadId);
        var stored = (await GetThreadAsync(thread.ThreadId)).Title;
        var listed = (await ListThreadsAsync()).Single(t => t.ThreadId == thread.ThreadId).Title;
        var second = await RegenerateAsync(thread.ThreadId);

        first.Title.Should().NotBeNullOrWhiteSpace("a non-empty title comes back");
        first.Title.Should().NotBe("Title me");
        stored.Should().Be(first.Title, "get-by-id carries the generated title");
        listed.Should().Be(first.Title, "the list carries it too");
        second.Title.Should().NotBeNullOrWhiteSpace("a repeat regenerate works");
    }

    private async Task<AiThreadsRegenerateTitle200Response> RegenerateAsync(string threadId) =>
        await _aiThreads.AiThreadsRegenerateTitleAsync(
            new AiThreadsRegenerateTitleRequest(threadId, await GetProfileAsync()),
            TestContext.Current.CancellationToken);
}

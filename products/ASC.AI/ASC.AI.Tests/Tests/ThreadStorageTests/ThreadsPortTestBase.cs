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
/// Helpers of the ThreadsApi suites ported from the TypeScript threads specs: snapshots of a thread's whole
/// observable state for "nothing changed" checks, the message reads, and raw requests for the bodies the typed
/// SDK cannot carry (a missing body, a mistyped field).
/// </summary>
/// <remarks>
/// The TypeScript suite scoped every thread to an agent and listed by that agent. Threads are one list per user
/// by design (Bug 82719): the chat service ignores the <c>entityId</c> of the list, so the threads here are
/// created without one and "the list" is the caller's whole list — which, on a portal of the test's own, holds
/// exactly the threads the test created.
/// </remarks>
public abstract class ThreadsPortTestBase(AspireAppFixture fixture) : ThreadTestBase(fixture)
{
    /// <summary>Well-formed, never issued.</summary>
    protected const string UnknownId = "019ed117-0000-7000-8000-000000000000";

    protected const string MalformedId = "not-a-guid";

    /// <summary>
    /// A thread's complete observable state: what get-by-id answers and every stored message, as JSON, so two
    /// snapshots compare as strings and a difference shows up in the assertion message.
    /// </summary>
    protected async Task<string> SnapshotAsync(string threadId)
    {
        var thread = await GetThreadAsync(threadId);
        var messages = await ReadAllAsync(threadId);

        return thread.ToJson() + "\n" + string.Join("\n", messages.Select(m => m.ToJson()));
    }

    protected Task<List<AiThreadMessageLike>> ReadAllAsync(string threadId) =>
        _aiThreads.AiThreadsReadMessagesAsync(threadId, cancellationToken: TestContext.Current.CancellationToken);

    protected async Task<List<string?>> ReadTextsAsync(string threadId) =>
        (await ReadAllAsync(threadId)).Select(TextOf).ToList();

    /// <summary>The ids of the caller's whole thread list, newest first.</summary>
    protected async Task<List<string>> ListIdsAsync() => (await ListThreadsAsync()).Select(t => t.ThreadId).ToList();

    protected async Task ExpectOnlyThreadsAsync(IEnumerable<string> expected, string because)
    {
        (await ListIdsAsync()).Should().BeEquivalentTo(expected, because);
    }

    /// <summary>Creates a thread and deletes it again; returns the id of the deleted thread.</summary>
    protected async Task<string> CreateDeletedThreadAsync()
    {
        var doomed = await CreateOwnThreadAsync("Doomed");
        (await _aiThreads.AiThreadsDeleteAsync(doomed.ThreadId, TestContext.Current.CancellationToken)).Success.Should().BeTrue();

        return doomed.ThreadId;
    }

    /// <summary>
    /// Opens an existing thread through <c>open-or-create</c> with the test's own profile. The SDK model requires a
    /// first message; for an existing thread it is ignored and never stored.
    /// </summary>
    protected async Task<AiOpenOrCreateResult> OpenAsync(string threadId, string firstMessage = "x")
    {
        var profile = await GetProfileAsync();

        return await _aiThreads.AiThreadsOpenOrCreateAsync(
            new AiThreadsOpenOrCreateRequest(
                threadId: threadId,
                profile: profile,
                profileId: profile.Id,
                firstMessage: BuildUserMessage(firstMessage)),
            TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// A raw request against the public AI surface — only for what the typed SDK cannot send: no body at all, a
    /// field of the wrong JSON type, a required field left out. <paramref name="json"/> null sends no body.
    /// </summary>
    protected async Task<(HttpStatusCode Status, string Body)> SendRawAsync(HttpMethod method, string path, string? json = null)
    {
        using var request = new HttpRequestMessage(method, path);
        if (json is not null)
        {
            request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        }

        using var response = await _aiChatClient.SendAsync(request, TestContext.Current.CancellationToken);

        return (response.StatusCode, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    /// <summary>The HTTP status an SDK call ended with: 200 when it succeeded, the <see cref="ApiException"/> code otherwise.</summary>
    protected static async Task<int> StatusOfAsync(Func<Task> call)
    {
        try
        {
            await call();
            return 200;
        }
        catch (ApiException exception)
        {
            return exception.ErrorCode;
        }
    }

    protected static bool IsUuid(string? value) => Guid.TryParse(value, out _);
}

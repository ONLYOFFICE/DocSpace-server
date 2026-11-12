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
/// Thread tests on the public surface (<c>/api/2.0/ai/threads/*</c>, the Node chat service). A public create needs a
/// model to resolve for the thread (an explicit <c>profileId</c> or the Chat assignment), so the threads here are bound
/// to a profile of the test's own, created on first use.
/// </summary>
public abstract class ThreadTestBase(AspireAppFixture fixture) : BaseTest(fixture)
{
    protected const string PublicThreadsPath = "/api/2.0/ai/threads";

    private AiProfile? _profile;

    /// <summary>The test's own profile, created through the public API on first use.</summary>
    protected async Task<AiProfile> GetProfileAsync() => _profile ??= await CreateProfileAsync();

    /// <summary>Creates a thread bound to the test's own profile.</summary>
    protected async Task<AiThread> CreateOwnThreadAsync(string? title = null) =>
        await CreateThreadAsync(title, (await GetProfileAsync()).Id);

    protected Task<AiThread> GetThreadAsync(string threadId) =>
        _aiThreads.AiThreadsGetByIdAsync(threadId, TestContext.Current.CancellationToken);

    /// <summary>
    /// One page of the public list. Without <paramref name="count"/> the chat service walks every page itself and
    /// answers the whole list; <paramref name="after"/> continues after that thread.
    /// </summary>
    protected Task<List<AiThread>> ListThreadsAsync(
        int? count = null,
        AiThread? after = null,
        string? query = null,
        string? entityId = null) =>
        _aiThreads.AiThreadsListAsync(
            entityId,
            count,
            after is null ? null : CursorAfter(after),
            query,
            TestContext.Current.CancellationToken);

    /// <summary>
    /// The public list answers a bare array with no next-page cursor: the client builds it from the last thread it
    /// received, as a JSON object passed as a string (<c>parseThreadsCursor</c> in threadsController.ts).
    /// </summary>
    protected static string CursorAfter(AiThread last) =>
        JsonSerializer.Serialize(new { id = last.ThreadId, lastEditDate = (long)last.LastEditDate });
}

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
/// The thread routes ChatAiDisabledTests does not gate-check — open-or-create, regenerate-title and the reads with
/// their paging parameters — with the portal AI switch off. Every call runs against a thread and message created while
/// AI was on: a made-up id would make a 403 indistinguishable from "that id does not exist". With the switch back on,
/// the thread must be exactly what it was.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "AI/Threads")]
public class ThreadsAiDisabledTests(AspireAppFixture fixture) : ThreadsPortTestBase(fixture)
{
    public static TheoryData<string> GatedRoutes => new()
    {
        "open-or-create",
        "regenerate-title",
        "list",
        "read-messages",
        "get-message-by-id"
    };

    [Theory]
    [MemberData(nameof(GatedRoutes))]
    public async Task Route_AiDisabled_Returns403AndWritesNothing(string route)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var profile = await GetProfileAsync();
        var thread = await CreateOwnThreadAsync("Gated thread");
        var messageId = await AppendUserMessageAsync(thread.ThreadId, "from before");
        var before = await SnapshotAsync(thread.ThreadId);
        var listBefore = (await ListThreadsAsync()).Select(t => t.ToJson()).ToList();

        await SetPortalAiAccessAsync(false);

        Func<Task> act = route switch
        {
            "open-or-create" => () => OpenAsync(thread.ThreadId, "while AI is off"),
            "regenerate-title" => () => _aiThreads.AiThreadsRegenerateTitleAsync(
                new AiThreadsRegenerateTitleRequest(thread.ThreadId, profile),
                cancellationToken),
            "list" => () => ListThreadsAsync(1, query: "Gated"),
            "read-messages" => () => _aiThreads.AiThreadsReadMessagesAsync(thread.ThreadId, 1, direction: "desc", cancellationToken: cancellationToken),
            _ => () => _aiThreads.AiThreadsGetMessageByIdAsync(messageId, cancellationToken)
        };
        var status = await StatusOfAsync(act);

        await SetPortalAiAccessAsync(true);

        (await SnapshotAsync(thread.ThreadId)).Should().Be(before);
        (await ListThreadsAsync()).Select(t => t.ToJson()).Should().Equal(listBefore, "no thread appeared");
        status.Should().Be(403);
    }
}

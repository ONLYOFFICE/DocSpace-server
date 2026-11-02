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
/// State changed from outside the conversation. The client keeps a local copy of threads and assignments and re-reads
/// them when told they are stale; what the backend owes it is that a plain re-read tells the truth.
/// </summary>
[Trait("Category", "Chat")]
[Trait("Feature", "AI/Chat")]
public class ChatOtherActorTests(AspireAppFixture fixture) : ChatPart3TestBase(fixture)
{
    [Fact]
    public async Task Assign_ChatRebound_ANewThreadTakesTheNewModelAndTheOldOneKeepsItsOwn()
    {
        // The assignment is the default for new conversations and never a retroactive setting: moving old threads
        // would rewrite history the client already rendered, not applying to new ones would make it look broken.
        var first = await CreateAssignableProfileAsync();
        var second = await CreateAssignableProfileAsync();

        await AssignAsync(AiActionType.Chat, first.Id);
        var before = await CreateThreadAsync("before the rebinding", first.Id);

        await AssignAsync(AiActionType.Chat, second.Id);
        (await GetAllAssignmentsAsync())[nameof(AiActionType.Chat)].Should().Be(second.Id, "the binding really changed");

        var after = await CreateThreadAsync("after the rebinding", second.Id);

        // One re-read of the list shows both, each on the model it was started with.
        var listed = (await _aiThreads.AiThreadsListAsync(cancellationToken: TestContext.Current.CancellationToken))
            .ToDictionary(t => t.ThreadId, t => t.ProfileId);

        listed.Should().ContainKey(before.ThreadId).WhoseValue.Should().Be(first.Id);
        listed.Should().ContainKey(after.ThreadId).WhoseValue.Should().Be(second.Id);
    }
}

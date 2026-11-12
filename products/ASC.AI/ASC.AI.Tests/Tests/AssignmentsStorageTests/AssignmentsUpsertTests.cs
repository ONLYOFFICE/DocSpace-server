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

namespace ASC.AI.Tests.Tests.AssignmentsStorageTests;

[Trait("Category", "CRUD")]
[Trait("Feature", "AI/Assignments")]
public class AssignmentsUpsertTests(AspireAppFixture fixture) : AssignmentsTestBase(fixture)
{
    [Fact]
    public async Task UpsertMany_InsertsNewRows()
    {
        var chatProfile = await CreateAssignableProfileAsync();
        var codeProfile = await CreateAssignableProfileAsync();

        var result = await _aiAssignments.AiAssignmentsBulkAssignAsync(
            new Dictionary<string, string>
            {
                [nameof(AiActionType.Chat)] = chatProfile.Id,
                [nameof(AiActionType.Code)] = codeProfile.Id
            },
            TestContext.Current.CancellationToken);

        result.Success.Should().BeTrue();

        // The first profile of the portal also holds the Default slot.
        var all = await GetAllAssignmentsAsync();
        all.Should().HaveCount(3);
        all[nameof(AiActionType.Chat)].Should().Be(chatProfile.Id);
        all[nameof(AiActionType.Code)].Should().Be(codeProfile.Id);
    }

    [Fact]
    public async Task UpsertMany_UpdatesExistingRows()
    {
        var initialChat = await CreateAssignableProfileAsync();
        var newChat = await CreateAssignableProfileAsync();
        var newCode = await CreateAssignableProfileAsync();
        await AssignAsync(AiActionType.Chat, initialChat.Id);

        var result = await _aiAssignments.AiAssignmentsBulkAssignAsync(
            new Dictionary<string, string>
            {
                [nameof(AiActionType.Chat)] = newChat.Id,
                [nameof(AiActionType.Code)] = newCode.Id
            },
            TestContext.Current.CancellationToken);

        result.Success.Should().BeTrue();

        var all = await GetAllAssignmentsAsync();
        all[nameof(AiActionType.Chat)].Should().Be(newChat.Id);
        all[nameof(AiActionType.Code)].Should().Be(newCode.Id);
    }

    // Internal contract: entity-scoped assignments have no public route (bulk-assign takes no entityId and the Node adapter drops it).
    [Fact]
    public async Task UpsertMany_WithEntityId_IsolatedFromGlobal()
    {
        var globalProfile = await CreateProfileInternalAsync();
        var scopedProfile = await CreateProfileInternalAsync();
        var roomId = await CreateRoomAsync();

        await CreateAssignmentInternalAsync("Chat", globalProfile.Id);

        using var response = await _ai.PutAsync(
            AssignmentsPath,
            new
            {
                assignments = new Dictionary<string, Guid>
                {
                    ["Chat"] = scopedProfile.Id
                },
                entityId = roomId.ToString()
            },
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        (await ReadAssignmentInternalAsync("Chat")).Should().Be(globalProfile.Id);
        (await ReadAssignmentInternalAsync("Chat", roomId.ToString())).Should().Be(scopedProfile.Id);
    }

    [Fact]
    public async Task UpsertMany_EmptyDict_NoOp()
    {
        var result = await _aiAssignments.AiAssignmentsBulkAssignAsync(
            new Dictionary<string, string>(),
            TestContext.Current.CancellationToken);

        result.Success.Should().BeTrue();
        (await GetAllAssignmentsAsync()).Should().BeEmpty();
    }

    // Internal contract: entity-scoped assignments have no public route (bulk-assign takes no entityId and the Node adapter drops it).
    [Fact]
    public async Task UpsertMany_NonExistentEntityId_Returns404()
    {
        var profile = await CreateProfileInternalAsync();

        using var response = await _ai.PutAsync(
            AssignmentsPath,
            new
            {
                assignments = new Dictionary<string, Guid>
                {
                    ["Chat"] = profile.Id
                },
                entityId = "999999999"
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

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

[Collection("Test Collection")]
[Trait("Category", "CRUD")]
[Trait("Feature", "AI/Assignments")]
public class AssignmentsUpsertTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task UpsertMany_InsertsNewRows()
    {
        var chatProfile = await CreateProfileAsync();
        var codeProfile = await CreateProfileAsync();

        using var response = await Ai.PutAsync(
            AssignmentsPath,
            new
            {
                assignments = new Dictionary<string, Guid>
                {
                    ["Chat"] = chatProfile.Id,
                    ["Code"] = codeProfile.Id
                }
            },
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var all = await ReadAllAssignmentsAsync();
        all.Should().HaveCount(2);
        all["Chat"].Should().Be(chatProfile.Id);
        all["Code"].Should().Be(codeProfile.Id);
    }

    [Fact]
    public async Task UpsertMany_UpdatesExistingRows()
    {
        var initialChat = await CreateProfileAsync();
        var newChat = await CreateProfileAsync();
        var newCode = await CreateProfileAsync();
        await CreateAssignmentAsync("Chat", initialChat.Id);

        using var response = await Ai.PutAsync(
            AssignmentsPath,
            new
            {
                assignments = new Dictionary<string, Guid>
                {
                    ["Chat"] = newChat.Id,
                    ["Code"] = newCode.Id
                }
            },
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var all = await ReadAllAssignmentsAsync();
        all["Chat"].Should().Be(newChat.Id);
        all["Code"].Should().Be(newCode.Id);
    }

    [Fact]
    public async Task UpsertMany_WithEntityId_IsolatedFromGlobal()
    {
        var globalProfile = await CreateProfileAsync();
        var scopedProfile = await CreateProfileAsync();
        var roomId = await CreateRoomAsync();

        await CreateAssignmentAsync("Chat", globalProfile.Id);

        using var response = await Ai.PutAsync(
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

        (await ReadAssignmentAsync("Chat")).Should().Be(globalProfile.Id);
        (await ReadAssignmentAsync("Chat", roomId.ToString())).Should().Be(scopedProfile.Id);
    }

    [Fact]
    public async Task UpsertMany_EmptyDict_NoOp()
    {
        using var response = await Ai.PutAsync(
            AssignmentsPath,
            new { assignments = new Dictionary<string, Guid>() },
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        (await ReadAllAssignmentsAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task UpsertMany_NonExistentEntityId_Returns404()
    {
        var profile = await CreateProfileAsync();

        using var response = await Ai.PutAsync(
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

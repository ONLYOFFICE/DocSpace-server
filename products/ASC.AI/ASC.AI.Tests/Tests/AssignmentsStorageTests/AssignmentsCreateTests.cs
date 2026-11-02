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
public class AssignmentsCreateTests(AspireAppFixture fixture) : AssignmentsTestBase(fixture)
{
    [Fact]
    public async Task Create_Owner_PersistsAndIsReadable()
    {
        var profile = await CreateAssignableProfileAsync();

        await AssignAsync(AiActionType.Chat, profile.Id);

        (await GetAssignmentAsync(AiActionType.Chat)).Should().Be(profile.Id);
    }

    // Internal contract: entity-scoped assignments have no public route (the Node assign takes no entityId and its storage adapter drops it).
    [Fact]
    public async Task Create_WithEntityId_PersistsForFolder()
    {
        var profile = await CreateProfileInternalAsync();
        var roomId = await CreateRoomAsync();

        await CreateAssignmentInternalAsync("Chat", profile.Id, roomId.ToString());

        (await ReadAssignmentInternalAsync("Chat", roomId.ToString())).Should().Be(profile.Id);
        (await ReadAssignmentInternalAsync("Chat")).Should().BeNull();
    }

    // Internal contract: the strict create (POST /assignments) has no public route (public assign is an upsert through PUT /assignments).
    [Fact]
    public async Task Create_DuplicateActionType_Fails()
    {
        var profile = await CreateProfileInternalAsync();
        await CreateAssignmentInternalAsync("Chat", profile.Id);

        using var response = await _ai.PostAsync(
            AssignmentsPath,
            new { actionType = "Chat", profileId = profile.Id },
            TestContext.Current.CancellationToken);

        response.IsSuccessStatusCode.Should().BeFalse();
    }

    // Internal contract: entity-scoped assignments have no public route (the Node assign takes no entityId and its storage adapter drops it).
    [Fact]
    public async Task Create_NonExistentEntityId_Returns404()
    {
        var profile = await CreateProfileInternalAsync();

        using var response = await _ai.PostAsync(
            AssignmentsPath,
            new { actionType = "Chat", profileId = profile.Id, entityId = "999999999" },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_EmptyProfileId_ReportsProfileNotFound()
    {
        // The engine looks the profile up before it writes: an id that names no profile is a soft failure
        // (200 with success=false), and nothing is bound.
        var result = await _aiAssignments.AiAssignmentsAssignAsync(
            new AiAssignmentsAssignRequest(AiActionType.Chat, Guid.Empty.ToString()),
            TestContext.Current.CancellationToken);

        result.Success.Should().BeFalse();
        result.Error.Message.Should().Contain("Profile not found");
        (await GetAssignmentAsync(AiActionType.Chat)).Should().BeNull();
    }

    [Fact]
    public async Task Create_MissingProfileId_Returns400()
    {
        using var response = await new RawApiClient(_aiChatClient).PutRawAsync(
            PublicAssignPath,
            """{ "actionType": "Chat" }""",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

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
public class AssignmentsReadTests(AspireAppFixture fixture) : AssignmentsTestBase(fixture)
{
    [Fact]
    public async Task ReadByType_Existing_ReturnsProfileId()
    {
        var profile = await CreateAssignableProfileAsync();
        await AssignAsync(AiActionType.Chat, profile.Id);

        (await GetAssignmentAsync(AiActionType.Chat)).Should().Be(profile.Id);
    }

    [Fact]
    public async Task ReadByType_NonExisting_ReturnsNull()
    {
        (await GetAssignmentAsync(AiActionType.Chat)).Should().BeNull();
    }

    // Internal contract: entity-scoped assignments have no public route (get-assignment takes no entityId and the Node adapter drops it).
    [Fact]
    public async Task ReadByType_WithEntityId_ReturnsScopedValue()
    {
        var globalProfile = await CreateProfileInternalAsync();
        var scopedProfile = await CreateProfileInternalAsync();
        var roomId = await CreateRoomAsync();

        await CreateAssignmentInternalAsync("Chat", globalProfile.Id);
        await CreateAssignmentInternalAsync("Chat", scopedProfile.Id, roomId.ToString());

        (await ReadAssignmentInternalAsync("Chat")).Should().Be(globalProfile.Id);
        (await ReadAssignmentInternalAsync("Chat", roomId.ToString())).Should().Be(scopedProfile.Id);
    }

    // Internal contract: entity-scoped assignments have no public route (get-assignment takes no entityId and the Node adapter drops it).
    [Fact]
    public async Task ReadByType_NonExistentEntityId_Returns404()
    {
        using var response = await _ai.GetAsync(
            $"{AssignmentsPath}/Chat?entityId=999999999",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReadAll_Owner_ReturnsAllCreated()
    {
        var chatProfile = await CreateAssignableProfileAsync();
        var codeProfile = await CreateAssignableProfileAsync();

        await AssignAsync(AiActionType.Chat, chatProfile.Id);
        await AssignAsync(AiActionType.Code, codeProfile.Id);

        var all = await GetAllAssignmentsAsync();

        // The first profile of the portal also holds the Default slot.
        all.Should().HaveCount(3);
        all.Should().ContainKey(DefaultSlot).WhoseValue.Should().Be(chatProfile.Id);
        all.Should().ContainKey(nameof(AiActionType.Chat)).WhoseValue.Should().Be(chatProfile.Id);
        all.Should().ContainKey(nameof(AiActionType.Code)).WhoseValue.Should().Be(codeProfile.Id);
    }

    [Fact]
    public async Task ReadAll_Empty_ReturnsEmpty()
    {
        var all = await GetAllAssignmentsAsync();

        all.Should().BeEmpty();
    }

    [Fact]
    public async Task ReadAll_WithRoomEntityId_ReturnsPortalWideMap()
    {
        // The public map is portal-wide: an accessible room is accepted and answers the same map.
        var profile = await CreateAssignableProfileAsync();
        var roomId = await CreateRoomAsync();
        await AssignAsync(AiActionType.Chat, profile.Id);

        var global = await GetAllAssignmentsAsync();
        var scoped = await GetAllAssignmentsAsync(roomId.ToString());

        scoped.Should().BeEquivalentTo(global);
        scoped.Should().ContainKey(nameof(AiActionType.Chat)).WhoseValue.Should().Be(profile.Id);
    }

    [Fact]
    public async Task ReadAll_NonExistentEntityId_Returns404()
    {
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await GetAllAssignmentsAsync("999999999"));

        exception.ErrorCode.Should().Be(404);
    }

    // Internal contract: entity-scoped assignments have no public route (get-all-assignments only validates entityId; the Node adapter drops it).
    [Fact]
    public async Task ReadAll_WithEntityId_ReturnsOnlyScopedAssignments()
    {
        var globalProfile = await CreateProfileInternalAsync();
        var scopedProfile = await CreateProfileInternalAsync();
        var roomId = await CreateRoomAsync();

        await CreateAssignmentInternalAsync("Chat", globalProfile.Id);
        await CreateAssignmentInternalAsync("Code", scopedProfile.Id, roomId.ToString());

        var globalAssignments = await ReadAllAssignmentsInternalAsync();
        var scopedAssignments = await ReadAllAssignmentsInternalAsync(roomId.ToString());

        globalAssignments.Should().HaveCount(1).And.ContainKey("Chat");
        scopedAssignments.Should().HaveCount(1).And.ContainKey("Code");
    }
}

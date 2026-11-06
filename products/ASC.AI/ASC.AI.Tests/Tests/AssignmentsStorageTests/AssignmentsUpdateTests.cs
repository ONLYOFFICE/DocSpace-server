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
public class AssignmentsUpdateTests(AspireAppFixture fixture) : AssignmentsTestBase(fixture)
{
    [Fact]
    public async Task Update_Existing_PersistsNewProfile()
    {
        // The public assign is an upsert: a second assign of a bound slot replaces its profile.
        var firstProfile = await CreateAssignableProfileAsync();
        var secondProfile = await CreateAssignableProfileAsync();
        await AssignAsync(AiActionType.Chat, firstProfile.Id);

        await AssignAsync(AiActionType.Chat, secondProfile.Id);

        (await GetAssignmentAsync(AiActionType.Chat)).Should().Be(secondProfile.Id);
    }

    // Internal contract: the strict update (PUT /assignments/{type}) of an empty slot has no public route (public assign upserts; only the cascade's Default promotion updates, and only an existing slot).
    [Fact]
    public async Task Update_NonExisting_Returns404()
    {
        var profile = await CreateProfileInternalAsync();

        using var response = await _ai.PutAsync(
            $"{AssignmentsPath}/Chat",
            new { profileId = profile.Id },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // Internal contract: entity-scoped assignments have no public route (the Node assign takes no entityId and its storage adapter drops it).
    [Fact]
    public async Task Update_WithEntityId_AffectsOnlyScopedRow()
    {
        var globalProfile = await CreateProfileInternalAsync();
        var scopedProfile = await CreateProfileInternalAsync();
        var replacementProfile = await CreateProfileInternalAsync();
        var roomId = await CreateRoomAsync();

        await CreateAssignmentInternalAsync("Chat", globalProfile.Id);
        await CreateAssignmentInternalAsync("Chat", scopedProfile.Id, roomId.ToString());

        using var response = await _ai.PutAsync(
            $"{AssignmentsPath}/Chat",
            new { profileId = replacementProfile.Id, entityId = roomId.ToString() },
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        (await ReadAssignmentInternalAsync("Chat", roomId.ToString())).Should().Be(replacementProfile.Id);
        (await ReadAssignmentInternalAsync("Chat")).Should().Be(globalProfile.Id);
    }

    // Internal contract: entity-scoped assignments have no public route (the Node assign takes no entityId and its storage adapter drops it).
    [Fact]
    public async Task Update_NonExistentEntityId_Returns404()
    {
        var profile = await CreateProfileInternalAsync();

        using var response = await _ai.PutAsync(
            $"{AssignmentsPath}/Chat",
            new { profileId = profile.Id, entityId = "999999999" },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

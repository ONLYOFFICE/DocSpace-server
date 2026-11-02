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

namespace ASC.AI.Tests.Tests.AssignmentTests;

/// <summary>
/// The entity scope of the assignment reads (assignments.spec.ts, "entity scope") and the profile expansion of
/// get-all-assignments.
/// </summary>
/// <remarks>
/// The TypeScript tests gave an agent its own Chat binding. Agents are gone, and the public routes keep ONE portal-wide
/// map: the Node storage adapter drops every <c>entityId</c> before it calls ASC.AI, so an entity-scoped binding exists
/// only on the internal contract (<c>AssignmentsStorageTests</c> checks it there). What the public routes still do with
/// an <c>entityId</c> is validate it, which is what this class pins.
/// </remarks>
[Trait("Category", "Read")]
[Trait("Feature", "AI/Assignments")]
public class AssignmentEntityScopeTests(AspireAppFixture fixture) : AssignmentTestBase(fixture)
{
    [Fact]
    public async Task GetAll_WithAnAccessibleRoom_AnswersThePortalWideMap_AndResolveIgnoresTheScope()
    {
        var chat = await CreateProfileWithAsync(TextCapabilities);
        var roomId = (await CreateRoomAsync()).ToString();
        await AssignAsync(AiActionType.Chat, chat.Id);

        var global = await GetAllAssignmentsAsync();
        var scoped = await GetAllAssignmentsAsync(roomId);

        scoped.Should().BeEquivalentTo(global, "the bindings are portal-wide whatever the room");
        await ExpectResolvedAsync(chat.Id, AiActionType.Chat, roomId);
    }

    /// <summary>The scope used to be dropped silently, so "this entity has no overrides" and "this entity is not a thing" read the same.</summary>
    [Fact]
    [Trait("Bug", "82832")]
    public async Task GetAll_UnknownEntityId_Is404AndDoesNotAnswerThePortalWideMap()
    {
        var text = await CreateProfileWithAsync(TextCapabilities);
        await AssignAsync(AiActionType.Summarization, text.Id);

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await GetAllAssignmentsAsync("999999"));

        exception.ErrorCode.Should().Be(404, "an unknown entity id");
        exception.ErrorContent?.ToString().Should().NotContain(text.Id, "and it does not answer with the portal-wide assignments");
    }

    /// <summary>The lookup the chat surface calls dropped the scope in silence, answering the portal-wide model for a room that is not there.</summary>
    [Fact]
    [Trait("Bug", "84279")]
    public async Task Resolve_UnknownEntityId_Is404OnBothRoutes()
    {
        var chat = await CreateProfileWithAsync(TextCapabilities);
        await AssignAsync(AiActionType.Chat, chat.Id);

        (await StatusOfAsync(() => ResolveAsync(nameof(AiActionType.Chat), "999999"))).Should().Be(404, "resolve with an unknown entity");
        (await StatusOfAsync(() => TryResolveAsync(nameof(AiActionType.Chat), "999999"))).Should().Be(404, "try-resolve with an unknown entity");
    }

    // Raw: the SDK types get-all-assignments as Dictionary<string, string>, which cannot carry the expanded answer
    // ({assignments, profilesTotal, chatProfilesTotal}) that expand=profile returns. An SDK defect worth reporting.
    [Fact]
    public async Task GetAll_ExpandProfile_AnswersTheProfilesAndTheCatalogueCounts()
    {
        var text = await CreateProfileWithAsync(TextCapabilities);
        await CreateProfileWithAsync(ImageCapabilities);
        await AssignAsync(AiActionType.Summarization, text.Id);

        var (status, body) = await GetRawAsync("get-all-assignments?expand=profile");

        status.Should().Be(200, body);
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;

        var assignments = root.GetProperty("assignments");
        assignments.GetProperty(DefaultSlot).GetProperty("id").GetString().Should().Be(text.Id, "Default is the first profile, expanded");
        assignments.GetProperty(nameof(AiActionType.Summarization)).GetProperty("id").GetString().Should().Be(text.Id);
        root.GetProperty("profilesTotal").GetInt32().Should().Be(2);
        root.GetProperty("chatProfilesTotal").GetInt32().Should().Be(1, "the drawing profile cannot chat");
    }
}

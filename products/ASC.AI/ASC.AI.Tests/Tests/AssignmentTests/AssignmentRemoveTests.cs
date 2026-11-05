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

/// <summary>Removing assignments: unassign and cascade-profile-delete (assignments.spec.ts, "removing assignments").</summary>
[Trait("Category", "Delete")]
[Trait("Feature", "AI/Assignments")]
public class AssignmentRemoveTests(AspireAppFixture fixture) : AssignmentTestBase(fixture)
{
    [Fact]
    public async Task Unassign_RemovesOneBinding_LeavesTheRest_AndTheActionResolvesThroughDefault()
    {
        var fallback = await CreateProfileWithAsync(TextCapabilities);
        var text = await CreateProfileWithAsync(TextCapabilities);
        await AssignAsync(AiActionType.Chat, text.Id);
        await AssignAsync(AiActionType.Summarization, text.Id);

        var result = await UnassignAsync(AiActionType.Chat);

        result.Success.Should().BeTrue();
        (await GetAssignmentAsync(AiActionType.Chat)).Should().BeNull();
        (await GetAssignmentAsync(AiActionType.Summarization)).Should().Be(text.Id, "Summarization survives");

        // After the unassign the action resolves through Default again rather than failing.
        var resolved = await ResolveAsync(nameof(AiActionType.Chat));
        resolved.ProfileId.Should().Be(fallback.Id).And.NotBe(text.Id);
    }

    [Fact]
    public async Task Unassign_Twice_IsAccepted_AnUnknownActionIs400()
    {
        var text = await CreateProfileWithAsync(TextCapabilities);
        await AssignAsync(AiActionType.Chat, text.Id);

        (await UnassignAsync(AiActionType.Chat)).Success.Should().BeTrue();
        (await UnassignAsync(AiActionType.Chat)).Success.Should().BeTrue();
        (await GetAssignmentAsync(AiActionType.Chat)).Should().BeNull();

        var unknown = await StatusOfAsync(() => _aiAssignments.AiAssignmentsUnassignAsync("NotAnAction", TestContext.Current.CancellationToken));
        unknown.Should().Be(400);
    }

    [Fact]
    public async Task CascadeProfileDelete_DropsEveryBindingOfOneProfileOnly()
    {
        await CreateProfileWithAsync(TextCapabilities);
        var doomed = await CreateProfileWithAsync(TextCapabilities);
        var keeper = await CreateProfileWithAsync(TextVisionCapabilities);

        // The profile backs several actions, and a second profile backs one more so the cascade is shown to be targeted.
        var seeded = await BulkAssignAsync(new Dictionary<string, string>
        {
            [nameof(AiActionType.Chat)] = doomed.Id,
            [nameof(AiActionType.Summarization)] = doomed.Id,
            [nameof(AiActionType.FormAnalysis)] = doomed.Id,
            [nameof(AiActionType.Vision)] = keeper.Id
        });
        seeded.Success.Should().BeTrue(JsonSerializer.Serialize(seeded.Errors));

        (await CascadeProfileDeleteAsync(doomed.Id)).Success.Should().BeTrue();

        var after = await GetAllAssignmentsAsync();
        after.Values.Should().NotContain(doomed.Id, "no binding still points at the cascaded profile");
        after.Keys.Should().NotContain(nameof(AiActionType.FormAnalysis));
        after[nameof(AiActionType.Vision)].Should().Be(keeper.Id, "the other profile's binding survives");
    }

    // Raw: the typed request carries the profile id as a required constructor parameter, so a bare value and an
    // empty object cannot be built.
    [Fact]
    public async Task CascadeProfileDelete_ProfileIdMustBeSentAsAnObjectField()
    {
        var profile = await CreateProfileWithAsync(TextCapabilities);

        var (bareStatus, bareBody) = await DeleteRawAsync("cascade-profile-delete", profile.Id);
        bareStatus.Should().Be(400, bareBody);
        bareBody.Should().Contain("profileId required");

        var (emptyStatus, emptyBody) = await DeleteRawAsync("cascade-profile-delete", new { });
        emptyStatus.Should().Be(400, emptyBody);
    }
}

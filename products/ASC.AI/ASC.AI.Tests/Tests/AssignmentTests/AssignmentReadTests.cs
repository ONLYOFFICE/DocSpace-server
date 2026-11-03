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
/// Reading assignments (assignments.spec.ts, "reading assignments"). The gateway-seeded <c>ImageGeneration</c> binding
/// of the TypeScript suite does not exist on the integration-test host; what the host does guarantee is that the first
/// profile of a portal takes the <c>Default</c> slot.
/// </summary>
[Trait("Category", "Read")]
[Trait("Feature", "AI/Assignments")]
public class AssignmentReadTests(AspireAppFixture fixture) : AssignmentTestBase(fixture)
{
    [Fact]
    public async Task GetAll_AfterTheFirstProfile_BindsDefaultToItAndCarriesNoCredential()
    {
        var profile = await CreateProfileWithAsync(TextCapabilities);

        var all = await GetAllAssignmentsAsync();

        all.Should().ContainKey(DefaultSlot).WhoseValue.Should().Be(profile.Id);
        JsonSerializer.Serialize(all).Should().NotContain(StubModelProvider.Key, "an assignment map names profiles, it never carries a credential");
    }

    [Fact]
    public async Task GetAssignment_UnassignedAction_ReadsBackAsNull()
    {
        await CreateProfileWithAsync(TextCapabilities);

        (await GetAssignmentAsync(AiActionType.Chat)).Should().BeNull();
    }

    [Theory]
    [InlineData(AiActionType.Chat)]
    [InlineData(AiActionType.Summarization)]
    [InlineData(AiActionType.Vision)]
    public async Task ResolveForAction_UnassignedAction_FallsBackToDefaultExpanded(AiActionType actionType)
    {
        // A vision-capable Default can serve every action in the case list.
        var fallback = await CreateProfileWithAsync(TextVisionCapabilities);
        (await GetAssignmentAsync(actionType)).Should().BeNull("the action has no binding of its own");

        var resolved = await ResolveAsync(actionType.ToString());
        var tried = await TryResolveAsync(actionType.ToString());

        resolved.ProfileId.Should().Be(fallback.Id);
        resolved.Profile.Id.Should().Be(fallback.Id);
        resolved.Profile.ModelId.Should().NotBeNullOrEmpty("the resolved profile is fully expanded");
        tried.Should().NotBeNull();
        tried.ProfileId.Should().Be(fallback.Id);
    }

    [Fact]
    public async Task UnknownActionType_IsRejectedByEveryReadRoute()
    {
        await CreateProfileWithAsync(TextCapabilities);

        var cancellationToken = TestContext.Current.CancellationToken;

        // The status is the signal; the message of this 400 reads "actionType required" and says nothing about the value.
        (await StatusOfAsync(() => _aiAssignments.AiAssignmentsGetAssignmentAsync("NotAnAction", cancellationToken))).Should().Be(400, "get-assignment");
        (await StatusOfAsync(() => ResolveAsync("NotAnAction"))).Should().Be(400, "resolve-for-action");
        (await StatusOfAsync(() => TryResolveAsync("NotAnAction"))).Should().Be(400, "try-resolve-for-action");
    }
}

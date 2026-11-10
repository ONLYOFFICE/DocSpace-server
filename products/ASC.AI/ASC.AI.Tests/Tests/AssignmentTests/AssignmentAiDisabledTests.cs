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
/// Model assignments with the portal AI switch off (assignments.ai-disabled.spec.ts): the write side is refused and the
/// read side answers a single documented status, 403. The second describe block of the TypeScript file (the unpaid AI
/// Tools wallet service, <c>aiReady:false</c>) needs the ONLYOFFICE AI gateway and wallet and is not portable.
/// </summary>
[Trait("Category", "AiDisabled")]
[Trait("Feature", "AI/Assignments")]
public class AssignmentAiDisabledTests(AspireAppFixture fixture) : AssignmentTestBase(fixture)
{
    [Fact]
    public async Task WholeSurface_Returns403_WhenAiAccessIsDisabled()
    {
        var text = await CreateProfileWithAsync(TextCapabilities);
        await AssignAsync(AiActionType.Chat, text.Id);
        var cancellationToken = TestContext.Current.CancellationToken;

        await SetPortalAiAccessAsync(false);

        var calls = new (string Route, Func<Task> Call)[]
        {
            ("get-all-assignments", () => GetAllAssignmentsAsync()),
            ("get-assignment", () => _aiAssignments.AiAssignmentsGetAssignmentAsync(nameof(AiActionType.Chat), cancellationToken)),
            ("resolve-for-action", () => ResolveAsync(nameof(AiActionType.Chat))),
            ("try-resolve-for-action", () => TryResolveAsync(nameof(AiActionType.Chat))),
            ("assign", () => TryAssignAsync(AiActionType.Chat, text.Id)),
            ("bulk-assign", () => BulkAssignAsync(new Dictionary<string, string> { [nameof(AiActionType.Summarization)] = text.Id })),
            ("unassign", () => UnassignAsync(AiActionType.Chat)),
            ("cascade-profile-delete", () => CascadeProfileDeleteAsync(text.Id))
        };

        foreach (var (route, call) in calls)
        {
            (await StatusOfAsync(call)).Should().Be(403, $"{route} with AI access disabled");
        }
    }

    [Fact]
    public async Task Writes_WhileAiAccessIsDisabled_ChangeNothing()
    {
        var seed = await CreateProfileWithAsync(TextCapabilities);
        var attempt = await CreateProfileWithAsync(TextVisionCapabilities);
        await AssignAsync(AiActionType.Chat, seed.Id);

        await SetPortalAiAccessAsync(false);

        (await StatusOfAsync(() => TryAssignAsync(AiActionType.Chat, attempt.Id))).Should().Be(403);
        (await StatusOfAsync(() => UnassignAsync(AiActionType.Chat))).Should().Be(403);
        (await StatusOfAsync(() => CascadeProfileDeleteAsync(seed.Id))).Should().Be(403);

        // Turning AI back on is the only way to see what the refused writes did: a 403 that silently applied would
        // otherwise be invisible.
        await SetPortalAiAccessAsync(true);

        (await GetAssignmentAsync(AiActionType.Chat)).Should().Be(seed.Id, "the assignment survived the refused writes");
    }
}

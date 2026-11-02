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

namespace ASC.AI.Tests.Tests.PromptTests;

/// <summary>
/// The portal AI switch off: the whole prompt surface refuses with 403, even for the owner. The prompts are created
/// while AI is still on; with the switch back on, the refused writes changed nothing.
/// </summary>
/// <remarks>
/// The TypeScript suite also enabled the AI gateway first; the standalone test stand has none, and the prompt routes
/// do not depend on it.
/// </remarks>
[Trait("Category", "Permissions")]
[Trait("Feature", "AI/Prompts")]
public class PromptAiDisabledTests(AspireAppFixture fixture) : PromptsPermissionTestBase(fixture)
{
    [Theory]
    [MemberData(nameof(Routes), MemberType = typeof(PromptsPermissionTestBase))]
    public async Task Route_AiDisabled_Returns403(string route)
    {
        var seed = await SeedOwnerAsync();

        await SetPortalAiAccessAsync(false);
        var exception = await Assert.ThrowsAsync<ApiException>(CallRoute(route, seed));

        // Switching AI back on shows the refused writes changed nothing.
        await SetPortalAiAccessAsync(true);
        await ExpectOwnerSeedIntactAsync(seed);
        (await ListIdsAsync()).Should().Equal(seed.Prompt.Id);
        exception.ErrorCode.Should().Be(403, exception.ErrorContent?.ToString());
    }

    [Fact]
    public Task Writes_AiDisabled_Are403_AndChangeNothing() => ExpectWritesRefusedAsync(403);

    protected override async Task ActAsRefusedCallerAsync() => await SetPortalAiAccessAsync(false);

    protected override async Task RestoreOwnerAsync() => await SetPortalAiAccessAsync(true);
}

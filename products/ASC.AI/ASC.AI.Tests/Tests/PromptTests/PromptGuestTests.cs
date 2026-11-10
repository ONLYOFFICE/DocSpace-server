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
/// A Guest has no prompt library: every route answers 403. The owner's ids are created before the Guest exists, so they
/// give the id-taking routes something real to aim at (a 403 on a made-up id would not tell "Guests are refused" from
/// "that id does not exist"), and the owner's library must be intact afterwards, a replace-import included.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "AI/Prompts")]
public class PromptGuestTests(AspireAppFixture fixture) : PromptsPermissionTestBase(fixture)
{
    [Theory]
    [MemberData(nameof(Routes), MemberType = typeof(PromptsPermissionTestBase))]
    public async Task Route_Guest_Returns403(string route)
    {
        var seed = await SeedOwnerAsync();
        var guest = await InviteGuest(TestContext.Current.CancellationToken);

        await _aiChatClient.Authenticate(guest);
        var exception = await Assert.ThrowsAsync<ApiException>(CallRoute(route, seed));

        // None of the refused writes reached the owner's library.
        await ExpectOwnerSeedIntactAsync(seed);
        exception.ErrorCode.Should().Be(403, exception.ErrorContent?.ToString());
    }

    [Fact]
    public Task Writes_Guest_Are403_AndChangeNothing() => ExpectWritesRefusedAsync(403);

    protected override async Task ActAsRefusedCallerAsync()
    {
        var guest = await InviteGuest(TestContext.Current.CancellationToken);
        await _aiChatClient.Authenticate(guest);
    }
}

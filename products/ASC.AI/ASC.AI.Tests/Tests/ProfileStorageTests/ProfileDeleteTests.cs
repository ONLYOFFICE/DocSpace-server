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

namespace ASC.AI.Tests.Tests.ProfileStorageTests;

[Trait("Category", "CRUD")]
[Trait("Feature", "AI/Profiles")]
public class ProfileDeleteTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task Delete_Owner_RemovesProfile()
    {
        var created = await CreateProfileAsync();

        var result = await _aiProfiles.AiProfilesDeleteAsync(created.Id, TestContext.Current.CancellationToken);
        result.Success.Should().BeTrue();

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _aiProfiles.AiProfilesGetByIdAsync(created.Id, TestContext.Current.CancellationToken));
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task Delete_NonExisting_ReturnsSuccess()
    {
        var result = await _aiProfiles.AiProfilesDeleteAsync(Guid.NewGuid().ToString(), TestContext.Current.CancellationToken);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_NonAdmin_Returns403()
    {
        var created = await CreateProfileAsync();

        var regularUser = await InviteContact(EmployeeType.User, TestContext.Current.CancellationToken);
        await _aiChatClient.Authenticate(regularUser);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _aiProfiles.AiProfilesDeleteAsync(created.Id, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);

        await _aiChatClient.Authenticate(Owner);
        (await _aiProfiles.AiProfilesGetByIdAsync(created.Id, TestContext.Current.CancellationToken)).Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task Delete_Unauthorized_Returns401()
    {
        var created = await CreateProfileAsync();
        await _aiChatClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _aiProfiles.AiProfilesDeleteAsync(created.Id, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);

        await _aiChatClient.Authenticate(Owner);
        (await _aiProfiles.AiProfilesGetByIdAsync(created.Id, TestContext.Current.CancellationToken)).Id.Should().Be(created.Id);
    }
}

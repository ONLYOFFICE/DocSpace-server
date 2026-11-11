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

namespace ASC.AI.Tests.Tests.DocumentAgentTests;

[Trait("Category", "Permissions")]
[Trait("Feature", "AI/DocumentAgent")]
public class DocumentAgentSettingsPermissionsTests(AspireAppFixture fixture) : DocumentAgentSettingsTestBase(fixture)
{
    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin, HttpStatusCode.OK)]
    [InlineData(EmployeeType.RoomAdmin, HttpStatusCode.OK)]
    [InlineData(EmployeeType.User, HttpStatusCode.OK)]
    [InlineData(EmployeeType.Guest, HttpStatusCode.Forbidden)]
    public async Task Read_ByRole(EmployeeType type, HttpStatusCode expected)
    {
        await _aiClient.Authenticate(await InviteMemberAsync(type));

        using var response = await GetSettingsResponseAsync();

        response.StatusCode.Should().Be(expected);
    }

    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin, HttpStatusCode.OK)]
    [InlineData(EmployeeType.RoomAdmin, HttpStatusCode.Forbidden)]
    [InlineData(EmployeeType.User, HttpStatusCode.Forbidden)]
    [InlineData(EmployeeType.Guest, HttpStatusCode.Forbidden)]
    public async Task Update_ByRole(EmployeeType type, HttpStatusCode expected)
    {
        await _aiClient.Authenticate(await InviteMemberAsync(type));

        using var response = await PutSettingsAsync(BuildSettings(enabled: false));

        response.StatusCode.Should().Be(expected);
    }

    [Fact]
    public async Task Update_ByMember_KeepsSettings()
    {
        await _aiClient.Authenticate(await InviteMemberAsync(EmployeeType.User));

        using var response = await PutSettingsAsync(BuildSettings(enabled: false));
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        await _aiClient.Authenticate(Owner);

        (await ReadSettingsAsync()).Enabled.Should().BeTrue();
    }

    [Fact]
    public async Task Read_Unauthorized_Returns401()
    {
        await _aiClient.Authenticate(null);

        using var response = await GetSettingsResponseAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Update_Unauthorized_Returns401()
    {
        await _aiClient.Authenticate(null);

        using var response = await PutSettingsAsync(BuildSettings());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Read_AiDisabled_Returns403()
    {
        await SwitchPortalAiOffAsync();

        using var response = await GetSettingsResponseAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}

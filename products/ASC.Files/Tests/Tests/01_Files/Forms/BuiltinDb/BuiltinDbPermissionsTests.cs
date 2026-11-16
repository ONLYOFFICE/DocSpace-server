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

namespace ASC.Files.Tests.Tests._01_Files.Forms.BuiltinDb;

/// <summary>
/// Who may get or reset the password of the built-in forms database: it reads every submission of the portal, whatever
/// room it came from, so only the EditPortalSettings right of the portal owner and DocSpace admins is enough. The lowest
/// role holding the right is a DocSpace admin; every type below it is refused before anything is provisioned.
/// </summary>
[Trait("Category", "Security")]
[Trait("Feature", "Forms")]
public class BuiltinDbPermissionsTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    public static TheoryData<EmployeeType> TypesWithoutPortalSettings => [EmployeeType.RoomAdmin, EmployeeType.User, EmployeeType.Guest];

    [Theory]
    [MemberData(nameof(TypesWithoutPortalSettings))]
    public async Task GetConnection_CallerCannotChangePortalSettings_IsRefused(EmployeeType type)
    {
        await _filesClient.Authenticate(await InviteMember(type));

        using var response = await BuiltinDbApi.GetConnectionAsync(_filesClient);

        await ShouldBeAccessDeniedAsync(response);
    }

    [Theory]
    [MemberData(nameof(TypesWithoutPortalSettings))]
    public async Task ResetPassword_CallerCannotChangePortalSettings_IsRefused(EmployeeType type)
    {
        await _filesClient.Authenticate(await InviteMember(type));

        using var response = await BuiltinDbApi.ResetPasswordAsync(_filesClient);

        await ShouldBeAccessDeniedAsync(response);
    }

    [Fact]
    public async Task DocSpaceAdmin_GetsTheConnection_AndResetsThePassword()
    {
        await _filesClient.Authenticate(await InviteContact(EmployeeType.DocSpaceAdmin));

        var connection = await BuiltinDbApi.GetReadyAsync(_filesClient);
        var reset = await BuiltinDbApi.GetReadyAsync(_filesClient, resetPassword: true);

        connection.Database.Should().Be($"forms_t{TenantId}");
        reset.Database.Should().Be(connection.Database);
        reset.Password.Should().NotBe(connection.Password);
    }

    [Fact]
    public async Task Anonymous_IsAskedToSignIn()
    {
        await _filesClient.Authenticate(null);

        using var get = await BuiltinDbApi.GetConnectionAsync(_filesClient);
        using var put = await BuiltinDbApi.ResetPasswordAsync(_filesClient);

        get.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        put.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // The refusal of the right itself, not of a token scope or of a server without the built-in database.
    private static async Task ShouldBeAccessDeniedAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).Should().Contain("Access denied");
    }
}

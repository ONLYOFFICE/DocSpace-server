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

namespace ASC.Web.Api.Tests.Tests._07_Migration;

/// <summary>
/// Access control on the write endpoints of the migration API (cancel, clear, finish, migrate,
/// init). <c>MigrationController.DemandPermissionAsync</c> requires a DocSpaceAdmin, so a
/// RoomAdmin, a User and a Guest are denied and an anonymous caller is rejected earlier.
/// </summary>
[Trait("Category", "Migration")]
public class MigrationWritePermissionsTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    public static TheoryData<string> Endpoints => ["cancel", "clear", "finish", "migrate", "init"];

    [Theory]
    [MemberData(nameof(Endpoints))]
    public async Task WriteEndpoint_Anonymous_ThrowsUnauthorized(string endpoint)
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await Call(endpoint));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Theory]
    [MemberData(nameof(Endpoints))]
    public async Task WriteEndpoint_RoomAdmin_ThrowsAccessDenied(string endpoint) => await AssertDenied(EmployeeType.RoomAdmin, endpoint);

    [Theory]
    [MemberData(nameof(Endpoints))]
    public async Task WriteEndpoint_User_ThrowsAccessDenied(string endpoint) => await AssertDenied(EmployeeType.User, endpoint);

    [Theory]
    [MemberData(nameof(Endpoints))]
    public async Task WriteEndpoint_Guest_ThrowsAccessDenied(string endpoint) => await AssertDenied(EmployeeType.Guest, endpoint);

    private async Task AssertDenied(EmployeeType employeeType, string endpoint)
    {
        // Arrange
        var member = await InviteMember(employeeType);
        await _webApiClient.Authenticate(member);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await Call(endpoint));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    private async Task Call(string endpoint)
    {
        var token = TestContext.Current.CancellationToken;

        switch (endpoint)
        {
            case "cancel":
                await _migrationApi.CancelMigrationAsync(token);
                break;
            case "clear":
                await _migrationApi.ClearMigrationAsync(token);
                break;
            case "finish":
                await _migrationApi.FinishMigrationAsync(new FinishDto(false), token);
                break;
            case "migrate":
                await _migrationApi.StartMigrationAsync(new MigrationApiInfo(), token);
                break;
            default:
                await _migrationApi.UploadAndInitializeMigrationAsync("Nextcloud", token);
                break;
        }
    }
}

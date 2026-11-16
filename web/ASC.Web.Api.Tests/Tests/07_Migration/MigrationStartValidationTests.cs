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
/// What <c>POST api/2.0/migration/migrate</c> checks before it queues the import: the body, the e-mail and
/// user-type format of the users, and the rule that only the owner may import someone as a DocSpace admin
/// who is not one on the portal yet. None of these needs an uploaded backup: an accepted body is queued and
/// its job then fails for the missing upload.
/// </summary>
[Trait("Category", "Migration")]
public class MigrationStartValidationTests(
    AspireAppFixture fixture)
    : MigrationTestBase(fixture)
{
    [Fact]
    public async Task StartMigration_NonOwnerAdminImportsNewDocSpaceAdmin_ThrowsAccessDenied()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(admin);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _migrationApi.StartMigrationAsync(Info(Select("u1", NewEmail(), EmployeeType.DocSpaceAdmin)), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent!.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task StartMigration_NonOwnerAdminImportsAnExistingAdminAsDocSpaceAdmin_Succeeds()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        var other = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(admin);

        // Act
        var act = async () => await _migrationApi.StartMigrationAsync(Info(Select("u1", other.Email.ToUpperInvariant(), EmployeeType.DocSpaceAdmin)), TestContext.Current.CancellationToken);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task StartMigration_NonOwnerAdminImportsNewUsersOfOtherTypes_Succeeds()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(admin);

        // Act
        var act = async () => await _migrationApi.StartMigrationAsync(
            Info(Select("u1", NewEmail(), EmployeeType.RoomAdmin), Select("u2", NewEmail())),
            TestContext.Current.CancellationToken);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task StartMigration_OwnerImportsNewDocSpaceAdmin_Succeeds()
    {
        // Act
        var act = async () => await _migrationApi.StartMigrationAsync(Info(Select("u1", NewEmail(), EmployeeType.DocSpaceAdmin)), TestContext.Current.CancellationToken);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task StartMigration_InvalidEmail_ThrowsBadRequest()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _migrationApi.StartMigrationAsync(Info(Select("u1", "not-an-email")), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    /// <summary>The SDK's <c>EmployeeType</c> enum cannot carry an unknown name, so the body goes over raw HTTP.</summary>
    [Fact]
    public async Task StartMigration_UnknownUserType_ThrowsBadRequest()
    {
        // Arrange
        using var content = new StringContent(
            """{"migratorName":"Nextcloud","users":[{"key":"u1","email":"u1@example.com","shouldImport":true,"userType":"SuperAdmin"}]}""",
            Encoding.UTF8,
            "application/json");

        // Act
        using var response = await _webApiClient.PostAsync("api/2.0/migration/migrate", content, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>The typed client sends no body for a null DTO, so the missing body goes over raw HTTP.</summary>
    [Fact]
    public async Task StartMigration_MissingBody_ThrowsBadRequest()
    {
        // Arrange
        using var content = new StringContent(string.Empty, Encoding.UTF8, "application/json");

        // Act
        using var response = await _webApiClient.PostAsync("api/2.0/migration/migrate", content, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static MigrationApiInfo Info(params MigratingApiUser[] users) => new(migratorName: "Nextcloud", users: [.. users]);
}

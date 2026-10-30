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
namespace ASC.Web.Api.Tests.Tests._02_Settings.Storage;

/// <summary>
/// Access control of the storage endpoints. The S3 region list needs only a signed-in caller; the backup storages
/// and the migration progress need the portal-settings right; every other endpoint is rejected for an anonymous
/// caller with 401, before the installation gate is ever reached.
/// </summary>
[Trait("Category", "Settings")]
public class StoragePermissionsTests(AspireAppFixture fixture) : StorageTestBase(fixture)
{
    [Fact]
    public async Task GetAmazonS3Regions_User_ReturnsRegions()
    {
        // Arrange
        await _webApiClient.Authenticate(await InviteContact(EmployeeType.User));

        // Act
        var regions = await Storage.GetAmazonS3RegionsAsync(TestContext.Current.CancellationToken);

        // Assert
        regions.Response.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetAmazonS3Regions_Anonymous_ThrowsUnauthorized()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await Storage.GetAmazonS3RegionsAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task GetAllBackupStorages_DocSpaceAdmin_ReturnsProviders()
    {
        // Arrange
        await _webApiClient.Authenticate(await InviteContact(EmployeeType.DocSpaceAdmin));

        // Act
        var storages = await Storage.GetAllBackupStoragesAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        storages.Response.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAllBackupStorages_User_ThrowsAccessDenied()
    {
        // Arrange
        await _webApiClient.Authenticate(await InviteContact(EmployeeType.User));

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await Storage.GetAllBackupStoragesAsync(cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetAllBackupStorages_Anonymous_ThrowsUnauthorized()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await Storage.GetAllBackupStoragesAsync(cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task GetStorageProgress_User_ThrowsAccessDenied()
    {
        // Arrange
        await _webApiClient.Authenticate(await InviteContact(EmployeeType.User));

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await Storage.GetStorageProgressAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetStorageProgress_Anonymous_ThrowsUnauthorized()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await Storage.GetStorageProgressAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Theory]
    [InlineData("GetAllStorages")]
    [InlineData("GetAllCdnStorages")]
    [InlineData("ResetCdnToDefault")]
    [InlineData("ResetStorageToDefault")]
    [InlineData("UpdateCdnStorage")]
    [InlineData("UpdateStorage")]
    public async Task InstallationGatedEndpoint_Anonymous_ThrowsUnauthorized(string endpoint)
    {
        // Arrange
        await _webApiClient.Authenticate(null);
        var cancellationToken = TestContext.Current.CancellationToken;
        var request = new StorageRequestDto("s3", []);

        // Act: an anonymous call never reaches the handler, so the resets cannot change anything
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
        {
            switch (endpoint)
            {
                case "GetAllStorages":
                    await Storage.GetAllStoragesAsync(cancellationToken);
                    break;
                case "GetAllCdnStorages":
                    await Storage.GetAllCdnStoragesAsync(cancellationToken);
                    break;
                case "ResetCdnToDefault":
                    await Storage.ResetCdnToDefaultAsync(cancellationToken);
                    break;
                case "ResetStorageToDefault":
                    await Storage.ResetStorageToDefaultAsync(cancellationToken);
                    break;
                case "UpdateCdnStorage":
                    await Storage.UpdateCdnStorageAsync(request, cancellationToken);
                    break;
                default:
                    await Storage.UpdateStorageAsync(request, cancellationToken);
                    break;
            }
        });

        // Assert
        exception.ErrorCode.Should().Be(401);
    }
}

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
/// The read endpoints of the storage settings: Amazon S3 regions (static reference data), the backup-capable
/// providers, and the portal and CDN storage lists (standalone: the installation gate is open).
/// </summary>
[Trait("Category", "Settings")]
public class StorageReadTests(AspireAppFixture fixture) : StorageTestBase(fixture)
{
    [Fact]
    public async Task GetAmazonS3Regions_Owner_ReturnsCompiledInRegions()
    {
        // Act
        var regions = (await Storage.GetAmazonS3RegionsAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        regions.Should().NotBeEmpty();
        regions[0].SystemName.Should().NotBeNullOrEmpty();
        regions[0].DisplayName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetAllBackupStorages_Owner_ReturnsProviders()
    {
        // Act
        var storages = (await Storage.GetAllBackupStoragesAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        storages.Should().NotBeEmpty();
        storages[0].Id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetAllBackupStorages_DumpTrue_ReturnsSameListShape()
    {
        // Act
        var storages = (await Storage.GetAllBackupStoragesAsync(true, TestContext.Current.CancellationToken)).Response;

        // Assert
        storages.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAllStorages_Owner_ReturnsStorages()
    {
        // Act
        var storages = (await Storage.GetAllStoragesAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        storages.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAllCdnStorages_Owner_ReturnsStorages()
    {
        // Act
        var storages = (await Storage.GetAllCdnStoragesAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        storages.Should().NotBeNull();
    }
}

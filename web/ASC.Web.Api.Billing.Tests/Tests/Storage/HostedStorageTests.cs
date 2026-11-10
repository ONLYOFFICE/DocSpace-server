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

namespace ASC.Web.Api.Billing.Tests.Tests.Storage;

/// <summary>
/// Storage settings on a hosted (SaaS) portal: the installation does not let a portal change its storage or encrypt
/// it, so even the owner is refused, and the migration progress is the fixed -1. Nothing here can switch anything.
/// </summary>
public class HostedStorageTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    private StorageApi? _storageApi;
    private EncryptionApi? _encryptionApi;

    private Configuration ApiConfiguration => new() { BasePath = _webApiClient.BaseAddress!.ToString().TrimEnd('/') };

    private StorageApi Storage => _storageApi ??= new StorageApi(_webApiClient, ApiConfiguration);

    private EncryptionApi Encryption => _encryptionApi ??= new EncryptionApi(_webApiClient, ApiConfiguration);

    [Fact]
    public async Task GetAllStorages_HostedOwner_ThrowsAccessDenied()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await Storage.GetAllStoragesAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent!.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task GetAllCdnStorages_HostedOwner_ThrowsAccessDenied()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await Storage.GetAllCdnStoragesAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task UpdateStorage_HostedOwner_ThrowsAccessDeniedBeforeModuleCheck()
    {
        // Arrange: the module is not configured either, which alone would be 400
        var request = new StorageRequestDto("s3", [new ItemKeyValuePairStringString("acesskey", "x")]);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await Storage.UpdateStorageAsync(request, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetStorageProgress_HostedOwner_ReturnsMinusOne()
    {
        // Act
        var progress = (await Storage.GetStorageProgressAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        progress.Should().Be(-1);
    }

    [Fact]
    public async Task StartStorageEncryption_HostedOwner_ThrowsAccessDenied()
    {
        // Act: refused on the hosted check, before the rights check and before anything is queued
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await Encryption.StartStorageEncryptionAsync(new StorageEncryptionRequestDto(false), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetStorageEncryptionProgress_HostedOwner_ThrowsUnsupportedMediaType()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await Encryption.GetStorageEncryptionProgressAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(415);
    }
}

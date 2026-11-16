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

namespace ASC.Web.Api.Tests.Tests._02_Settings.Storage;

/// <summary>
/// Storage settings for callers without the portal-settings right, the shape of the storage list, a module name that
/// names no storage at all, and the migration progress of a portal that never migrated. Nothing here switches the
/// storage or the CDN: every call is refused before the switch, or only reads.
/// </summary>
[Trait("Category", "Settings")]
public class StorageAccessTests(AspireAppFixture fixture) : StorageTestBase(fixture)
{
    [Theory]
    [InlineData("GetAllStorages")]
    [InlineData("GetAllCdnStorages")]
    [InlineData("ResetCdnToDefault")]
    [InlineData("ResetStorageToDefault")]
    [InlineData("UpdateCdnStorage")]
    [InlineData("UpdateStorage")]
    public async Task StorageSettingsEndpoint_User_ThrowsAccessDenied(string endpoint)
    {
        // Arrange
        await _webApiClient.Authenticate(await InviteContact(EmployeeType.User));
        var cancellationToken = TestContext.Current.CancellationToken;
        var request = UnconfiguredModuleRequest();

        // Act: the right is demanded first, so neither reset nor update reaches the storage
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
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetAllStorages_FreshPortal_EveryEntryNamedAndNoneCurrent()
    {
        // Act
        var storages = (await Storage.GetAllStoragesAsync(TestContext.Current.CancellationToken)).Response;

        // Assert: no current entry means the portal is on the built-in storage
        storages.Should().NotBeEmpty();
        storages.Should().OnlyContain(s => !string.IsNullOrEmpty(s.Id) && !string.IsNullOrEmpty(s.Title) && s.Properties != null);
        storages.Should().NotContain(s => s.Current);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpdateStorage_ModuleNamingNoStorage_ThrowsBadRequest(bool cdn)
    {
        // Arrange
        var request = new StorageRequestDto("autotest-no-such-module", [new ItemKeyValuePairStringString("key", "value")]);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
        {
            if (cdn)
            {
                await Storage.UpdateCdnStorageAsync(request, TestContext.Current.CancellationToken);
            }
            else
            {
                await Storage.UpdateStorageAsync(request, TestContext.Current.CancellationToken);
            }
        });

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    /// <summary>
    /// SI3: on a server installation a portal that never started a migration has no progress remembered, and the
    /// value is dereferenced anyway, so the owner gets 500 instead of an answer.
    /// </summary>
    [Fact]
    [Trait("Category", "Bug")]
    public async Task GetStorageProgress_OwnerNoMigrationStarted_DoesNotFail()
    {
        // Act
        var progress = (await Storage.GetStorageProgressAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        progress.Should().BeInRange(0, 100);
    }
}

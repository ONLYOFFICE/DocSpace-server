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
/// Storage encryption refusals and reads. Starting encryption encrypts every portal of the installation, so it is
/// only ever called by callers it refuses before anything is queued.
/// </summary>
[Trait("Category", "Settings")]
public class EncryptionAccessTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    /// <summary>
    /// SI10: the action catches every exception, so a caller without the portal-settings right gets 200 with an empty
    /// body instead of the refusal the other storage settings give.
    /// </summary>
    [Fact]
    [Trait("Category", "Bug")]
    public async Task GetStorageEncryptionSettings_User_ThrowsAccessDenied()
    {
        // Arrange
        await _webApiClient.Authenticate(await InviteContact(EmployeeType.User));

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _encryptionApi.GetStorageEncryptionSettingsAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task StartStorageEncryption_NonAdministrator_ThrowsAccessDenied(EmployeeType employeeType)
    {
        // Arrange
        await _webApiClient.Authenticate(await InviteMember(employeeType));

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _encryptionApi.StartStorageEncryptionAsync(new StorageEncryptionRequestDto(false), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task StartStorageEncryption_Anonymous_ThrowsUnauthorized()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _encryptionApi.StartStorageEncryptionAsync(new StorageEncryptionRequestDto(false), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task GetStorageEncryptionProgress_OwnerNoRunInFlight_ReturnsNothing()
    {
        // Act
        var progress = (await _encryptionApi.GetStorageEncryptionProgressAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        progress.Should().BeNull();
    }
}

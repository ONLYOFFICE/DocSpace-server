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


namespace ASC.Data.Backup.Tests.Tests.Schedule;

/// <summary>
/// GET /api/2.0/settings/storage/backup: the storages a backup can be written to, with the one of a saved
/// third-party schedule marked current, and who may read the list. It is served by Web.Api and reads the
/// schedule of the backup service.
/// </summary>
public class GetAllBackupStoragesTests(
    AspireAppFixture fixture)
    : BackupScheduleTestBase(fixture)
{
    private async Task<List<StorageDto>> ReadStoragesAsync()
    {
        var answer = await _settingsStorageApi.GetAllBackupStoragesAsync(cancellationToken: TestContext.Current.CancellationToken);

        return answer.Response ?? [];
    }

    [Fact]
    public async Task GetAllBackupStorages_NoSchedule_ListsTheConsumersAndNoneIsCurrent()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var storages = await ReadStoragesAsync();

        // Assert
        storages.Should().NotBeEmpty();
        storages.Should().OnlyContain(storage => !string.IsNullOrEmpty(storage.Id));
        storages.Should().OnlyContain(storage => !storage.Current);
    }

    [Fact]
    public async Task GetAllBackupStorages_DataStoreSchedule_NoneIsCurrent()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        await SetScheduleAsync(EveryDay(4));

        // Act
        var storages = await ReadStoragesAsync();

        // Assert
        storages.Should().NotBeEmpty();
        storages.Should().OnlyContain(storage => !storage.Current);
    }

    [Fact]
    public async Task GetAllBackupStorages_ThirdPartyConsumerSchedule_MarksExactlyThatConsumerCurrent()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var module = (await ReadStoragesAsync()).First().Id;

        var dto = EveryDay(5);
        dto.StorageType = BackupStorageType.ThirdPartyConsumer;
        dto.StorageParams = [new ItemKeyValuePairObjectObject("module", module)];
        await SetScheduleAsync(dto);

        // Act
        var storages = await ReadStoragesAsync();

        // Assert
        storages.Where(storage => storage.Current).Select(storage => storage.Id).Should().Equal(module);
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task GetAllBackupStorages_NonAdmin_ReturnsForbidden(EmployeeType employeeType)
    {
        // Arrange
        var member = await InviteMember(employeeType);
        await _webApiClient.Authenticate(member);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            () => _settingsStorageApi.GetAllBackupStoragesAsync(cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetAllBackupStorages_DocSpaceAdmin_ListsTheConsumers()
    {
        // Arrange
        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(admin);

        // Act
        var storages = await ReadStoragesAsync();

        // Assert
        storages.Should().NotBeEmpty();
    }
}

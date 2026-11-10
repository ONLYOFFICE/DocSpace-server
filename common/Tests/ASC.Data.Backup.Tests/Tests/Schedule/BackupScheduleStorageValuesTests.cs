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
/// POST /api/2.0/backup/createbackupschedule into the storages a schedule can name: the portal documents
/// (a room), a Nextcloud account and an AWS S3 bucket. The last two skip when their credentials are not in
/// the environment: Nextcloud comes from <c>NEXTCLOUD_URL</c>, <c>NEXTCLOUD_LOGIN</c> and
/// <c>NEXTCLOUD_PASSWORD</c>, which the fixture fills from the Nextcloud container of the stack; S3 from
/// <c>S3_ACCESS_KEY</c>, <c>S3_SECRET_KEY</c>, <c>S3_BUCKET</c> and <c>S3_REGION</c>.
/// </summary>
public class BackupScheduleStorageValuesTests(
    AspireAppFixture fixture)
    : BackupScheduleTestBase(fixture)
{
    [Fact]
    public async Task CreateBackupSchedule_MovedBetweenStorages_ReportsTheFolderOnlyWhileItIsDocuments()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        var room = await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto(title: "Autotest Schedule Storage Room", roomType: RoomType.CustomRoom),
            TestContext.Current.CancellationToken);
        var roomId = room.Response.Id.ToString();

        // Act & Assert - DataStore has no folder
        var dataStore = await SetScheduleAsync(EveryDay(2));
        FolderOf(dataStore).Should().BeNull();

        // DataStore -> Documents: the folder is the room
        var documents = EveryDay(2);
        documents.StorageType = BackupStorageType.Documents;
        documents.StorageParams = [new ItemKeyValuePairObjectObject("folderId", roomId)];

        FolderOf(await SetScheduleAsync(documents)).Should().Be(roomId);

        // Documents -> DataStore: the folder is gone
        FolderOf(await SetScheduleAsync(EveryDay(2))).Should().BeNull();
    }

    [Fact]
    public async Task CreateBackupSchedule_ThirdPartyStorage_ReportsTheFolder()
    {
        // Arrange
        Assert.SkipWhen(
            EnvironmentValue("NEXTCLOUD_URL") == "" || EnvironmentValue("NEXTCLOUD_LOGIN") == "" || EnvironmentValue("NEXTCLOUD_PASSWORD") == "",
            "Nextcloud is not configured: set NEXTCLOUD_URL, NEXTCLOUD_LOGIN and NEXTCLOUD_PASSWORD.");

        await _filesClient.Authenticate(Owner);

        var connection = await _thirdPartyIntegrationApi.SaveThirdPartyBackupAsync(
            new ThirdPartyBackupRequestDto
            {
                Url = EnvironmentValue("NEXTCLOUD_URL"),
                Login = EnvironmentValue("NEXTCLOUD_LOGIN"),
                Password = EnvironmentValue("NEXTCLOUD_PASSWORD"),
                CustomerTitle = "Nextcloud Backup",
                ProviderKey = "Nextcloud"
            },
            TestContext.Current.CancellationToken);
        var folderId = connection.Response.Id;

        var dto = EveryDay(1);
        dto.StorageType = BackupStorageType.ThridpartyDocuments;
        dto.StorageParams = [new ItemKeyValuePairObjectObject("folderId", folderId)];

        // Act
        var schedule = await SetScheduleAsync(dto);

        // Assert
        FolderOf(schedule).Should().Be(folderId);
    }

    [Fact]
    public async Task CreateBackupSchedule_AwsS3_ReportsTheSavedParameters()
    {
        // Arrange
        Assert.SkipWhen(
            EnvironmentValue("S3_ACCESS_KEY") == "" || EnvironmentValue("S3_SECRET_KEY") == ""
            || EnvironmentValue("S3_BUCKET") == "" || EnvironmentValue("S3_REGION") == "",
            "AWS S3 is not configured: set S3_ACCESS_KEY, S3_SECRET_KEY, S3_BUCKET and S3_REGION.");

        await _webApiClient.Authenticate(Owner);

        await _settingsAuthorizationApi.SaveAuthKeysAsync(
            new SaveAuthKeysRequestDto
            {
                Name = "s3",
                Props =
                [
                    new AuthKeyRequest("acesskey", EnvironmentValue("S3_ACCESS_KEY")),
                    new AuthKeyRequest("secretaccesskey", EnvironmentValue("S3_SECRET_KEY"))
                ]
            },
            TestContext.Current.CancellationToken);

        var dto = EveryDay(1);
        dto.StorageType = BackupStorageType.ThirdPartyConsumer;
        dto.StorageParams =
        [
            new ItemKeyValuePairObjectObject("module", "s3"),
            new ItemKeyValuePairObjectObject("bucket", EnvironmentValue("S3_BUCKET")),
            new ItemKeyValuePairObjectObject("region", EnvironmentValue("S3_REGION")),
            new ItemKeyValuePairObjectObject("serviceurl", ""),
            new ItemKeyValuePairObjectObject("forcepathstyle", "false"),
            new ItemKeyValuePairObjectObject("usehttp", "false")
        ];

        // Act
        var schedule = await SetScheduleAsync(dto);

        // Assert - the parameters come back as an object keyed by name, and for this storage they are the
        // saved ones, not rebuilt from a path
        schedule.StorageParams.Should().BeEquivalentTo(new Dictionary<string, string>
        {
            ["module"] = "s3",
            ["bucket"] = EnvironmentValue("S3_BUCKET"),
            ["region"] = EnvironmentValue("S3_REGION"),
            ["serviceurl"] = "",
            ["forcepathstyle"] = "false",
            ["usehttp"] = "false"
        });
    }

    // The folderId a stored schedule reports; the key is always present and its value is null when the
    // storage has no folder.
    private static string? FolderOf(ScheduleDto schedule)
    {
        schedule.StorageParams.Should().ContainKey("folderId");

        return schedule.StorageParams["folderId"];
    }
}

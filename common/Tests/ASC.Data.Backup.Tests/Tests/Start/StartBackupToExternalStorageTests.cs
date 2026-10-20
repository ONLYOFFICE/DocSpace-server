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


namespace ASC.Data.Backup.Tests.Tests.Start;

/// <summary>
/// POST /api/2.0/backup/startbackup into storages outside the portal: a Nextcloud account and an AWS S3
/// bucket. Both skip when their credentials are not in the environment. Nextcloud comes from
/// <c>NEXTCLOUD_URL</c>, <c>NEXTCLOUD_LOGIN</c> and <c>NEXTCLOUD_PASSWORD</c>, which the fixture fills
/// from the Nextcloud container of the stack; S3 from <c>S3_ACCESS_KEY</c>, <c>S3_SECRET_KEY</c>,
/// <c>S3_BUCKET</c> and <c>S3_REGION</c>. The storage is always connected as the owner, whoever then
/// starts the backup.
/// </summary>
public class StartBackupToExternalStorageTests(
    AspireAppFixture fixture)
    : BackupFunctionalTestBase(fixture)
{
    private static string EnvironmentValue(string name)
    {
        return Environment.GetEnvironmentVariable(name) ?? "";
    }

    [Theory]
    [MemberData(nameof(BackupCallerData.All), MemberType = typeof(BackupCallerData))]
    public async Task StartBackup_ThirdPartyResource_StartsAndFinishes(BackupCaller caller)
    {
        // Arrange
        Assert.SkipWhen(
            EnvironmentValue("NEXTCLOUD_URL") == "" || EnvironmentValue("NEXTCLOUD_LOGIN") == "" || EnvironmentValue("NEXTCLOUD_PASSWORD") == "",
            "Nextcloud is not configured: set NEXTCLOUD_URL, NEXTCLOUD_LOGIN and NEXTCLOUD_PASSWORD.");

        await _filesClient.Authenticate(Owner);

        var folder = await _thirdPartyIntegrationApi.SaveThirdPartyBackupAsync(
            new ThirdPartyBackupRequestDto
            {
                Url = EnvironmentValue("NEXTCLOUD_URL"),
                Login = EnvironmentValue("NEXTCLOUD_LOGIN"),
                Password = EnvironmentValue("NEXTCLOUD_PASSWORD"),
                CustomerTitle = "Nextcloud Backup",
                ProviderKey = "Nextcloud"
            },
            TestContext.Current.CancellationToken);

        await ActAsAsync(caller);

        // Act & Assert
        await StartBackupAndWaitAsync(new StartBackupRequestDto
        {
            StorageType = BackupStorageType.ThridpartyDocuments,
            StorageParams = [new ItemKeyValuePairObjectObject("folderId", folder.Response.Id)]
        });
    }

    [Theory]
    [MemberData(nameof(BackupCallerData.All), MemberType = typeof(BackupCallerData))]
    public async Task StartBackup_AwsS3_StartsAndFinishes(BackupCaller caller)
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

        await ActAsAsync(caller);

        // Act & Assert
        await StartBackupAndWaitAsync(new StartBackupRequestDto
        {
            StorageType = BackupStorageType.ThirdPartyConsumer,
            StorageParams =
            [
                new ItemKeyValuePairObjectObject("module", "s3"),
                new ItemKeyValuePairObjectObject("bucket", EnvironmentValue("S3_BUCKET")),
                new ItemKeyValuePairObjectObject("region", EnvironmentValue("S3_REGION")),
                new ItemKeyValuePairObjectObject("serviceurl", ""),
                new ItemKeyValuePairObjectObject("forcepathstyle", "false"),
                new ItemKeyValuePairObjectObject("usehttp", "false")
            ]
        });
    }
}

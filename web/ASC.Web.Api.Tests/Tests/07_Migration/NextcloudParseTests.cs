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
/// POST /api/2.0/migration/init/Nextcloud on a backup uploaded through <c>migrationFileUpload.ashx</c>,
/// read back from GET /api/2.0/migration/status. The upload handler is not an API action, so it is
/// called over raw HTTP the way the web client calls it; the parse itself runs in Web.Studio.
/// The backup is a minimal Nextcloud layout: one top-level folder holding a MySQL dump and
/// <c>data/{uid}/files</c>, packed as each supported archive type.
/// </summary>
[Trait("Category", "Migration")]
public class NextcloudParseTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    private const string Uid = "alice";
    private const string Email = "alice.smith@example.com";
    private const string FileContent = "hello from nextcloud";

    private const string Dump =
        $"INSERT INTO `oc_accounts` VALUES ('{Uid}','{{}}');\n" +
        $"INSERT INTO `oc_accounts_data` VALUES (1,'{Uid}','displayname','Alice Smith'),(2,'{Uid}','email','{Email}');\n" +
        $"INSERT INTO `oc_storages` VALUES (7,'home::{Uid}',1,NULL);\n" +
        "INSERT INTO `oc_filecache` VALUES (10,7,'files'),(11,7,'files/hello.txt'),(12,7,'files/link.txt');\n";

    [Theory]
    [InlineData("backup.zip")]
    [InlineData("backup.ZIP")]
    [InlineData("backup.tar.gz")]
    [InlineData("backup.TAR.GZ")]
    [InlineData("backup.tgz")]
    public async Task Parse_SupportedArchive_ReadsUsersAndFiles(string fileName)
    {
        // Arrange
        var archive = fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ? BuildZip() : BuildTarGz(withLink: false);
        await UploadBackupAsync(fileName, archive);

        // Act
        await _migrationApi.UploadAndInitializeMigrationAsync("Nextcloud", TestContext.Current.CancellationToken);
        var status = await WaitForParseAsync();

        // Assert
        status.IsCompleted.Should().BeTrue();
        status.Error.Should().BeNullOrEmpty();
        status.ParseResult.FailedArchives.Should().BeEmpty();
        var user = status.ParseResult.Users.Should().ContainSingle().Which;
        user.Email.Should().Be(Email);
        user.FirstName.Should().Be("Alice");
        user.LastName.Should().Be("Smith");
        user.MigratingFiles.FilesCount.Should().Be(1);
        user.MigratingFiles.BytesTotal.Should().Be(Encoding.UTF8.GetByteCount(FileContent));
    }

    /// <summary>
    /// A tar entry can be a symbolic link whose target lies outside the extraction folder. Such
    /// entries are not extracted, so the link listed in the dump is not counted as a file of the
    /// user - only the regular file next to it is.
    /// </summary>
    [Fact]
    public async Task Parse_TarGzWithSymbolicLink_SkipsLink()
    {
        // Arrange
        await UploadBackupAsync("backup.tar.gz", BuildTarGz(withLink: true));

        // Act
        await _migrationApi.UploadAndInitializeMigrationAsync("Nextcloud", TestContext.Current.CancellationToken);
        var status = await WaitForParseAsync();

        // Assert
        status.IsCompleted.Should().BeTrue();
        status.Error.Should().BeNullOrEmpty();
        var user = status.ParseResult.Users.Should().ContainSingle().Which;
        user.MigratingFiles.FilesCount.Should().Be(1);
        user.MigratingFiles.BytesTotal.Should().Be(Encoding.UTF8.GetByteCount(FileContent));
    }

    [Fact]
    public async Task Parse_UnsupportedArchive_ReportsAcceptedFormats()
    {
        // Arrange
        await UploadBackupAsync("backup.rar", BuildZip());

        // Act
        await _migrationApi.UploadAndInitializeMigrationAsync("Nextcloud", TestContext.Current.CancellationToken);
        var status = await WaitForParseAsync();

        // Assert
        status.IsCompleted.Should().BeTrue();
        status.Error.Should().Contain(".zip, .tar.gz or .tgz");
    }

    private async Task UploadBackupAsync(string fileName, byte[] archive)
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        using var init = await _webApiClient.PostAsync("migrationFileUpload.ashx?Init=true", null, cancellationToken);
        await AssertUploadSucceededAsync(init);

        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(archive), "file", fileName);
        using var upload = await _webApiClient.PostAsync($"migrationFileUpload.ashx?Name={Uri.EscapeDataString(fileName)}", form, cancellationToken);
        await AssertUploadSucceededAsync(upload);
    }

    private static async Task AssertUploadSucceededAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("Success").GetBoolean().Should().BeTrue(body);
    }

    private async Task<MigrationStatusDto> WaitForParseAsync()
    {
        var deadline = DateTime.UtcNow.AddSeconds(60);

        while (true)
        {
            var status = (await _migrationApi.GetMigrationStatusAsync(TestContext.Current.CancellationToken)).Response;

            if (status is { IsCompleted: true } || DateTime.UtcNow >= deadline)
            {
                return status;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }

    private static byte[] BuildZip()
    {
        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteZipEntry(zip, "nextcloud/nextcloud.sql", Dump);
            WriteZipEntry(zip, $"nextcloud/data/{Uid}/files/hello.txt", FileContent);
        }

        return buffer.ToArray();
    }

    private static void WriteZipEntry(ZipArchive zip, string name, string content)
    {
        using var writer = new StreamWriter(zip.CreateEntry(name).Open());
        writer.Write(content);
    }

    private static byte[] BuildTarGz(bool withLink)
    {
        using var buffer = new MemoryStream();
        using (var gzip = new GZipStream(buffer, CompressionLevel.Fastest, leaveOpen: true))
        using (var tar = new TarWriter(gzip))
        {
            tar.WriteEntry(new PaxTarEntry(TarEntryType.Directory, "nextcloud/"));
            WriteTarEntry(tar, "nextcloud/nextcloud.sql", Dump);

            // the link goes before the regular file: an extractor that fails on it then loses the file too
            if (withLink)
            {
                tar.WriteEntry(new PaxTarEntry(TarEntryType.SymbolicLink, $"nextcloud/data/{Uid}/files/link.txt") { LinkName = "/etc/passwd" });
            }

            WriteTarEntry(tar, $"nextcloud/data/{Uid}/files/hello.txt", FileContent);
        }

        return buffer.ToArray();
    }

    private static void WriteTarEntry(TarWriter tar, string name, string content)
    {
        tar.WriteEntry(new PaxTarEntry(TarEntryType.RegularFile, name)
        {
            DataStream = new MemoryStream(Encoding.UTF8.GetBytes(content))
        });
    }
}

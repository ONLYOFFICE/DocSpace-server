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
/// POST /api/2.0/migration/init/Nextcloud on an uploaded backup, read back from
/// GET /api/2.0/migration/status. The backup is a minimal Nextcloud layout: one top-level folder
/// holding a MySQL dump and <c>data/{uid}/files</c>, packed as each supported archive type.
/// </summary>
[Trait("Category", "Migration")]
public class NextcloudParseTests(
    AspireAppFixture fixture)
    : MigrationParseTestBase(fixture)
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

    /// <summary>
    /// Paths in <c>oc_filecache</c> come from the uploaded dump. A row that climbs out of the user's
    /// folder with ".." - here to the uploaded archive itself, which lies next to the extracted
    /// backup - is skipped instead of being imported, and the user's real file is still read.
    /// </summary>
    [Fact]
    public async Task Parse_FileCachePathOutsideUserFolder_SkipsEntry()
    {
        // Arrange
        // every ancestor gets its own row, as the migrator resolves a file's parent folder by path
        var dump = AddRows(Dump, "oc_filecache",
            "(13,7,'files/..'),(14,7,'files/../..'),(15,7,'files/../../..'),(16,7,'files/../../../..'),(17,7,'files/../../../../backup.zip')");
        await UploadBackupAsync("backup.zip", BuildZip(dump));

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

    /// <summary>
    /// The user id from <c>oc_accounts</c> names the user's folder under <c>data/</c>. An id of ".."
    /// would point that folder at the backup root, so the user is not offered for import; the other
    /// users of the dump still are.
    /// </summary>
    [Fact]
    public async Task Parse_UserIdOutsideDataFolder_SkipsUser()
    {
        // Arrange
        var dump = Dump;
        dump = AddRows(dump, "oc_accounts", "('..','{}')");
        dump = AddRows(dump, "oc_accounts_data", "(3,'..','displayname','Mallory Evil'),(4,'..','email','mallory@example.com')");
        dump = AddRows(dump, "oc_storages", "(8,'home::..',1,NULL)");
        dump = AddRows(dump, "oc_filecache", "(20,8,'files'),(21,8,'files/hello.txt')");
        await UploadBackupAsync("backup.zip", BuildZip(dump, "nextcloud/files/hello.txt"));

        // Act
        await _migrationApi.UploadAndInitializeMigrationAsync("Nextcloud", TestContext.Current.CancellationToken);
        var status = await WaitForParseAsync();

        // Assert
        status.IsCompleted.Should().BeTrue();
        status.Error.Should().BeNullOrEmpty();
        status.ParseResult.Users.Should().ContainSingle().Which.Email.Should().Be(Email);
        status.ParseResult.ExistUsers.Should().BeEmpty();
        status.ParseResult.WithoutEmailUsers.Should().BeEmpty();
    }

    /// <summary>
    /// An archive may hold the same path twice - <c>tar czf b.tgz nextcloud nextcloud/data</c> stores
    /// <c>data/</c> twice. The later entry wins, as with unzip and tar, and the entries after the
    /// repeated one are still unpacked.
    /// </summary>
    [Theory]
    [InlineData("backup.zip")]
    [InlineData("backup.tar.gz")]
    public async Task Parse_ArchiveWithRepeatedPath_KeepsLaterEntry(string fileName)
    {
        // Arrange
        var helloPath = $"nextcloud/data/{Uid}/files/hello.txt";
        var dump = AddRows(Dump, "oc_filecache", "(13,7,'files/after.txt')");
        await UploadBackupAsync(fileName, BuildArchive(fileName,
            ("nextcloud/nextcloud.sql", dump),
            (helloPath, "stale"),
            (helloPath, FileContent),
            ($"nextcloud/data/{Uid}/files/after.txt", FileContent)));

        // Act
        await _migrationApi.UploadAndInitializeMigrationAsync("Nextcloud", TestContext.Current.CancellationToken);
        var status = await WaitForParseAsync();

        // Assert
        status.IsCompleted.Should().BeTrue();
        status.Error.Should().BeNullOrEmpty();
        var user = status.ParseResult.Users.Should().ContainSingle().Which;
        user.MigratingFiles.FilesCount.Should().Be(2);
        user.MigratingFiles.BytesTotal.Should().Be(2 * Encoding.UTF8.GetByteCount(FileContent));
    }

    /// <summary>
    /// Every progress report publishes the operation state and writes a line to the migration log,
    /// so unpacking reports at most once per whole percent of its 5-50% share, however many entries
    /// the backup holds.
    /// </summary>
    [Theory]
    [InlineData("backup.zip")]
    [InlineData("backup.tar.gz")]
    public async Task Parse_ArchiveWithManyEntries_ReportsUnpackingOncePerPercent(string fileName)
    {
        // Arrange
        const int extraFiles = 300;
        var entries = new List<(string Name, string Content)>
        {
            ("nextcloud/nextcloud.sql", Dump),
            ($"nextcloud/data/{Uid}/files/hello.txt", FileContent)
        };
        entries.AddRange(Enumerable.Range(0, extraFiles).Select(i => ($"nextcloud/data/{Uid}/files/extra/{i}.txt", FileContent)));
        await UploadBackupAsync(fileName, BuildArchive(fileName, [.. entries]));

        // Act
        await _migrationApi.UploadAndInitializeMigrationAsync("Nextcloud", TestContext.Current.CancellationToken);
        var status = await WaitForParseAsync();
        var log = await GetMigrationLogAsync();

        // Assert
        status.IsCompleted.Should().BeTrue();
        status.Error.Should().BeNullOrEmpty();
        var unzippingReports = log.Split('\n').Count(line => line.TrimEnd().EndsWith("progress: Unzipping...", StringComparison.Ordinal));
        unzippingReports.Should().BeInRange(1, 46, log);
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

    /// <summary>
    /// The migrator reads only the first INSERT of each table, so extra rows go into that statement.
    /// </summary>
    private static string AddRows(string dump, string table, string rows)
    {
        var start = dump.IndexOf($"INSERT INTO `{table}` VALUES ", StringComparison.Ordinal);
        var end = dump.IndexOf(";\n", start, StringComparison.Ordinal);
        return dump.Insert(end, "," + rows);
    }

    private static byte[] BuildArchive(string fileName, params (string Name, string Content)[] entries)
    {
        using var buffer = new MemoryStream();
        if (fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            using var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true);
            foreach (var (name, content) in entries)
            {
                WriteZipEntry(zip, name, content);
            }
        }
        else
        {
            using var gzip = new GZipStream(buffer, CompressionLevel.Fastest, leaveOpen: true);
            using var tar = new TarWriter(gzip);
            foreach (var (name, content) in entries)
            {
                WriteTarEntry(tar, name, content);
            }
        }

        return buffer.ToArray();
    }

    private static byte[] BuildZip(string dump = Dump, string? extraFile = null)
    {
        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteZipEntry(zip, "nextcloud/nextcloud.sql", dump);
            WriteZipEntry(zip, $"nextcloud/data/{Uid}/files/hello.txt", FileContent);
            if (extraFile != null)
            {
                WriteZipEntry(zip, extraFile, FileContent);
            }
        }

        return buffer.ToArray();
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
}

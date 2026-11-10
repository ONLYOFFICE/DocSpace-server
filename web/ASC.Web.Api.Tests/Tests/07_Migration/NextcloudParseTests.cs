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
/// GET /api/2.0/migration/status, and the import that follows it. What the parse does with the
/// entries of a broken or hostile archive is in <see cref="NextcloudUnpackTests"/>.
/// </summary>
[Trait("Category", "Migration")]
public class NextcloudParseTests(
    AspireAppFixture fixture)
    : NextcloudParseTestBase(fixture)
{
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

    /// <summary>
    /// The import after a parse reads the tree the parse unpacked next to the archive. It used to
    /// unpack the archive again over that tree, which failed on the first file already there and
    /// wrote "Couldn't unzip" into the log of every import; now that an unpacking error fails the
    /// parse, unpacking again would fail the import itself.
    /// </summary>
    [Theory]
    [InlineData("backup.zip")]
    [InlineData("backup.tar.gz")]
    public async Task Migrate_ParsedArchive_ImportsUserFromUnpackedTree(string fileName)
    {
        // Arrange
        var archive = fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ? BuildZip() : BuildTarGz(withLink: false);
        await UploadBackupAsync(fileName, archive);
        await _migrationApi.UploadAndInitializeMigrationAsync("Nextcloud", TestContext.Current.CancellationToken);
        var parsed = await WaitForParseAsync();
        parsed.Error.Should().BeNullOrEmpty();
        var user = parsed.ParseResult.Users.Should().ContainSingle().Which;

        // Act
        await StartMigrationAsync(user);
        var status = await WaitForMigrationAsync();
        var log = await GetMigrationLogAsync();

        // Assert
        status.IsCompleted.Should().BeTrue();
        status.Error.Should().BeNullOrEmpty();
        status.ParseResult.SuccessedUsers.Should().Be(1);
        status.ParseResult.FailedUsers.Should().Be(0);
        status.ParseResult.Errors.Should().BeEmpty();
        log.Should().NotContain("Couldn't unzip");
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
    /// The SDK's <c>MigratingApiUser</c> has no <c>shouldImport</c> - the server's model takes it from
    /// a base class the generated client does not carry - so the import is started over raw HTTP with
    /// the body the web client sends.
    /// </summary>
    private async Task StartMigrationAsync(MigratingApiUser user)
    {
        var body = new
        {
            migratorName = "Nextcloud",
            users = new[] { new { key = user.Key, email = user.Email, firstName = user.FirstName, lastName = user.LastName, userType = "User", shouldImport = true } },
            importPersonalFiles = true
        };
        using var response = await _webApiClient.PostAsJsonAsync("api/2.0/migration/migrate", body, TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Right after the import is queued the status may still describe the finished parse, so the wait
    /// is for a completed job of the import itself.
    /// </summary>
    private async Task<MigrationStatusDto> WaitForMigrationAsync()
    {
        var deadline = DateTime.UtcNow.AddSeconds(60);

        while (true)
        {
            var status = (await _migrationApi.GetMigrationStatusAsync(TestContext.Current.CancellationToken)).Response;

            if (status is { IsCompleted: true, ParseResult.Operation: "migration" } || DateTime.UtcNow >= deadline)
            {
                return status;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }
}

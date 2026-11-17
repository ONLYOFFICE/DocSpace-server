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
/// POST /api/2.0/migration/init/Nextcloud on a backup whose entries are not plain files and
/// folders inside the backup: links, paths that leave the extraction folder, an archive cut short.
/// None of them may write outside the folder, and none may leave part of the backup out of an
/// import that looks complete.
/// </summary>
[Trait("Category", "Migration")]
public class NextcloudUnpackTests(
    AspireAppFixture fixture)
    : NextcloudParseTestBase(fixture)
{
    /// <summary>
    /// A tar entry can be a symbolic link whose target lies outside the extraction folder. Such
    /// entries are not extracted, so the link listed in the dump is not counted as a file of the
    /// user - only the regular file next to it is. The skipped entry is named in the migration log,
    /// so a file missing from the import can be traced.
    /// </summary>
    [Fact]
    public async Task Parse_TarGzWithSymbolicLink_SkipsLink()
    {
        // Arrange
        await UploadBackupAsync("backup.tar.gz", BuildTarGz(withLink: true));

        // Act
        await _migrationApi.UploadAndInitializeMigrationAsync("Nextcloud", TestContext.Current.CancellationToken);
        var status = await WaitForParseAsync();
        var log = await GetMigrationLogAsync();

        // Assert
        status.IsCompleted.Should().BeTrue();
        status.Error.Should().BeNullOrEmpty();
        var user = status.ParseResult.Users.Should().ContainSingle().Which;
        user.MigratingFiles.FilesCount.Should().Be(1);
        user.MigratingFiles.BytesTotal.Should().Be(Encoding.UTF8.GetByteCount(FileContent));
        log.Should().Contain($"Skipped nextcloud/data/{Uid}/files/link.txt: entries of type SymbolicLink are not unpacked");
    }

    /// <summary>
    /// tar stores a file with two names once and the second name as a hard link to the first. The
    /// link used to be skipped without a word, so the file vanished under that name; it is now
    /// unpacked as a copy of the file it names, which the archive has already unpacked.
    /// </summary>
    [Fact]
    public async Task Parse_TarGzWithHardLink_UnpacksLinkAsCopy()
    {
        // Arrange
        await UploadBackupAsync("backup.tar.gz", BuildTarGzWithHardLink($"nextcloud/data/{Uid}/files/hello.txt"));

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
    /// A hard link may name any path, and a copy of what it names would be imported as the user's
    /// file. Only a file this extraction unpacked is copied: a link out of the extraction folder,
    /// or to a file that was there before - the uploaded archive, which lies next to the unpacked
    /// tree - is skipped and logged like a symbolic link.
    /// </summary>
    [Theory]
    [InlineData("../../../../etc/passwd")]
    [InlineData("backup.tar.gz")]
    public async Task Parse_TarGzWithHardLinkToFileNotUnpacked_SkipsLink(string linkName)
    {
        // Arrange
        await UploadBackupAsync("backup.tar.gz", BuildTarGzWithHardLink(linkName));

        // Act
        await _migrationApi.UploadAndInitializeMigrationAsync("Nextcloud", TestContext.Current.CancellationToken);
        var status = await WaitForParseAsync();
        var log = await GetMigrationLogAsync();

        // Assert
        status.IsCompleted.Should().BeTrue();
        status.Error.Should().BeNullOrEmpty();
        var user = status.ParseResult.Users.Should().ContainSingle().Which;
        user.MigratingFiles.FilesCount.Should().Be(1);
        log.Should().Contain($"Skipped nextcloud/data/{Uid}/files/copy.txt: entries of type HardLink are not unpacked");
    }

    /// <summary>
    /// An entry whose name climbs out of the extraction folder with ".." or is an absolute path is
    /// never written; it fails the parse, as any broken entry does, rather than being left out of
    /// an import that otherwise looks complete.
    /// </summary>
    [Theory]
    [InlineData("backup.tar.gz", "../evil.txt")]
    [InlineData("backup.tar.gz", "nextcloud/../../evil.txt")]
    [InlineData("backup.tar.gz", "/tmp/evil.txt")]
    [InlineData("backup.zip", "../evil.txt")]
    [InlineData("backup.zip", "nextcloud/../../evil.txt")]
    public async Task Parse_ArchiveEntryOutsideFolder_FailsParse(string fileName, string entryName)
    {
        // Arrange
        await UploadBackupAsync(fileName, BuildArchive(fileName,
            ("nextcloud/nextcloud.sql", Dump),
            ($"nextcloud/data/{Uid}/files/hello.txt", FileContent),
            (entryName, "evil")));

        // Act
        await _migrationApi.UploadAndInitializeMigrationAsync("Nextcloud", TestContext.Current.CancellationToken);
        var status = await WaitForParseAsync();
        var log = await GetMigrationLogAsync();

        // Assert
        status.IsCompleted.Should().BeTrue();
        status.Error.Should().Contain("Couldn't parse");
        status.ParseResult.FailedArchives.Should().Equal(fileName);
        log.Should().Contain("Archive entry is outside the extraction directory");
    }

    /// <summary>
    /// An archive that was cut short fails the parse. Before, an unpacking error was only logged and
    /// the parse went on with the part already unpacked: the dump comes first in a backup, so a
    /// .tar.gz cut in the middle parsed "successfully" with the files after the cut silently missing.
    /// GZipStream does not notice a cut-off stream, so the tar that ends without its end-of-archive
    /// block - a cut on an entry boundary - is covered on its own.
    /// </summary>
    [Theory]
    [InlineData("backup.tar.gz", "cut")]
    [InlineData("backup.tar.gz", "no end block")]
    [InlineData("backup.zip", "cut")]
    public async Task Parse_TruncatedArchive_FailsParse(string fileName, string truncation)
    {
        // Arrange
        await UploadBackupAsync(fileName, BuildTruncatedArchive(fileName, truncation));

        // Act
        await _migrationApi.UploadAndInitializeMigrationAsync("Nextcloud", TestContext.Current.CancellationToken);
        var status = await WaitForParseAsync();

        // Assert
        status.IsCompleted.Should().BeTrue();
        status.Error.Should().Contain("Couldn't parse");
        status.ParseResult.FailedArchives.Should().Equal(fileName);
    }
}

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
/// POST /api/2.0/migration/init/{migratorName} on a backup that is small when uploaded but unpacks
/// to more than it may. The quota check before the parse sees only the compressed upload, so the
/// limit is enforced while the backup is unpacked and the parse fails with the "backup is larger"
/// error. The test portals have an unlimited quota, so the limit here is the
/// <c>migration:maxExtractedSize</c> ceiling of the integration-test profile; the entry is a run of
/// zeros one megabyte over it, which compresses to well under a megabyte. The byte limit does not see
/// directories and empty files, so the number of entries is limited too, by
/// <c>migration:maxExtractedEntries</c>; an archive of one entry more than that fails the same way.
/// </summary>
[Trait("Category", "Migration")]
public class MigrationSizeLimitTests(
    AspireAppFixture fixture)
    : MigrationParseTestBase(fixture)
{
    private const long MaxExtractedSize = ASC.AppHost.Configuration.Constants.MigrationTestMaxExtractedSize;
    private const long MaxExtractedEntries = ASC.AppHost.Configuration.Constants.MigrationTestMaxExtractedEntries;
    private const string LargeBackupError = "larger than the total size of the portal";

    /// <summary>
    /// The Nextcloud migrator unpacks the archive itself and stops once the bytes it has written
    /// pass the quota.
    /// </summary>
    [Fact]
    public async Task Parse_NextcloudTarGzLargerThanQuota_FailsWithLargeBackup()
    {
        // Arrange
        await UploadBackupAsync("backup.tar.gz", BuildZeroTarGz("nextcloud/data/alice/files/zeros.bin"));

        // Act
        await _migrationApi.UploadAndInitializeMigrationAsync("Nextcloud", TestContext.Current.CancellationToken);
        var status = await WaitForParseAsync();

        // Assert
        status.IsCompleted.Should().BeTrue();
        status.Error.Should().Contain(LargeBackupError);
        status.ParseResult.FailedArchives.Should().Equal("backup.tar.gz");
    }

    /// <summary>
    /// The ONLYOFFICE backup is unpacked by the migrator in a single pass that stops once the bytes
    /// written pass the quota.
    /// </summary>
    [Fact]
    public async Task Parse_WorkspaceBackupLargerThanQuota_FailsWithLargeBackup()
    {
        // Arrange
        await UploadBackupAsync("backup.tar.gz", BuildZeroTarGz("zeros.bin"));

        // Act
        await _migrationApi.UploadAndInitializeMigrationAsync("Workspace", TestContext.Current.CancellationToken);
        var status = await WaitForParseAsync();

        // Assert
        status.IsCompleted.Should().BeTrue();
        status.Error.Should().Contain(LargeBackupError);
        status.ParseResult.FailedArchives.Should().Equal("backup.tar.gz");
    }

    /// <summary>
    /// A Google Takeout over the quota is listed as the archive that failed, as with the other
    /// migrators, instead of the error passing the per-takeout catch with no archive named.
    /// </summary>
    [Fact]
    public async Task Parse_GoogleTakeoutLargerThanQuota_FailsWithLargeBackup()
    {
        // Arrange
        await UploadBackupAsync("takeout.zip", BuildZeroZip("Takeout/Drive/zeros.bin"));

        // Act
        await _migrationApi.UploadAndInitializeMigrationAsync("GoogleWorkspace", TestContext.Current.CancellationToken);
        var status = await WaitForParseAsync();

        // Assert
        status.IsCompleted.Should().BeTrue();
        status.Error.Should().Contain(LargeBackupError);
        status.ParseResult.FailedArchives.Should().Equal("takeout.zip");
    }

    /// <summary>
    /// An entry of a type that is not unpacked is still decompressed to be read past, so its data counts against
    /// the byte limit as well; otherwise a small archive could keep the parse inflating zeros for hours.
    /// </summary>
    [Fact]
    public async Task Parse_NextcloudTarGzWithLargeSkippedEntry_FailsWithLargeBackup()
    {
        // Arrange
        await UploadBackupAsync("backup.tar.gz", BuildUnknownTypeZeroTarGz("nextcloud/data/alice/files/zeros.bin"));

        // Act
        await _migrationApi.UploadAndInitializeMigrationAsync("Nextcloud", TestContext.Current.CancellationToken);
        var status = await WaitForParseAsync();

        // Assert
        status.IsCompleted.Should().BeTrue();
        status.Error.Should().Contain(LargeBackupError);
        status.ParseResult.FailedArchives.Should().Equal("backup.tar.gz");
    }

    /// <summary>
    /// A small .tar.gz of empty directories passes the byte limit however many it holds; the entry
    /// limit stops it, so it cannot exhaust inodes or keep the parse busy.
    /// </summary>
    [Fact]
    public async Task Parse_NextcloudTarGzWithTooManyEntries_FailsWithLargeBackup()
    {
        // Arrange
        await UploadBackupAsync("backup.tar.gz", BuildEmptyEntriesTarGz("nextcloud/data/alice/files/"));

        // Act
        await _migrationApi.UploadAndInitializeMigrationAsync("Nextcloud", TestContext.Current.CancellationToken);
        var status = await WaitForParseAsync();

        // Assert
        status.IsCompleted.Should().BeTrue();
        status.Error.Should().Contain(LargeBackupError);
        status.ParseResult.FailedArchives.Should().Equal("backup.tar.gz");
    }

    [Fact]
    public async Task Parse_NextcloudZipWithTooManyEntries_FailsWithLargeBackup()
    {
        // Arrange
        await UploadBackupAsync("backup.zip", BuildEmptyEntriesZip("nextcloud/data/alice/files/"));

        // Act
        await _migrationApi.UploadAndInitializeMigrationAsync("Nextcloud", TestContext.Current.CancellationToken);
        var status = await WaitForParseAsync();

        // Assert
        status.IsCompleted.Should().BeTrue();
        status.Error.Should().Contain(LargeBackupError);
        status.ParseResult.FailedArchives.Should().Equal("backup.zip");
    }

    [Fact]
    public async Task Parse_WorkspaceBackupWithTooManyEntries_FailsWithLargeBackup()
    {
        // Arrange
        await UploadBackupAsync("backup.tar.gz", BuildEmptyEntriesTarGz("extra/"));

        // Act
        await _migrationApi.UploadAndInitializeMigrationAsync("Workspace", TestContext.Current.CancellationToken);
        var status = await WaitForParseAsync();

        // Assert
        status.IsCompleted.Should().BeTrue();
        status.Error.Should().Contain(LargeBackupError);
        status.ParseResult.FailedArchives.Should().Equal("backup.tar.gz");
    }

    [Fact]
    public async Task Parse_GoogleTakeoutWithTooManyEntries_FailsWithLargeBackup()
    {
        // Arrange
        await UploadBackupAsync("takeout.zip", BuildEmptyEntriesZip("Takeout/Drive/"));

        // Act
        await _migrationApi.UploadAndInitializeMigrationAsync("GoogleWorkspace", TestContext.Current.CancellationToken);
        var status = await WaitForParseAsync();

        // Assert
        status.IsCompleted.Should().BeTrue();
        status.Error.Should().Contain(LargeBackupError);
        status.ParseResult.FailedArchives.Should().Equal("takeout.zip");
    }

    /// <summary>One empty file more than the entry limit allows.</summary>
    private static byte[] BuildEmptyEntriesZip(string prefix)
    {
        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            for (var i = 0; i <= MaxExtractedEntries; i++)
            {
                zip.CreateEntry($"{prefix}{i}.txt");
            }
        }

        return buffer.ToArray();
    }

    /// <summary>One empty directory more than the entry limit allows.</summary>
    private static byte[] BuildEmptyEntriesTarGz(string prefix)
    {
        using var buffer = new MemoryStream();
        using (var gzip = new GZipStream(buffer, CompressionLevel.Optimal, leaveOpen: true))
        using (var tar = new TarWriter(gzip))
        {
            for (var i = 0; i <= MaxExtractedEntries; i++)
            {
                tar.WriteEntry(new PaxTarEntry(TarEntryType.Directory, $"{prefix}{i}/"));
            }
        }

        return buffer.ToArray();
    }

    private static byte[] BuildZeroZip(string entryName)
    {
        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        using (var entry = zip.CreateEntry(entryName, CompressionLevel.Optimal).Open())
        {
            new ZeroStream(MaxExtractedSize + 1024 * 1024).CopyTo(entry);
        }

        return buffer.ToArray();
    }

    private static byte[] BuildZeroTarGz(string entryName)
    {
        using var buffer = new MemoryStream();
        using (var gzip = new GZipStream(buffer, CompressionLevel.Optimal, leaveOpen: true))
        using (var tar = new TarWriter(gzip))
        {
            WriteTarEntry(tar, entryName, new ZeroStream(MaxExtractedSize + 1024 * 1024));
        }

        return buffer.ToArray();
    }

    /// <summary>
    /// A .tar.gz holding one entry of a type TarReader does not know ('Z'), with zeros one megabyte over the limit as
    /// its data, then the end-of-archive block. TarWriter writes no such entry, so the ustar header is built by hand.
    /// </summary>
    private static byte[] BuildUnknownTypeZeroTarGz(string entryName)
    {
        var size = MaxExtractedSize + 1024 * 1024;
        var header = new byte[512];
        Encoding.ASCII.GetBytes(entryName).CopyTo(header, 0);
        Encoding.ASCII.GetBytes("0000644\0").CopyTo(header, 100);
        Encoding.ASCII.GetBytes("0000000\0").CopyTo(header, 108);
        Encoding.ASCII.GetBytes("0000000\0").CopyTo(header, 116);
        Encoding.ASCII.GetBytes(Convert.ToString(size, 8).PadLeft(11, '0') + "\0").CopyTo(header, 124);
        Encoding.ASCII.GetBytes("00000000000\0").CopyTo(header, 136);
        header[156] = (byte)'Z';
        Encoding.ASCII.GetBytes("ustar\0" + "00").CopyTo(header, 257);
        Array.Fill(header, (byte)' ', 148, 8);
        var checksum = header.Sum(b => (int)b);
        Encoding.ASCII.GetBytes(Convert.ToString(checksum, 8).PadLeft(6, '0') + "\0 ").CopyTo(header, 148);

        using var buffer = new MemoryStream();
        using (var gzip = new GZipStream(buffer, CompressionLevel.Optimal, leaveOpen: true))
        {
            gzip.Write(header);
            new ZeroStream(size).CopyTo(gzip);
            // the data is a whole number of blocks, so no padding; then the two zero blocks that end the archive
            gzip.Write(new byte[1024]);
        }

        return buffer.ToArray();
    }
}

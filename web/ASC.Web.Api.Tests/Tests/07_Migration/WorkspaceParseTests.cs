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
/// POST /api/2.0/migration/init/Workspace on an uploaded ONLYOFFICE backup, read back from
/// GET /api/2.0/migration/status. The backup is a minimal layout: the database tables the parse
/// reads, each a <see cref="DataTable"/> written with its schema as the backup writes them, one user
/// with a root folder holding one file, and that file's content under <c>files/</c>.
/// </summary>
[Trait("Category", "Migration")]
public class WorkspaceParseTests(
    AspireAppFixture fixture)
    : MigrationParseTestBase(fixture)
{
    private const string UserId = "6b5bd0b6-3f2a-4f8e-9d4c-2f1e5a7c9b10";
    private const string Email = "carol.white@example.com";
    private const string FileContent = "hello from workspace";

    /// <summary>
    /// The migrator unpacks the backup itself, a gzipped one as well as a plain tar, and the parse
    /// reads the user and the file from what it unpacked.
    /// </summary>
    [Theory]
    [InlineData("backup.tar.gz")]
    [InlineData("backup.tar")]
    public async Task Parse_Backup_ReadsUserAndFile(string fileName)
    {
        // Arrange
        await UploadBackupAsync(fileName, BuildBackup(gzip: fileName.EndsWith(".gz")));

        // Act
        await _migrationApi.UploadAndInitializeMigrationAsync("Workspace", TestContext.Current.CancellationToken);
        var status = await WaitForParseAsync();

        // Assert
        status.IsCompleted.Should().BeTrue();
        status.Error.Should().BeNullOrEmpty();
        status.ParseResult.FailedArchives.Should().BeEmpty();
        var user = status.ParseResult.Users.Should().ContainSingle().Which;
        user.Email.Should().Be(Email);
        user.MigratingFiles.FilesCount.Should().Be(1);
        user.MigratingFiles.BytesTotal.Should().Be(Encoding.UTF8.GetByteCount(FileContent));
    }

    /// <summary>
    /// Every progress report publishes the operation state and writes a line to the migration log,
    /// so unpacking reports at most once per whole percent of its 5-30% share - and does report
    /// within it, rather than standing at 5% until the whole backup is unpacked. The extra entries
    /// are random, so that a gzipped backup is read in many chunks too.
    /// </summary>
    [Theory]
    [InlineData("backup.tar.gz")]
    [InlineData("backup.tar")]
    public async Task Parse_BackupWithManyEntries_ReportsUnpackingOncePerPercent(string fileName)
    {
        // Arrange
        await UploadBackupAsync(fileName, BuildBackup(gzip: fileName.EndsWith(".gz"), extraFiles: 300));

        // Act
        await _migrationApi.UploadAndInitializeMigrationAsync("Workspace", TestContext.Current.CancellationToken);
        var status = await WaitForParseAsync();
        var log = await GetMigrationLogAsync();

        // Assert
        status.IsCompleted.Should().BeTrue();
        status.Error.Should().BeNullOrEmpty();
        CountProgressReports(log, "progress: Unzipping...").Should().BeInRange(2, 26, log);
    }

    /// <summary>
    /// The photo manager decodes a profile photo whole, and a photo of a few kilobytes can declare
    /// any number of pixels. The import reads the size from the header first and leaves a photo over
    /// 4096 x 4096 out with a line in the log; the user is imported all the same. A photo of an
    /// ordinary size is imported as before.
    /// </summary>
    [Theory]
    [InlineData(64, false)]
    [InlineData(5000, true)]
    public async Task Migrate_UserPhoto_SkipsPhotoOverPixelLimit(int side, bool skipped)
    {
        // Arrange
        await UploadBackupAsync("backup.tar.gz", BuildBackup(gzip: true, photo: BuildBlankPng(side, side)));
        await _migrationApi.UploadAndInitializeMigrationAsync("Workspace", TestContext.Current.CancellationToken);
        var parsed = await WaitForParseAsync();
        parsed.Error.Should().BeNullOrEmpty();
        var user = parsed.ParseResult.Users.Should().ContainSingle().Which;

        // Act
        await StartMigrationAsync("Workspace", user, importPersonalFiles: false);
        var status = await WaitForMigrationAsync();
        var log = await GetMigrationLogAsync();

        // Assert
        status.IsCompleted.Should().BeTrue();
        status.Error.Should().BeNullOrEmpty();
        status.ParseResult.SuccessedUsers.Should().Be(1);
        status.ParseResult.Errors.Should().BeEmpty();
        if (skipped)
        {
            log.Should().Contain($"Skipped the photo of Carol White: {side}x{side} pixels");
        }
        else
        {
            log.Should().NotContain("Skipped the photo");
        }
    }

    /// <summary>
    /// A backup cut short on an entry boundary - everything is there except the end-of-archive block
    /// and whatever followed - fails the parse rather than being imported in part. GZipStream does not
    /// notice a cut-off stream, so the tar has to.
    /// </summary>
    [Theory]
    [InlineData("backup.tar.gz")]
    [InlineData("backup.tar")]
    public async Task Parse_BackupWithoutEndBlock_FailsParse(string fileName)
    {
        // Arrange
        await UploadBackupAsync(fileName, BuildBackup(gzip: fileName.EndsWith(".gz"), withEndBlock: false));

        // Act
        await _migrationApi.UploadAndInitializeMigrationAsync("Workspace", TestContext.Current.CancellationToken);
        var status = await WaitForParseAsync();

        // Assert
        status.IsCompleted.Should().BeTrue();
        status.Error.Should().Contain("Couldn't parse");
        status.ParseResult.FailedArchives.Should().Equal(fileName);
    }

    private static byte[] BuildBackup(bool gzip, int extraFiles = 0, bool withEndBlock = true, byte[]? photo = null)
    {
        using var buffer = new MemoryStream();
        byte[] withoutEndBlock;
        using (var tar = new TarWriter(buffer, leaveOpen: true))
        {
            WriteTable(tar, "core/core_user", ["id", "email", "firstname", "lastname", "removed"],
                [UserId, Email, "Carol", "White", "0"]);
            using (var photos = new DataTable("core_userphoto"))
            {
                photos.Columns.Add("userId", typeof(string));
                photos.Columns.Add("photo", typeof(byte[]));
                if (photo != null)
                {
                    photos.Rows.Add(UserId, photo);
                }

                WriteTable(tar, "core/core_userphoto", photos);
            }
            WriteTable(tar, "core/core_group", ["id", "name", "removed"]);
            WriteTable(tar, "core/core_usergroup", ["userid", "groupid", "removed", "ref_type"]);

            // the user's root folder (folder_type 5 = USER), which is its own level-0 tree entry
            WriteTable(tar, "files/files_folder", ["id", "parent_id", "title", "folder_type", "create_by"],
                ["1", "0", "My documents", "5", UserId]);
            WriteTable(tar, "files/files_folder_tree", ["folder_id", "parent_id", "level"],
                ["1", "1", "0"]);
            WriteTable(tar, "files/files_file", ["id", "folder_id", "title", "version", "version_group", "comment", "create_on", "modified_on", "content_length"],
                ["1", "1", "hello.txt", "1", "1", "", "2024-01-01 00:00:00", "2024-01-01 00:00:00", Encoding.UTF8.GetByteCount(FileContent).ToString(CultureInfo.InvariantCulture)]);
            WriteTable(tar, "files/files_security", ["entry_id", "owner", "subject", "entry_type", "security"]);
            WriteTable(tar, "files/files_bunch_objects", ["left_node", "right_node"]);
            WriteTable(tar, "projects/projects_projects", ["id", "title", "private", "responsible_id"]);
            WriteTable(tar, "projects/projects_project_participant", ["project_id", "removed", "participant_id"]);

            // files/folder_{(id / 1000 + 1) * 1000}/file_{id}/v{version}/content{extension}
            WriteTarEntry(tar, "files/folder_1000/file_1/v1/content.txt", FileContent);

            // files no table refers to: they are unpacked and then ignored by the parse
            var random = new Random(42);
            for (var i = 0; i < extraFiles; i++)
            {
                var content = new byte[4096];
                random.NextBytes(content);
                WriteTarEntry(tar, $"extra/{i}.bin", new MemoryStream(content));
            }

            // the writer adds the end-of-archive block only when it is disposed
            withoutEndBlock = buffer.ToArray();
        }

        var backup = withEndBlock ? buffer.ToArray() : withoutEndBlock;
        return gzip ? Gzip(backup) : backup;
    }

    private static void WriteTable(TarWriter tar, string name, string[] columns, params string[][] rows)
    {
        using var table = new DataTable(name.Split('/')[^1]);
        foreach (var column in columns)
        {
            table.Columns.Add(column, typeof(string));
        }

        foreach (var row in rows)
        {
            table.Rows.Add(row);
        }

        WriteTable(tar, name, table);
    }

    private static void WriteTable(TarWriter tar, string name, DataTable table)
    {
        var xml = new MemoryStream();
        table.WriteXml(xml, XmlWriteMode.WriteSchema);
        xml.Position = 0;
        WriteTarEntry(tar, $"databases/{name}", xml);
    }

    /// <summary>
    /// A black grayscale PNG of <paramref name="width"/> x <paramref name="height"/>: its rows of zeros
    /// compress to a few kilobytes whatever the size.
    /// </summary>
    private static byte[] BuildBlankPng(int width, int height)
    {
        using var png = new MemoryStream();
        png.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);

        // bit depth 8, color type 0 (grayscale); compression, filter and interlace methods 0
        WritePngChunk(png, "IHDR", [.. BigEndian(width), .. BigEndian(height), 8, 0, 0, 0, 0]);

        using var pixels = new MemoryStream();
        using (var zlib = new ZLibStream(pixels, CompressionLevel.Optimal, leaveOpen: true))
        {
            // each row is its filter type (0, none) followed by one byte per pixel
            var row = new byte[width + 1];
            for (var y = 0; y < height; y++)
            {
                zlib.Write(row);
            }
        }

        WritePngChunk(png, "IDAT", pixels.ToArray());
        WritePngChunk(png, "IEND", []);
        return png.ToArray();
    }

    private static void WritePngChunk(Stream png, string type, byte[] data)
    {
        var typeBytes = Encoding.ASCII.GetBytes(type);
        png.Write(BigEndian(data.Length));
        png.Write(typeBytes);
        png.Write(data);
        png.Write(BigEndian((int)Crc32([.. typeBytes, .. data])));
    }

    private static byte[] BigEndian(int value)
    {
        return [(byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value];
    }

    /// <summary>The CRC-32 a PNG chunk ends with (ISO 3309, as in zlib).</summary>
    private static uint Crc32(byte[] data)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var b in data)
        {
            crc ^= b;
            for (var bit = 0; bit < 8; bit++)
            {
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320u : crc >> 1;
            }
        }

        return ~crc;
    }
}

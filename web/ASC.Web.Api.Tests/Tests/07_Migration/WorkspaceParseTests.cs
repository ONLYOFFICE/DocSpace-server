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

    private static byte[] BuildBackup(bool gzip)
    {
        using var buffer = new MemoryStream();
        using (var compressed = gzip ? new GZipStream(buffer, CompressionLevel.Optimal, leaveOpen: true) : null)
        using (var tar = new TarWriter(compressed ?? (Stream)buffer, leaveOpen: true))
        {
            WriteTable(tar, "core/core_user", ["id", "email", "firstname", "lastname", "removed"],
                [UserId, Email, "Carol", "White", "0"]);
            WriteTable(tar, "core/core_userphoto", ["userId", "photo"]);
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
        }

        return buffer.ToArray();
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

        var xml = new MemoryStream();
        table.WriteXml(xml, XmlWriteMode.WriteSchema);
        xml.Position = 0;
        WriteTarEntry(tar, $"databases/{name}", xml);
    }
}

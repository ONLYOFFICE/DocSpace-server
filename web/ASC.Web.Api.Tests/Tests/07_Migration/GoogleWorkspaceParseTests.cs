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
/// POST /api/2.0/migration/init/GoogleWorkspace on an uploaded Takeout, read back from
/// GET /api/2.0/migration/status. The Takeout is a minimal layout: a root html naming the account
/// and a <c>Drive</c> folder with one file.
/// </summary>
[Trait("Category", "Migration")]
public class GoogleWorkspaceParseTests(
    AspireAppFixture fixture)
    : MigrationParseTestBase(fixture)
{
    private const string Email = "bob.jones@example.com";
    private const string FileContent = "hello from google drive";

    /// <summary>
    /// The root html maps each English folder name to the localized one the Takeout uses, and that
    /// name comes from the archive. A Drive folder named "../.." would list everything next to the
    /// extracted Takeout - the uploaded archive among it - as the user's files; it is ignored and
    /// the real Drive folder is read instead.
    /// </summary>
    [Fact]
    public async Task Parse_LocalizedFolderNameOutsideTakeout_ReadsRealDriveFolder()
    {
        // Arrange
        await UploadBackupAsync("takeout.zip", BuildTakeout(localizedDriveName: "../.."));

        // Act
        await _migrationApi.UploadAndInitializeMigrationAsync("GoogleWorkspace", TestContext.Current.CancellationToken);
        var status = await WaitForParseAsync();

        // Assert
        status.IsCompleted.Should().BeTrue();
        status.Error.Should().BeNullOrEmpty();
        var user = status.ParseResult.Users.Should().ContainSingle().Which;
        user.Email.Should().Be(Email);
        user.MigratingFiles.FilesCount.Should().Be(1);
        user.MigratingFiles.BytesTotal.Should().Be(Encoding.UTF8.GetByteCount(FileContent));
    }

    private static byte[] BuildTakeout(string localizedDriveName)
    {
        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteZipEntry(zip, "Takeout/archive_browser.html",
                "<html><body>" +
                $"<h1 class=\"header_title\">Takeout for {Email}</h1>" +
                $"<h1 class=\"data-folder-name\" data-english-name=\"Drive\" data-folder-name=\"{localizedDriveName}\">Drive</h1>" +
                "</body></html>");
            WriteZipEntry(zip, "Takeout/Drive/hello.txt", FileContent);
        }

        return buffer.ToArray();
    }
}

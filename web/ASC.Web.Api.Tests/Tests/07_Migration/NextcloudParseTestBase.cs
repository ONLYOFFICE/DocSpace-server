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
/// The backup the Nextcloud parse suites upload: a minimal Nextcloud layout - one top-level folder
/// holding a MySQL dump and <c>data/{uid}/files</c> - packed as each supported archive type, and
/// the broken variants of it.
/// </summary>
public abstract class NextcloudParseTestBase(
    AspireAppFixture fixture)
    : MigrationParseTestBase(fixture)
{
    protected const string Uid = "alice";
    protected const string Email = "alice.smith@example.com";
    protected const string FileContent = "hello from nextcloud";

    protected const string Dump =
        $"INSERT INTO `oc_accounts` VALUES ('{Uid}','{{}}');\n" +
        $"INSERT INTO `oc_accounts_data` VALUES (1,'{Uid}','displayname','Alice Smith'),(2,'{Uid}','email','{Email}');\n" +
        $"INSERT INTO `oc_storages` VALUES (7,'home::{Uid}',1,NULL);\n" +
        "INSERT INTO `oc_filecache` VALUES (10,7,'files'),(11,7,'files/hello.txt'),(12,7,'files/link.txt');\n";

    /// <summary>
    /// The migrator reads only the first INSERT of each table, so extra rows go into that statement.
    /// </summary>
    protected static string AddRows(string dump, string table, string rows)
    {
        var start = dump.IndexOf($"INSERT INTO `{table}` VALUES ", StringComparison.Ordinal);
        var end = dump.IndexOf(";\n", start, StringComparison.Ordinal);
        return dump.Insert(end, "," + rows);
    }

    protected static byte[] BuildArchive(string fileName, params (string Name, string Content)[] entries)
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

    protected static byte[] BuildZip(string dump = Dump, string? extraFile = null)
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

    protected static byte[] BuildTarGz(bool withLink)
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
    /// <summary>
    /// The dump lists <c>hello.txt</c> and <c>copy.txt</c>; the archive holds <c>hello.txt</c> and
    /// <c>copy.txt</c> as a hard link to <paramref name="linkName"/>.
    /// </summary>
    protected static byte[] BuildTarGzWithHardLink(string linkName)
    {
        var dump = AddRows(Dump, "oc_filecache", "(13,7,'files/copy.txt')");

        using var buffer = new MemoryStream();
        using (var gzip = new GZipStream(buffer, CompressionLevel.Fastest, leaveOpen: true))
        using (var tar = new TarWriter(gzip))
        {
            WriteTarEntry(tar, "nextcloud/nextcloud.sql", dump);
            WriteTarEntry(tar, $"nextcloud/data/{Uid}/files/hello.txt", FileContent);
            tar.WriteEntry(new PaxTarEntry(TarEntryType.HardLink, $"nextcloud/data/{Uid}/files/copy.txt") { LinkName = linkName });
        }

        return buffer.ToArray();
    }

    /// <summary>
    /// A backup whose dump and first file come before the cut and a file of random bytes - which
    /// compresses to its own size, so a cut at three quarters lands in it - after.
    /// </summary>
    protected static byte[] BuildTruncatedArchive(string fileName, string truncation)
    {
        var later = new byte[64 * 1024];
        new Random(42).NextBytes(later);

        if (fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            using var zipBuffer = new MemoryStream();
            using (var zip = new ZipArchive(zipBuffer, ZipArchiveMode.Create, leaveOpen: true))
            {
                WriteZipEntry(zip, "nextcloud/nextcloud.sql", Dump);
                WriteZipEntry(zip, $"nextcloud/data/{Uid}/files/hello.txt", FileContent);
                using var entry = zip.CreateEntry($"nextcloud/data/{Uid}/files/later.bin").Open();
                entry.Write(later);
            }

            var zipBytes = zipBuffer.ToArray();
            return zipBytes[..(zipBytes.Length * 3 / 4)];
        }

        using var tarBuffer = new MemoryStream();
        byte[] withoutEndBlock;
        using (var tar = new TarWriter(tarBuffer, leaveOpen: true))
        {
            WriteTarEntry(tar, "nextcloud/nextcloud.sql", Dump);
            WriteTarEntry(tar, $"nextcloud/data/{Uid}/files/hello.txt", FileContent);
            WriteTarEntry(tar, $"nextcloud/data/{Uid}/files/later.bin", new MemoryStream(later));

            // the writer adds the end-of-archive block only when it is disposed
            withoutEndBlock = tarBuffer.ToArray();
        }

        if (truncation == "no end block")
        {
            return Gzip(withoutEndBlock);
        }

        var tarGz = Gzip(tarBuffer.ToArray());
        return tarGz[..(tarGz.Length * 3 / 4)];
    }
}

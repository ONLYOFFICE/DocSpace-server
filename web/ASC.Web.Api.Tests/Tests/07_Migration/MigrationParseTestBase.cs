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
/// Upload and parse helpers shared by the migration parse suites. A backup is uploaded through
/// <c>migrationFileUpload.ashx</c>, which is not an API action, so it is called over raw HTTP the
/// way the web client calls it, in the chunks the handler announces; the parse itself runs in
/// Web.Studio and is read back from GET /api/2.0/migration/status.
/// </summary>
public abstract class MigrationParseTestBase(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    protected async Task UploadBackupAsync(string fileName, byte[] archive)
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        using var init = await _webApiClient.PostAsync("migrationFileUpload.ashx?Init=true", null, cancellationToken);
        var chunkSize = await AssertUploadSucceededAsync(init);

        for (var offset = 0; offset < archive.Length; offset += chunkSize)
        {
            using var form = new MultipartFormDataContent();
            form.Add(new ByteArrayContent(archive, offset, Math.Min(chunkSize, archive.Length - offset)), "file", fileName);
            using var upload = await _webApiClient.PostAsync($"migrationFileUpload.ashx?Name={Uri.EscapeDataString(fileName)}", form, cancellationToken);
            await AssertUploadSucceededAsync(upload);
        }
    }

    /// <returns>The chunk size the handler announces; it is sent only in the answer to the init call.</returns>
    private static async Task<int> AssertUploadSucceededAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("Success").GetBoolean().Should().BeTrue(body);
        return json.RootElement.GetProperty("ChunkSize").GetInt32();
    }

    protected async Task<MigrationStatusDto> WaitForParseAsync()
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

    /// <summary>
    /// The SDK's <c>GetMigrationLogsWithHttpInfoAsync</c> is typed <c>Object</c> and parses the body as
    /// JSON, while the endpoint answers <c>text/plain</c>, so the log is read over raw HTTP.
    /// </summary>
    protected async Task<string> GetMigrationLogAsync()
    {
        using var response = await _webApiClient.GetAsync("api/2.0/migration/logs", TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// The number of progress reports in the migration log whose status ends with <paramref name="status"/>;
    /// each report is one line, <c>{progress} progress: {status}</c>.
    /// </summary>
    protected static int CountProgressReports(string log, string status)
    {
        return log.Split('\n').Count(line => line.TrimEnd().EndsWith(status, StringComparison.Ordinal) && line.Contains("progress: ", StringComparison.Ordinal));
    }

    protected static void WriteZipEntry(ZipArchive zip, string name, string content)
    {
        using var writer = new StreamWriter(zip.CreateEntry(name).Open());
        writer.Write(content);
    }

    protected static void WriteTarEntry(TarWriter tar, string name, string content)
    {
        WriteTarEntry(tar, name, new MemoryStream(Encoding.UTF8.GetBytes(content)));
    }

    protected static void WriteTarEntry(TarWriter tar, string name, Stream content)
    {
        tar.WriteEntry(new PaxTarEntry(TarEntryType.RegularFile, name)
        {
            DataStream = content
        });
    }

    /// <summary>
    /// A seekable stream of <paramref name="length"/> zero bytes that holds nothing in memory, so a
    /// test can pack an entry larger than the portal quota into an archive of a few megabytes.
    /// </summary>
    protected sealed class ZeroStream(long length) : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => length;
        public override long Position { get; set; }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = (int)Math.Min(count, length - Position);
            Array.Clear(buffer, offset, read);
            Position += read;
            return read;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            Position = origin switch
            {
                SeekOrigin.Begin => offset,
                SeekOrigin.Current => Position + offset,
                _ => length + offset
            };
            return Position;
        }

        public override void Flush()
        {
        }

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}

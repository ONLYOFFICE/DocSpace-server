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
/// What <c>migrationFileUpload.ashx</c> answered: the HTTP status and its JSON body
/// <c>{Success, Message, ChunkSize}</c>.
/// </summary>
public sealed record UploadAnswer(HttpStatusCode Status, bool Success, string? Message, int ChunkSize);

/// <summary>
/// Shared steps of the migration tests that move a real backup: the upload handler
/// <c>migrationFileUpload.ashx</c> is not in the OpenAPI document nor in the SDK, so it is called over raw
/// HTTP on the Web.Api client (the way the client wizard calls it); the parse and the import run in the
/// Web.Studio host and are read back through the status endpoint, polled on a deadline.
/// </summary>
public abstract class MigrationTestBase(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    protected const string FileNotFoundMessage = "The required file was not found";

    protected async Task<UploadAnswer> PostUpload(string query, HttpContent? content = null)
    {
        using var response = await _webApiClient.PostAsync($"migrationFileUpload.ashx{query}", content, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var json = JsonDocument.Parse(body);
        var root = json.RootElement;

        return new UploadAnswer(
            response.StatusCode,
            root.GetProperty("Success").GetBoolean(),
            root.TryGetProperty("Message", out var message) ? message.GetString() : null,
            root.TryGetProperty("ChunkSize", out var chunkSize) ? chunkSize.GetInt32() : 0);
    }

    protected async Task<UploadAnswer> PostChunk(string name, byte[] bytes)
    {
        using var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        content.Add(file, "file", name);

        return await PostUpload($"?Name={Uri.EscapeDataString(name)}", content);
    }

    /// <summary>
    /// Starts a new upload and sends <paramref name="bytes"/> as <paramref name="chunks"/> chunks of one file,
    /// as the wizard does; every answer must report success.
    /// </summary>
    protected async Task UploadBackup(string name, byte[] bytes, int chunks = 1)
    {
        var init = await PostUpload("?Init=true");
        init.Success.Should().BeTrue(init.Message);

        var size = (bytes.Length + chunks - 1) / chunks;

        for (var offset = 0; offset < bytes.Length; offset += size)
        {
            var answer = await PostChunk(name, bytes[offset..Math.Min(offset + size, bytes.Length)]);
            answer.Success.Should().BeTrue(answer.Message);
        }
    }

    /// <summary>Queues a parse with <paramref name="migratorName"/> and waits until a parse job has finished.</summary>
    protected async Task<MigrationStatusDto?> Parse(string migratorName, Func<MigrationStatusDto, bool>? until = null)
    {
        await _migrationApi.UploadAndInitializeMigrationAsync(migratorName, TestContext.Current.CancellationToken);

        return await WaitForStatus(s => s is { IsCompleted: true, ParseResult.Operation: "parse" } && (until == null || until(s)));
    }

    /// <summary>Queues an import of <paramref name="info"/> and waits until an import job has finished.</summary>
    protected async Task<MigrationStatusDto?> Import(MigrationApiInfo info)
    {
        await _migrationApi.StartMigrationAsync(info, TestContext.Current.CancellationToken);

        return await WaitForStatus(s => s is { IsCompleted: true, ParseResult.Operation: "migration" }, 120);
    }

    protected async Task<MigrationStatusDto?> WaitForStatus(Func<MigrationStatusDto?, bool> until, int seconds = 60)
    {
        var deadline = DateTime.UtcNow.AddSeconds(seconds);

        while (true)
        {
            var status = (await _migrationApi.GetMigrationStatusAsync(TestContext.Current.CancellationToken)).Response;

            if (until(status) || DateTime.UtcNow >= deadline)
            {
                return status;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }

    protected static string NewEmail() => $"mig.{Guid.NewGuid():N}@example.com";

    protected static MigratingApiUser Select(string key, string email, EmployeeType userType = EmployeeType.User) =>
        new(key: key, email: email, userType: userType) { ShouldImport = true };
}

/// <summary>
/// A minimal Nextcloud backup that <c>NextcloudWorkspaceMigrator</c> accepts: a <c>.zip</c> with one root
/// folder holding an SQL dump (one <c>INSERT INTO</c> line per table: <c>oc_accounts</c>,
/// <c>oc_accounts_data</c>, <c>oc_storages</c>, <c>oc_filecache</c>, <c>oc_groups</c>,
/// <c>oc_group_user</c>) and the user files under <c>data/&lt;uid&gt;/files</c>.
/// </summary>
public sealed class NextcloudBackup
{
    private readonly List<(string Uid, string DisplayName, string? Email, string[] Files)> _accounts = [];
    private readonly List<(string Gid, string[] Members)> _groups = [];

    public NextcloudBackup Account(string uid, string displayName, string? email, params string[] files)
    {
        _accounts.Add((uid, displayName, email, files));
        return this;
    }

    public NextcloudBackup Group(string gid, params string[] members)
    {
        _groups.Add((gid, members));
        return this;
    }

    public byte[] ToZip(bool withDump = true)
    {
        using var ms = new MemoryStream();

        using (var zip = new System.IO.Compression.ZipArchive(ms, System.IO.Compression.ZipArchiveMode.Create, true))
        {
            zip.CreateEntry("nc/data/");

            foreach (var (uid, _, _, files) in _accounts)
            {
                foreach (var file in files)
                {
                    using var writer = new StreamWriter(zip.CreateEntry($"nc/data/{uid}/files/{file}").Open());
                    writer.Write($"content of {file}");
                }
            }

            if (withDump)
            {
                using var writer = new StreamWriter(zip.CreateEntry("nc/nextcloud.sql").Open());
                writer.Write(Dump());
            }
        }

        return ms.ToArray();
    }

    private string Dump()
    {
        var accounts = new List<string>();
        var data = new List<string>();
        var storages = new List<string>();
        var caches = new List<string>();
        var id = 1;
        var fileId = 100;

        for (var i = 0; i < _accounts.Count; i++)
        {
            var (uid, displayName, email, files) = _accounts[i];
            var storage = i + 1;

            accounts.Add($"('{uid}','{{}}')");
            data.Add($"({id++},'{uid}','displayname','{displayName}')");

            if (email != null)
            {
                data.Add($"({id++},'{uid}','email','{email}')");
            }

            storages.Add($"({storage},'home::{uid}')");

            if (files.Length == 0)
            {
                continue;
            }

            var paths = new List<string> { "files" };

            foreach (var file in files)
            {
                var parts = file.Split('/');

                for (var level = 1; level <= parts.Length; level++)
                {
                    var path = "files/" + string.Join('/', parts[..level]);

                    if (!paths.Contains(path))
                    {
                        paths.Add(path);
                    }
                }
            }

            caches.AddRange(paths.Select(p => $"({fileId++},{storage},'{p}')"));
        }

        var sql = new StringBuilder();
        sql.AppendLine(Insert("oc_accounts", accounts));
        sql.AppendLine(Insert("oc_accounts_data", data));
        sql.AppendLine(Insert("oc_storages", storages));

        if (caches.Count > 0)
        {
            sql.AppendLine(Insert("oc_filecache", caches));
        }

        if (_groups.Count > 0)
        {
            sql.AppendLine(Insert("oc_groups", _groups.Select(g => $"('{g.Gid}')")));
            sql.AppendLine(Insert("oc_group_user", _groups.SelectMany(g => g.Members.Select(m => $"('{g.Gid}','{m}')"))));
        }

        return sql.ToString();
    }

    private static string Insert(string table, IEnumerable<string> rows) => $"INSERT INTO `{table}` VALUES {string.Join(',', rows)};";
}

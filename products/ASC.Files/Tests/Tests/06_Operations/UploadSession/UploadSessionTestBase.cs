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

namespace ASC.Files.Tests.Tests._06_Operations.UploadSession;

/// <summary>
/// Shared helpers for the chunked-upload suites and for the batch-operation suites that need files with
/// exact bytes: opening a session, sending parts, storing a file in one call and reading its content back.
/// </summary>
public abstract class UploadSessionTestBase(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    /// <summary>Deterministic pseudo-random bytes, so a swapped or truncated part changes the hash.</summary>
    protected static byte[] MakeBytes(int length, int seed)
    {
        var bytes = new byte[length];
        new Random(seed).NextBytes(bytes);

        return bytes;
    }

    protected static string Sha256(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));

    protected static FileParameter Part(byte[] bytes, string name = "part.bin") =>
        new(name, "application/octet-stream", new MemoryStream(bytes));

    /// <summary>The status of a call: its HTTP status on success, the error status when the SDK throws.</summary>
    protected static async Task<int> StatusOf<T>(Func<Task<ApiResponse<T>>> call)
    {
        try
        {
            return (int)(await call()).StatusCode;
        }
        catch (ApiException exception)
        {
            return exception.ErrorCode;
        }
    }

    protected async Task<int> GetChunkSize()
    {
        var settings = (await _filesSettingsApi.GetFilesSettingsAsync(TestContext.Current.CancellationToken)).Response;
        settings.ChunkUploadSize.Should().BePositive();

        return (int)settings.ChunkUploadSize;
    }

    /// <summary>Opens a session and checks the shape of the answer; returns the session id.</summary>
    protected async Task<string> OpenSession(int folderId, string fileName, long fileSize, bool createNewIfExist = true)
    {
        var session = (await _filesOperationsApi.CreateUploadSessionInFolderAsync(
            folderId,
            new SessionRequest(fileName, fileSize, createNewIfExist: createNewIfExist),
            TestContext.Current.CancellationToken)).Response;

        session.Id.Should().MatchRegex("^[0-9a-f]{32}$");
        session.BytesTotal.Should().Be(fileSize);

        return session.Id;
    }

    /// <summary>
    /// Calls the deprecated <c>POST api/2.0/files/{folderId}/upload/create_session</c>. It goes over raw HTTP
    /// because its SDK method is marked obsolete; returns the status and the <c>response</c> member of the body.
    /// </summary>
    protected async Task<(int Status, JsonElement Response)> CreateLegacySession(
        int folderId, string fileName, long fileSize, bool createNewIfExist = false)
    {
        using var response = await _filesClient.PostAsJsonAsync(
            $"api/2.0/files/{folderId}/upload/create_session",
            new { fileName, fileSize, createNewIfExist },
            TestContext.Current.CancellationToken);

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var document = JsonDocument.Parse(string.IsNullOrEmpty(body) ? "{}" : body);
        var content = document.RootElement.TryGetProperty("response", out var member) ? member.Clone() : default;

        return ((int)response.StatusCode, content);
    }

    protected Task<int> ChunkStatus(int folderId, string sessionId, int chunkNumber, byte[] bytes) =>
        StatusOf(() => _filesOperationsApi.UploadAsyncSessionWithHttpInfoAsync(
            folderId, sessionId, chunkNumber, Part(bytes), TestContext.Current.CancellationToken));

    protected Task<int> SequentialChunkStatus(int folderId, string sessionId, byte[] bytes) =>
        StatusOf(() => _filesOperationsApi.UploadSessionWithHttpInfoAsync(
            folderId, sessionId, Part(bytes), TestContext.Current.CancellationToken));

    protected Task<int> FinalizeStatus(int folderId, string sessionId) =>
        StatusOf(() => _filesOperationsApi.FinalizeSessionWithHttpInfoAsync(
            folderId, sessionId, TestContext.Current.CancellationToken));

    protected Task<int> AbortStatus(int folderId, string sessionId) =>
        StatusOf(() => _filesOperationsApi.AbortUploadSessionWithHttpInfoAsync(
            sessionId, folderId, TestContext.Current.CancellationToken));

    /// <summary>Sends a part and expects it to be accepted as part of a still-open session.</summary>
    protected async Task SendChunk(int folderId, string sessionId, int chunkNumber, byte[] bytes)
    {
        var response = await _filesOperationsApi.UploadAsyncSessionWithHttpInfoAsync(
            folderId, sessionId, chunkNumber, Part(bytes), TestContext.Current.CancellationToken);

        ((int)response.StatusCode).Should().Be(200);
        response.Data.Response.Id.Should().Be(sessionId);
    }

    /// <summary>Finalizes a session and expects 201; returns the stored upload result.</summary>
    protected async Task<UploadSessionResponseDto> Finalize(int folderId, string sessionId)
    {
        var response = await _filesOperationsApi.FinalizeSessionWithHttpInfoAsync(
            folderId, sessionId, TestContext.Current.CancellationToken);

        ((int)response.StatusCode).Should().Be(201);

        return response.Data.Response;
    }

    /// <summary>
    /// Stores a file with exact bytes through a one-shot session. With <paramref name="createNewIfExist"/> off,
    /// a same-named file receives the bytes as its next version.
    /// </summary>
    protected async Task<(int Id, int Version)> PutFile(int folderId, string fileName, byte[] bytes, bool createNewIfExist = true)
    {
        var sessionId = await OpenSession(folderId, fileName, bytes.Length, createNewIfExist);

        var response = await _filesOperationsApi.UploadSessionWithHttpInfoAsync(
            folderId, sessionId, Part(bytes, fileName), TestContext.Current.CancellationToken);

        ((int)response.StatusCode).Should().Be(201);

        var file = response.Data.Response.File;

        return (file.Id, file.Version);
    }

    protected async Task<byte[]> ReadContent(int fileId)
    {
        var url = (await _filesApi.GetPresignedUriAsync(fileId, TestContext.Current.CancellationToken)).Response;

        return await _filesClient.GetByteArrayAsync(url, TestContext.Current.CancellationToken);
    }

    protected async Task ExpectContent(int fileId, byte[] expected)
    {
        var actual = await ReadContent(fileId);

        actual.Length.Should().Be(expected.Length);
        Sha256(actual).Should().Be(Sha256(expected));
    }

    /// <summary>Files directly inside a folder whose title starts with <paramref name="titlePrefix"/>.</summary>
    protected async Task<List<FileDto>> FilesIn(int folderId, string titlePrefix = "")
    {
        var (files, _) = await FolderListing.ReadAsync(_foldersApi, folderId, TestContext.Current.CancellationToken);

        return files.FindAll(f => f.Title.StartsWith(titlePrefix, StringComparison.Ordinal));
    }

    /// <summary>Ids and titles of everything directly inside a folder, for exact before/after comparisons.</summary>
    protected async Task<List<string>> Listing(string folder)
    {
        var (files, folders) = await FolderListing.ReadAsync(_foldersApi, folder, TestContext.Current.CancellationToken);

        return [.. files.Select(e => $"{e.Id}:{e.Title}").Concat(folders.Select(e => $"{e.Id}:{e.Title}")).Order(StringComparer.Ordinal)];
    }

    protected Task<List<string>> Listing(int folderId) => Listing(folderId.ToString(CultureInfo.InvariantCulture));

    /// <summary>Relative path and SHA-256 of every file below a folder.</summary>
    protected async Task<List<string>> ReadTree(int folderId, string prefix = "")
    {
        var (files, folders) = await FolderListing.ReadAsync(_foldersApi, folderId, TestContext.Current.CancellationToken);

        var result = new List<string>();

        foreach (var file in files)
        {
            result.Add($"{prefix}{file.Title}:{Sha256(await ReadContent(file.Id))}");
        }

        foreach (var folder in folders)
        {
            result.AddRange(await ReadTree(folder.Id, $"{prefix}{folder.Title}/"));
        }

        result.Sort(StringComparer.Ordinal);

        return result;
    }

    protected async Task<List<int>> VersionsOf(int fileId)
    {
        var versions = (await _filesApi.GetFileVersionInfoAsync(fileId, TestContext.Current.CancellationToken)).Response;

        return [.. versions.Select(v => v.Version).Order()];
    }
}

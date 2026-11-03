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

namespace ASC.Files.Tests.Tests._06_Operations;

/// <summary>
/// Files with exact, known bytes for the operations suites. A file created through
/// <c>CreateFileAsync</c> holds the built-in template, so "the copy has the original's content" or
/// "version 3 still holds what was uploaded as version 3" cannot be told from "an empty document".
/// These helpers upload pseudo-random content through a one-chunk upload session and read it back
/// through <c>filehandler.ashx</c>, which is how the TypeScript suite compares content by hash.
/// </summary>
public static class OperationFileBytes
{
    /// <summary>Deterministic pseudo-random bytes (xorshift32), so swapped content is caught.</summary>
    public static byte[] Make(int length, int seed)
    {
        var bytes = new byte[length];
        var state = unchecked((uint)seed * 2654435761u);

        if (state == 0)
        {
            state = 1;
        }

        for (var i = 0; i < length; i++)
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            bytes[i] = (byte)(state & 0xff);
        }

        return bytes;
    }

    /// <summary>
    /// Stores <paramref name="bytes"/> as <paramref name="fileName"/>. With
    /// <paramref name="createNewIfExist"/> <c>false</c> and a file of that name already in the folder,
    /// the bytes become its next version instead of a second file. The content must fit one chunk.
    /// </summary>
    public static async Task<UploadSessionResponseDto> UploadAsync(
        OperationsApi operationsApi,
        int folderId,
        string fileName,
        byte[] bytes,
        bool createNewIfExist,
        CancellationToken cancellationToken)
    {
        var session = (await operationsApi.CreateUploadSessionInFolderAsync(
            folderId,
            new SessionRequest(fileName, bytes.Length, createNewIfExist: createNewIfExist),
            cancellationToken)).Response;

        await using var chunk = new MemoryStream(bytes);

        await operationsApi.UploadAsyncSessionAsync(folderId, session.Id, 1, new FileParameter(chunk), cancellationToken);

        return (await operationsApi.FinalizeSessionAsync(folderId, session.Id, cancellationToken)).Response;
    }

    /// <summary>A file whose versions 1..n hold the given contents, in order. Returns the file id.</summary>
    public static async Task<int> UploadVersionsAsync(
        OperationsApi operationsApi,
        int folderId,
        string fileName,
        IReadOnlyList<byte[]> contents,
        CancellationToken cancellationToken)
    {
        var first = await UploadAsync(operationsApi, folderId, fileName, contents[0], true, cancellationToken);

        for (var i = 1; i < contents.Count; i++)
        {
            await UploadAsync(operationsApi, folderId, fileName, contents[i], false, cancellationToken);
        }

        return first.File.Id;
    }

    /// <summary>Downloads a file, or one stored version of it, through <c>filehandler.ashx</c>.</summary>
    public static async Task<byte[]> DownloadAsync(HttpClient client, int fileId, int? version, CancellationToken cancellationToken)
    {
        var url = $"filehandler.ashx?action=download&fileid={fileId}" + (version.HasValue ? $"&version={version.Value}" : "");

        using var response = await client.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }
}

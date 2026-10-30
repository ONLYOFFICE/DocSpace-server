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

namespace ASC.Files.Tests.Tests._06_Operations.CheckConversion;

/// <summary>
/// Shared setup for the <c>checkconversion</c> suites that start real conversions
/// (<c>PUT</c> = <c>startFileConversion</c>, <c>GET</c> = <c>checkConversionStatus</c>, same address).
/// </summary>
/// <remarks>
/// What the portal does, found by probing the TypeScript suite and used as the contract: only formats
/// that must be converted are queued (RTF and HTML become .docx); anything already editable (docx,
/// xlsx, pptx) and plain text, CSV and Markdown are answered 200 with an empty list and no work; the
/// entry's <c>progress</c> is 0 until the job is over and then 100, whether it ended with a file or with
/// an <c>error</c>; a finished entry is handed out once.
/// <para>
/// Tests that need the finished result of a successful conversion (the converted text, the second
/// version of a result, <c>outputType</c> pdf, <c>sync</c>) are not ported: the integration-test
/// AppHost has no document server that can reach the portal (see <c>SaveFileAsPdfTests</c>), so such a
/// conversion can never succeed here. Everything below decides on what the endpoints answer
/// synchronously, or on a failure.
/// </para>
/// </remarks>
public abstract class FileConversionTestBase(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    protected static byte[] Rtf(string text)
    {
        return Encoding.UTF8.GetBytes($"{{\\rtf1\\ansi\\deff0{{\\fonttbl{{\\f0 Arial;}}}}{text}\\par}}");
    }

    protected static byte[] Html(params string[] paragraphs)
    {
        return Encoding.UTF8.GetBytes(
            $"<html><head><meta charset=\"utf-8\"></head><body>{string.Concat(paragraphs.Select(p => $"<p>{p}</p>"))}</body></html>");
    }

    protected async Task<int> Put(int folderId, string name, byte[] bytes, bool createNewIfExist = true)
    {
        return (await OperationFileBytes.UploadAsync(
            _filesOperationsApi, folderId, name, bytes, createNewIfExist, TestContext.Current.CancellationToken)).File.Id;
    }

    protected Task<List<ConversationResultDto>> Start(int fileId, CheckConversionRequestDto? body = null)
    {
        return StartAsync(fileId, body);
    }

    private async Task<List<ConversationResultDto>> StartAsync(int fileId, CheckConversionRequestDto? body)
    {
        // a typed call without a body drops the Content-Type header and is refused with 415 before the controller runs
        return (await _filesOperationsApi.StartFileConversionAsync(
            fileId, body ?? new CheckConversionRequestDto(), TestContext.Current.CancellationToken)).Response;
    }

    protected async Task<List<ConversationResultDto>> ReadEntries(int fileId, bool? start = null)
    {
        return (await _filesOperationsApi.CheckConversionStatusAsync(fileId, start, TestContext.Current.CancellationToken)).Response;
    }

    protected async Task<List<string>> FileTitles(int folderId)
    {
        var content = (await _foldersApi.GetFolderByFolderIdAsync(folderId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        return [.. (content.Files ?? []).Select(f => f.Title).Order()];
    }

    protected async Task AssertSourceIntact(int fileId, byte[] original)
    {
        var bytes = await OperationFileBytes.DownloadAsync(_filesClient, fileId, null, TestContext.Current.CancellationToken);
        bytes.Should().Equal(original);

        var versions = (await _filesApi.GetFileVersionInfoAsync(fileId, TestContext.Current.CancellationToken)).Response;
        versions.Should().ContainSingle("a conversion never adds a version to its source");
    }
}

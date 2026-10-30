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

/// <summary>One entry of a folder listing: id and title of a file or a folder.</summary>
public sealed record ListedEntry(int Id, string Title);

/// <summary>
/// Reads a folder listing as raw JSON. <c>FolderContentDto.Folders</c> is typed
/// <c>List&lt;FileEntryBaseDto&gt;</c>, which has <c>Title</c> but no <c>Id</c>, so a folder id cannot be read
/// through the typed client - an SDK defect worth reporting, and the reason this helper exists. Files are
/// listed here too so that one request yields both halves of a snapshot.
/// </summary>
public static class RawFolderListing
{
    /// <param name="client">The Files HTTP client, already authenticated as the user whose view is wanted.</param>
    /// <param name="path">A folder id, or <c>@trash</c>, <c>@my</c>.</param>
    /// <param name="cancellationToken">The test cancellation token.</param>
    public static async Task<(List<ListedEntry> Files, List<ListedEntry> Folders)> ReadAsync(HttpClient client, string path, CancellationToken cancellationToken)
    {
        using var response = await client.GetAsync($"api/2.0/files/{path}?count=100", cancellationToken);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var content = document.RootElement.GetProperty("response");

        return (Read(content, "files"), Read(content, "folders"));
    }

    private static List<ListedEntry> Read(JsonElement content, string name)
    {
        var entries = new List<ListedEntry>();

        if (!content.TryGetProperty(name, out var array) || array.ValueKind != JsonValueKind.Array)
        {
            return entries;
        }

        foreach (var item in array.EnumerateArray())
        {
            entries.Add(new ListedEntry(item.GetProperty("id").GetInt32(), item.GetProperty("title").GetString() ?? string.Empty));
        }

        return entries;
    }
}

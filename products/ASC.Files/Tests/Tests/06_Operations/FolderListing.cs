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
/// Reads a folder listing through the typed client, with every entry as the file or folder it is. The items of
/// <c>FolderContentDto.Files</c> and <c>.Folders</c> are declared as the base <c>FileEntryBaseDto</c>; the
/// <c>entryKind</c> discriminator makes the SDK build each of them as its concrete model, which is where the id is.
/// </summary>
public static class FolderListing
{
    /// <param name="foldersApi">The folders client, already authenticated as the user whose view is wanted.</param>
    /// <param name="path">A folder id, or <c>@trash</c>, <c>@my</c>.</param>
    /// <param name="cancellationToken">The test cancellation token.</param>
    public static async Task<(List<FileDto> Files, List<FolderDto> Folders)> ReadAsync(FoldersApi foldersApi, string path, CancellationToken cancellationToken)
    {
        var content = path switch
        {
            "@trash" => (await foldersApi.GetTrashFolderAsync(count: 100, cancellationToken: cancellationToken)).Response,
            "@my" => (await foldersApi.GetMyFolderAsync(count: 100, cancellationToken: cancellationToken)).Response,
            _ => (await foldersApi.GetFolderByFolderIdAsync(int.Parse(path, CultureInfo.InvariantCulture), count: 100, cancellationToken: cancellationToken)).Response
        };

        return ([.. (content.Files ?? []).OfType<FileDto>()], [.. (content.Folders ?? []).OfType<FolderDto>()]);
    }

    public static Task<(List<FileDto> Files, List<FolderDto> Folders)> ReadAsync(FoldersApi foldersApi, int folderId, CancellationToken cancellationToken)
    {
        return ReadAsync(foldersApi, folderId.ToString(CultureInfo.InvariantCulture), cancellationToken);
    }
}

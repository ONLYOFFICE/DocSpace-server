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

namespace ASC.Files.Tests.Tests._03_Rooms.Links;

/// <summary>
/// <c>GET /api/2.0/files/{folderId}</c> on a public room: every listed file carries its primary
/// external link in <c>shortWebUrl</c>, which the listing reads once per file through the
/// primary-external-link share query. A custom room has no such link and leaves the field empty.
/// The listing model (<c>FileEntryBaseDto</c>) has no <c>Id</c>, so files are matched by title.
/// </summary>
[Trait("Category", "Rooms")]
public class PublicRoomListingShortLinkTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    private const int FilesInRoom = 10;

    [Fact]
    public async Task GetFolder_PublicRoom_EveryFileHasItsPrimaryLinkAsShortWebUrl()
    {
        var room = await CreatePublicRoom("Autotest Public Listing Links");
        var files = await CreateFiles(room.Id, "Public Link");

        var listed = await ListFiles(room.Id);

        listed.Should().HaveCount(FilesInRoom);

        foreach (var file in files)
        {
            var link = (await _filesApi.GetFilePrimaryExternalLinkAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

            listed.Should().ContainSingle(f => f.Title == file.Title)
                .Which.ShortWebUrl.Should().Be(link.SharedLink.ShareLink, "file {0}", file.Title);
        }
    }

    [Fact]
    public async Task GetFolder_CustomRoom_FilesHaveNoShortWebUrl()
    {
        var room = await CreateCustomRoom("Autotest Custom Listing Links");
        await CreateFiles(room.Id, "Custom Link", 3);

        var listed = await ListFiles(room.Id);

        listed.Should().HaveCount(3);
        listed.Should().OnlyContain(f => string.IsNullOrEmpty(f.ShortWebUrl));
    }

    [Fact]
    public async Task GetFolder_PublicRoomListedInParallel_EveryResponseCarriesAllLinks()
    {
        var room = await CreatePublicRoom("Autotest Public Listing Parallel");
        await CreateFiles(room.Id, "Parallel Link");

        var listings = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => ListFiles(room.Id)));

        foreach (var listed in listings)
        {
            listed.Should().HaveCount(FilesInRoom);
            listed.Should().OnlyContain(f => !string.IsNullOrEmpty(f.ShortWebUrl));
        }

        listings.Select(l => string.Join("|", l.OrderBy(f => f.Title).Select(f => f.ShortWebUrl)))
            .Distinct()
            .Should().ContainSingle("parallel listings must agree on every file's link");
    }

    private async Task<List<FileDto>> CreateFiles(int roomId, string prefix, int count = FilesInRoom)
    {
        var files = new List<FileDto>();

        for (var i = 0; i < count; i++)
        {
            files.Add(await CreateFile($"Autotest {prefix} {i}.docx", roomId));
        }

        return files;
    }

    private async Task<List<FileEntryBaseDto>> ListFiles(int roomId)
    {
        var content = (await _foldersApi.GetFolderByFolderIdAsync(roomId, count: 100, cancellationToken: TestContext.Current.CancellationToken)).Response;

        return content.Files;
    }
}

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
// design elements, icons, logos, and text, are the property of
// Ascensio System SIA and are protected by copyright and trademark laws.
// These elements may not be used in derivative works or modified in any way
// without prior written permission from Ascensio System SIA.
//
// Pursuant to Section 7 § 3(b) of the License you must retain the original
// Product logo when distributing the program. Pursuant to Section 7 § 3(e) we
// decline to grant you any rights under trademark law for use of our trademarks.
//
// SPDX-License-Identifier: AGPL-3.0-only

namespace ASC.Files.Tests.Tests._10_Metadata;

/// <summary>
/// Covers the paging and sorting members of the typed search bodies: <c>searchFolder</c> and <c>searchRooms</c> sort
/// the filtered result as asked, cut the page from it and count the whole filtered result in <c>total</c>.
/// </summary>
[Trait("Category", "Metadata")]
public class MetadataTypedSearchPagingTests(AspireAppFixture fixture) : MetadataApiTestBase(fixture)
{
    [Fact]
    public async Task SearchFolder_SortsByTitleDescending_AndCutsThePage()
    {
        await _filesClient.Authenticate(Owner);
        var suffix = Suffix();
        var template = await CreateTemplateAsync();
        var room = await CreateCustomRoom($"Paging {suffix}");

        var folders = new List<FolderDto>();

        foreach (var title in new[] { "Alpha", "Bravo", "Charlie" })
        {
            var folder = await CreateFolder($"{title} {suffix}", room.Id);
            await AssignToFolderAsync(folder.Id, template.Id);
            folders.Add(folder);
        }

        await CreateFolder($"Delta {suffix}", room.Id);

        var all = await PollAsync(() => _foldersApi.SearchFolderWithMetadataAsync(room.Id, new FolderMetadataSearch(template.Id, count: 100), TestContext.Current.CancellationToken), c => c.Total == 3);
        all.FolderIds().Should().BeEquivalentTo(folders.Select(f => f.Id), "the template alone keeps its carriers, not the fourth folder");

        var page = await _foldersApi.SearchFolderWithMetadataAsync(room.Id,
            new FolderMetadataSearch(template.Id, count: 1, startIndex: 1, sortBy: "AZ", sortOrder: SortOrder.Descending), TestContext.Current.CancellationToken);

        // Charlie, Bravo, Alpha: the second of them
        page.FolderIds().Should().Equal(folders[1].Id);
        page.Total.Should().Be(3, "the total counts the filtered result, not the page");
    }

    [Fact]
    public async Task SearchRooms_SortsByTitleAscending_AndCutsThePage()
    {
        await _filesClient.Authenticate(Owner);
        var suffix = Suffix();
        var template = await CreateTemplateAsync();

        var rooms = new List<FolderDto>();

        foreach (var title in new[] { "Alpha", "Bravo", "Charlie" })
        {
            var room = await CreateCustomRoom($"{title} {suffix}");
            await AssignToFolderAsync(room.Id, template.Id);
            rooms.Add(room);
        }

        await CreateCustomRoom($"Delta {suffix}");

        var all = await PollAsync(async () => Wrap(await _roomsApi.SearchRoomsWithMetadataAsync(new RoomsMetadataSearchRequestDto(template.Id, count: 100), TestContext.Current.CancellationToken)), c => c.Total == 3);
        all.FolderIds().Should().BeEquivalentTo(rooms.Select(r => r.Id));

        var page = await _roomsApi.SearchRoomsWithMetadataAsync(
            new RoomsMetadataSearchRequestDto(template.Id, count: 2, startIndex: 1, sortBy: "AZ", sortOrder: SortOrder.Ascending), TestContext.Current.CancellationToken);

        page.RoomIds().Should().Equal(rooms[1].Id, rooms[2].Id);
        page.Total.Should().Be(3);
    }

    private static FolderContentResponse Wrap(RoomsContentResponse rooms)
    {
        return new FolderContentResponse { Folders = rooms.Folders, Total = rooms.Total, Count = rooms.Count };
    }

    private static async Task<FolderContentResponse> PollAsync(Func<Task<FolderContentResponse>> search, Func<FolderContentResponse, bool> until)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);

        while (true)
        {
            var content = await search();

            if (until(content) || DateTime.UtcNow >= deadline)
            {
                return content;
            }

            await Task.Delay(300, TestContext.Current.CancellationToken);
        }
    }
}

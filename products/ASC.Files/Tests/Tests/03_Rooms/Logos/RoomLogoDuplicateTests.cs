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

namespace ASC.Files.Tests.Tests._03_Rooms.Logos;

/// <summary>
/// A room duplicated with <c>duplicateBatchItems</c> carries a logo of its own, copied from the original's.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomLogoDuplicateTests(
    AspireAppFixture fixture)
    : RoomLogoTestBase(fixture)
{
    [Fact]
    public async Task DuplicateRoom_WithLogo_CopyCarriesItsOwnCopyOfTheLogo()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var title = "Autotest Dup Logo " + Guid.NewGuid().ToString()[..8];
        var room = await CreateRoomWithLogo(title);
        room.Logo.Original.Should().NotBeNullOrEmpty("the premise is that the original has a logo");

        // Act
        var result = await _filesOperationsApi.DuplicateBatchItemsAsync(
            new DuplicateRequestDto(folderIds: [new DuplicateRequestDtoAllOfFolderIds(room.Id)], fileIds: []) { ReturnSingleOperation = true },
            TestContext.Current.CancellationToken);
        await WaitLongOperation(result.Response.FirstOrDefault()?.Id);

        // Assert
        var copy = await FindCopyAsync(room.Id, title);
        copy.Logo.Should().NotBeNull();
        copy.Logo.Original.Should().NotBeNullOrEmpty("the logo is copied with the room");
        copy.Logo.Original.Should().NotBe(room.Logo.Original, "the copy stores a logo of its own rather than pointing at the original's");
    }

    private async Task<FolderDto> FindCopyAsync(int originalId, string title)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);

        while (true)
        {
            var listed = (await _roomsApi.GetRoomsFolderAsync(searchArea: SearchArea.Active, cancellationToken: TestContext.Current.CancellationToken)).Response;
            var copy = (listed.Folders ?? []).OfType<FolderDto>().FirstOrDefault(f => f.Id != originalId && f.Title.StartsWith(title, StringComparison.Ordinal));

            if (copy != null)
            {
                return (await _roomsApi.GetRoomInfoAsync(copy.Id, TestContext.Current.CancellationToken)).Response;
            }

            if (DateTime.UtcNow >= deadline)
            {
                throw new InvalidOperationException($"No copy of room {originalId} ('{title}') appeared in the Rooms section.");
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }
}

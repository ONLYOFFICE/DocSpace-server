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

namespace ASC.Files.Tests.Tests._03_Rooms.FormFilling;

/// <summary>
/// Open bugs in the Recent, Favorites and room-template sections that are specific to form filling
/// rooms after the Forms/Rooms split. Every test here asserts what the product is supposed to do,
/// so it stays red while the bug is open and turns green the day it is fixed - see
/// <c>Tests/03_Rooms/Listing/RoomsFolderFilterTests.cs</c> for the same convention.
/// </summary>
[Trait("Category", "Rooms")]
public class FormFillingSectionBugTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    #region GET /files/recent - searchArea has no effect on which files come back

    /// <summary>
    /// Creates a form filling room holding one form and a custom room holding one .docx, adds both
    /// files to Recent, and returns their titles (the SDK's <see cref="FileEntryBaseDto"/> carries a
    /// <c>Title</c> but no <c>Id</c> for Recent listings, so titles are the only stable identity here).
    /// </summary>
    private async Task<(string FormTitle, string DocxTitle)> SeedRecent()
    {
        var ffr = await CreateFillingFormsRoom("Recent Form Room " + Guid.NewGuid().ToString()[..8]);
        var custom = await CreateCustomRoom("Recent Custom Room " + Guid.NewGuid().ToString()[..8]);

        var formTitle = "Autotest Recent Form " + Guid.NewGuid().ToString()[..8] + ".pdf";
        var docxTitle = "Autotest Recent Docx " + Guid.NewGuid().ToString()[..8] + ".docx";

        var formFile = await CreateFile(formTitle, ffr.Id);
        var docxFile = await CreateFile(docxTitle, custom.Id);

        await _filesApi.AddFileToRecentAsync(formFile.Id, cancellationToken: TestContext.Current.CancellationToken);
        await _filesApi.AddFileToRecentAsync(docxFile.Id, cancellationToken: TestContext.Current.CancellationToken);

        return (formTitle, docxTitle);
    }

    /// <remarks>
    /// Bug 82873: GET /files/recent accepts a <c>searchArea</c> parameter, but it has no effect on
    /// the result - the same set of files comes back for every value. Requesting
    /// <see cref="SearchArea.Forms"/> should return only files from form filling rooms; instead it
    /// still returns a file from a Rooms-section (Custom) room.
    /// </remarks>
    [Fact]
    [Trait("Bug", "82873")]
    public async Task GetRecentFolder_SearchAreaForms_StillReturnsRoomsSectionFile()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var (formTitle, docxTitle) = await SeedRecent();

        // Act
        var recent = (await _foldersApi.GetRecentFolderAsync(
            searchArea: SearchArea.Forms,
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        var titles = recent.Files.Select(f => f.Title).ToList();
        titles.Should().Contain(formTitle);
        titles.Should().NotContain(docxTitle,
            "searchArea=Forms must return only files that live in form filling rooms");
    }

    /// <inheritdoc cref="GetRecentFolder_SearchAreaForms_StillReturnsRoomsSectionFile"/>
    [Fact]
    [Trait("Bug", "82873")]
    public async Task GetRecentFolder_SearchAreaActive_StillReturnsFormFillingRoomFile()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var (formTitle, docxTitle) = await SeedRecent();

        // Act
        var recent = (await _foldersApi.GetRecentFolderAsync(
            searchArea: SearchArea.Active,
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        var titles = recent.Files.Select(f => f.Title).ToList();
        titles.Should().Contain(docxTitle);
        titles.Should().NotContain(formTitle,
            "searchArea=Active must return only files that live in Rooms-section rooms");
    }

    /// <inheritdoc cref="GetRecentFolder_SearchAreaForms_StillReturnsRoomsSectionFile"/>
    [Fact]
    [Trait("Bug", "82873")]
    public async Task GetRecentFolder_SearchAreaFormsWithFilterValue_StillMatchesRoomsSectionFile()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var (_, docxTitle) = await SeedRecent();
        var needle = docxTitle[..^5]; // drop the ".docx" extension, matching the TS source's partial-title search

        // Act
        var recent = (await _foldersApi.GetRecentFolderAsync(
            searchArea: SearchArea.Forms,
            filterValue: needle,
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        recent.Files.Select(f => f.Title).Should().NotContain(docxTitle,
            "filterValue under searchArea=Forms must not match a file from a Rooms-section room");
    }

    #endregion

    #region GET /files/recent - total collapses to the page size

    /// <remarks>
    /// Bug 82874: <c>total</c> must report the size of the whole selection, not of the current page.
    /// Requesting one page of two available Recent files currently reports <c>total = 1</c> (the page
    /// size) instead of <c>total = 2</c> (the full selection).
    /// </remarks>
    [Fact]
    [Trait("Bug", "82874")]
    public async Task GetRecentFolder_PagedRequest_TotalCollapsesToPageSize()
    {
        // Arrange - both files go into Rooms-section rooms on purpose. SeedRecent puts one of its two
        // files in a form filling room, which the default searchArea=Active correctly leaves out, so it
        // cannot serve a test about `total`.
        await _filesClient.Authenticate(Owner);

        var first = await CreateCustomRoom("Recent Total Room A " + Guid.NewGuid().ToString()[..8]);
        var second = await CreateCustomRoom("Recent Total Room B " + Guid.NewGuid().ToString()[..8]);

        var firstFile = await CreateFile("Autotest Recent Total A " + Guid.NewGuid().ToString()[..8] + ".docx", first.Id);
        var secondFile = await CreateFile("Autotest Recent Total B " + Guid.NewGuid().ToString()[..8] + ".docx", second.Id);

        await _filesApi.AddFileToRecentAsync(firstFile.Id, cancellationToken: TestContext.Current.CancellationToken);
        await _filesApi.AddFileToRecentAsync(secondFile.Id, cancellationToken: TestContext.Current.CancellationToken);

        var all = (await _foldersApi.GetRecentFolderAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        all.Total.Should().Be(2, "the premise is that both seeded files are present in Recent");

        // Act
        var page = (await _foldersApi.GetRecentFolderAsync(
            count: 1,
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        page.Count.Should().Be(1);
        page.Total.Should().Be(2, "total reports the size of the whole selection, not of the page");
    }

    #endregion

    #region GET /files/@favorites - pagination and total

    /// <summary>
    /// Two favorited form filling rooms with titles chosen to sort deterministically, plus one
    /// deliberately unfavored room as a negative control.
    /// </summary>
    private async Task<(string ATitle, string ZTitle)> SeedFavorites()
    {
        var aTitle = "AAA Fav Form " + Guid.NewGuid().ToString()[..8];
        var zTitle = "ZZZ Fav Form " + Guid.NewGuid().ToString()[..8];

        var aForm = await CreateFillingFormsRoom(aTitle);
        var zForm = await CreateFillingFormsRoom(zTitle);
        await CreateFillingFormsRoom("MMM Fav Form Excluded " + Guid.NewGuid().ToString()[..8]);

        await _filesOperationsApi.AddFavoritesAsync(
            new BaseBatchRequestDto { FolderIds = [new BaseBatchRequestDtoAllOfFolderIds(aForm.Id)] },
            cancellationToken: TestContext.Current.CancellationToken);
        await _filesOperationsApi.AddFavoritesAsync(
            new BaseBatchRequestDto { FolderIds = [new BaseBatchRequestDtoAllOfFolderIds(zForm.Id)] },
            cancellationToken: TestContext.Current.CancellationToken);

        return (aTitle, zTitle);
    }

    /// <remarks>
    /// Bug 82875: paginating Favorites with <c>count</c> together with a non-zero <c>startIndex</c>
    /// currently returns an empty page instead of the second item.
    /// </remarks>
    [Fact]
    [Trait("Bug", "82875")]
    public async Task GetFavoritesFolder_CountWithStartIndex_ReturnsEmptyPage()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var (aTitle, zTitle) = await SeedFavorites();

        var asc = (await _foldersApi.GetFavoritesFolderAsync(
            sortBy: "AZ",
            sortOrder: SortOrder.Ascending,
            cancellationToken: TestContext.Current.CancellationToken)).Response;
        // ContainInOrder takes only the expected items - a trailing reason string would be read as
        // one more item to find.
        asc.Folders.Select(f => f.Title).Should().ContainInOrder(aTitle, zTitle);

        // Act
        var page = (await _foldersApi.GetFavoritesFolderAsync(
            sortBy: "AZ",
            sortOrder: SortOrder.Ascending,
            count: 1,
            startIndex: 1,
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        page.Folders.Select(f => f.Title).Should().ContainSingle().Which.Should().Be(zTitle,
            "count together with startIndex must return the second item, not an empty page");
    }

    /// <remarks>
    /// Bug 82877: <c>total</c> must report the size of the whole Favorites selection, not of the
    /// current page. Requesting one page of two favorited rooms currently reports <c>total = 1</c>
    /// (the page size) instead of <c>total = 2</c>.
    /// </remarks>
    [Fact]
    [Trait("Bug", "82877")]
    public async Task GetFavoritesFolder_PagedRequest_TotalCollapsesToPageSize()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await SeedFavorites();

        var all = (await _foldersApi.GetFavoritesFolderAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        all.Total.Should().Be(2, "the premise is that both seeded rooms are favorited");

        // Act
        var page = (await _foldersApi.GetFavoritesFolderAsync(
            count: 1,
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        page.Count.Should().Be(1);
        page.Total.Should().Be(2, "total reports the size of the whole selection, not of the page");
    }

    #endregion

    #region POST /files/rooms/fromtemplate - a form template's tags are dropped

    /// <remarks>
    /// Bug 82878: a form room template stores whatever tags it was created with
    /// (<c>createRoomTemplate</c> copies the <c>tags</c> field verbatim), but creating a room from
    /// that template currently drops them - the new room is created with the right
    /// <see cref="RoomType.FillingFormsRoom"/> type but an empty tag list.
    /// </remarks>
    [Fact]
    [Trait("Bug", "82878")]
    public async Task CreateRoomFromTemplate_FormTemplateTags_AreNotAppliedToNewRoom()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var formRoom = await CreateFillingFormsRoom("Tagged Form Source " + Guid.NewGuid().ToString()[..8]);
        const string tag = "FormTemplateTag";

        await _roomsApi.CreateRoomTemplateAsync(
            new RoomTemplateDto(roomId: formRoom.Id, title: "Tagged Form Template " + Guid.NewGuid().ToString()[..8], tags: [tag]),
            TestContext.Current.CancellationToken);
        var templateId = await WaitForRoomTemplate();

        var template = (await _roomsApi.GetRoomInfoAsync(templateId, TestContext.Current.CancellationToken)).Response;
        template.Tags.Should().Contain(tag, "the premise is that the template itself really does hold the tag");

        // Act
        await _roomsApi.CreateRoomFromTemplateAsync(
            new CreateRoomFromTemplateDto(templateId: templateId, title: "Room From Tagged Form Template " + Guid.NewGuid().ToString()[..8]),
            TestContext.Current.CancellationToken);
        var newRoomId = await WaitForRoomFromTemplate();

        // Assert
        var info = (await _roomsApi.GetRoomInfoAsync(newRoomId, TestContext.Current.CancellationToken)).Response;
        info.RoomType.Should().Be(RoomType.FillingFormsRoom);
        info.Tags.Should().Contain(tag,
            "a room created from a template inherits the template's tags");
    }

    #endregion
}

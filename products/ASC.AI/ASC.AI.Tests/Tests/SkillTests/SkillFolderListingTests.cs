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

namespace ASC.AI.Tests.Tests.SkillTests;

/// <summary>
/// The room's skills folder listing, <c>GET files/rooms/{id}/ai</c> (skills.spec.ts, "the room's skills folder
/// listing"). The TypeScript tests used a custom room with an optional <c>.ai</c> folder; the skills folder is now the
/// root of an Instructions room, and the route answers 404 for a room of any other type by design.
/// </summary>
[Trait("Category", "Read")]
[Trait("Feature", "AI/Skills")]
public class SkillFolderListingTests(AspireAppFixture fixture) : SkillTestBase(fixture)
{
    /// <summary>The UI asks for this listing for every room it opens; a room with no skill yet used to answer 404 instead of an empty listing.</summary>
    [Fact]
    [Trait("Bug", "84329")]
    public async Task GetRoomAiFolder_InstructionsRoomWithoutSkills_ReturnsAnEmptyListing()
    {
        var roomId = await CreateInstructionsRoomAsync();

        var listing = await RoomsApi.GetRoomAiFolderAsync(roomId, cancellationToken: TestContext.Current.CancellationToken);

        listing.Response.Files.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRoomAiFolder_InstructionsRoomWithASkill_ListsIt()
    {
        var roomId = await CreateInstructionsRoomAsync();
        var qa = await UploadQaSkillAsync(roomId);

        var listing = await RoomsApi.GetRoomAiFolderAsync(roomId, cancellationToken: TestContext.Current.CancellationToken);

        listing.Response.Files.OfType<FileDto>().Select(f => f.Id).Should().Equal(qa.Id);
    }

    [Fact]
    public async Task GetRoomAiFolder_RoomOfAnotherType_Is404()
    {
        await FilesHttpClient.Authenticate(Owner);
        var customRoom = (await RoomsApi.CreateRoomAsync(
            new CreateRoomRequestDto($"Autotest Not Instructions {Guid.NewGuid():N}", roomType: RoomType.CustomRoom),
            TestContext.Current.CancellationToken)).Response;

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await RoomsApi.GetRoomAiFolderAsync(customRoom.Id, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }
}

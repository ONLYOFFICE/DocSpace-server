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

namespace ASC.AI.Tests.Tests.AiProjectsTests;

[Trait("Category", "Update")]
[Trait("Feature", "AI/Projects")]
public class AiProjectRoomsLifecycleTests(AspireAppFixture fixture) : AiProjectRoomsTestBase(fixture)
{
    [Fact]
    public async Task DeletedRoom_LosesLink()
    {
        // Arrange
        var projectId = await CreateProjectAsync();
        var deleted = await CreateInstructionsRoomAsync();
        var kept = await CreateInstructionsRoomAsync();
        using var linked = await LinkRoomsAsync(projectId, deleted, kept);

        // Act
        await DeleteRoomAsync(deleted);

        // Assert
        (await GetInstructionsRoomIdsAsync(projectId)).Should().Equal(kept);
    }

    [Fact]
    public async Task LinkRooms_OverLimit_Returns400AndKeepsExistingLinks()
    {
        // Arrange
        var projectId = await CreateProjectAsync();
        var rooms = new List<int>();
        for (var i = 0; i <= 100; i++)
        {
            rooms.Add(await CreateInstructionsRoomAsync($"Autotest Instructions {i:D3}"));
        }

        using var filled = await LinkRoomsAsync(projectId, [.. rooms.Take(100)]);

        // Act
        using var response = await LinkRoomsAsync(projectId, rooms[100]);

        // Assert
        filled.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await GetInstructionsRoomIdsAsync(projectId)).Should().HaveCount(100).And.NotContain(rooms[100]);
    }
}

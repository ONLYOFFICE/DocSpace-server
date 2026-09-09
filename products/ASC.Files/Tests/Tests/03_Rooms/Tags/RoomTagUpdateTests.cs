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

namespace ASC.Files.Tests.Tests._03_Rooms.Tags;

/// <summary>
/// PUT /files/tags (updateRoomTag) — core rename contract, catalog/room propagation and body
/// validation. Case sensitivity, whitespace, Unicode and other edge cases live in
/// <see cref="RoomTagUpdateEdgeCasesTests"/>. Access-level coverage lives in
/// <c>Permissions/RoomCustomTagValidationPermissionsTests</c>; the plain "owner renames a tag"
/// case from the TS suite is dropped here as a duplicate of that suite's
/// <c>UpdateTag_AllowedRoles_Renamed</c> and folded into
/// <see cref="UpdateTag_ValidRename_ReturnsNewNameWithCorrectStructure"/> instead, which also
/// asserts the response shape (count, type).
/// </summary>
[Trait("Category", "Rooms")]
public class RoomTagUpdateTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task UpdateTag_ValidRename_ReturnsNewNameWithCorrectStructure()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("Autotest Structure Old"), TestContext.Current.CancellationToken);

        // Act
        var response = await _roomsApi.UpdateRoomTagAsync(
            new UpdateTagRequestDto("Autotest Structure Old", "Autotest Structure New"),
            TestContext.Current.CancellationToken);

        // Assert
        response.Count.Should().Be(1);
        response.Response.Should().BeOfType<string>();
        response.Response.Should().Be("Autotest Structure New");
    }

    [Fact]
    public async Task UpdateTag_OldNameRemovedFromCatalogAfterRename()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("Autotest Old Name Gone"), TestContext.Current.CancellationToken);

        // Act
        await _roomsApi.UpdateRoomTagAsync(
            new UpdateTagRequestDto("Autotest Old Name Gone", "Autotest Old Name Replaced"),
            TestContext.Current.CancellationToken);

        // Assert
        var tags = await GetTagCatalog();
        tags.Should().NotContain("Autotest Old Name Gone");
    }

    [Fact]
    public async Task UpdateTag_NewNameAppearsInCatalogAfterRename()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("Autotest New Name Check Old"), TestContext.Current.CancellationToken);

        // Act
        await _roomsApi.UpdateRoomTagAsync(
            new UpdateTagRequestDto("Autotest New Name Check Old", "Autotest New Name Check New"),
            TestContext.Current.CancellationToken);

        // Assert
        var tags = await GetTagCatalog();
        tags.Should().Contain("Autotest New Name Check New");
    }

    [Fact]
    public async Task UpdateTag_RoomTagReflectsNewNameAfterRename()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("Autotest Room Tag Old Name"), TestContext.Current.CancellationToken);
        var room = await CreateCustomRoom("Autotest Room With Renamed Tag");
        await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto(["Autotest Room Tag Old Name"]), TestContext.Current.CancellationToken);

        // Act
        await _roomsApi.UpdateRoomTagAsync(
            new UpdateTagRequestDto("Autotest Room Tag Old Name", "Autotest Room Tag New Name"),
            TestContext.Current.CancellationToken);

        // Assert
        var roomInfo = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        roomInfo.Tags.Should().NotContain("Autotest Room Tag Old Name");
        roomInfo.Tags.Should().Contain("Autotest Room Tag New Name");
    }

    [Fact]
    public async Task UpdateTag_GlobalRename_UpdatesAllRoomsAndCatalogOnce()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("Autotest Global Old"), TestContext.Current.CancellationToken);

        var room1 = await CreateCustomRoom("Autotest Global Room 1");
        var room2 = await CreateCustomRoom("Autotest Global Room 2");
        await _roomsApi.AddRoomTagsAsync(room1.Id, new BatchTagsRequestDto(["Autotest Global Old"]), TestContext.Current.CancellationToken);
        await _roomsApi.AddRoomTagsAsync(room2.Id, new BatchTagsRequestDto(["Autotest Global Old"]), TestContext.Current.CancellationToken);

        // Act
        await _roomsApi.UpdateRoomTagAsync(
            new UpdateTagRequestDto("Autotest Global Old", "Autotest Global New"),
            TestContext.Current.CancellationToken);

        // Assert
        var info1 = (await _roomsApi.GetRoomInfoAsync(room1.Id, TestContext.Current.CancellationToken)).Response;
        var info2 = (await _roomsApi.GetRoomInfoAsync(room2.Id, TestContext.Current.CancellationToken)).Response;
        info1.Tags.Should().Contain("Autotest Global New").And.NotContain("Autotest Global Old");
        info2.Tags.Should().Contain("Autotest Global New").And.NotContain("Autotest Global Old");

        // Not two separate tags: catalog holds only the new name, once.
        var catalog = await GetTagCatalog();
        catalog.Should().Contain("Autotest Global New").And.NotContain("Autotest Global Old");
        catalog.Count(t => t == "Autotest Global New").Should().Be(1);
    }

    [Fact]
    public async Task UpdateTag_NonExistentTag_ReturnsNotFound()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.UpdateRoomTagAsync(
                new UpdateTagRequestDto("Autotest NonExistent Tag 99999", "Autotest New Name For NonExistent"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    /// <remarks>Business rule: renaming a tag to its own name is treated as a duplicate.</remarks>
    [Fact]
    public async Task UpdateTag_SameNameRename_ReturnsBadRequest()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("Autotest Same Name Tag"), TestContext.Current.CancellationToken);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.UpdateRoomTagAsync(
                new UpdateTagRequestDto("Autotest Same Name Tag", "Autotest Same Name Tag"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent?.ToString().Should().Contain("already exists");
    }

    [Fact]
    public async Task UpdateTag_ConflictingExistingName_ReturnsBadRequest()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("Autotest Conflict Source"), TestContext.Current.CancellationToken);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("Autotest Conflict Target"), TestContext.Current.CancellationToken);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.UpdateRoomTagAsync(
                new UpdateTagRequestDto("Autotest Conflict Source", "Autotest Conflict Target"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent?.ToString().Should().Contain("already exists");
    }

    [Fact]
    public async Task UpdateTag_EmptyNewName_ReturnsBadRequest()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("Autotest Empty New Name"), TestContext.Current.CancellationToken);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.UpdateRoomTagAsync(
                new UpdateTagRequestDto("Autotest Empty New Name", ""),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    // The typed DTO's constructor requires non-null oldName/newName and throws client-side for
    // null, so these bodies — which the TS suite sends with null/missing fields — can only be
    // produced over raw HTTP.
    [Theory]
    [InlineData("""{"oldName":"","newName":"Autotest Empty Old Name New"}""")]
    [InlineData("""{"newName":"Autotest Missing Old Name"}""")]
    [InlineData("{}")]
    [InlineData("""{"oldName":null,"newName":"Autotest Null Old Name New"}""")]
    [InlineData("""{"oldName":null,"newName":null}""")]
    public async Task UpdateTag_MalformedBody_ReturnsBadRequest(string body)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        using var response = await SendRawTagsUpdate(body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateTag_MissingNewName_ReturnsBadRequestAndLeavesOldTagIntact()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("Autotest Missing New Name"), TestContext.Current.CancellationToken);

        // Act
        using var response = await SendRawTagsUpdate("""{"oldName":"Autotest Missing New Name"}""");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var tags = await GetTagCatalog();
        tags.Should().Contain("Autotest Missing New Name");
    }

    [Fact]
    public async Task UpdateTag_NullNewName_ReturnsBadRequestAndLeavesOldTagIntact()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("Autotest Null New Name"), TestContext.Current.CancellationToken);

        // Act
        using var response = await SendRawTagsUpdate("""{"oldName":"Autotest Null New Name","newName":null}""");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var tags = await GetTagCatalog();
        tags.Should().Contain("Autotest Null New Name");
    }

    /// <summary>Reads the tag catalog and unwraps it into plain strings.</summary>
    private async Task<List<string>> GetTagCatalog()
    {
        var tags = (await _roomsApi.GetRoomTagsInfoAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        return tags.ConvertAll(t => t.ToString()!);
    }

    /// <summary>
    /// Sends a raw PUT /api/2.0/files/tags with an arbitrary JSON body, bypassing the typed SDK
    /// so that bodies with missing/null required fields can be tested.
    /// </summary>
    private async Task<HttpResponseMessage> SendRawTagsUpdate(string json)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, "api/2.0/files/tags")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        return await _filesClient.SendAsync(request, TestContext.Current.CancellationToken);
    }
}

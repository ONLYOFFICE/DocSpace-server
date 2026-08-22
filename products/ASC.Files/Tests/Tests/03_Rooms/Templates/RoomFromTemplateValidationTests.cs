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

namespace ASC.Files.Tests.Tests._03_Rooms.Templates;

/// <summary>
/// Request validation of POST /files/rooms/fromTemplate. <c>CreateRoomFromTemplateDto</c> declares
/// <c>TemplateId</c> and <c>Title</c> as C# <c>required</c> members, so a body missing either one
/// can only be sent raw — the typed client would never put such a request on the wire.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomFromTemplateValidationTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    [Fact]
    public async Task CreateRoomFromTemplate_MissingTemplateId_BadRequest()
    {
        // Arrange & Act
        await _filesClient.Authenticate(Owner);
        var response = await SendRawCreateRoomFromTemplate("""{"title":"Room"}""");

        // Assert
        ((int)response.StatusCode).Should().Be(400);
    }

    [Fact]
    public async Task CreateRoomFromTemplate_NullTemplateId_BadRequest()
    {
        // Arrange & Act
        await _filesClient.Authenticate(Owner);
        var response = await SendRawCreateRoomFromTemplate("""{"templateId":null,"title":"Room"}""");

        // Assert
        ((int)response.StatusCode).Should().Be(400);
    }

    /// <remarks>
    /// Bug 81667: a template id that cannot resolve to a template (0, an id nobody has, or one that
    /// has just been deleted) used to answer 403 "access denied", which blamed the caller's
    /// permissions for what is a plain missing resource. Fixed in
    /// <c>FileStorageService.CheckCanCreateRoomFromTemplateAsync</c>, which now separates "no such
    /// template" from "you may not read this template".
    ///
    /// The TypeScript suite asked for 400 here; this asserts 404 instead, deliberately. A
    /// well-formed id that resolves to nothing is a missing resource, and the sibling endpoint's own
    /// bug (81691, <c>RoomTemplateCreateTests</c>) settles on 404 for exactly the same shape of
    /// error — two adjacent endpoints answering differently would be the real defect.
    /// </remarks>
    [Theory]
    [InlineData(0)]
    [InlineData(999999999)]
    [Trait("Bug", "81667")]
    public async Task CreateRoomFromTemplate_NonResolvableTemplateId_ShouldReturnNotFound(int templateId)
    {
        // Act
        await _filesClient.Authenticate(Owner);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.CreateRoomFromTemplateAsync(
                new CreateRoomFromTemplateDto(templateId, "Room"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);

        var titles = await GetRoomTitles();
        titles.Should().NotContain("Room");
    }

    /// <remarks>See the id-based cases above for why this asserts 404 rather than the 400 the
    /// TypeScript suite asked for.</remarks>
    [Fact]
    [Trait("Bug", "81667")]
    public async Task CreateRoomFromTemplate_DeletedTemplate_ShouldReturnNotFound()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var templateId = await CreateTemplate("Autotest Deleted Tmpl", isPublic: false);

        await _roomsApi.DeleteRoomAsync(templateId, new DeleteRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.CreateRoomFromTemplateAsync(
                new CreateRoomFromTemplateDto(templateId, "Room After Delete"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);

        var titles = await GetRoomTitles();
        titles.Should().NotContain("Room After Delete");
    }

    [Fact]
    public async Task CreateRoomFromTemplate_MissingTitle_BadRequest()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var templateId = await CreateTemplate("Autotest Missing Title", isPublic: false);

        // Act
        var response = await SendRawCreateRoomFromTemplate($$"""{"templateId":{{templateId}}}""");

        // Assert
        ((int)response.StatusCode).Should().Be(400);
    }

    /// <remarks>
    /// Bug 81669: unlike the sibling POST /files/roomtemplate endpoint (see
    /// <see cref="RoomTemplateCreateValidationTests"/>), an empty, whitespace-only or excessively
    /// long title is accepted here and queues a room-creation operation instead of being rejected.
    /// </remarks>
    [Fact]
    [Trait("Bug", "81669")]
    public async Task CreateRoomFromTemplate_EmptyTitle_ShouldBeBadRequest()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var templateId = await CreateTemplate("Autotest Empty Title", isPublic: false);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.CreateRoomFromTemplateAsync(
                new CreateRoomFromTemplateDto(templateId, string.Empty),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);

        var titles = await GetRoomTitles();
        titles.Should().NotContain(string.Empty);
    }

    [Fact]
    [Trait("Bug", "81669")]
    public async Task CreateRoomFromTemplate_WhitespaceTitle_ShouldBeBadRequest()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var templateId = await CreateTemplate("Autotest Blank Title", isPublic: false);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.CreateRoomFromTemplateAsync(
                new CreateRoomFromTemplateDto(templateId, "   "),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);

        var titles = await GetRoomTitles();
        titles.Should().NotContain("   ");
    }

    [Fact]
    [Trait("Bug", "81669")]
    public async Task CreateRoomFromTemplate_ExcessivelyLongTitle_ShouldBeBadRequest()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var templateId = await CreateTemplate("Autotest Long Title", isPublic: false);
        var longTitle = new string('A', 1000);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.CreateRoomFromTemplateAsync(
                new CreateRoomFromTemplateDto(templateId, longTitle),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);

        var titles = await GetRoomTitles();
        titles.Should().NotContain(longTitle);
    }

    [Fact]
    public async Task CreateRoomFromTemplate_ForbiddenCharsInTitle_SanitizedToUnderscore()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var templateId = await CreateTemplate("Autotest Forbidden", isPublic: false);

        // Act
        await _roomsApi.CreateRoomFromTemplateAsync(
            new CreateRoomFromTemplateDto(templateId, "Bad\" \\ < > / Title"),
            TestContext.Current.CancellationToken);
        var roomId = await WaitForRoomFromTemplate();

        // Assert - forbidden characters are silently replaced with `_`.
        var info = (await _roomsApi.GetRoomInfoAsync(roomId, TestContext.Current.CancellationToken)).Response;
        info.Title.Should().NotContain("\"");
        info.Title.Should().NotContain("\\");
        info.Title.Should().NotContain("<");
        info.Title.Should().NotContain(">");
        info.Title.Should().NotContain("/");
        info.Title.Should().Contain("_");
    }

    [Fact]
    public async Task CreateRoomFromTemplate_DuplicateTitles_Allowed()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var templateId = await CreateTemplate("Autotest Dup", isPublic: false);

        // Act
        await _roomsApi.CreateRoomFromTemplateAsync(new CreateRoomFromTemplateDto(templateId, "Duplicate Title"), TestContext.Current.CancellationToken);
        var roomAId = await WaitForRoomFromTemplate();

        await _roomsApi.CreateRoomFromTemplateAsync(new CreateRoomFromTemplateDto(templateId, "Duplicate Title"), TestContext.Current.CancellationToken);
        var roomBId = await WaitForRoomFromTemplate();

        // Assert
        roomAId.Should().NotBe(roomBId);
    }

    /// <summary>
    /// Sends a raw POST /api/2.0/files/rooms/fromTemplate, for a body the generated DTO cannot
    /// express (a missing required member).
    /// </summary>
    private async Task<HttpResponseMessage> SendRawCreateRoomFromTemplate(string json)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/2.0/files/rooms/fromTemplate")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        return await _filesClient.SendAsync(request, TestContext.Current.CancellationToken);
    }
}

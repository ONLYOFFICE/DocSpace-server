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

using QuotaSettingsRequestsDto = DocSpace.API.SDK.Model.QuotaSettingsRequestsDto;

namespace ASC.Files.Tests.Tests._03_Rooms.Create;

/// <summary>
/// Input validation of <c>POST /files/rooms</c>: the required <c>title</c>/<c>roomType</c> pair and
/// every optional field that the typed DTO can carry but the server is still expected to reject or
/// normalise.
/// </summary>
/// <remarks>
/// A few cases send a body the generated <see cref="CreateRoomRequestDto"/> cannot express - a
/// missing/null required property, or a string where the DTO declares an array/object - so those go
/// over raw HTTP instead of through the SDK (see `.claude/rules/tests.md`, "Endpoints the SDK does
/// not expose").
/// </remarks>
[Trait("Category", "Rooms")]
[Trait("Feature", "RoomCreate")]
public class RoomCreateValidationTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task CreateRoom_MissingTitle_BadRequest()
    {
        // Act
        using var response = await CreateRoomRawAsync("""{"roomType":5}""");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateRoom_NullTitle_BadRequest()
    {
        // Act
        using var response = await CreateRoomRawAsync("""{"title":null,"roomType":5}""");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateRoom_EmptyTitle_BadRequest()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.CreateRoomAsync(
                new CreateRoomRequestDto("", roomType: RoomType.CustomRoom),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task CreateRoom_WhitespaceOnlyTitle_BadRequest()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.CreateRoomAsync(
                new CreateRoomRequestDto("   ", roomType: RoomType.CustomRoom),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task CreateRoom_MissingRoomType_BadRequest()
    {
        // Act
        using var response = await CreateRoomRawAsync("""{"title":"Autotest"}""");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateRoom_NullRoomType_BadRequest()
    {
        // Act
        using var response = await CreateRoomRawAsync("""{"title":"Autotest","roomType":null}""");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateRoom_UnknownRoomType_BadRequest()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.CreateRoomAsync(
                new CreateRoomRequestDto("Autotest", roomType: (RoomType)99999),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task CreateRoom_ExcessivelyLongTitle_BadRequest()
    {
        // Arrange
        var title = new string('A', 1000);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.CreateRoomAsync(
                new CreateRoomRequestDto(title, roomType: RoomType.CustomRoom),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task CreateRoom_NegativeQuota_RejectedOrNormalized()
    {
        // Arrange
        // SaveRoomQuotaSettings is served by Web.Api, which carries its own auth header - authenticating
        // _filesClient alone is not enough.
        await _webApiClient.Authenticate(Owner);
        await _settingsQuotaApi.SaveRoomQuotaSettingsAsync(
            new QuotaSettingsRequestsDto(true, new QuotaSettingsRequestsDtoDefaultQuota(100 * 1024 * 1024)),
            TestContext.Current.CancellationToken);

        // Act
        var created = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto("Autotest NegativeQuota", roomType: RoomType.CustomRoom, quota: -100),
            TestContext.Current.CancellationToken)).Response;

        var info = (await _roomsApi.GetRoomInfoAsync(created.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        info.QuotaLimit.Should().BeGreaterThanOrEqualTo(0,
            "a negative quota must not be stored verbatim (got {0})", info.QuotaLimit);
    }

    [Fact]
    public async Task CreateRoom_InvalidLifetimePeriod_BadRequest()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.CreateRoomAsync(
                new CreateRoomRequestDto(
                    "Autotest",
                    roomType: RoomType.VirtualDataRoom,
                    lifetime: new RoomDataLifetimeDto(period: (RoomDataLifetimePeriod)999, value: 10, enabled: true)),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task CreateRoom_InvalidColor_NotHex_BadRequest()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.CreateRoomAsync(
                new CreateRoomRequestDto("Autotest", roomType: RoomType.CustomRoom, color: "not-a-color"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task CreateRoom_NonExistentCoverId_BadRequest()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.CreateRoomAsync(
                new CreateRoomRequestDto("Autotest", roomType: RoomType.CustomRoom, cover: "this-cover-does-not-exist"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task CreateRoom_InvalidTagsType_StringInsteadOfArray_BadRequest()
    {
        // Act
        using var response = await CreateRoomRawAsync(
            """{"title":"Autotest","roomType":5,"tags":"not-an-array"}""");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateRoom_NullTags_TreatedAsNoOp()
    {
        // Act
        var room = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto("Autotest NullTags", roomType: RoomType.CustomRoom, tags: null),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        (room.Tags ?? []).Should().BeEmpty();
    }

    [Fact]
    public async Task CreateRoom_InvalidSharePayload_BadRequest()
    {
        // Act
        using var response = await CreateRoomRawAsync(
            """{"title":"Autotest","roomType":5,"share":"broken"}""");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateRoom_InvalidChatSettings_BadRequest()
    {
        // Act
        using var response = await CreateRoomRawAsync(
            """{"title":"Autotest","roomType":5,"chatSettings":"broken"}""");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Sends a raw POST /api/2.0/files/rooms with an arbitrary JSON body, bypassing the typed SDK for
    /// bodies the generated DTO cannot express (a missing/null required property, or a value of the
    /// wrong JSON type).
    /// </summary>
    private async Task<HttpResponseMessage> CreateRoomRawAsync(string json)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/2.0/files/rooms")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        return await _filesClient.SendAsync(request, TestContext.Current.CancellationToken);
    }
}

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
/// Invalid-id and request-body validation for both room link endpoints (GET .../link, GET
/// .../links and PUT .../links). DocSpace tends to be permissive with link bodies (defaults /
/// normalizes rather than 400); the two BUG-marked cases below are where it currently is not
/// consistent about it.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomLinkValidationTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    /// <summary>
    /// The typed SDK's <c>id</c> parameter is an <c>int</c>, so a non-numeric route segment (the TS
    /// suite's <c>"abc"</c>) cannot be produced through it; this goes over raw HTTP instead.
    /// </summary>
    private async Task<HttpResponseMessage> GetPrimaryLinkRaw(string id)
    {
        return await _filesClient.GetAsync($"api/2.0/files/rooms/{id}/link", TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// <see cref="RoomLinkRequest.ExpirationDate"/> is a typed <see cref="ApiDateTime"/>, which cannot
    /// carry a malformed date string; this sends the raw JSON body the TS suite used.
    /// </summary>
    private async Task<HttpResponseMessage> SetLinkRaw(int roomId, string json)
    {
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        return await _filesClient.PutAsync($"api/2.0/files/rooms/{roomId}/links", content, TestContext.Current.CancellationToken);
    }

    #region GET /files/rooms/{id}/links

    [Fact]
    public async Task GetRoomLinks_NonExistingRoomId_Returns404()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetRoomLinksAsync(99999999, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task GetRoomLinks_DeletedRoom_Returns404()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Links Deleted Room");

        await _roomsApi.DeleteRoomAsync(room.Id, new DeleteRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetRoomLinksAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    #endregion

    #region GET /files/rooms/{id}/link

    [Fact]
    public async Task GetPrimaryLink_NonExistingRoomId_Returns404()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetRoomsPrimaryExternalLinkAsync(99999999, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task GetPrimaryLink_IdZero_Returns404()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetRoomsPrimaryExternalLinkAsync(0, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task GetPrimaryLink_NegativeId_Returns404()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetRoomsPrimaryExternalLinkAsync(-1, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task GetPrimaryLink_NonNumericId_Returns404()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        using var response = await GetPrimaryLinkRaw("abc");

        // Assert
        ((int)response.StatusCode).Should().Be(404);
    }

    #endregion

    #region PUT /files/rooms/{id}/links — invalid room / room state

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(999999999)]
    public async Task SetRoomLink_InvalidRoomId_Returns404(int badId)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.SetRoomLinkAsync(
                badId,
                new RoomLinkRequest(access: FileShare.Read, linkType: LinkType.External, title: "Bad Room Id", denyDownload: false),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task SetRoomLink_DeletedRoom_Returns404()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest setLink Deleted Room");

        await _roomsApi.DeleteRoomAsync(room.Id, new DeleteRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.SetRoomLinkAsync(
                room.Id,
                new RoomLinkRequest(access: FileShare.Read, linkType: LinkType.External, title: "On Deleted Room", denyDownload: false),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task SetRoomLink_ArchivedRoom_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreatePublicRoom("Autotest setLink Archived Room");

        await _roomsApi.ArchiveRoomAsync(room.Id, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.SetRoomLinkAsync(
                room.Id,
                new RoomLinkRequest(access: FileShare.Read, linkType: LinkType.External, title: "On Archived Room", denyDownload: false),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    #endregion

    #region PUT /files/rooms/{id}/links — request-body validation

    /// <remarks>
    /// BUG 82370: an out-of-range <c>linkType</c> should be rejected with 400 Bad Request; the API
    /// currently returns 403.
    /// </remarks>
    [Fact]
    [Trait("Bug", "82370")]
    public async Task SetRoomLink_InvalidLinkType_ShouldBeRejectedWith400()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest setLink Bad LinkType");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.SetRoomLinkAsync(
                room.Id,
                new RoomLinkRequest(access: FileShare.Read, linkType: (LinkType)5, title: "Bad LinkType", denyDownload: false),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    /// <remarks>
    /// BUG 82371: an out-of-range <c>access</c> should be rejected with 400 Bad Request; the API
    /// currently returns 403.
    /// </remarks>
    [Fact]
    [Trait("Bug", "82371")]
    public async Task SetRoomLink_InvalidAccess_ShouldBeRejectedWith400()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest setLink Bad Access");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.SetRoomLinkAsync(
                room.Id,
                new RoomLinkRequest(access: (FileShare)99, linkType: LinkType.Invitation, title: "Bad Access", denyDownload: false),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task SetRoomLink_TitleOverTheLengthLimit_RejectedWith400()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest setLink Long Title");
        var longTitle = new string('L', 300);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.SetRoomLinkAsync(
                room.Id,
                new RoomLinkRequest(access: FileShare.Read, linkType: LinkType.Invitation, title: longTitle, denyDownload: false),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task SetRoomLink_MalformedExpirationDate_IsSilentlyIgnored()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest setLink Bad Date");

        var json = JsonSerializer.Serialize(new
        {
            access = (int)FileShare.Read,
            linkType = (int)LinkType.External,
            title = "Bad Date",
            denyDownload = false,
            expirationDate = "not-a-date"
        });

        // Act
        using var response = await SetLinkRaw(room.Id, json);

        // Assert — a malformed date is dropped (like a past date), the link is still created.
        ((int)response.StatusCode).Should().Be(200);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("response").GetProperty("sharedLink").TryGetProperty("expirationDate", out _).Should().BeFalse();
    }

    [Fact]
    public async Task SetRoomLink_MaxUseCountZero_RejectedWith400()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest setLink MaxUse Zero");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.SetRoomLinkAsync(
                room.Id,
                new RoomLinkRequest(access: FileShare.Read, linkType: LinkType.Invitation, title: "MaxUse Zero", denyDownload: false, maxUseCount: 0),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task SetRoomLink_NegativeMaxUseCount_RejectedWith400()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest setLink MaxUse Negative");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.SetRoomLinkAsync(
                room.Id,
                new RoomLinkRequest(access: FileShare.Read, linkType: LinkType.Invitation, title: "MaxUse Negative", denyDownload: false, maxUseCount: -1),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    #endregion
}

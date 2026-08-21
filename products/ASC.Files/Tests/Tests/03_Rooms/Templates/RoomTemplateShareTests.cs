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
/// Functional behavior and validation of GET /files/roomtemplate/{id}/public and
/// PUT /files/roomtemplate/public — reading and writing whether a room template is shared. The
/// idempotency of repeated identical writes lives in <see cref="RoomTemplatePublicSettingsTests"/>,
/// permission coverage in <see cref="RoomTemplatePublicReadPermissionsTests"/> and
/// <see cref="RoomTemplatePublicWritePermissionsTests"/>, and visibility of a public template in the
/// catalogue in <see cref="RoomTemplateContentTests"/>.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomTemplateShareTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    #region GET /files/roomtemplate/{id}/public

    [Fact]
    public async Task GetTemplatePublicSettings_ReflectsExplicitAndToggledState()
    {
        // Arrange: created explicitly private, matching the API default.
        await _filesClient.Authenticate(Owner);
        var templateId = await CreateTemplate("Autotest PublicExplicitFalse Template", isPublic: false);

        // Assert: default is false and three consecutive reads agree, without mutating anything.
        var first = (await _roomsApi.GetPublicSettingsAsync(templateId, TestContext.Current.CancellationToken)).Response;
        var second = (await _roomsApi.GetPublicSettingsAsync(templateId, TestContext.Current.CancellationToken)).Response;
        var third = (await _roomsApi.GetPublicSettingsAsync(templateId, TestContext.Current.CancellationToken)).Response;
        first.Should().BeFalse();
        second.Should().BeFalse();
        third.Should().BeFalse();

        // Act + Assert: toggling true -> false is reflected immediately.
        await _roomsApi.SetPublicSettingsAsync(new SetPublicDto(templateId, true), TestContext.Current.CancellationToken);
        (await _roomsApi.GetPublicSettingsAsync(templateId, TestContext.Current.CancellationToken)).Response.Should().BeTrue();

        // Act + Assert: reflects the last value across a sequence of toggles.
        foreach (var expected in new[] { false, true, false })
        {
            await _roomsApi.SetPublicSettingsAsync(new SetPublicDto(templateId, expected), TestContext.Current.CancellationToken);
            (await _roomsApi.GetPublicSettingsAsync(templateId, TestContext.Current.CancellationToken)).Response.Should().Be(expected);
        }
    }

    [Fact]
    public async Task GetTemplatePublicSettings_StillWorksAfterSourceRoomDeleted()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var sourceRoom = await CreateCustomRoom("Autotest GetAfterSrcDeleted Source");
        await _roomsApi.CreateRoomTemplateAsync(
            new RoomTemplateDto(sourceRoom.Id, "Autotest GetAfterSrcDeleted Template", @public: true),
            TestContext.Current.CancellationToken);
        var templateId = await WaitForRoomTemplate();

        await _roomsApi.DeleteRoomAsync(sourceRoom.Id, new DeleteRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Act
        var actual = (await _roomsApi.GetPublicSettingsAsync(templateId, TestContext.Current.CancellationToken)).Response;

        // Assert
        actual.Should().BeTrue();
    }

    [Fact]
    public async Task GetTemplatePublicSettings_TogglingDoesNotBreakCreatingRoomFromTemplate()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var templateId = await CreateTemplate("Autotest ToggleThenUse Template", isPublic: false);
        await _roomsApi.SetPublicSettingsAsync(new SetPublicDto(templateId, true), TestContext.Current.CancellationToken);
        await _roomsApi.SetPublicSettingsAsync(new SetPublicDto(templateId, false), TestContext.Current.CancellationToken);

        (await _roomsApi.GetPublicSettingsAsync(templateId, TestContext.Current.CancellationToken)).Response.Should().BeFalse();

        // Act
        await _roomsApi.CreateRoomFromTemplateAsync(
            new CreateRoomFromTemplateDto(templateId, "Room From Toggled Template"),
            TestContext.Current.CancellationToken);
        var createdId = await WaitForRoomFromTemplate();

        // Assert
        createdId.Should().BePositive();
    }

    [Fact]
    public async Task GetTemplatePublicSettings_ReturnsNotFoundAfterTemplateDeleted()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var templateId = await CreateTemplate("Autotest GetAfterTmplDeleted Template", isPublic: false);

        await _roomsApi.DeleteRoomAsync(templateId, new DeleteRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetPublicSettingsAsync(templateId, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    /// <remarks>
    /// Bug 81726: the endpoint does not verify that the id it is given actually belongs to a room
    /// template, and silently answers for a regular room instead of returning 404.
    /// </remarks>
    [Fact]
    [Trait("Bug", "81726")]
    public async Task GetTemplatePublicSettings_RegularRoomId_ShouldReturnNotFound()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest RoomIdNotTemplate");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetPublicSettingsAsync(room.Id, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Theory]
    [InlineData(999999999)]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetTemplatePublicSettings_NonExistentOrOutOfRangeId_ReturnsNotFound(int id)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetPublicSettingsAsync(id, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    // The id is a route parameter typed as a plain int: a non-integer value can only be sent raw,
    // the typed client's signature cannot produce this request.
    [Theory]
    [InlineData("abc")]
    [InlineData("1.5")]
    [InlineData("9007199254740991")] // Number.MAX_SAFE_INTEGER: overflows Int32
    public async Task GetTemplatePublicSettings_NonIntegerId_ReturnsBadRequest(string id)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        using var response = await _filesClient.GetAsync($"api/2.0/files/roomtemplate/{id}/public", TestContext.Current.CancellationToken);

        // Assert
        ((int)response.StatusCode).Should().Be(400);
    }

    #endregion

    #region PUT /files/roomtemplate/public

    [Fact]
    public async Task SetTemplatePublicSettings_DoesNotAffectOtherTemplates()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var templateA = await CreateTemplate("Autotest SetPublic Isolation A", isPublic: false);
        var templateB = await CreateTemplate("Autotest SetPublic Isolation B", isPublic: false);

        // Act
        await _roomsApi.SetPublicSettingsAsync(new SetPublicDto(templateA, true), TestContext.Current.CancellationToken);

        // Assert
        (await _roomsApi.GetPublicSettingsAsync(templateA, TestContext.Current.CancellationToken)).Response.Should().BeTrue();
        (await _roomsApi.GetPublicSettingsAsync(templateB, TestContext.Current.CancellationToken)).Response.Should().BeFalse();
    }

    [Fact]
    public async Task SetTemplatePublicSettings_DefaultPublicFalse_LeavesStateUnchanged()
    {
        // SetPublicDto.Public is a plain bool (default false), so "omitting" it from the TypeScript
        // body is, on the typed client, the same request as sending public:false explicitly.
        await _filesClient.Authenticate(Owner);
        var templateId = await CreateTemplate("Autotest SetPublic MissingPublic", isPublic: false);

        // Act
        await _roomsApi.SetPublicSettingsAsync(new SetPublicDto(templateId), TestContext.Current.CancellationToken);

        // Assert
        (await _roomsApi.GetPublicSettingsAsync(templateId, TestContext.Current.CancellationToken)).Response.Should().BeFalse();
    }

    [Fact]
    public async Task SetTemplatePublicSettings_NonExistingId_ReturnsNotFound()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.SetPublicSettingsAsync(new SetPublicDto(999999999, true), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    /// <remarks>
    /// Bug 81949: id 0 and a negative id should be rejected as malformed input (400), but the
    /// endpoint currently treats them as "not found" (404) instead.
    /// </remarks>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [Trait("Bug", "81949")]
    public async Task SetTemplatePublicSettings_ZeroOrNegativeId_ShouldReturnBadRequest(int id)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.SetPublicSettingsAsync(new SetPublicDto(id, true), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    // SetPublicDto.Id is a plain int body field: a non-integer value can only be sent raw, the typed
    // client's signature cannot produce this request.
    [Theory]
    [InlineData("\"abc\"")]
    [InlineData("1.5")]
    [InlineData("9007199254740991")] // Number.MAX_SAFE_INTEGER: overflows Int32
    public async Task SetTemplatePublicSettings_NonIntegerId_ReturnsBadRequest(string idLiteral)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var response = await SendRawSetPublicSettings($$"""{"id":{{idLiteral}},"public":true}""");

        // Assert
        ((int)response.StatusCode).Should().Be(400);
    }

    [Fact]
    public async Task SetTemplatePublicSettings_NullBody_ReturnsBadRequest()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var response = await SendRawSetPublicSettings("null");

        // Assert
        ((int)response.StatusCode).Should().Be(400);
    }

    /// <remarks>
    /// Bug 81939: the same defect class as bug 81726 on the GET side — the endpoint accepts a
    /// regular room id (200) instead of confirming the id belongs to a template (404).
    /// </remarks>
    [Fact]
    [Trait("Bug", "81939")]
    public async Task SetTemplatePublicSettings_RegularRoomId_ShouldReturnNotFound()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest SetPublic RoomNotTemplate");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.SetPublicSettingsAsync(new SetPublicDto(room.Id, true), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task SetTemplatePublicSettings_StillWorksAfterSourceRoomDeleted()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var sourceRoom = await CreateCustomRoom("Autotest SetPublic AfterSrcDeleted Source");
        await _roomsApi.CreateRoomTemplateAsync(
            new RoomTemplateDto(sourceRoom.Id, "Autotest SetPublic AfterSrcDeleted Template"),
            TestContext.Current.CancellationToken);
        var templateId = await WaitForRoomTemplate();

        await _roomsApi.DeleteRoomAsync(sourceRoom.Id, new DeleteRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Act
        await _roomsApi.SetPublicSettingsAsync(new SetPublicDto(templateId, true), TestContext.Current.CancellationToken);

        // Assert
        (await _roomsApi.GetPublicSettingsAsync(templateId, TestContext.Current.CancellationToken)).Response.Should().BeTrue();
    }

    [Fact]
    public async Task SetTemplatePublicSettings_StillWorksAfterSourceRoomArchived()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var sourceRoom = await CreateCustomRoom("Autotest SetPublic AfterSrcArchived Source");
        await _roomsApi.CreateRoomTemplateAsync(
            new RoomTemplateDto(sourceRoom.Id, "Autotest SetPublic AfterSrcArchived Template"),
            TestContext.Current.CancellationToken);
        var templateId = await WaitForRoomTemplate();

        await ArchiveRoom(sourceRoom.Id);

        // Act
        await _roomsApi.SetPublicSettingsAsync(new SetPublicDto(templateId, true), TestContext.Current.CancellationToken);

        // Assert
        (await _roomsApi.GetPublicSettingsAsync(templateId, TestContext.Current.CancellationToken)).Response.Should().BeTrue();
    }

    /// <summary>
    /// Sends a raw PUT /api/2.0/files/roomtemplate/public, for a body the generated DTO cannot
    /// express (a non-integer id, or a null body).
    /// </summary>
    private async Task<HttpResponseMessage> SendRawSetPublicSettings(string json)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, "api/2.0/files/roomtemplate/public")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        return await _filesClient.SendAsync(request, TestContext.Current.CancellationToken);
    }

    #endregion
}

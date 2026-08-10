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

namespace ASC.Files.Tests.Tests._03_Rooms.Logos;

/// <summary>
/// <c>POST /files/rooms/{id}/logo</c> — request validation: the <c>tmpFile</c>, the room id and the
/// crop rectangle. Two TS cases are not ported: a numeric/object/array <c>tmpFile</c> and a string
/// crop coordinate only exercise ASP.NET's own JSON-to-CLR type coercion, not DocSpace logic (see
/// <c>.claude/rules/tests.md</c>); a non-numeric room id is the same kind of framework route-binding
/// behaviour.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomLogoCreateValidationTests(
    AspireAppFixture fixture)
    : RoomLogoTestBase(fixture)
{
    #region tmpFile validation

    [Fact]
    [Trait("Bug", "81677")]
    public async Task CreateLogo_MissingTmpFile_BadRequest()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Logo Missing TmpFile Room");

        // Act
        using var response = await CreateRoomLogoRaw(room.Id, "{}");

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)400);
    }

    [Fact]
    [Trait("Bug", "81677")]
    public async Task CreateLogo_EmptyStringTmpFile_BadRequest()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Logo Empty TmpFile Room");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await CreateLogo(room.Id, string.Empty));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    [Trait("Bug", "81677")]
    public async Task CreateLogo_NullTmpFile_BadRequest()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Logo Null TmpFile Room");

        // Act
        using var response = await CreateRoomLogoRaw(
            room.Id,
            """{"tmpFile":null,"x":0,"y":0,"width":1,"height":1}""");

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)400);
    }

    #endregion

    #region Room id validation

    [Fact]
    public async Task CreateLogo_DeletedRoom_NotFound()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Logo Deleted Room");
        var tmpFile = await UploadLogo(CreateTestImageBytes());

        await _roomsApi.DeleteRoomAsync(room.Id, new DeleteRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await CreateLogo(room.Id, tmpFile));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    #endregion

    #region Crop parameters validation

    [Fact]
    [Trait("Bug", "81678")]
    public async Task CreateLogo_NegativeX_ShouldBeRejected()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Logo Negative X Room");
        var tmpFile = await UploadLogo(CreateTestImageBytes());

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await CreateLogo(room.Id, tmpFile, x: -1));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    [Trait("Bug", "81678")]
    public async Task CreateLogo_NegativeY_ShouldBeRejected()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Logo Negative Y Room");
        var tmpFile = await UploadLogo(CreateTestImageBytes());

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await CreateLogo(room.Id, tmpFile, y: -1));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    [Trait("Bug", "81678")]
    public async Task CreateLogo_ZeroWidth_ShouldBeRejected()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Logo Zero Width Room");
        var tmpFile = await UploadLogo(CreateTestImageBytes());

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await CreateLogo(room.Id, tmpFile, width: 0, height: 1));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    [Trait("Bug", "81678")]
    public async Task CreateLogo_ZeroHeight_ShouldBeRejected()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Logo Zero Height Room");
        var tmpFile = await UploadLogo(CreateTestImageBytes());

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await CreateLogo(room.Id, tmpFile, width: 1, height: 0));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task CreateLogo_NegativeWidth_BadRequest()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Logo Negative Width Room");
        var tmpFile = await UploadLogo(CreateTestImageBytes());

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await CreateLogo(room.Id, tmpFile, width: -10, height: 1));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task CreateLogo_NegativeHeight_BadRequest()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Logo Negative Height Room");
        var tmpFile = await UploadLogo(CreateTestImageBytes());

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await CreateLogo(room.Id, tmpFile, width: 1, height: -10));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    [Trait("Bug", "81678")]
    public async Task CreateLogo_CropOutsideImageBounds_ShouldBeRejected()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Logo Crop Out Of Bounds Room");
        var tmpFile = await UploadLogo(CreateTestImageBytes());

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await CreateLogo(room.Id, tmpFile, x: 100, y: 100, width: 1000, height: 1000));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task CreateLogo_OnlyXAndYWithoutWidthHeight_BadRequest()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Logo Partial Crop XY Room");
        var tmpFile = await UploadLogo(CreateTestImageBytes());

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.CreateRoomLogoAsync(
                room.Id,
                new LogoRequest(tmpFile, x: 0, y: 0),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task CreateLogo_OnlyWidthAndHeightWithoutXY_Applied()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Logo Partial Crop WH Room");
        var tmpFile = await UploadLogo(CreateTestImageBytes());

        // Act
        var updated = (await _roomsApi.CreateRoomLogoAsync(
            room.Id,
            new LogoRequest(tmpFile, width: 1, height: 1),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.Logo.Original.Should().NotBeNullOrEmpty();
    }

    #endregion
}

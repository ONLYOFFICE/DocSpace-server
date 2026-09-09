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
/// <c>POST /api/2.0/files/logos</c> — uploading a temporary logo image: acceptance, upload
/// behaviour and the range of image content the endpoint stores. Access control for the same
/// endpoint lives in <see cref="RoomLogoUploadPermissionsTests"/>; the multipart/HTTP contract in
/// <see cref="RoomLogoUploadContractTests"/> and <see cref="RoomLogoUploadBugTests"/>.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomLogoUploadTests(
    AspireAppFixture fixture)
    : RoomLogoTestBase(fixture)
{
    #region Basic acceptance

    [Fact]
    public async Task Upload_ValidPng_Succeeds()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var result = (await _roomsApi.UploadRoomLogoAsync(
            new FileParameter("logo.png", "image/png", new MemoryStream(CreateTestImageBytes())),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Upload_ValidPng_ReturnsTmpFileAsString()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var tmpFile = await UploadLogo(CreateTestImageBytes());

        // Assert
        tmpFile.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region Upload behaviour

    [Fact]
    public async Task Upload_Sequential_ReturnsDifferentTmpFiles()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var first = await UploadLogo(CreateTestImageBytes());
        var second = await UploadLogo(CreateTestImageBytes());

        // Assert
        first.Should().NotBeNullOrEmpty();
        second.Should().NotBeNullOrEmpty();
        second.Should().NotBe(first);
    }

    [Fact]
    public async Task Upload_AfterLogoWasSet_ReturnsNewTmpFile()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Logo Reupload After Set");

        var firstTmpFile = await UploadLogo(CreateTestImageBytes());
        await CreateLogo(room.Id, firstTmpFile);

        // Act
        var secondTmpFile = await UploadLogo(CreateTestImageBytes());

        // Assert
        secondTmpFile.Should().NotBeNullOrEmpty();
        secondTmpFile.Should().NotBe(firstTmpFile);
    }

    [Fact]
    public async Task Upload_Alone_DoesNotChangeRoomLogo()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Logo Upload Is Isolated");

        var before = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        before.Logo.Original.Should().BeNullOrEmpty();

        // Act
        await UploadLogo(CreateTestImageBytes());

        // Assert
        var after = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        after.Logo.Original.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task Upload_TmpFile_IsNonEmptyNonWhitespaceString()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var tmpFile = await UploadLogo(CreateTestImageBytes());

        // Assert
        tmpFile.Trim().Should().NotBeEmpty();
    }

    [Fact]
    public async Task Upload_TmpFile_DoesNotExposeAbsolutePath()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var tmpFile = await UploadLogo(CreateTestImageBytes());

        // Assert
        tmpFile.Should().NotMatchRegex("^[a-zA-Z]:\\\\");
        tmpFile.Should().NotStartWith("/home/");
        tmpFile.Should().NotStartWith("/var/");
        tmpFile.Should().NotStartWith("/tmp/");
    }

    #endregion

    #region Image content

    [Fact]
    public async Task Upload_Minimal1x1Png_Accepted()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var result = (await _roomsApi.UploadRoomLogoAsync(
            new FileParameter("logo.png", "image/png", new MemoryStream(CreateTestImageBytes())),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Upload_TransparentPng_Accepted()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var result = (await _roomsApi.UploadRoomLogoAsync(
            new FileParameter("logo.png", "image/png", new MemoryStream(CreateTransparentPng())),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Upload_OpaquePng_Accepted()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var result = (await _roomsApi.UploadRoomLogoAsync(
            new FileParameter("logo.png", "image/png", new MemoryStream(CreateOpaquePng())),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Upload_GrayscalePng_Accepted()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var result = (await _roomsApi.UploadRoomLogoAsync(
            new FileParameter("logo.png", "image/png", new MemoryStream(CreateGrayscalePng())),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Upload_Medium512Png_Accepted()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var result = (await _roomsApi.UploadRoomLogoAsync(
            new FileParameter("logo.png", "image/png", new MemoryStream(CreatePng(512, 512))),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Upload_Large4000Png_Accepted()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var result = (await _roomsApi.UploadRoomLogoAsync(
            new FileParameter("logo.png", "image/png", new MemoryStream(CreatePng(4000, 4000))),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        result.Success.Should().BeTrue();
    }

    /// <summary>
    /// Upload stores bytes without decoding, so a decompression bomb is not detonated here; it
    /// just returns success. Decode-time protection, if any, belongs to CreateRoomLogo.
    /// </summary>
    [Fact]
    public async Task Upload_DecompressionBombPng_StoredWithout500()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var result = (await _roomsApi.UploadRoomLogoAsync(
            new FileParameter("logo.png", "image/png", new MemoryStream(CreateDecompressionBombPng())),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Upload_PngWithTextMetadata_Accepted()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var result = (await _roomsApi.UploadRoomLogoAsync(
            new FileParameter("logo.png", "image/png", new MemoryStream(CreatePngWithText("Comment", "hello metadata"))),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Upload_PngWithSuspiciousMetadata_HandledSafely()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string payload = "<script>alert(1)</script> '; DROP TABLE rooms;--";

        // Act
        var result = (await _roomsApi.UploadRoomLogoAsync(
            new FileParameter("logo.png", "image/png", new MemoryStream(CreatePngWithText("Comment", payload))),
            TestContext.Current.CancellationToken)).Response;

        // Assert: metadata must not leak into the tmpFile path.
        result.Success.Should().BeTrue();
        var tmpFile = result.Data?.ToString() ?? string.Empty;
        tmpFile.Should().NotContain("<script>");
        tmpFile.Should().NotContain("DROP TABLE");
    }

    [Fact]
    public async Task Upload_PngWithLargeMetadataBlock_Accepted()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var big = new string('x', 2 * 1024 * 1024);

        // Act
        var result = (await _roomsApi.UploadRoomLogoAsync(
            new FileParameter("logo.png", "image/png", new MemoryStream(CreatePngWithText("Comment", big))),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Upload_PolyglotPngWithHtml_HandledSafely()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var result = (await _roomsApi.UploadRoomLogoAsync(
            new FileParameter("logo.png", "image/png", new MemoryStream(CreatePolyglotPng())),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        result.Success.Should().BeTrue();
        (result.Data?.ToString() ?? string.Empty).Should().NotContain("<script>");
    }

    #endregion
}

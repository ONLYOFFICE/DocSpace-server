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
/// <c>POST /api/2.0/files/logos</c> performs no content validation at all: it stores whatever is
/// posted and reports success regardless of the bytes, and it answers a non-multipart or bodyless
/// request the same way instead of rejecting it. These tests document the contract the endpoint
/// should have (reject with 400) and stay red until it is fixed.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomLogoUploadBugTests(
    AspireAppFixture fixture)
    : RoomLogoTestBase(fixture)
{
    #region Non-PNG content declared as image/png

    [Fact]
    [Trait("Bug", "82518")]
    public async Task Upload_PlainTextDeclaredAsPng_ShouldBeRejected()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        using var response = await UploadRoomLogoRaw(
            HttpMethod.Post,
            [(Encoding.UTF8.GetBytes("this is not an image"), "logo.png", "image/png")]);

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)400);
    }

    [Fact]
    [Trait("Bug", "82518")]
    public async Task Upload_JpegDeclaredAsPng_ShouldBeRejected()
    {
        // Rejected for its content, not for the declared MIME type: JPEG is an accepted logo format,
        // but this fixture is a malformed one ("Invalid component ID 2 in SOS"), so the decoder
        // refuses it. A valid JPEG mislabelled as PNG is still accepted, the same way
        // RoomLogoUploadContractTests.Upload_OctetStreamMimeWithValidPng_Accepted is.
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        using var response = await UploadRoomLogoRaw(
            HttpMethod.Post,
            [(CreateJpegBytes(), "logo.png", "image/png")]);

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)400);
    }

    [Fact]
    [Trait("Bug", "82518")]
    public async Task Upload_GifDeclaredAsPng_ShouldBeRejected()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        using var response = await UploadRoomLogoRaw(
            HttpMethod.Post,
            [(CreateGifBytes(), "logo.png", "image/png")]);

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)400);
    }

    [Fact]
    [Trait("Bug", "82518")]
    public async Task Upload_WebpDeclaredAsPng_ShouldBeRejected()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        using var response = await UploadRoomLogoRaw(
            HttpMethod.Post,
            [(CreateWebpBytes(), "logo.png", "image/png")]);

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)400);
    }

    [Fact]
    [Trait("Bug", "82518")]
    public async Task Upload_SvgDeclaredAsPng_ShouldBeRejected()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        using var response = await UploadRoomLogoRaw(
            HttpMethod.Post,
            [(CreateSvgBytes(), "logo.png", "image/png")]);

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)400);
    }

    [Fact]
    [Trait("Bug", "82518")]
    public async Task Upload_CorruptPng_ShouldBeRejected()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        using var response = await UploadRoomLogoRaw(
            HttpMethod.Post,
            [(CreateCorruptPng(), "logo.png", "image/png")]);

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)400);
    }

    [Fact]
    [Trait("Bug", "82518")]
    public async Task Upload_RandomBinaryDeclaredAsPng_ShouldBeRejected()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        using var response = await UploadRoomLogoRaw(
            HttpMethod.Post,
            [(CreateRandomBinaryBytes(), "logo.png", "image/png")]);

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)400);
    }

    #endregion

    #region Multipart contract

    [Fact]
    [Trait("Bug", "82518")]
    public async Task Upload_NoBody_ShouldBeRejected()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        using var response = await UploadRoomLogoRaw(HttpMethod.Post, omitBody: true);

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)400);
    }

    [Fact]
    [Trait("Bug", "82518")]
    public async Task Upload_MultipartWithoutFileField_ShouldBeRejected()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        using var response = await UploadRoomLogoRaw(
            HttpMethod.Post,
            fields: new Dictionary<string, string> { ["image"] = "not-a-file" });

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)400);
    }

    [Fact]
    [Trait("Bug", "82518")]
    public async Task Upload_FileFieldAsPlainString_ShouldBeRejected()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        using var response = await UploadRoomLogoRaw(
            HttpMethod.Post,
            fields: new Dictionary<string, string> { ["file"] = "just-a-string" });

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)400);
    }

    [Fact]
    [Trait("Bug", "82518")]
    public async Task Upload_EmptyFile_ShouldBeRejected()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        using var response = await UploadRoomLogoRaw(
            HttpMethod.Post,
            [([], "logo.png", "image/png")]);

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)400);
    }

    [Fact]
    [Trait("Bug", "82518")]
    public async Task Upload_JsonContentType_ShouldBeRejected()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        using var response = await UploadRoomLogoRaw(
            JsonSerializer.Serialize(new { file = "x" }),
            "application/json");

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)400);
    }

    #endregion

    #region HTTP method contract

    /// <summary>Wrong-method requests should return 405; GET currently returns 404.</summary>
    [Fact]
    [Trait("Bug", "82519")]
    public async Task Upload_Get_ShouldBeMethodNotAllowed()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        using var response = await UploadRoomLogoRaw(HttpMethod.Get, omitBody: true);

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)405);
    }

    #endregion
}

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
/// <c>POST /api/2.0/files/logos</c> — the parts of the multipart/HTTP contract that already behave
/// correctly. The parts that do not (BUG 82518, BUG 82519) live in
/// <see cref="RoomLogoUploadBugTests"/>.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomLogoUploadContractTests(
    AspireAppFixture fixture)
    : RoomLogoTestBase(fixture)
{
    [Fact]
    public async Task Upload_MultipleFileFields_Accepted()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        using var response = await UploadRoomLogoRaw(
            HttpMethod.Post,
            [
                (CreateTestImageBytes(), "a.png", "image/png"),
                (CreateTestImageBytes(), "b.png", "image/png")
            ]);

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)200);
    }

    [Fact]
    public async Task Upload_ExtraUnknownFieldsAlongsideValidFile_Accepted()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        using var response = await UploadRoomLogoRaw(
            HttpMethod.Post,
            [(CreateTestImageBytes(), "logo.png", "image/png")],
            new Dictionary<string, string> { ["unexpected"] = "value", ["another"] = "42" });

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)200);
    }

    [Fact]
    public async Task Upload_OctetStreamMimeWithValidPng_Accepted()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        using var response = await UploadRoomLogoRaw(
            HttpMethod.Post,
            [(CreateTestImageBytes(), "logo.png", "application/octet-stream")]);

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)200);
    }

    [Fact]
    public async Task Upload_Put_MethodNotAllowed()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        using var response = await UploadRoomLogoRaw(
            HttpMethod.Put,
            [(CreateTestImageBytes(), "logo.png", "image/png")]);

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)405);
    }

    [Fact]
    public async Task Upload_Delete_MethodNotAllowed()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        using var response = await UploadRoomLogoRaw(HttpMethod.Delete, omitBody: true);

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)405);
    }
}

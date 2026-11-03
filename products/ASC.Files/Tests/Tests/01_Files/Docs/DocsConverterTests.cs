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

namespace ASC.Files.Tests.Tests._01_Files.Docs;

/// <summary>
/// <c>POST /api/2.0/docs/converter</c>: access control and request validation. A conversion that is actually
/// performed needs the document server and is not covered here.
/// </summary>
[Trait("Category", "Validation")]
[Trait("Feature", "Docs")]
public class DocsConverterTests(
    AspireAppFixture fixture)
    : DocsTestBase(fixture)
{
    [Fact]
    public async Task ConvertFile_UserWithoutAccessToTheFile_Returns403()
    {
        // Arrange
        var (_, file) = await CreateRoomWithDocx("Docs Converter No Access");
        var user = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await DocsApi.ConvertFileAsync(new ConvertFileRequestDto(file.Id, outputtype: "pdf"), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task ConvertFile_Anonymous_Returns401()
    {
        // Arrange
        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await DocsApi.ConvertFileAsync(new ConvertFileRequestDto(1, outputtype: "pdf"), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task ConvertFile_NonExistentFile_Returns404()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await DocsApi.ConvertFileAsync(new ConvertFileRequestDto(999999999, outputtype: "pdf"), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Theory]
    [InlineData("")]
    [InlineData("notarealformat")]
    public async Task ConvertFile_MissingOrUnsupportedOutputType_Returns400(string outputType)
    {
        // Arrange
        var (_, file) = await CreateRoomWithDocx("Docs Converter Output Type");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await DocsApi.ConvertFileAsync(new ConvertFileRequestDto(file.Id, outputtype: outputType), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }
}

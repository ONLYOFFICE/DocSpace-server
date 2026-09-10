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

namespace ASC.Files.Tests.Tests._06_Operations.CheckConversion;

/// <summary>
/// <c>GET /api/2.0/files/file/{fileId}/checkconversion</c> (<c>checkConversionStatus</c>) and
/// <c>PUT /api/2.0/files/file/{fileId}/checkconversion</c> (<c>startFileConversion</c>) — functional
/// coverage. Both endpoints only enqueue/report the conversion; they never wait for a document
/// server to actually convert anything, so none of these need one running. Access control lives in
/// <see cref="CheckConversionStatusPermissionsTests"/> and
/// <see cref="StartFileConversionPermissionsTests"/>; the Editor-access bug is in
/// <see cref="CheckConversionStatusBugTests"/>.
/// </summary>
[Trait("Category", "Operations")]
[Trait("Feature", "Files")]
public class CheckConversionStatusTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task StartFileConversion_Docx_ReturnsEmptyResponseWithPutLink()
    {
        // Arrange
        var file = await CreateFileInMy("Autotest StartConversion docx.docx", Owner);

        // Act
        var result = await _filesOperationsApi.StartFileConversionAsync(
            file.Id,
            new CheckConversionRequestDtoInteger(startConvert: true, outputType: "pdf"),
            TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().BeEmpty();
        result.Links.Should().ContainSingle();
        result.Links[0].Href.Should().Contain("checkconversion");
        result.Links[0].Action.Should().Be("PUT");
    }

    [Fact]
    public async Task CheckConversionStatus_NoActiveConversion_ReturnsEmptyArrayWithGetLink()
    {
        // Arrange
        var file = await CreateFileInMy("Autotest CheckConversion No Active.docx", Owner);

        // Act
        var result = await _filesOperationsApi.CheckConversionStatusAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().BeEmpty();
        result.Links.Should().ContainSingle();
        result.Links[0].Href.Should().Contain("checkconversion");
        result.Links[0].Action.Should().Be("GET");
    }

    [Fact]
    public async Task StartFileConversion_XlsxToPdf_ReturnsEmptyResponseWithPutLink()
    {
        // Arrange
        var file = await CreateFileInMy("Autotest StartConversion xlsx.xlsx", Owner);

        // Act
        var result = await _filesOperationsApi.StartFileConversionAsync(
            file.Id,
            new CheckConversionRequestDtoInteger(startConvert: true, outputType: "pdf"),
            TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().BeEmpty();
        result.Links.Should().ContainSingle();
        result.Links[0].Href.Should().Contain("checkconversion");
        result.Links[0].Action.Should().Be("PUT");
    }

    [Fact]
    public async Task StartFileConversion_PptxToPdf_ReturnsEmptyResponseWithPutLink()
    {
        // Arrange
        var file = await CreateFileInMy("Autotest StartConversion pptx.pptx", Owner);

        // Act
        var result = await _filesOperationsApi.StartFileConversionAsync(
            file.Id,
            new CheckConversionRequestDtoInteger(startConvert: true, outputType: "pdf"),
            TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().BeEmpty();
        result.Links.Should().ContainSingle();
        result.Links[0].Href.Should().Contain("checkconversion");
        result.Links[0].Action.Should().Be("PUT");
    }

    [Fact]
    public async Task StartFileConversion_StartConvertFalse_ReturnsLinkToCheckConversion()
    {
        // Arrange
        var file = await CreateFileInMy("Autotest StartConversion startConvertFalse.docx", Owner);

        // Act
        var result = await _filesOperationsApi.StartFileConversionAsync(
            file.Id,
            new CheckConversionRequestDtoInteger(startConvert: false),
            TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().BeEmpty();
        result.Links.Should().ContainSingle();
        result.Links[0].Href.Should().Contain("checkconversion");
    }

    [Fact]
    public async Task StartFileConversion_NonExistentFileId_ReturnsEmptyResponse()
    {
        // Act
        var result = await _filesOperationsApi.StartFileConversionAsync(
            999999999,
            new CheckConversionRequestDtoInteger(startConvert: true, outputType: "pdf"),
            TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().BeEmpty();
    }
}

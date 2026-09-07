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

namespace ASC.Files.Tests.Tests._01_Files.Forms;

/// <summary>
/// <c>GET /files/file/{fileId}/isformpdf</c> — whether a file is an ONLYOFFICE PDF form.
/// </summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "Forms")]
public class IsFormPdfTests(
    AspireAppFixture fixture)
    : FormsTestBase(fixture)
{
    [Fact]
    public async Task IsFormPDF_RegularDocxFile_ReturnsFalse()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest IsFormPDF Docx Room");
        var file = await CreateFile("Autotest IsFormPDF Docx File", room.Id);

        // Act
        var isForm = (await _filesApi.IsFormPDFAsync(file.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        isForm.Should().BeFalse();
    }

    /// <summary>
    /// The TS source builds this file by converting a .docx through the document server
    /// (<c>saveFileAsPdf</c>). No document-server resource is provisioned here, so the same intent —
    /// a PDF with no ONLYOFFICE form signature — is reproduced with a hand-built PDF instead.
    /// </summary>
    [Fact]
    public async Task IsFormPDF_NonOnlyofficePdfFile_ReturnsFalse()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest IsFormPDF PDF Room");
        var fileId = await UploadRegularPdfAsync(room.Id, "Autotest IsFormPDF Converted PDF.pdf");

        // Act
        var isForm = (await _filesApi.IsFormPDFAsync(fileId, TestContext.Current.CancellationToken)).Response;

        // Assert
        isForm.Should().BeFalse();
    }

    [Fact]
    public async Task IsFormPDF_DocxfFormTemplate_ReturnsFalse()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest IsFormPDF Docxf Room");
        var fileId = await UploadDocxfTemplateAsync(room.Id, "Autotest IsFormPDF Form.docxf");

        // Act
        var isForm = (await _filesApi.IsFormPDFAsync(fileId, TestContext.Current.CancellationToken)).Response;

        // Assert - a .docxf is an OOXML form template, not an ONLYOFFICE PDF form
        isForm.Should().BeFalse();
    }

    [Fact]
    public async Task IsFormPDF_NonExistentFile_Returns404()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.IsFormPDFAsync(999999999, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    /// <summary>
    /// Merges the TS suite's "created via docx -&gt; docxf -&gt; pdf conversion chain" and "uploaded OO
    /// form binary" cases: the conversion chain needs a document server that this AppHost does not
    /// provision, so only the direct-upload path is reproducible, and it already exercises exactly
    /// what both TS cases assert — a real ONLYOFFICE PDF form is recognised as one.
    /// </summary>
    [Fact]
    public async Task IsFormPDF_UploadedOnlyofficeForm_ReturnsTrue()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateFillingFormsRoom("Autotest IsFormPDF Binary Form Room");
        var fileId = await UploadOoFormAsync(room.Id, "Autotest IsFormPDF OO Form.pdf");

        // Act
        var isForm = (await _filesApi.IsFormPDFAsync(fileId, TestContext.Current.CancellationToken)).Response;

        // Assert
        isForm.Should().BeTrue();
    }
}

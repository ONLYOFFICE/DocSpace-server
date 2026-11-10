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
    /// A PDF with no ONLYOFFICE form signature, built by hand; the one the document server writes is
    /// checked by <see cref="IsFormPDF_PdfSavedFromDocx_ReturnsFalse"/>.
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

    /// <summary>
    /// Every <c>.pdf</c> is offered as a fillable form (<c>isForm</c>), whatever its content says;
    /// <c>isformpdf</c> is what tells a form from a document. Any other format carries no flag.
    /// </summary>
    [Fact]
    public async Task FileInfo_IsForm_TrueForEveryPdfAndAbsentForOtherFormats()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest IsForm Flag Room");
        var pdfId = await UploadRegularPdfAsync(room.Id, "Autotest IsForm Flag.pdf");
        var docx = await CreateFile("Autotest IsForm Flag.docx", room.Id);

        // Act
        var pdf = (await _filesApi.GetFileInfoAsync(pdfId, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var doc = (await _filesApi.GetFileInfoAsync(docx.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        pdf.IsForm.Should().BeTrue("a PDF without the form signature is still offered for filling");
        (await _filesApi.IsFormPDFAsync(pdfId, TestContext.Current.CancellationToken)).Response.Should().BeFalse();
        doc.IsForm.Should().BeNull();
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
    /// The TS suite's "uploaded OO form binary" case.
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

    /// <summary>
    /// The TS suite's "created via docx -&gt; docxf -&gt; pdf conversion chain" case: the form the document
    /// server writes is recognised as one. The other suites upload a fixed form instead, so this is the
    /// test that notices when the signature the document server writes stops matching the one the portal
    /// looks for.
    /// </summary>
    [Fact]
    public async Task IsFormPDF_FormConvertedFromDocx_ReturnsTrue()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateFillingFormsRoom("Autotest IsFormPDF Converted Form Room");
        var fileId = await ConvertToOoFormAsync(room.Id, "Autotest IsFormPDF Converted Form.pdf");

        // Act
        var isForm = (await _filesApi.IsFormPDFAsync(fileId, TestContext.Current.CancellationToken)).Response;

        // Assert
        isForm.Should().BeTrue();
    }

    /// <summary>
    /// The TS suite's "Regular PDF converted from .docx" case: a plain PDF the document server writes is
    /// not taken for a form.
    /// </summary>
    [Fact]
    public async Task IsFormPDF_PdfSavedFromDocx_ReturnsFalse()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var source = await CreateFileInMy("Autotest IsFormPDF Source Docx.docx", Owner);
        var room = await CreateCustomRoom("Autotest IsFormPDF Saved PDF Room");

        var pdf = (await _filesApi.SaveFileAsPdfAsync(
            source.Id,
            new SaveAsPdfRequest(room.Id, "Autotest IsFormPDF Saved PDF"),
            TestContext.Current.CancellationToken)).Response;

        // Act
        var isForm = (await _filesApi.IsFormPDFAsync(pdf.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        pdf.Title.Should().EndWith(".pdf");
        isForm.Should().BeFalse();
    }
}

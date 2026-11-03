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
/// Shared setup for the form-metadata suites (<c>isformpdf</c>, <c>formroles</c>, <c>fillresult</c>).
/// </summary>
/// <remarks>
/// A genuine ONLYOFFICE PDF form is obtained by uploading the shared <c>new.pdf</c> asset (embedded
/// resource <c>ASC.Files.Tests.Data.new.pdf</c>), which carries the <c>ONLYOFFICEFORM</c> signature the
/// product's <c>FileChecker</c> looks for. The TypeScript suite converts .docx → .docxf → .pdf through
/// the document server instead; the upload is kept on purpose: these suites are about how the portal
/// treats a form, not how one is made, and an upload takes milliseconds and does not depend on the
/// document server. A converted upload ends up in the same <c>InsertFileAsync</c> and <c>isformpdf</c>
/// reads the content each time, so the two are the same file to the portal. The forms the document
/// server produces are checked once, in <see cref="IsFormPdfTests"/>, through
/// <see cref="ConvertToOoFormAsync"/> — which also catches a change of the signature that a fixed
/// asset would not. A deliberately non-form PDF is built in memory, since the product's own blank-PDF
/// template is itself an ONLYOFFICE form and would defeat a "not a form" test.
/// </remarks>
public abstract class FormsTestBase(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    /// <summary>
    /// Uploads the shared <c>new.pdf</c> asset — a real ONLYOFFICE PDF form — into the given folder
    /// through a chunked upload session, which is how the product's <c>FileChecker</c> gets to
    /// inspect real content and tag the file <c>FilterType.PdfForm</c> on finalize.
    /// </summary>
    protected async Task<int> UploadOoFormAsync(int folderId, string fileName = "Autotest OO Form.pdf")
    {
        using var content = new MemoryStream();

        await using (var stream = typeof(FormsTestBase).Assembly.GetManifestResourceStream("ASC.Files.Tests.Data.new.pdf")!)
        {
            await stream.CopyToAsync(content, TestContext.Current.CancellationToken);
        }

        return await UploadFileAsync(folderId, fileName, content.ToArray());
    }

    /// <summary>
    /// Makes an ONLYOFFICE PDF form the way a user does and the TypeScript <c>createOoForm</c> helper
    /// does: a new .docx in My documents, turned into a .docxf template and then into a .pdf form in
    /// <paramref name="folderId"/>, both steps through <c>copyas</c> with <c>toForm</c>, which the
    /// document server converts.
    /// </summary>
    protected async Task<int> ConvertToOoFormAsync(int folderId, string fileName)
    {
        var source = await CreateFileInMy("Autotest OO Form Source.docx", Owner);
        var myDocsId = source.FolderId;

        var template = (await _filesApi.CopyFileAsAsync(
            source.Id,
            new CopyAsRequest("Autotest OO Form Template.docxf", new CopyAsRequestDestFolderId(myDocsId), toForm: true),
            TestContext.Current.CancellationToken)).Response;

        var form = (await _filesApi.CopyFileAsAsync(
            template.Should().BeOfType<FileDto>().Subject.Id,
            new CopyAsRequest(fileName, new CopyAsRequestDestFolderId(folderId), toForm: true),
            TestContext.Current.CancellationToken)).Response;

        return form.Should().BeOfType<FileDto>().Subject.Id;
    }

    /// <summary>
    /// Uploads a small in-memory PDF that does not carry the ONLYOFFICE form signature, so it is a
    /// genuine "regular PDF" as far as the product's form check is concerned.
    /// </summary>
    protected async Task<int> UploadRegularPdfAsync(int folderId, string fileName = "Autotest Regular.pdf")
    {
        var content = Encoding.ASCII.GetBytes("%PDF-1.4\n1 0 obj\n<< /Type /Catalog >>\nendobj\ntrailer\n<< /Root 1 0 R >>\n%%EOF");

        return await UploadFileAsync(folderId, fileName, content);
    }

    /// <summary>
    /// Uploads a small in-memory file with a <c>.docxf</c> title. The content is never actually
    /// inspected — <c>isformpdf</c> and <c>formroles</c> both reject non-PDF extensions before
    /// reading any bytes — so any placeholder content is enough to stand in for a form template.
    /// </summary>
    protected async Task<int> UploadDocxfTemplateAsync(int folderId, string fileName = "Autotest Template.docxf")
    {
        var content = "PK"u8.ToArray();

        return await UploadFileAsync(folderId, fileName, content);
    }

    private async Task<int> UploadFileAsync(int folderId, string fileName, byte[] content)
    {
        var settings = (await _filesSettingsApi.GetFilesSettingsAsync(TestContext.Current.CancellationToken)).Response;

        var session = (await _filesOperationsApi.CreateUploadSessionInFolderAsync(
            folderId,
            new SessionRequest(fileName, content.Length),
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        var chunkSize = (int)settings.ChunkUploadSize;
        var chunkNumber = 1;

        for (var offset = 0; offset < content.Length; offset += chunkSize)
        {
            var length = Math.Min(chunkSize, content.Length - offset);
            await using var chunkStream = new MemoryStream(content, offset, length);

            await _filesOperationsApi.UploadAsyncSessionAsync(
                folderId,
                session.Id,
                chunkNumber,
                new FileParameter(chunkStream),
                TestContext.Current.CancellationToken);

            chunkNumber++;
        }

        var uploaded = (await _filesOperationsApi.FinalizeSessionAsync(
            folderId,
            session.Id,
            TestContext.Current.CancellationToken)).Response;

        return uploaded.File.Id;
    }
}

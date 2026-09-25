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
/// <c>GET /files/{folderId}</c> on the "Shared with me" section with the <c>Pdf</c> and
/// <c>PdfForm</c> filter types.
/// </summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "Forms")]
public class SharedWithMePdfFilterTests(
    AspireAppFixture fixture)
    : FormsTestBase(fixture)
{
    /// <summary>
    /// "Shared with me" used to ignore the <c>PdfForm</c> filter and return every shared file:
    /// <c>FileDao.GetFilesFilteredAsync</c> had no <c>Pdf</c>/<c>PdfForm</c> branch in its category
    /// switch. Fixed by filtering those two types by category, as the regular folder listing does.
    /// </summary>
    [Fact]
    [Trait("Bug", "81919")]
    public async Task GetSharedWithMe_PdfFormFilter_ReturnsOnlyPdfForms()
    {
        // Arrange
        var (user, docxTitle, pdfTitle, formTitle) = await ShareDocxPdfAndForm("PdfForm");
        await _filesClient.Authenticate(user);
        var sharedWithMeId = await GetShareFolderIdAsync(user);

        // Act
        var content = (await _foldersApi.GetFolderByFolderIdAsync(sharedWithMeId, filterType: FilterType.PdfForm, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        var titles = content.Files.Select(f => f.Title).ToList();
        titles.Should().NotContain(docxTitle);
        titles.Should().NotContain(pdfTitle);
        titles.Should().Contain(formTitle);
    }

    /// <summary>
    /// "Shared with me" used to ignore the <c>Pdf</c> filter and return every shared file, for the
    /// same missing branch in <c>FileDao.GetFilesFilteredAsync</c>; fixed together with the case above.
    /// </summary>
    [Fact]
    [Trait("Bug", "81919")]
    public async Task GetSharedWithMe_PdfFilter_ReturnsOnlyPlainPdfs()
    {
        // Arrange
        var (user, docxTitle, pdfTitle, formTitle) = await ShareDocxPdfAndForm("Pdf");
        await _filesClient.Authenticate(user);
        var sharedWithMeId = await GetShareFolderIdAsync(user);

        // Act
        var content = (await _foldersApi.GetFolderByFolderIdAsync(sharedWithMeId, filterType: FilterType.Pdf, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        var titles = content.Files.Select(f => f.Title).ToList();
        titles.Should().NotContain(docxTitle);
        titles.Should().NotContain(formTitle);
        titles.Should().Contain(pdfTitle);
    }

    /// <summary>
    /// Control for the two cases above: the other filter types already work in "Shared with me".
    /// </summary>
    [Fact]
    public async Task GetSharedWithMe_FoldersOnlyFilter_ReturnsOnlyFolders()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var user = await InviteContact(EmployeeType.User);
        var myDocsId = await GetUserFolderIdAsync(Owner);
        var file = await CreateFile("Autotest Shared FoldersOnly Filter Doc.docx", myDocsId);
        var folder = await CreateFolder("Autotest Shared FoldersOnly Filter Folder", myDocsId);
        await _sharingApi.SetFileSecurityInfoAsync(file.Id, ReadShare(user), TestContext.Current.CancellationToken);
        await _sharingApi.SetFolderSecurityInfoAsync(folder.Id, ReadShare(user), TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(user);
        var sharedWithMeId = await GetShareFolderIdAsync(user);

        // Act
        var content = (await _foldersApi.GetFolderByFolderIdAsync(sharedWithMeId, filterType: FilterType.FoldersOnly, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        content.Files.Should().BeEmpty();
        content.Folders.Select(f => f.Title).Should().Contain(folder.Title);
    }

    /// <summary>
    /// Has the owner share a .docx, a plain PDF and an ONLYOFFICE PDF form from "My documents" with a
    /// new user for reading. The TS source builds the plain PDF through <c>saveFileAsPdf</c>, which
    /// needs a document server; a hand-built PDF without the form signature stands in for it here.
    /// </summary>
    private async Task<(User User, string DocxTitle, string PdfTitle, string FormTitle)> ShareDocxPdfAndForm(string prefix)
    {
        await _filesClient.Authenticate(Owner);
        var user = await InviteContact(EmployeeType.User);
        var myDocsId = await GetUserFolderIdAsync(Owner);

        var docx = await CreateFile($"Autotest Shared {prefix} Filter Doc.docx", myDocsId);
        var pdfId = await UploadRegularPdfAsync(myDocsId, $"Autotest Shared {prefix} Filter Pdf.pdf");
        var formId = await UploadOoFormAsync(myDocsId, $"Autotest Shared {prefix} Filter Form.pdf");

        foreach (var id in new[] { docx.Id, pdfId, formId })
        {
            await _sharingApi.SetFileSecurityInfoAsync(id, ReadShare(user), TestContext.Current.CancellationToken);
        }

        var pdf = await GetFile(pdfId);
        var form = await GetFile(formId);

        return (user, docx.Title, pdf.Title, form.Title);
    }

    private static SecurityInfoSimpleRequestDto ReadShare(User user)
    {
        return new SecurityInfoSimpleRequestDto
        {
            Share = [new FileShareParams { ShareTo = user.Id, Access = FileShare.Read }],
            Notify = false
        };
    }
}

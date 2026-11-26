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
namespace ASC.Files.Tests.Tests._01_Files.CopySave;

/// <summary>
/// A conversion the document service cannot do, on the routes that convert in the request: <c>saveaspdf</c> and
/// <c>copyas</c> into another format. <c>DocumentServiceConnector.GetConvertedUriAsync</c> wrapped every failure of the
/// document service into a plain exception, so a 500 came back; it is a failure of the document service, a 502, as for
/// the conversion of an uploaded file.
/// </summary>
/// <remarks>
/// The source is a .docx whose content is a broken zip: the document service accepts the task and fails to open the
/// file. Should the converter of the integration-test host be down or restarting, the call fails before the
/// conversion with the same 502, so the expected answer does not depend on it.
/// </remarks>
[Trait("Category", "Validation")]
[Trait("Feature", "Files")]
public class DocumentServiceConversionFailureTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    private const string DocServiceError = "Error occurred in the Documents Service";

    [Fact]
    public async Task SaveFileAsPdf_DamagedSource_Returns502AndCreatesNothing()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Save PDF Damaged");
        var source = await UploadDamagedDocx(room.Id, "Autotest Damaged Source.docx");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.SaveFileAsPdfAsync(
                source.Id,
                new SaveAsPdfRequest(room.Id, "Autotest Damaged Copy"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(502);
        ErrorMessageOf(exception.ErrorContent!.ToString()).Should().Contain(DocServiceError);
        (await GetFileTitles(room.Id)).Should().Equal(source.Title);
    }

    [Fact]
    public async Task CopyFileAs_DamagedSourceToPdf_Returns502AndCreatesNothing()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Copy As Damaged");
        var source = await UploadDamagedDocx(room.Id, "Autotest Damaged Source.docx");

        // Act - a destination title with another extension goes through FileConverter.ExecAsync
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.CopyFileAsAsync(
                source.Id,
                new CopyAsRequest("Autotest Damaged Copy.pdf", new CopyAsRequestDestFolderId(room.Id)),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(502);
        ErrorMessageOf(exception.ErrorContent!.ToString()).Should().Contain(DocServiceError);
        (await GetFileTitles(room.Id)).Should().Equal(source.Title);
    }

    /// <summary>Uploads a .docx that starts as a zip and breaks off right after the header.</summary>
    private async Task<FileDto> UploadDamagedDocx(int folderId, string fileName)
    {
        var content = new byte[] { 0x50, 0x4B, 0x03, 0x04 }.Concat("not a document"u8.ToArray()).ToArray();
        var file = new FileParameter(fileName, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", new MemoryStream(content));

        var uploaded = (await _foldersApi.UploadFileAsync(folderId, file: file, cancellationToken: TestContext.Current.CancellationToken)).Response;

        return uploaded.Should().ContainSingle().Subject;
    }

    /// <summary>The titles of the files directly inside a folder.</summary>
    private async Task<List<string>> GetFileTitles(int folderId)
    {
        var content = (await _foldersApi.GetFolderByFolderIdAsync(folderId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        return (content.Files ?? []).OfType<FileDto>().Select(f => f.Title).ToList();
    }
}

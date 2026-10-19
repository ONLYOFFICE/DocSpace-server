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

namespace ASC.Files.Tests.Tests._01_Files.FormFilling;

/// <summary>
/// <c>POST /files/file/{fileId}/xlsx</c> and <c>GET /files/file/{fileId}/xlsx</c> — functional and
/// validation scenarios of the form answers report.
/// </summary>
/// <remarks>
/// The report is built asynchronously from a document builder script, so every test that waits for
/// completion needs the Document Server of the host to be reachable. The status of an unknown id on
/// the read side is documented as "no run on record", not as an error.
/// </remarks>
[Trait("Category", "CRUD")]
[Trait("Feature", "FormFilling")]
public class FormXlsxTests(
    AspireAppFixture fixture)
    : FormResultsTestBase(fixture)
{
    private const string DocumentServerUnavailable =
        "The report cannot complete in the integration-test profile: the document builder fails at " +
        "the download step, as the room index export does (see RoomIndexExportTests). Remove this " +
        "Skip once the integration-test host serves a working document server.";

    [Fact(Skip = DocumentServerUnavailable)]
    public async Task GenerateXlsx_StartedForm_ProducesReportThatGetXlsxReportsAsCompleted()
    {
        // Arrange
        var (_, formId) = await CreateStartedForm("Autotest Xlsx Generate Room");

        // Act
        var generated = (await _filesApi.GenerateXlsxAsync(formId, TestContext.Current.CancellationToken)).Response;

        // Assert
        generated.IsNewFile.Should().BeTrue();
        generated.Form.Id.Should().Be(formId);
        generated.Task.Id.Should().NotBeNullOrEmpty();
        var resultsFolderId = generated.Form.ResultsFolderId;
        resultsFolderId.Should().BeGreaterThan(0);

        var task = await WaitForXlsxCompleted(formId);

        task.Percentage.Should().Be(100);
        task.Error.Should().BeNullOrEmpty();
        task.ResultFileName.Should().EndWith(".xlsx");

        var resultFileId = int.Parse(task.ResultFileId.ToString()!);
        resultFileId.Should().BeGreaterThan(0);

        var reportFile = await GetFile(resultFileId);
        reportFile.FileExst.Should().Be(".xlsx");
        reportFile.FolderId.Should().Be(resultsFolderId);
    }

    [Fact]
    public async Task GenerateXlsx_SecondCall_ReusesTheReportInsteadOfCreatingAnother()
    {
        // Arrange
        var (_, formId) = await CreateStartedForm("Autotest Xlsx Reuse Room");
        var first = (await _filesApi.GenerateXlsxAsync(formId, TestContext.Current.CancellationToken)).Response;
        var resultsFolderId = first.Form.ResultsFolderId!.Value;
        await WaitForXlsxCompleted(formId);

        // Act
        var second = (await _filesApi.GenerateXlsxAsync(formId, TestContext.Current.CancellationToken)).Response;

        // Assert
        second.IsNewFile.Should().BeFalse();
        (await GetXlsxFilesIn(resultsFolderId)).Should().HaveCount(1);
    }

    [Fact(Skip = DocumentServerUnavailable)]
    public async Task GenerateXlsx_ResultFileId_ResolvesToTheOriginalForm()
    {
        // Arrange
        var (_, formId) = await CreateStartedForm("Autotest Xlsx Result Id Room");
        await _filesApi.GenerateXlsxAsync(formId, TestContext.Current.CancellationToken);
        var task = await WaitForXlsxCompleted(formId);
        var resultFileId = int.Parse(task.ResultFileId.ToString()!);

        // Act
        var generated = (await _filesApi.GenerateXlsxAsync(resultFileId, TestContext.Current.CancellationToken)).Response;

        // Assert
        generated.Form.Id.Should().Be(formId);
        generated.IsNewFile.Should().BeFalse();
    }

    [Fact]
    public async Task GenerateXlsx_RegularDocxFile_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest Xlsx Docx.docx", Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.GenerateXlsxAsync(file.Id, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GenerateXlsx_FormThatIsNotStarted_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateFillingFormsRoom("Autotest Xlsx Not Started Room " + Guid.NewGuid().ToString()[..8]);
        var form = await CreateFormInRoom(room.Id);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.GenerateXlsxAsync(form.Id, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GenerateXlsx_NonExistentFileId_Returns404()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.GenerateXlsxAsync(999999999, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task GetXlsx_BeforeAnyGeneration_ReturnsNoTask()
    {
        // Arrange
        var (_, formId) = await CreateStartedForm("Autotest Xlsx No Run Room");

        // Act
        var result = await _filesApi.GetXlsxAsync(formId, TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().BeNull();
        result.Count.Should().Be(0);
    }

    [Fact]
    public async Task GetXlsx_UnknownFileId_ReadsAsNoRunOnRecord()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var result = await _filesApi.GetXlsxAsync(999999999, TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().BeNull();
    }
}

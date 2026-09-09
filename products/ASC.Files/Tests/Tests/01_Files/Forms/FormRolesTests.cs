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
/// <c>GET /files/file/{fileId}/formroles</c> — the filling roles defined on a form.
/// </summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "Forms")]
public class FormRolesTests(
    AspireAppFixture fixture)
    : FormsTestBase(fixture)
{
    [Fact]
    public async Task GetAllFormRoles_OnlyofficeForm_ReturnsEmptyArray()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest GetAllFormRoles OO Form Room");
        var fileId = await UploadOoFormAsync(room.Id, "Autotest GetAllFormRoles OO Form.pdf");

        // Act
        var roles = (await _filesApi.GetAllFormRolesAsync(fileId, TestContext.Current.CancellationToken)).Response;

        // Assert - a freshly uploaded form has no roles defined yet
        roles.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task GetAllFormRoles_RegularDocxFile_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest GetAllFormRoles Docx Room");
        var file = await CreateFile("Autotest GetAllFormRoles Docx File", room.Id);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.GetAllFormRolesAsync(file.Id, TestContext.Current.CancellationToken));

        // Assert - a regular office file is not a form
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetAllFormRoles_DocxfFormTemplate_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest GetAllFormRoles Docxf Room");
        var fileId = await UploadDocxfTemplateAsync(room.Id, "Autotest GetAllFormRoles Template.docxf");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.GetAllFormRolesAsync(fileId, TestContext.Current.CancellationToken));

        // Assert - a .docxf is a form template, not a PDF form
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetAllFormRoles_OnlyofficeFormInArchivedRoom_Returns200()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest GetAllFormRoles Archive Room");
        var fileId = await UploadOoFormAsync(room.Id, "Autotest GetAllFormRoles Archive Form.pdf");

        await ArchiveRoom(room.Id);

        // Act
        var response = await _filesApi.GetAllFormRolesWithHttpInfoAsync(fileId, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAllFormRoles_OnlyofficeFormInMyDocuments_Returns200()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var myDocsId = await GetUserFolderIdAsync(Owner);
        var fileId = await UploadOoFormAsync(myDocsId, "Autotest GetAllFormRoles My Docs Form.pdf");

        // Act
        var response = await _filesApi.GetAllFormRolesWithHttpInfoAsync(fileId, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAllFormRoles_FileIdZero_Returns404()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.GetAllFormRolesAsync(0, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    /// <summary>Historically bug 81346; already fixed, this is the expected behaviour today.</summary>
    [Fact]
    public async Task GetAllFormRoles_NonExistentFile_Returns404()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.GetAllFormRolesAsync(999999999, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }
}

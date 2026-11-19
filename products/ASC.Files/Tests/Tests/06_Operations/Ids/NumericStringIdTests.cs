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

namespace ASC.Files.Tests.Tests._06_Operations.Ids;

/// <summary>
/// The file-creation calls (<c>templateId</c>) and the default template setting (<c>selectedFile</c>) read a JSON string
/// as a third-party id. A string holding a whole number - here the id of a real portal file - matches no third-party
/// storage, and the provider DAO took the missing selector for one: a NullReferenceException, a 500. It now finds
/// nothing, so each call answers as it does for an id that names no file: 403 "The required file was not found" for a
/// template, 404 for a default template. The string is not read as the portal file's number.
/// </summary>
[Trait("Category", "Operations")]
public class NumericStringIdTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task CreateFile_TemplateIdNumericString_IsFileNotFound()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var template = await CreateFileInMy("Autotest template.docx", Owner);
        var folderId = await GetUserFolderIdAsync(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.CreateFileAsync(
                folderId,
                new CreateFileRequest("Autotest from template.docx", new CreateFileRequestTemplateId(template.Id.ToString())),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent!.ToString().Should().Contain("The required file was not found");
    }

    [Fact]
    public async Task CreateFileInMyDocuments_TemplateIdNumericString_IsFileNotFound()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var template = await CreateFileInMy("Autotest template.docx", Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.CreateFileInMyDocumentsAsync(
                new CreateFileRequest("Autotest from template.docx", new CreateFileRequestTemplateId(template.Id.ToString())),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent!.ToString().Should().Contain("The required file was not found");
    }

    [Fact]
    public async Task SetDefaultTemplate_SelectedFileNumericString_IsNotFound()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest default template.docx", Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesSettingsApi.SetDefaultTemplateAsync(
                new DefaultTemplateSettingsRequestDto(new DefaultTemplateSettingsRequestDtoSelectedFile(file.Id.ToString()), ".docx"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }
}

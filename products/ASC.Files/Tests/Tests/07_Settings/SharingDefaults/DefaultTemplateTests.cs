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

namespace ASC.Files.Tests.Tests._07_Settings.SharingDefaults;

/// <summary>
/// <c>PUT</c>/<c>POST /files/settings/defaulttemplate</c> - the portal-wide default template used
/// when creating new documents.
/// </summary>
[Trait("Category", "Settings")]
[Trait("Feature", "SharingDefaults")]
public class DefaultTemplateTests(AspireAppFixture fixture) : SharingDefaultsTestBase(fixture)
{
    [Fact]
    public async Task SetDefaultTemplate_WithoutSelectedFile_ReturnsBadRequest()
    {
        // Arrange - selectedFile is a required, non-nullable constructor parameter on the generated
        // DTO, so omitting it can only be exercised over raw HTTP.
        using var content = new StringContent("""{"fileExtension":".docx"}""", Encoding.UTF8, "application/json");

        // Act
        using var response = await _filesClient.PutAsync("api/2.0/files/settings/defaulttemplate", content, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Regression test for <c>BUG 81953</c>: a DocSpaceAdmin was able to set another user's file as
    /// the portal-wide default template even without access to that file. The endpoint is now
    /// expected to demand sharing access to the selected file before accepting it.
    /// </summary>
    [Fact]
    public async Task SetDefaultTemplate_DocSpaceAdminCannotUseOwnersFile_ReturnsForbidden()
    {
        // Arrange
        var file = await CreateFileInMy("Autotest Default Template File.docx", Owner);

        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesSettingsApi.SetDefaultTemplateAsync(
                new DefaultTemplateSettingsRequestDto(new DefaultTemplateSettingsRequestDtoSelectedFile(file.Id), ".docx"),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("You don't have enough permission to perform the operation");
    }

    /// <summary>
    /// BUG 79837: uploading a template over 100MB made Kestrel abort the connection instead of
    /// answering. Fixed by putting <c>[DisableRequestSizeLimit]</c> on <c>UploadDefaultTemplate</c>
    /// plus an explicit size check answering 400 with a message.
    /// </summary>
    [Fact]
    [Trait("Bug", "79837")]
    public async Task UploadDefaultTemplate_LargerThan100MB_ReturnsBadRequestWithErrorMessage()
    {
        // Arrange
        using var stream = new MemoryStream(new byte[101 * 1024 * 1024]);
        var file = new FileParameter("template.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", stream);

        // Act & Assert - the upload used to be rejected with 400 and no error message in the body
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesSettingsApi.UploadDefaultTemplateAsync(".docx", file, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent?.ToString().Should().NotBeNullOrEmpty();
    }
}

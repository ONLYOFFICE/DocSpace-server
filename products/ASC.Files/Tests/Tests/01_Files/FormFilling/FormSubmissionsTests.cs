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
/// <c>GET /files/file/{fileId}/submissions</c> — functional and validation scenarios. The status of an
/// unknown id is not documented by the endpoint, so it is deliberately not asserted.
/// </summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "FormFilling")]
public class FormSubmissionsTests(
    AspireAppFixture fixture)
    : FormResultsTestBase(fixture)
{
    [Fact]
    public async Task GetFormSubmissions_StartedFormWithoutAnswers_ReturnsEmptyMetadataAndSubmissions()
    {
        // Arrange
        var (_, formId) = await CreateStartedForm("Autotest Submissions Empty Room");

        // Act
        var result = (await _filesApi.GetFormSubmissionsAsync(formId, TestContext.Current.CancellationToken)).Response;

        // Assert
        result.Metadata.Should().NotBeNull().And.BeEmpty();
        result.Submissions.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task GetFormSubmissions_RepeatedRead_IsStableAndHasNoSideEffects()
    {
        // Arrange
        var (_, formId) = await CreateStartedForm("Autotest Submissions Repeat Room");

        // Act
        var first = (await _filesApi.GetFormSubmissionsAsync(formId, TestContext.Current.CancellationToken)).Response;
        var second = (await _filesApi.GetFormSubmissionsAsync(formId, TestContext.Current.CancellationToken)).Response;

        // Assert
        second.Should().BeEquivalentTo(first);
        (await GetFile(formId)).Version.Should().Be(1);
    }

    [Fact]
    public async Task GetFormSubmissions_RegularDocxFile_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest Submissions Docx.docx", Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.GetFormSubmissionsAsync(file.Id, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetFormSubmissions_FormThatIsNotStarted_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateFillingFormsRoom("Autotest Submissions Not Started Room " + Guid.NewGuid().ToString()[..8]);
        var form = await CreateFormInRoom(room.Id);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.GetFormSubmissionsAsync(form.Id, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }
}

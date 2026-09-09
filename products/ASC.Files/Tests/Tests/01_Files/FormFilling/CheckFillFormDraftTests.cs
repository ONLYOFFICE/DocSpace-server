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
/// <c>POST /files/masterform/:fileId/checkfillformdraft</c>. For a caller who can edit the form
/// (the room owner, here) the endpoint short-circuits straight to the editor URL - see
/// <c>FileStorageService.CheckFillFormDraftAsync</c> - so this never needs a live document server to
/// exercise from the owner's point of view.
/// </summary>
[Trait("Category", "Features")]
[Trait("Feature", "FormFilling")]
public class CheckFillFormDraftTests(
    AspireAppFixture fixture)
    : FormFillingTestBase(fixture)
{
    private async Task<int> SetupForm()
    {
        await _filesClient.Authenticate(Owner);
        var room = await CreateFillingFormsRoom("Autotest CheckFillFormDraft Room " + Guid.NewGuid().ToString()[..8]);
        var form = await CreateFormInRoom(room.Id);
        await StartFormFilling(form.Id);

        return form.Id;
    }

    [Fact]
    public async Task CheckFillFormDraft_ValidDraft_ReturnsEditorUrl()
    {
        // Arrange
        var formId = await SetupForm();

        // Act
        var result = (await _filesApi.CheckFillFormDraftAsync(
            formId, new CheckFillFormDraft(version: 1), TestContext.Current.CancellationToken)).Response;

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("/doceditor");
        result.Should().Contain($"fileid={formId}");
    }

    /// <summary>
    /// <see cref="CheckFillFormDraft.RequestView"/> has a private setter and is never serialised
    /// (<c>ShouldSerializeRequestView</c> always returns false), so the typed SDK cannot actually put
    /// <c>requestView: true</c> on the wire - a raw request is the only way to send it.
    /// </summary>
    [Fact]
    public async Task CheckFillFormDraft_RequestView_ReturnsEditorUrl()
    {
        // Arrange
        var formId = await SetupForm();

        // Act
        using var response = await _filesClient.PostAsync(
            $"api/2.0/files/masterform/{formId}/checkfillformdraft",
            new StringContent(JsonSerializer.Serialize(new { version = 1, requestView = true }), Encoding.UTF8, "application/json"),
            TestContext.Current.CancellationToken);

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue(body);

        using var json = JsonDocument.Parse(body);
        var result = json.RootElement.GetProperty("response").GetString();

        result.Should().Contain("/doceditor");
        result.Should().Contain($"fileid={formId}");
    }

    [Fact]
    public async Task CheckFillFormDraft_NonExistentFileId_ReturnsNotFound()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.CheckFillFormDraftAsync(
                999999999, new CheckFillFormDraft(version: 1), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }
}

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

namespace ASC.Files.Tests.Tests._03_Rooms.ThirdParty;

/// <summary>
/// Renaming a third-party file that is open for editing through <c>PUT file/{fileId}</c> is refused with 403, as
/// the other changes of a file being edited are. <c>EntryManager.FileRenameAsync</c> raised a plain exception for
/// it, and a 500 came back. Skipped unless Nextcloud is configured in the environment.
/// </summary>
[Trait("Category", "Rooms")]
[Trait("Requires", "Nextcloud")]
[Collection("Nextcloud")]
public class ThirdPartyFileRenameEditedTests(
    AspireAppFixture fixture)
    : ThirdPartyTestBase(fixture)
{
    private const string EditedFileMessage = "You cannot update a file which is opened for editing";

    [Fact]
    public async Task UpdateFile_RenameThirdPartyFileOpenForEditing_Returns403()
    {
        // Arrange
        RequireNextcloud();
        await _filesClient.Authenticate(Owner);
        var (_, work) = await CreateWorkFolder("TP Rename Edited");
        var file = await CreateThirdPartyFile(work.Id, $"{UniqueTitle("TP Rename Edited")}.docx");
        var docKey = (await _filesApi.StartEditFileAsync(
            file.Id, new StartEditRequest(editingAlone: true), TestContext.Current.CancellationToken)).Response;

        try
        {
            // Act
            var exception = await Assert.ThrowsAsync<ApiException>(
                async () => await _filesApi.UpdateFileAsync(
                    file.Id, new UpdateFileRequest($"{UniqueTitle("TP Renamed Edited")}.docx"), TestContext.Current.CancellationToken));

            // Assert
            exception.ErrorCode.Should().Be(403);
            ErrorMessageOf(exception.ErrorContent!.ToString()).Should().Be(EditedFileMessage);

            var (files, _) = await ListThirdPartyFolder(work.Id);
            files.Should().Equal(file.Title);
        }
        finally
        {
            // The editing mark would otherwise keep the base class from deleting the work folder from the storage.
            await _filesApi.TrackEditFileAsync(file.Id, Guid.Empty, docKey, isFinish: true, TestContext.Current.CancellationToken);
        }
    }

    /// <summary>The <c>error.message</c> of an error body, or an empty string when it has none.</summary>
    private static string ErrorMessageOf(string? body)
    {
        using var json = JsonDocument.Parse(string.IsNullOrEmpty(body) ? "{}" : body);

        return json.RootElement.TryGetProperty("error", out var error)
            && error.ValueKind == JsonValueKind.Object
            && error.TryGetProperty("message", out var message)
            && message.ValueKind == JsonValueKind.String
                ? message.GetString() ?? ""
                : "";
    }
}

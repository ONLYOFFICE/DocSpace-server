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

namespace ASC.Files.Tests.Tests._01_Files.Editing;

/// <summary>
/// <c>POST /files/file/{fileId}/startedit</c> on the co-authoring branch (<c>editingAlone: false</c>, which is also
/// what a body without the field means). That branch asks the document server to track the session
/// (<c>DocumentServiceTracker.StartTrackAsync</c>); the TypeScript suite expects the refusal "File editing start error" (403).
/// </summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "Files")]
public class EditingStartCoAuthoringTests(AspireAppFixture fixture) : EditingTestBase(fixture)
{
    [Fact]
    [Trait("Bug", "81267")]
    public async Task StartEditFile_EditingAloneFalse_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var (_, file) = await CreateRoomWithFile("Autotest Start Edit Room", "Autotest Start Edit File");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.StartEditFileAsync(file.Id, new StartEditRequest(editingAlone: false), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("File editing start error");
    }

    // The typed StartEditRequest always writes "editingAlone":false, so a body without the field can only be sent raw.
    [Fact]
    [Trait("Bug", "81267")]
    public async Task StartEditFile_RequestWithoutEditingAloneField_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var (_, file) = await CreateRoomWithFile("Autotest StartEdit No Body Room", "Autotest StartEdit No Body File");

        // Act
        using var response = await _filesClient.PostAsync(
            $"api/2.0/files/file/{file.Id}/startedit",
            new StringContent("{}", Encoding.UTF8, "application/json"),
            TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden, body);
        body.Should().Contain("File editing start error");
    }
}

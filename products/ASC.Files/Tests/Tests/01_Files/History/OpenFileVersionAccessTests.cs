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

namespace ASC.Files.Tests.Tests._01_Files.History;

/// <summary>GET /files/file/{id}/openedit?version= - file version access control.</summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "Files")]
public class OpenFileVersionAccessTests(
    AspireAppFixture fixture)
    : HistoryTestBase(fixture)
{
    /// <summary>
    /// BUG 80683: the server answered 200 and served the content of a specific past version to a
    /// viewer whose Read access did not cover history. Fixed by requiring <c>CanReadHistoryAsync</c>
    /// for any explicit version parameter in <c>DocumentServiceHelper.GetCurFileInfoAsync</c>.
    /// </summary>
    [Fact]
    [Trait("Bug", "80683")]
    public async Task OpenEditFile_ViewerInRoom_CannotOpenSpecificFileVersion()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var (room, file) = await CreateRoomWithFile("Autotest Version Access Room", "Autotest Version Access File.docx");

        var viewer = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, viewer, FileShare.Read);

        await _filesClient.Authenticate(viewer);

        // Act
        // A viewer may open the current version of the file...
        var current = (await _filesApi.OpenEditFileAsync(file.Id, view: true, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        current.Should().NotBeNull();

        // Act & Assert
        // ...but must not be able to open a specific past version, which is not covered by their
        // Read access to the file itself.
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.OpenEditFileAsync(file.Id, version: 1, view: true, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }
}

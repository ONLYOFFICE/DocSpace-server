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


namespace ASC.Files.Tests.Tests._06_Operations.UploadSession;

/// <summary>
/// Suspected defect F24 of the file-operations verification record: the part and finalize routes find a
/// session by its id alone, so a caller who cannot read the target folder but holds the id writes into the
/// owner's session and finalizes it into the owner's folder. Abort already checks the owner. The test asserts
/// what the access rules say - the stranger is refused and nothing lands - so it stays red while the defect is open.
/// </summary>
[Trait("Category", "Bug")]
[Trait("Feature", "Files")]
public class UploadSessionForeignIdBugTests(
    AspireAppFixture fixture)
    : UploadSessionTestBase(fixture)
{
    [Fact]
    public async Task UploadSession_UserWithoutFolderAccessPresentsOwnersSessionId_IsRefusedAndNothingLands()
    {
        // Arrange
        var myDocs = await GetUserFolderIdAsync(Owner);
        var folder = await CreateFolder("Autotest Upload Foreign Id Folder", myDocs);
        var ownerBytes = MakeBytes(300, 91);
        var foreignBytes = MakeBytes(300, 92);
        var sessionId = await OpenSession(folder.Id, "Autotest Upload Foreign Id.bin", ownerBytes.Length);

        var intruder = await InviteContact(EmployeeType.RoomAdmin);
        await _filesClient.Authenticate(intruder);

        // Act
        var chunk = await ChunkStatus(folder.Id, sessionId, 1, foreignBytes);
        var finalize = await FinalizeStatus(folder.Id, sessionId);

        // Assert
        await _filesClient.Authenticate(Owner);
        var landed = await FilesIn(folder.Id, "Autotest Upload Foreign Id");

        landed.Should().BeEmpty("a caller without access to the folder must not store a file in it");
        chunk.Should().Be(403);
        finalize.Should().Be(403);
    }
}

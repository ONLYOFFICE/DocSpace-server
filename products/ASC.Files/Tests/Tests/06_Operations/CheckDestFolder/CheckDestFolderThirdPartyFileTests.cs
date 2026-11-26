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

namespace ASC.Files.Tests.Tests._06_Operations.CheckDestFolder;

/// <summary>
/// <c>GET /api/2.0/files/fileops/checkdestfolder</c> with a file of a third-party storage (Nextcloud). A third-party
/// file has a string id, and the accepted ids used to be read back as numbers, so any destination that accepted such
/// a file ended as 500. Both kinds are now looked up with their own DAO, as <c>GET fileops/move</c> does. The cases
/// need a reachable Nextcloud (<c>RequireNextcloud</c>) and skip otherwise.
/// </summary>
[Trait("Category", "Operations")]
[Trait("Feature", "Files")]
[Trait("Requires", "Nextcloud")]
[Collection("Nextcloud")]
public class CheckDestFolderThirdPartyFileTests(
    AspireAppFixture fixture)
    : ASC.Files.Tests.Tests._03_Rooms.ThirdParty.ThirdPartyTestBase(fixture)
{
    [Fact]
    public async Task CheckDestFolder_ThirdPartyFileToMyDocuments_ReturnsAllAllowedWithTheFile()
    {
        RequireNextcloud();

        // Arrange
        await _filesClient.Authenticate(Owner);
        var (_, work) = await CreateWorkFolder("CheckDestFolder TP Source");
        var title = $"{UniqueTitle("CheckDestFolder TP File")}.docx";
        var file = await CreateThirdPartyFile(work.Id, title);
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);

        // Act
        var payload = (await _filesOperationsApi.CheckMoveOrCopyDestFolderAsync(
            fileIds: [file.Id],
            destFolderId: myDocsFolderId.ToString(CultureInfo.InvariantCulture),
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        payload.Result.Should().Be(CheckDestFolderResult.AllAllowed);
        (payload.Files ?? []).Select(f => f.Title).Should().Equal(title);
    }

    [Fact]
    public async Task CheckDestFolder_ThirdPartyAndPortalFileToMyDocuments_ReturnsAllAllowedWithBoth()
    {
        RequireNextcloud();

        // Arrange
        await _filesClient.Authenticate(Owner);
        var (_, work) = await CreateWorkFolder("CheckDestFolder TP Mixed");
        var thirdPartyTitle = $"{UniqueTitle("CheckDestFolder TP Mixed File")}.docx";
        var thirdPartyFile = await CreateThirdPartyFile(work.Id, thirdPartyTitle);
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var room = await CreateCustomRoom("Autotest CheckDestFolder TP Mixed Room");
        const string portalTitle = "Autotest CheckDestFolder TP Mixed Portal File.docx";
        var portalFile = await CreateFile(portalTitle, myDocsFolderId);

        // Act
        var payload = (await _filesOperationsApi.CheckMoveOrCopyDestFolderAsync(
            fileIds: [portalFile.Id.ToString(CultureInfo.InvariantCulture), thirdPartyFile.Id],
            destFolderId: room.Id.ToString(CultureInfo.InvariantCulture),
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        payload.Result.Should().Be(CheckDestFolderResult.AllAllowed);
        (payload.Files ?? []).Select(f => f.Title).Should().BeEquivalentTo([portalTitle, thirdPartyTitle]);
    }
}

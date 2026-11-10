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
/// The limits a chunked upload session is held to: the twelve-hour reservation reported when the session
/// opens, the size limit checked before the session exists, and a part larger than <c>chunkUploadSize</c>.
/// </summary>
[Trait("Category", "Validation")]
[Trait("Feature", "Files")]
public class UploadSessionLimitsTests(
    AspireAppFixture fixture)
    : UploadSessionTestBase(fixture)
{
    [Fact]
    public async Task CreateUploadSessionInFolder_ReportsAReservationOfTwelveHours()
    {
        // Arrange
        var folderId = await GetUserFolderIdAsync(Owner);

        // Act
        var session = (await _filesOperationsApi.CreateUploadSessionInFolderAsync(
            folderId, new SessionRequest("Autotest Upload Expiry.bin", 100), TestContext.Current.CancellationToken)).Response;

        // Assert
        (session.Expired - session.Created).Should().BeCloseTo(TimeSpan.FromHours(12), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task CreateUploadSessionInFolder_SizeAboveTheUploadLimit_Returns402AndStoresNothing()
    {
        // Arrange
        var folderId = await GetUserFolderIdAsync(Owner);
        const string fileName = "Autotest Upload Above Limit.bin";

        // Act: one petabyte is above any limit the portal can have
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.CreateUploadSessionInFolderAsync(
                folderId, new SessionRequest(fileName, 1L << 50), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(402);
        (await FilesIn(folderId, fileName)).Should().BeEmpty();
    }

    [Fact]
    public async Task UploadAsyncSession_PartAboveChunkUploadSize_Returns402()
    {
        // Arrange
        var chunkSize = await GetChunkSize();
        var folderId = await GetUserFolderIdAsync(Owner);
        var sessionId = await OpenSession(folderId, "Autotest Upload Oversize 402.bin", chunkSize + 100);

        // Act
        var status = await ChunkStatus(folderId, sessionId, 1, MakeBytes(chunkSize + 1, 7));

        // Assert
        status.Should().Be(402);
    }
}

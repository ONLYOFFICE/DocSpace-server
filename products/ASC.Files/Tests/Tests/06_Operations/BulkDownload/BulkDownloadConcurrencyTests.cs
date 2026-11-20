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


namespace ASC.Files.Tests.Tests._06_Operations.BulkDownload;

/// <summary>
/// One download at a time per caller: a second <c>bulkDownload</c> while the first still runs is refused with
/// 403. The first one converts several documents through the document server, which keeps it running long
/// enough for the second call to arrive while it is still in progress.
/// </summary>
[Trait("Category", "Operations")]
[Trait("Feature", "Files")]
public class BulkDownloadConcurrencyTests(
    AspireAppFixture fixture)
    : BulkDownloadTestBase(fixture)
{
    [Fact]
    public async Task BulkDownload_SecondCallWhileTheFirstRuns_Returns403AndTheFirstFinishes()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var documents = new List<DownloadRequestItemDto>();

        for (var i = 0; i < 4; i++)
        {
            var document = await CreateFileInMy($"Autotest BulkDownload Busy {i}.docx", Owner);
            documents.Add(new DownloadRequestItemDto(new DownloadRequestItemDtoKey(document.Id), "pdf"));
        }

        var plain = await CreateFileInMy("Autotest BulkDownload Second.docx", Owner);

        // Act
        var first = (await _filesOperationsApi.BulkDownloadAsync(
            new DownloadRequestDto(fileConvertIds: documents, folderIds: [], fileIds: []) { ReturnSingleOperation = true },
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        var second = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.BulkDownloadAsync(
                new DownloadRequestDto(fileIds: [new(plain.Id)], folderIds: [], fileConvertIds: []) { ReturnSingleOperation = true },
                cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        second.ErrorCode.Should().Be(403);

        var operation = await WaitForBulkDownload(first);
        operation.Finished.Should().BeTrue();
        operation.Error.Should().BeEmpty();
    }
}
